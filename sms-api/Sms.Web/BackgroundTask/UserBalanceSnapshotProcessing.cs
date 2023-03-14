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
    public class UserBalanceSnapshotProcessing : BackgroundService
    {
        private readonly ILogger _logger;
        public IServiceProvider Services { get; }

        public UserBalanceSnapshotProcessing(IServiceProvider services, ILogger<UserBalanceSnapshotProcessing> logger)
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
                var now = DateTime.Now; // UTC +7
                var hour = now.Hour;
                var minute = now.Minute;
                var second = now.Second;
                if (hour == 0 && minute == 0 && second == 0)
                {
                    try
                    {
                        _logger.LogInformation("Start UserBalanceSnapshotProcessing!");
                        using (var scope = Services.CreateScope())
                        {
                            var service =
                                scope.ServiceProvider
                                    .GetRequiredService<IUserService>();

                            await service.SnapshotBalance();
                        }
                        _logger.LogInformation("End UserBalanceSnapshotProcessing!");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "UserBalanceSnapshotProcessing error");

                    }
                }
                await Task.Delay(1000);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "UserBalanceSnapshotProcessing Service is stopping.");

            await Task.CompletedTask;
        }
    }
}
