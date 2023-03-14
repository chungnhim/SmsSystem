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
    public class DiscountProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public DiscountProcessing(IServiceProvider services, ILogger<DiscountProcessing> logger)
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
                    _logger.LogInformation("Start DiscountProcessing processing!");
                    using (var scope = Services.CreateScope())
                    {
                        var discountService =
                            scope.ServiceProvider
                                .GetRequiredService<IDiscountService>();
                        await discountService.BackgroundProcessDiscountTable();
                    }
                    _logger.LogInformation("End DiscountProcessing processing!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "DiscountProcessing processing error");

                }
                await Task.Delay(60*60*1000);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "DiscountProcessing Hosted Service is stopping.");

            await Task.CompletedTask;
        }
    }
}
