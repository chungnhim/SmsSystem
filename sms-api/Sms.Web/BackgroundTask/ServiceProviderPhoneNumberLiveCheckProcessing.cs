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
    public class ServiceProviderPhoneNumberLiveCheckProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public ServiceProviderPhoneNumberLiveCheckProcessing(IServiceProvider services, ILogger<ServiceProviderPhoneNumberLiveCheckProcessing> logger)
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
                    _logger.LogInformation("Start ServiceProviderPhoneNumberLiveCheckProcessing!");
                    using (var scope = Services.CreateScope())
                    {
                        var service =
                            scope.ServiceProvider
                                .GetRequiredService<IServiceProviderPhoneNumberLiveCheckService>();

                        await service.BackgroundEnqueueCheckingJob();
                    }
                    _logger.LogInformation("End ServiceProviderPhoneNumberLiveCheckProcessing!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "ServiceProviderPhoneNumberLiveCheckProcessing error");
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
