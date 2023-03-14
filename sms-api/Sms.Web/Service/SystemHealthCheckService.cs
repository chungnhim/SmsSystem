using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface ISystemHealthCheckService
    {
        Task OverloadAlertProcess();
    }
    public class SystemHealthCheckService : ISystemHealthCheckService
    {
        private readonly SmsDataContext _smsDataContext;
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly ISystemAlertService _systemAlertService;
        private readonly IDateTimeService _dateTimeService;
        public SystemHealthCheckService(SmsDataContext smsDataContext,
            ISystemConfigurationService systemConfigurationService,
            ISystemAlertService systemAlertService,
            IDateTimeService dateTimeService)
        {
            _smsDataContext = smsDataContext;
            _systemConfigurationService = systemConfigurationService;
            _systemAlertService = systemAlertService;
            _dateTimeService = dateTimeService;
        }
        public async Task OverloadAlertProcess()
        {
            var configuration = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
            var floatingOverloadAlert = configuration.FloatingOrderWarning ?? 30;
            var timeCheck = _dateTimeService.UtcNow().AddSeconds(-20);
            var floatingCount = await _smsDataContext.RentCodeOrders.Where(r => r.Status == OrderStatus.Floating && r.Created < timeCheck).CountAsync();
            if(floatingCount > floatingOverloadAlert)
            {
                await _systemAlertService.RaiseAnAlert(new SystemAlert()
                {
                    Topic = "Order",
                    Thread = "FloatingOrderOverload",
                    DetailJson = JsonConvert.SerializeObject(floatingCount),
                });
            }
        }
    }
}
