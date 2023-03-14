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
    public interface ISystemAlertService : IServiceBase<SystemAlert>
    {
        Task RaiseAnAlert(SystemAlert model);
        Task ProcessAlerts();
    }
    public class SystemAlertService : ServiceBase<SystemAlert>, ISystemAlertService
    {
        private readonly IEmailSender _emailSender;
        private readonly IDateTimeService _dateTimeService;
        private readonly ISystemConfigurationService _systemConfigurationService;
        public SystemAlertService(SmsDataContext smsDataContext,
            IDateTimeService dateTimeService,
            IEmailSender emailSender,
            ISystemConfigurationService systemConfigurationService) : base(smsDataContext)
        {
            _dateTimeService = dateTimeService;
            _emailSender = emailSender;
            _systemConfigurationService = systemConfigurationService;
        }

        public override void Map(SystemAlert entity, SystemAlert model)
        {
        }

        public async Task RaiseAnAlert(SystemAlert model)
        {
            var atLeastTime = _dateTimeService.UtcNow().AddMinutes(-GetIgnoreAlertDurationInMinutes(model));
            if (await _smsDataContext.SystemAlerts.AnyAsync(r => r.Thread == model.Thread && r.Topic == model.Topic && r.Created > atLeastTime))
            {
                return;
            }
            await Create(model);
        }
        private int GetIgnoreAlertDurationInMinutes(SystemAlert alert)
        {
            if (alert.Topic == "Order")
            {
                return 2;
            }
            if (alert.Topic == "GsmDevice" && alert.Thread == "ErrorGsmWarning")
            {
                return 30;
            }
            return 1;
        }

        public async Task ProcessAlerts()
        {
            var notSentAlerts = await _smsDataContext.SystemAlerts.Where(r => !r.IsSent).OrderBy(r => r.Created).Take(100).ToListAsync();
            if (notSentAlerts.Count > 0)
            {
                var systemConfiguraton = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
                var toEmails = new List<string>() { systemConfiguraton.Email };
                if (!string.IsNullOrEmpty(systemConfiguraton.BccEmail))
                {
                    toEmails.AddRange(systemConfiguraton.BccEmail.Split(";"));
                }
                foreach (var alert in notSentAlerts)
                {
                    if (alert.Topic == "Order" && alert.Thread == "FloatingOrderOverload")
                    {
                        await _emailSender.SendEmailAsync(new EmailRequest()
                        {
                            Tos = toEmails,
                            Subject = $"Cảnh báo đơn hàng quá tải {_dateTimeService.UtcNow().Ticks}",
                            TemplateName = "WarningOrderOverload",
                            Params = new List<string>() { alert.DetailJson }
                        });
                    }
                    if (alert.Topic == "GsmDevice" && alert.Thread == "ErrorGsmWarning")
                    {
                        var gsmErrorPayload = JsonConvert.DeserializeObject<GsmWarningPayload>(alert.DetailJson);
                        await _emailSender.SendEmailAsync(new EmailRequest()
                        {
                            Tos = toEmails.Concat(new List<string>() { gsmErrorPayload.StaffEmail }).ToList(),
                            Subject = $"[Rentcode] Cảnh báo lỗi GSM",
                            TemplateName = "WarningTemplate",
                            Params = new List<string>() {
                                gsmErrorPayload.TotalErrors.ToString(),
                                gsmErrorPayload.ContinuosErrors.ToString(),
                                gsmErrorPayload.GsmCode,
                                gsmErrorPayload.GsmName
                            }
                        });
                    }
                    if (alert.Topic == "Order" && alert.Thread == "ServiceProviderContinuosFailed")
                    {
                        var failedPayload = JsonConvert.DeserializeObject<ServiceProviderContinuosFailedAlertPayload>(alert.DetailJson);
                        var serviceProvider = await _smsDataContext.ServiceProviders.FirstOrDefaultAsync(r => r.Id == failedPayload.ServiceProviderId);
                        if (serviceProvider == null) continue;
                        var top10Orders = await _smsDataContext.RentCodeOrders.Where(r => r.ServiceProviderId == failedPayload.ServiceProviderId && r.Status == OrderStatus.Error)
                            .OrderByDescending(r => r.Id).Take(10).ToListAsync();
                        await _emailSender.SendEmailAsync(new EmailRequest()
                        {
                            Tos = toEmails,
                            Subject = $"[Rentcode] Cảnh báo dịch vụ lỗi {_dateTimeService.UtcNow().Ticks}",
                            TemplateName = "ServiceProviderContinuosFailed",
                            Params = new List<string>() {
                                serviceProvider.Name,
                                failedPayload.ContinuosFailedCount.ToString(),
                                string.Join('\n',top10Orders.Select(r=>r.Guid).ToList())
                            }
                        });
                    }
                    if (alert.Topic == "Order" && alert.Thread == "UserContinuosFailed")
                    {
                        var failedPayload = JsonConvert.DeserializeObject<UserContinuosFailedAlertPayload>(alert.DetailJson);
                        var user = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Id == failedPayload.UserId);
                        if (user == null) continue;
                        var top10Orders = await _smsDataContext.Orders.Where(r => r.UserId == failedPayload.UserId && r.Status == OrderStatus.Error)
                            .OrderByDescending(r => r.Id).Take(10).ToListAsync();
                        await _emailSender.SendEmailAsync(new EmailRequest()
                        {
                            Tos = toEmails,
                            Subject = $"[Rentcode] Cảnh báo user gọi lỗi liên tiếp {_dateTimeService.UtcNow().Ticks}",
                            TemplateName = "UserContinuosFailed",
                            Params = new List<string>() {
                                user.Username,
                                failedPayload.ContinuosFailedCount.ToString(),
                                string.Join('\n',top10Orders.Select(r=>r.Guid).ToList())
                            }
                        });
                    }
                    if (alert.Topic == "Order" && alert.Thread == "GsmServiceProviderContinuosFailed")
                    {
                        var failedPayload = JsonConvert.DeserializeObject<GsmServiceProviderContinuosFailedAlertPayload>(alert.DetailJson);
                        var gsmDevice = await _smsDataContext.GsmDevices
                            .Include(r => r.UserGsmDevices).ThenInclude(x => x.User).FirstOrDefaultAsync(r => r.Id == failedPayload.GsmId);

                        if (gsmDevice == null) continue;

                        var staff = gsmDevice?.UserGsmDevices.FirstOrDefault()?.User?.Username;

                        var serviceProvider = await _smsDataContext.ServiceProviders.FirstOrDefaultAsync(r=>r.Id == failedPayload.ServiceProviderId);

                        if(serviceProvider == null) continue;

                        var top10Orders = await _smsDataContext.RentCodeOrders.Where(r => r.ServiceProviderId == failedPayload.ServiceProviderId && r.ConnectedGsmId == failedPayload.ContinuosFailedCount && r.Status == OrderStatus.Error)
                            .OrderByDescending(r => r.Id).Take(10).ToListAsync();
                        await _emailSender.SendEmailAsync(new EmailRequest()
                        {
                            Tos = toEmails,
                            Subject = $"[Rentcode] Cảnh báo dịch vụ lỗi liên tiếp trên GSM {_dateTimeService.UtcNow().Ticks}",
                            TemplateName = "GsmServiceProviderContinuosFailed",
                            Params = new List<string>() {
                                gsmDevice.Name,
                                staff,
                                serviceProvider.Name,
                                failedPayload.ContinuosFailedCount.ToString(),
                                string.Join('\n',top10Orders.Select(r=>r.Guid).ToList())
                            }
                        });
                    }
                    alert.IsSent = true;
                }
                await _smsDataContext.SaveChangesAsync();
            }
        }
    }
}
