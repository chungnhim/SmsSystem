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
    public interface IForgotPasswordService
    {
        Task<ApiResponseBaseModel<string>> ForgotPassword(string email, string callbackUrl);
        Task<ApiResponseBaseModel<string>> CheckForgotPassword(string token);
        Task<ApiResponseBaseModel<string>> SubmitForgotPassword(string forgotPasswordToken, string newPassword);
    }
    public class ForgotPasswordService : IForgotPasswordService
    {
        private readonly IDateTimeService _dateTimeService;
        private readonly SmsDataContext _smsDataContext;
        private readonly IEmailSender _emailSender;
        private readonly AppSettings _appSettings;

        public ForgotPasswordService(SmsDataContext smsDataContext, IEmailSender emailSender, IOptions<AppSettings> appSettings, IDateTimeService dateTimeService)
        {
            _smsDataContext = smsDataContext;
            _appSettings = appSettings.Value;
            _emailSender = emailSender;
            _dateTimeService = dateTimeService;
        }

        public async Task<ApiResponseBaseModel<string>> CheckForgotPassword(string token)
        {
            var tokenEntity = await _smsDataContext.ForgotPasswordTokens.FirstOrDefaultAsync(x => x.Token == token);
            return CheckForgotPasswordToken(tokenEntity);
        }

        private ApiResponseBaseModel<string> CheckForgotPasswordToken(ForgotPasswordToken tokenEntity)
        {
            if (tokenEntity == null) return new ApiResponseBaseModel<string>()
            {
                Success = false,
                Message = "TokenNotFound"
            };
            if (tokenEntity.Used)
            {
                return new ApiResponseBaseModel<string>()
                {
                    Success = false,
                    Message = "AlreadyUsed"
                };
            }
            if (tokenEntity.Created.HasValue && tokenEntity.Created.Value.AddHours(5) < _dateTimeService.UtcNow())
            {
                return new ApiResponseBaseModel<string>()
                {
                    Success = false,
                    Message = "Expired"
                };
            }

            return new ApiResponseBaseModel<string>()
            {
                Success = true
            };
        }

        public async Task<ApiResponseBaseModel<string>> ForgotPassword(string email, string callbackUrl)
        {
            var userProfile = await _smsDataContext.UserProfiles.FirstOrDefaultAsync(r => r.Email == email);
            if (userProfile == null)
            {
                return new ApiResponseBaseModel<string>()
                {
                    Message = "NotFoundEmail",
                    Success = false
                };
            }
            var forgotPasswordToken = new ForgotPasswordToken()
            {
                UserId = userProfile.UserId,
                Token = Guid.NewGuid().ToString(),
            };
            _smsDataContext.ForgotPasswordTokens.Add(forgotPasswordToken);
            await _smsDataContext.SaveChangesAsync();
            var link = $"{callbackUrl ?? _appSettings.FrontEndUrl}/new-password?token={forgotPasswordToken.Token}";
            _emailSender.SendEmail(new EmailRequest()
            {
                Tos = new List<string>() { email },
                Subject = "Forgot password",
                Params = new List<string>() { link},
                TemplateName = "ForgotPasswordTemplate"
            });
            return new ApiResponseBaseModel<string>()
            {
                Success = true
            };
        }

        public async Task<ApiResponseBaseModel<string>> SubmitForgotPassword(string forgotPasswordToken, string newPassword)
        {
            var tokenEntity = await _smsDataContext.ForgotPasswordTokens.FirstOrDefaultAsync(x => x.Token == forgotPasswordToken);
            var checkResult = CheckForgotPasswordToken(tokenEntity);
            if (!checkResult.Success)
            {
                return checkResult;
            }
            tokenEntity.Used = true;
            var user = await _smsDataContext.Users.FindAsync(tokenEntity.UserId);
            if (user != null)
            {
                user.Password = newPassword;
            }

            await _smsDataContext.SaveChangesAsync();

            return new ApiResponseBaseModel<string>()
            {
                Success = true
            };
        }
    }
}
