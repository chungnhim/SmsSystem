using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IPortalService
    {
        Task<PortalUser> CheckUsername(string username);
        Task<ApiResponseBaseModel> TransferMoney(PortalTransferMoneyRequest request);
        Task<PortalUser> CheckTkaoUsername(string username);
        Task<ApiResponseBaseModel<decimal>> TransferMoneyToTkao(ExternalTransferMoneyRequest request);

    }
    public class PortalService : IPortalService
    {
        private readonly SmsDataContext _smsDataContext;
        private readonly IUserService _userService;
        private readonly IUserTransactionService _userTransactionService;
        private readonly IPortalTkaoConnector _portalTkaoConnector;
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly ICacheService _cacheService;
        public PortalService(SmsDataContext smsDataContext,
          IUserTransactionService userTransactionService,
          IUserService userService,
          IPortalTkaoConnector portalTkaoConnector,
          ISystemConfigurationService systemConfigurationService,
          ICacheService cacheService
        )
        {
            _smsDataContext = smsDataContext;
            _userTransactionService = userTransactionService;
            _userService = userService;
            _portalTkaoConnector = portalTkaoConnector;
            _systemConfigurationService = systemConfigurationService;
            _cacheService = cacheService;
        }
        public async Task<PortalUser> CheckUsername(string username)
        {
            var user = await _smsDataContext.Users.Where(r => r.Username == username).FirstOrDefaultAsync();
            if (user == null) return null;
            return new PortalUser()
            {
                Role = user.Role,
                Username = user.Username,
                UserId = user.Id
            };
        }

        public async Task<PortalUser> CheckTkaoUsername(string username)
        {
            var tkaoUser = await _portalTkaoConnector.CheckUsername(username);
            if (tkaoUser != null)
            {
                await _cacheService.CacheTkaoPortalUser(tkaoUser);
            }
            return tkaoUser;
        }

        public async Task<ApiResponseBaseModel<decimal>> TransferMoneyToTkao(ExternalTransferMoneyRequest request)
        {
            throw new Exception("Maintainance external!");
            var toUser = await _cacheService.GetTkaoPortalUser(request.ToUserId);
            if (toUser == null)
            {
                toUser = new PortalUser()
                {
                    UserId = request.ToUserId,
                    Username = request.ToUserId.ToString()
                };
            }
            var fromUser = await _userService.GetCurrentUser();
            if (fromUser == null)
            {
                return new ApiResponseBaseModel<decimal>()
                {
                    Success = false,
                    Message = "Unauthorized"
                };
            }
            var fromUserBalance = fromUser.Ballance;
            if (fromUserBalance < request.Amount)
            {
                return new ApiResponseBaseModel<decimal>()
                {
                    Success = false,
                    Message = "NotEnoughtCredit"
                };
            }
            var transferFeePercent = (await _systemConfigurationService.GetSystemConfiguration()).ExternalTransferFee / 100;
            var transferFee = Math.Ceiling(request.Amount * transferFeePercent);
            var transferMoney = request.Amount - transferFee;

            fromUserBalance -= transferMoney;
            await _userTransactionService.Create(new UserTransaction()
            {
                UserId = fromUser.Id,
                Amount = transferMoney,
                Balance = fromUserBalance,
                Comment = $"TransferTo {toUser.Username} taikhoanao",
                IsImport = false,
                UserTransactionType = UserTransactionType.TransferMoney,
            });
            fromUserBalance -= transferFee;
            await _userTransactionService.Create(new UserTransaction()
            {
                UserId = fromUser.Id,
                Amount = transferFee,
                Balance = fromUserBalance,
                Comment = $"Fee TransferTo {toUser.Username} taikhoanao",
                IsImport = false,
                UserTransactionType = UserTransactionType.TransferFee,
            });
            var sendResult = await _portalTkaoConnector.SendMoneyToUser(fromUser.Username, request.ToUserId, transferMoney);

            return new ApiResponseBaseModel<decimal>()
            {
                Success = sendResult.Success,
                Results = fromUserBalance
            };
        }

        public async Task<ApiResponseBaseModel> TransferMoney(PortalTransferMoneyRequest request)
        {
            var user = await _userService.Get(request.ReceiverId);
            if (user == null) return ApiResponseBaseModel.NotFoundResourceResponse();
            if (user.Role != RoleType.User) return ApiResponseBaseModel.NotFoundResourceResponse();

            await _userTransactionService.Create(new UserTransaction()
            {
                UserId = request.ReceiverId,
                Amount = request.Money,
                Balance = user.Ballance + request.Money,
                Comment = $"ReceiveFrom {request.Sender}",
                IsImport = true,
                UserTransactionType = UserTransactionType.TransferMoney
            });
            return new ApiResponseBaseModel()
            {
                Success = true
            };
        }
    }
}
