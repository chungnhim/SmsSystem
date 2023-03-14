using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sms.Web.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sms.Web.BackgroundTask
{
    public class Bank512Processing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public Bank512Processing(IServiceProvider services, ILogger<Bank512Processing> logger)
        {
            Services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DoWork(stoppingToken);
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    _logger.LogInformation("Start Bank512Processing processing!");
                    using (var scope = Services.CreateScope())
                    {
                        var service =
                            scope.ServiceProvider
                                .GetRequiredService<IGsmDeviceService>();

                        await service.ProcessBank512Check();
                    }
                    _logger.LogInformation("End Bank512Processing processing!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Bank512Processing processing error");

                }
                await Task.Delay(1000);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is stopping.");

            await Task.CompletedTask;
        }
    }
}
