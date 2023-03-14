using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sms.Web.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IReferalService
    {
        Task ProcessReferFee();
    }
    public class ReferalService : IReferalService
    {
        private static int TIME_TO_RUN_PROCESS_FEE = 3; // from 3 AM to 4 AM every day
        private readonly SmsDataContext _smsDataContext;
        private readonly IUserService _userService;
        private readonly ISystemConfigurationService _systemConfigurationService;
        private readonly IUserTransactionService _userTransactionService;
        private readonly ILogger _logger;
        private readonly IDateTimeService _dateTimeService;
        private readonly IUserReferalFeeService _userReferalFeeService;
        private readonly IUserReferredFeeService _userReferredFeeService;

        public ReferalService(SmsDataContext smsDataContext, IUserService userService,
            ISystemConfigurationService systemConfigurationService,
            IUserTransactionService userTransactionService,
            ILogger<ReferalService> logger,
            IDateTimeService dateTimeService,
            IUserReferalFeeService userReferalFeeService,
            IUserReferredFeeService userReferredFeeService)
        {
            _smsDataContext = smsDataContext;
            _userService = userService;
            _systemConfigurationService = systemConfigurationService;
            _userTransactionService = userTransactionService;
            _logger = logger;
            _dateTimeService = dateTimeService;
            _userReferalFeeService = userReferalFeeService;
            _userReferredFeeService = userReferredFeeService;
        }
        public async Task ProcessReferFee()
        {
            var nowGMT7 = _dateTimeService.GMT7Now();
            var nowUTC = _dateTimeService.UtcNow();
            if (nowGMT7.Hour != TIME_TO_RUN_PROCESS_FEE || nowGMT7.Minute >= 30) return;
            var count = await (from u in _smsDataContext.Users
                               where u.ReferalId.HasValue
                               select u.Id).CountAsync();
            var idOfReferedUserNeedCalculateNeedCalculateList = new List<int>();
            var pageSize = 1000;
            var page = 0;
            do
            {
                idOfReferedUserNeedCalculateNeedCalculateList
                    .AddRange(await (from u in _smsDataContext.Users
                                     where u.ReferalId.HasValue
                                     select u.Id).Skip(page * pageSize).Take(pageSize).ToListAsync());
                page++;
            } while (idOfReferedUserNeedCalculateNeedCalculateList.Count < count);
            _logger.LogInformation("Total refered users: {0}", idOfReferedUserNeedCalculateNeedCalculateList.Count);

            var configuration = (await _systemConfigurationService.GetAlls()).FirstOrDefault();
            var referalFeePercent = configuration.ReferalFee;
            var referedUserFeePercent = configuration.ReferredUserFee;

            foreach (var referedUserId in idOfReferedUserNeedCalculateNeedCalculateList)
            {
                _logger.LogInformation("Start calculate for refered user: {0}", referedUserId);
                var referedUser = await _userService.Get(referedUserId);
                var referalUser = await _userService.Get(referedUser.ReferalId.Value);
                if (referedUser == null || referalUser == null) continue;


                var queryForBasic = (from or in _smsDataContext.OrderResults
                                     join rcOrder in _smsDataContext.RentCodeOrders on or.OrderId equals rcOrder.Id
                                     where rcOrder.UserId == referedUserId
                                         && rcOrder.Status == Helpers.OrderStatus.Success
                                         && rcOrder.PendingReferalCalculate == true
                                         && rcOrder.ServiceProvider.ServiceType != Helpers.ServiceType.Basic
                                     select or.Cost
                                );

                var costForBasic = await queryForBasic.SumAsync();
                var countForBasic = await queryForBasic.CountAsync();

                _logger.LogInformation("Basic cost: {0}", costForBasic);
                _logger.LogInformation("Basic count: {0}", countForBasic);

                var queryForByTime = (from o in _smsDataContext.RentCodeOrders
                                      where o.UserId == referedUserId
                                      && o.Status == Helpers.OrderStatus.Success
                                      && o.PendingReferalCalculate == true
                                      && o.ServiceProvider.ServiceType == Helpers.ServiceType.ByTime
                                      select o.Price
                                          );
                var costForByTime = await queryForByTime.SumAsync();
                var countForByTime = await queryForByTime.CountAsync();
                _logger.LogInformation("By time cost: {0}", costForByTime);
                _logger.LogInformation("By time count: {0}", countForByTime);

                var totalCost = costForBasic + costForByTime;
                if (totalCost > 0)
                {
                    var totalCount = countForBasic + countForByTime;
                    var referedFee = totalCost * referedUserFeePercent / 100;
                    await _userTransactionService.Create(new UserTransaction()
                    {
                        Amount = referedFee,
                        Comment = "Chiec khau gioi thieu",
                        IsImport = true,
                        UserId = referedUser.Id,
                        UserTransactionType = Helpers.UserTransactionType.ReferedFee
                    });
                    _logger.LogInformation("Update refered balance: {0}", referedFee);
                    await _userReferredFeeService.Create(new UserReferredFee()
                    {
                        ReportTime = nowUTC,
                        TotalCost = totalCost,
                        FeeAmount = referedFee,
                        TotalOrderCount = totalCount,
                        ReferFeePercent = referedUserFeePercent,
                        UserId = referedUserId
                    });

                    var referalFee = totalCost * referalFeePercent / 100;
                    await _userTransactionService.Create(new UserTransaction()
                    {
                        Amount = referalFee,
                        Comment = $"Phi gioi thieu <{referedUser.Username}>",
                        IsImport = true,
                        UserId = referalUser.Id,
                        UserTransactionType = Helpers.UserTransactionType.ReferalFee
                    });
                    _logger.LogInformation("Update referal balance: {0}", referalFee);
                    await _userReferalFeeService.Create(new UserReferalFee()
                    {
                        ReportTime = nowUTC,
                        TotalCost = totalCost,
                        FeeAmount = referalFee,
                        TotalOrderCount = totalCount,
                        ReferFeePercent = referalFeePercent,
                        UserId = referalUser.Id,
                        ReferredUserId = referedUserId
                    });
                }
                var sql = @"UPDATE Orders set PendingReferalCalculate = 0 where UserId = {0} and Status = 4 and PendingReferalCalculate = 1";
                await _smsDataContext.Database.ExecuteSqlCommandAsync(
                    sql,
                    referedUserId
                    );
                _logger.LogInformation("Clean up order and end");
            }
            await _smsDataContext.Database.ExecuteSqlCommandAsync("UPDATE Orders set PendingReferalCalculate = 0 where Status = 4 and PendingReferalCalculate = 1");
        }
    }
}
