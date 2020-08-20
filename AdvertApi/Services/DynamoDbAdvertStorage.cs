using AdvertApi.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdvertApi.Services
{
    public class DynamoDbAdvertStorage : IAdvertStorageService
    {
        private readonly IMapper _mapper;
        public DynamoDbAdvertStorage(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<string> Add(AdvertModel model)
        {
            var dbModel = _mapper.Map<AdvertDbModel>(model);
            dbModel.Id = Guid.NewGuid().ToString();
            dbModel.CreationDateTime = DateTime.UtcNow;
            dbModel.Status = AdvertStatus.Pending;

            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    await context.SaveAsync(dbModel);
                }
            }
            return dbModel.Id;
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                using (var client = new AmazonDynamoDBClient())
                {
                    var tableData = await client.DescribeTableAsync("Adverts");
                    return string.Compare(tableData.Table.TableStatus, "active", true) == 0;
                }
            }
            catch (Exception e)
            {

                throw;
            }
      
        }

        public async Task Confirm(ConfirmAdvertModel model)
        {
            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    var record = await context.LoadAsync<AdvertDbModel>(model.Id);
                    if (record == null)
                    {
                        throw new KeyNotFoundException($"A record with ID={model.Id} was not found.");
                    }
                    if(model.Status == AdvertStatus.Active)
                    {
                        record.Status = AdvertStatus.Active;
                        await context.SaveAsync(record);
                    }else
                    {
                        await context.DeleteAsync(record);
                    }
                }
            }
        }

        public async Task<AdvertDbModel> GetByIdAsync(string id)
        {
            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    var record = await context.LoadAsync<AdvertDbModel>(id);
                    if (record == null)
                    {
                        throw new KeyNotFoundException($"A record with ID={id} was not found.");
                    }

                    return record;
                }
            }
        }

        public async Task<List<AdvertModel>> GetAllAsync()
        {
            using (var client = new AmazonDynamoDBClient())
            {
                using (var context = new DynamoDBContext(client))
                {
                    var scanResult =
                        await context.ScanAsync<AdvertDbModel>(new List<ScanCondition>()).GetNextSetAsync();
                    return scanResult.Select(item => _mapper.Map<AdvertModel>(item)).ToList();
                }
            }
        }
    }
}
