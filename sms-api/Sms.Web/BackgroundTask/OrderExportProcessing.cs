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
    public class OrderExportProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public OrderExportProcessing(IServiceProvider services, ILogger<OrderExportProcessing> logger)
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
                        var exportService =
                            scope.ServiceProvider
                                .GetRequiredService<IExportService>();
                        await exportService.BackgroundProcessOrderExport();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Order processing error");

                }
                await Task.Delay(1);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Order processing Service Hosted Service is stopping.");

            await Task.CompletedTask;
        }
    }
}
