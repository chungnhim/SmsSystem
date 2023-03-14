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
    public class ReferFeeProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public ReferFeeProcessing(IServiceProvider services, ILogger<ReferFeeProcessing> logger)
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
                    _logger.LogInformation("Start refer fee processing!");
                    using (var scope = Services.CreateScope())
                    {
                        var service =
                            scope.ServiceProvider
                                .GetRequiredService<IReferalService>();

                        await service.ProcessReferFee();
                    }
                    _logger.LogInformation("End refer fee processing!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Alert processing error");

                }
                await Task.Delay(30 * 60 * 1000);
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
