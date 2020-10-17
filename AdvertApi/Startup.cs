using System.Collections.Generic;
using System.Threading.Tasks;
using AdvertApi.HealthChecks;
using AdvertApi.Services;
using Amazon.ServiceDiscovery;
using Amazon.ServiceDiscovery.Model;
using Amazon.Util;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace AdvertApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(Startup));
            services.AddTransient<IAdvertStorageService, DynamoDbAdvertStorage>();
            services.AddSingleton<StorageHealthCheck>();

            services.AddControllers();
            services.AddHealthChecks().AddCheck<StorageHealthCheck>("Storage");
            //services.AddHealthChecks(checks => {
            //    checks.AddCheck<StorageHealthCheck>("Storage", new TimeSpan(0, 1, 0));
            //});
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Web Advertisement Apis",
                    Version = "Version 1",
                    Contact = new OpenApiContact
                    {
                        Name = "Mohsen Sanjari",
                        Email = "mohsen.sanjari@outlook.com"
                    }
                });
                options.CustomOperationIds(d => (d.ActionDescriptor as ControllerActionDescriptor)?.ActionName);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async Task Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseHealthChecks("/health");
            app.UseSwagger(o => { o.SerializeAsV2 = true; });
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web Advert Api"); });
            app.UseCors();
            await RegisterToCloudMap();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        private async Task RegisterToCloudMap()
        {
            const string serviceId = "srv-ym7ek4whzb5yspdc";
            var instanceId = EC2InstanceMetadata.InstanceId;
            if (!string.IsNullOrEmpty(instanceId))
            {
                var ipv4 = EC2InstanceMetadata.PrivateIpAddress;
                var client = new AmazonServiceDiscoveryClient();
                await client.RegisterInstanceAsync(new RegisterInstanceRequest
                {
                    InstanceId = instanceId,
                    ServiceId = serviceId,
                    Attributes = new Dictionary<string, string>
                    {
                        {"AWS_INSTANCE_IPV4", ipv4},
                        {"AWS_INSTANCE_PORT", "80"}
                    }
                });
            }
        }
    }
}