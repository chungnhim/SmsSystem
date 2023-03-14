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
    public class AlertProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public AlertProcessing(IServiceProvider services, ILogger<AlertProcessing> logger)
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
                    _logger.LogInformation("Start alert processing!");
                    using (var scope = Services.CreateScope())
                    {
                        var service =
                            scope.ServiceProvider
                                .GetRequiredService<ISystemAlertService>();

                        await service.ProcessAlerts();
                    }
                    _logger.LogInformation("End alert processing!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Alert processing error");

                }
                await Task.Delay(60*1000);
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
