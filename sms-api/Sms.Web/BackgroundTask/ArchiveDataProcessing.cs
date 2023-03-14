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
    public class ArchiveDataProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public ArchiveDataProcessing(IServiceProvider services, ILogger<ArchiveDataProcessing> logger)
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
                    _logger.LogInformation("Start ArchiveData processing user transactions!");
                    using (var scope = Services.CreateScope())
                    {
                        var service =
                            scope.ServiceProvider
                                .GetRequiredService<IUserTransactionService>();

                        await service.ArchiveOldTransactions(_logger);
                    }
                    _logger.LogInformation("End ArchiveData processing user transactions!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "ArchiveData processing user transactions error");
                }
                try
                {
                    _logger.LogInformation("Start ArchiveData processing orders!");
                    using (var scope = Services.CreateScope())
                    {
                        var service =
                            scope.ServiceProvider
                                .GetRequiredService<IOrderService>();

                        await service.ArchiveOldOrders(_logger);
                    }
                    _logger.LogInformation("End ArchiveData processing orders!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "ArchiveData processing orders error");
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
