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
    public class AvailableServiceProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public AvailableServiceProcessing(IServiceProvider services, ILogger<AvailableServiceProcessing> logger)
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
                    using (var scope = Services.CreateScope())
                    {
                        var service =
                            scope.ServiceProvider
                                .GetRequiredService<IStatisticService>();
                        await service.GenerateServiceProviderAvailableReport();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "AvailableServiceProcessing error");
                }
                await Task.Delay(20*1000);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "AvailableServiceProcessing Service Hosted Service is stopping.");

            await Task.CompletedTask;
        }
    }
}
