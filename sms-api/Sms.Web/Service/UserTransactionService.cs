using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IUserTransactionService : IServiceBase<UserTransaction>
    {
        Task<ApiResponseBaseModel<decimal>> TransferMoney(TransferMoneyRequest request);
        Task<int> ManualFixTransactionsByUser(int userId, DateTime from);
        Task ArchiveOldTransactions(ILogger logger);
    }
    public class UserTransactionService : ServiceBase<UserTransaction>, IUserTransactionService
    {
        private readonly IUserService _userService;
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly IDateTimeService _dateTimeService;
        public UserTransactionService(SmsDataContext smsDataContext, IUserService userService,
          ISystemConfigurationService systemConfigurationService, IDateTimeService dateTimeService
        ) : base(smsDataContext)
        {
            _userService = userService;
            _systemConfigurationService = systemConfigurationService;
            _dateTimeService = dateTimeService;
        }
        public override async Task<ApiResponseBaseModel<UserTransaction>> Create(UserTransaction model)
        {
            if (model.UserId == 0) return new ApiResponseBaseModel<UserTransaction>()
            {

                Success = false,
                Message = "UserIdRequired"
            };
            var user = await _userService.Get(model.UserId);
            if (user == null)
            {
                return new ApiResponseBaseModel<UserTransaction>()
                {
                    Success = false,
                    Message = "UserNotFound"
                };
            }
            await _smsDataContext.SaveChangesAsync();
            var obj = await base.Create(model);
            await UpdateUserBallance(user.Id, model.Amount * (model.IsImport ? 1 : -1));
            return obj;
        }
        protected override IQueryable<UserTransaction> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include(x => x.Order);
            query = query.Include(r => r.User);
            var userId = 0;
            {
                if (filterRequest.SearchObject.TryGetValue("UserId", out object obj))
                {
                    userId = int.Parse(obj.ToString());
                }
            }
            DateTime? fromDate = null;
            DateTime? toDate = null;
            {
                if (filterRequest.SearchObject.TryGetValue("CreatedFrom", out object obj))
                {
                    fromDate = (DateTime)obj;
                }
            }
            {
                if (filterRequest.SearchObject.TryGetValue("CreatedTo", out object obj))
                {
                    toDate = (DateTime)obj;
                }
            }
            {
                if (filterRequest.SearchObject.TryGetValue("TransactionType", out object obj))
                {
                    var type = int.Parse(obj.ToString());
                    if (type == 1)
                    {
                        query = query.Where(r => r.IsImport);
                    }
                    else if (type == 2)
                    {
                        query = query.Where(r => !r.IsImport);
                    }
                }
            }
            {
                if (filterRequest.SearchObject.TryGetValue("TransactionNewType", out object obj))
                {
                    var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<UserTransactionType>>();
                    if (ts.Count > 0)
                    {
                        query = query.Where(r => ts.Contains(r.UserTransactionType));
                    }
                }
            }
            {
                if (filterRequest.SearchObject.TryGetValue("PaymentMethodTypes", out object obj))
                {
                    var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<OfflinePaymentMethodType>>();
                    if (ts.Count > 0)
                    {
                        query = query.Where(r => r.OfflinePaymentMethodType.HasValue && ts.Contains(r.OfflinePaymentMethodType.Value));
                    }
                }
            }
            {
                if (filterRequest.SearchObject.TryGetValue("user", out object obj))
                {
                    var username = obj.ToString();
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        username = username.ToLower();

                        if (username.StartsWith("[") && username.EndsWith("]"))
                        {
                            username = username.Substring(1, username.Length - 2);
                            query = query.Where(r => r.User != null && r.User.Username.ToLower() == username);
                        }
                        else
                        {
                            query = query.Where(r => r.User != null && r.User.Username.ToLower().Contains(username));
                        }
                    }
                }
            }
            {
                if (filterRequest.SearchObject.TryGetValue("guid", out object obj))
                {
                    var guid = obj.ToString();
                    if (!string.IsNullOrWhiteSpace(guid))
                    {
                        guid = guid.ToLower();
                        query = query.Where(r => r.User != null && r.Order.Guid.ToLower() == guid);
                    }
                }
            }
            {
                if (filterRequest.SearchObject.TryGetValue("isAdminConfirm", out object obj))
                {
                    var isAdminConfirm = (bool)obj;
                    if (isAdminConfirm)
                    {
                        query = query.Where(r => r.IsAdminConfirm == isAdminConfirm);
                    }
                }
            }
            if (userId != 0)
            {
                query = query.Where(r => r.UserId == userId);
            }
            if (fromDate != null)
            {
                query = query.Where(r => r.Created >= fromDate);
            }
            if (toDate != null)
            {
                query = query.Where(r => r.Created < toDate);
            }
            return query;
        }
        public override async Task<Dictionary<string, object>> GetFilterAdditionalData(FilterRequest filterRequest, IQueryable<UserTransaction> query)
        {
            var dic = new Dictionary<string, object>();
            var income = await query.Where(r => r.IsImport == true).SumAsync(r => r.Amount);
            dic.Add("income", income);
            var outcome = await query.Where(r => r.IsImport == false).SumAsync(r => r.Amount);
            dic.Add("outcome", outcome);
            return dic;
        }
        public override void Map(UserTransaction entity, UserTransaction model)
        {

        }
        public async Task<ApiResponseBaseModel<decimal>> TransferMoney(TransferMoneyRequest request)
        {
            throw new Exception("Maintainance internal!");
            var toUser = await _userService.Get(request.ToUserId);
            if (toUser == null)
            {
                return new ApiResponseBaseModel<decimal>()
                {
                    Success = false,
                    Message = "UserNotFound"
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
            var transferFeePercent = (await _systemConfigurationService.GetSystemConfiguration()).InternalTransferFee / 100;
            var transferFee = Math.Ceiling(request.Amount * transferFeePercent);
            var transferMoney = request.Amount - transferFee;

            fromUserBalance -= transferMoney;
            await Create(new UserTransaction()
            {
                UserId = fromUser.Id,
                Amount = transferMoney,
                Balance = fromUserBalance,
                Comment = $"TransferTo {toUser.Username}",
                IsImport = false,
                UserTransactionType = UserTransactionType.TransferMoney,
            });
            fromUserBalance -= transferFee;
            await Create(new UserTransaction()
            {
                UserId = fromUser.Id,
                Amount = transferFee,
                Balance = fromUserBalance,
                Comment = $"Fee TransferTo {toUser.Username}",
                IsImport = false,
                UserTransactionType = UserTransactionType.TransferFee,
            });

            await Create(new UserTransaction()
            {
                UserId = toUser.Id,
                Amount = transferMoney,
                Balance = toUser.Ballance + transferMoney,
                Comment = $"ReceiveFrom {fromUser.Username}",
                IsImport = true,
                UserTransactionType = UserTransactionType.TransferMoney
            });
            return new ApiResponseBaseModel<decimal>()
            {
                Success = true,
                Results = fromUserBalance
            };
        }

        public async Task<int> ManualFixTransactionsByUser(int userId, DateTime from)
        {
            var transactionsQuery = _smsDataContext.UserTransactions.Where(u => u.UserId == userId && u.Created >= from).OrderBy(r => r.Id);
            var totalCount = await transactionsQuery.CountAsync();
            var pageSize = 1000;
            var pageIndex = 0;
            decimal? lastBalance = null;
            var totalEffects = 0;
            while (totalCount > 0)
            {
                var transactions = await transactionsQuery.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
                totalCount -= transactions.Count();
                pageIndex++;
                foreach (var transaction in transactions)
                {
                    if (lastBalance.HasValue)
                    {
                        transaction.Balance = lastBalance + transaction.Amount * (transaction.IsImport ? 1 : -1);
                    }
                    lastBalance = transaction.Balance;
                }
                var effect = await _smsDataContext.SaveChangesAsync();
                totalEffects += effect;
            }
            if (lastBalance.HasValue)
            {
                var user = await _smsDataContext.Users.FirstOrDefaultAsync(r => r.Id == userId);
                user.Ballance = lastBalance.Value;
                await _smsDataContext.SaveChangesAsync();
            }
            return totalEffects;
        }

        public async Task ArchiveOldTransactions(ILogger logger)
        {
            var systemConfiguration = await _systemConfigurationService.GetSystemConfiguration();
            if (systemConfiguration == null || !systemConfiguration.AllowArchiveUserTransaction)
            {
                logger.LogWarning("Not allow archive user transaction");
                return;
            }
            var mileStone = _dateTimeService.UtcNow().AddMonths(-3);
            var query = _smsDataContext.UserTransactions.Where(r => r.Created < mileStone).OrderBy(x=>x.Id);
            var maxCount = 1000;
            var transactions = await query.Take(maxCount).ToListAsync();
            logger.LogInformation("Delete expired transactions: {0}", transactions.Count);
            _smsDataContext.UserTransactions.RemoveRange(transactions);
            await _smsDataContext.SaveChangesAsync();
        }
    }
}
