using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sms.Web.BackgroundTask
{
    public class SmsStructure
    {
        public string Sender { get; set; }
        public string Content { get; set; }
    }
    public class SmsHardCoding : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private readonly List<string> phoneNumbers = new List<string>()
        {
            "84-905000111",
            "84-905000112",
            "84-905000113",
            "84-905000114",
            "84-905000115",
            "84-905000116",
            "84-905000117",
            "84-905000118",
            "84-905000119",
            "84-905000120",
            "84-905000121",
            "84-905000122",
            "84-905000123",
            "84-905000124",
            "84-905000125",
            "84-905000126",
            "84-905000127",
            "84-905000128",
            "84-905000129",
            "84-905000130",
        };
        private readonly List<SmsStructure> smsList = new List<SmsStructure>()
        {
            new SmsStructure()
            {
                Sender = "Google",
                Content = "Your google verifycation code is G-{0}"
            },
            new SmsStructure()
            {
                Sender = "Facebook",
                Content = "Facebook code is {0}"
            },
            new SmsStructure()
            {
                Sender = "Yalo",
                Content = "Ma OTP tai khoan yalo cua ban la {0}."
            },
            new SmsStructure()
            {
                Sender = "Zing",
                Content = "{0} la ma xac nhan cua ban"
            },
            new SmsStructure()
            {
                Sender = "Zing",
                Content = "{0} la ma xac nhan cua ban"
            },
            new SmsStructure()
            {
                Sender = "Zing",
                Content = "{0} la ma xac nhan cua ban"
            },
            new SmsStructure()
            {
                Sender = "84-987654321",
                Content = "hi, day la he thong tin nhan tu dong"
            },
        };
        public SmsHardCoding(ILogger<SmsHardCoding> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
