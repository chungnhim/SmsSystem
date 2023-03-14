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
    public class PhoneEfficiencyProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public PhoneEfficiencyProcessing(IServiceProvider services, ILogger<PhoneEfficiencyProcessing> logger)
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
                    _logger.LogInformation("Start PhoneEfficiencyProcessing!");
                    using (var scope = Services.CreateScope())
                    {
                        var service =
                            scope.ServiceProvider
                                .GetRequiredService<IComService>();

                        await service.CalculatePhoneEfficiencyForAllComs();
                    }
                    _logger.LogInformation("End PhoneEfficiencyProcessing!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Alert PhoneEfficiencyProcessing error");
                }
                await Task.Delay(1000);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "PhoneEfficiencyProcessing is stopping.");

            await Task.CompletedTask;
        }
    }
}
