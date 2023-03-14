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
    public class SystemHealthCheckProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public SystemHealthCheckProcessing(IServiceProvider services, ILogger<AlertProcessing> logger)
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
                    _logger.LogInformation("Start health check!");
                    using (var scope = Services.CreateScope())
                    {
                        var systemHealthCheckService =
                            scope.ServiceProvider
                                .GetRequiredService<ISystemHealthCheckService>();

                        await systemHealthCheckService.OverloadAlertProcess();
                    }
                    _logger.LogInformation("End health check!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Health check error");

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
