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
  public class OrderProposedPhoneNumberProcessing : BackgroundService
  {
    private readonly ILogger _logger;
    public IServiceProvider Services { get; }

    public OrderProposedPhoneNumberProcessing(IServiceProvider services, ILogger<OrderProposedPhoneNumberProcessing> logger)
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
            var orderService =
                scope.ServiceProvider
                    .GetRequiredService<IOrderService>();
            await orderService.BackgroundProcessProposedPhoneNumber();
          }
        }
        catch (Exception e)
        {
          _logger.LogError(e, "Order proposed phone number processing error");

        }
        await Task.Delay(1);
      }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation(
          "Order proposed phone number processing Service Hosted Service is stopping.");

      await Task.CompletedTask;
    }
  }
}
