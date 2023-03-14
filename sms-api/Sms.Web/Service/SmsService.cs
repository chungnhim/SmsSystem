using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
    public interface ISmsService
    {
        Task<ApiResponseBaseModel<SmsHistory>> ReceiveSms(string content, string phoneNumber, string sender, DateTime receivedDate);
        Task<ApiResponseBaseModel<SmsHistory>> ReceiveAudioSms(string audioUrl, string phoneNumber, string sender, DateTime receivedDate);
    }
    public class SmsService : ISmsService
    {
        private readonly ISmsHistoryService _smsHistoryService;
        private readonly IPhoneFailedCountService _phoneFailedCountService;
        private readonly IOrderService _orderService;

        private readonly IProviderHistoryService _providerHistoryService;
        public SmsService(IOrderService orderService, ISmsHistoryService smsHistoryService, IPhoneFailedCountService phoneFailedCountService, IProviderHistoryService providerHistoryService)
        {
            _orderService = orderService;
            _smsHistoryService = smsHistoryService;
            _phoneFailedCountService = phoneFailedCountService;
            _providerHistoryService = providerHistoryService;
        }
        public async Task<ApiResponseBaseModel<SmsHistory>> ReceiveSms(string content, string phoneNumber, string sender, DateTime receivedDate)
        {
            var putResult = await _smsHistoryService.Create(new SmsHistory() { Content = content, PhoneNumber = phoneNumber, Sender = sender, ReceivedDate = receivedDate });
            if (putResult.Success)
            {
                await _phoneFailedCountService.ResetContinuousFailedOfPhoneNumber(putResult.Results.PhoneNumber);
                var assignResult = await _orderService.AssignResult(putResult.Results.PhoneNumber, putResult.Results.Content, putResult.Results.Sender, putResult.Results.Id);
                if (assignResult.Success)
                {
                    putResult.Message = assignResult.Message;
                    var providerHistory = new ProviderHistory()
                    {
                        PhoneNumber = putResult.Results.PhoneNumber,
                        ServiceProviderId = assignResult.Results.ServiceProviderId
                    };
                    await _providerHistoryService.Create(providerHistory);
                }
                else if (assignResult.Message == "MessageDidNotMatchWithService")
                {
                    await _smsHistoryService.MarkSmsHistoryNotMatch(putResult.Results.Id);
                }
            }
            return putResult;
        }
        public async Task<ApiResponseBaseModel<SmsHistory>> ReceiveAudioSms(string audioUrl, string phoneNumber, string sender, DateTime receivedDate)
        {
            var putResult = await _smsHistoryService.Create(new SmsHistory() { AudioUrl = audioUrl, SmsType = SmsType.Audio, PhoneNumber = phoneNumber, Sender = sender, ReceivedDate = receivedDate });
            if (putResult.Success)
            {
                await _phoneFailedCountService.ResetContinuousFailedOfPhoneNumber(putResult.Results.PhoneNumber);
                var assignResult = await _orderService.AssignAudioResult(putResult.Results.PhoneNumber, putResult.Results.AudioUrl, putResult.Results.Sender, putResult.Results.Id);
                if (assignResult.Success)
                {
                    putResult.Message = assignResult.Message;
                    var providerHistory = new ProviderHistory()
                    {
                        PhoneNumber = putResult.Results.PhoneNumber,
                        ServiceProviderId = assignResult.Results.ServiceProviderId
                    };
                    await _providerHistoryService.Create(providerHistory);
                }
            }
            return putResult;
        }
    }
}
