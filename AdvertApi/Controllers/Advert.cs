using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AdvertApi.Models;
using AdvertApi.Models.Messages;
using AdvertApi.Services;
using Amazon.SimpleNotificationService;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AdvertApi.Controllers
{
    [ApiController]
    [Route("adverts/v1")]
    public class Advert : ControllerBase
    {
        private readonly IConfiguration _configuration;

        private readonly IAdvertStorageService _advertStorageService;
        public Advert(IAdvertStorageService advertStorageService, IConfiguration configuration)
        {
            _advertStorageService = advertStorageService;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("Create")]
        [ProducesResponseType(400)]
        [ProducesResponseType(201, Type = typeof(CreateAdvertResponse))]
        public async Task<IActionResult> Create(AdvertModel model)
        {
            string recordId = string.Empty;
            try
            {
                recordId = await _advertStorageService.Add(model);
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }

            return StatusCode(201, new CreateAdvertResponse() { Id = recordId });
        }

        [HttpPut]
        [Route("Confirm")]
        [ProducesResponseType(400)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Confirm(ConfirmAdvertModel model)
        {
            try
            {
                await _advertStorageService.Confirm(model);
                await RaiseAdvertConfirmedMessage(model);
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }

            return Ok();
        }

        private async Task RaiseAdvertConfirmedMessage(ConfirmAdvertModel model)
        {
            var topicArn = _configuration.GetValue<string>("TopicArn");
            var dbmodel = await _advertStorageService.GetByIdAsync(model.Id.ToString());
            using (var client = new AmazonSimpleNotificationServiceClient())
            {
                var message = new AdvertConfirmedMessage()
                {
                    Id = dbmodel.Id,
                    Title = dbmodel.Title
                };
                var messageJson = JsonConvert.SerializeObject(message);
                await client.PublishAsync(topicArn, messageJson);
            }
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(404)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                var advert = await _advertStorageService.GetByIdAsync(id);
                return new JsonResult(advert);
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }
            catch (Exception)
            {
                return new StatusCodeResult(500);
            }
        }

        [HttpGet]
        [Route("all")]
        [ProducesResponseType(200)]
        [EnableCors("AllOrigin")]
        public async Task<IActionResult> All()
        {
            return new JsonResult(await _advertStorageService.GetAllAsync());
        }
    }
}
