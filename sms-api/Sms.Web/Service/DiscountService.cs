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
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IDiscountService : IServiceBase<Discount>
    {
        Task<ApiResponseBaseModel<List<Discount>>> GetDiscountTable(int gsmId, int month, int year);
        Task BackgroundProcessDiscountTable();
        Task<ApiResponseBaseModel> ApplyDiscountForAll(int templateId);
    }
    public class DiscountService : ServiceBase<Discount>, IDiscountService
    {
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger _logger;
        public DiscountService(SmsDataContext smsDataContext, IDateTimeService dateTimeService, ILogger<DiscountService> logger) : base(smsDataContext)
        {
            _dateTimeService = dateTimeService;
            _logger = logger;
        }
        public override void Map(Discount entity, Discount model)
        {
            entity.Percent = model.Percent;
        }
        protected override async Task<string> ValidateEntry(Discount entity)
        {
            var duplicateDiscountCode = await _smsDataContext.Discounts.AnyAsync(r => r.GsmDeviceId == entity.GsmDeviceId
                && r.ServiceProviderId == entity.ServiceProviderId
                && r.Month == entity.Month
                && r.Year == entity.Year
                && r.Id != entity.Id);
            if (duplicateDiscountCode)
            {
                return "DuplicateDiscountCode";
            }
            return await base.ValidateEntry(entity);
        }
        public async Task<ApiResponseBaseModel<List<Discount>>> GetDiscountTable(int gsmId, int month, int year)
        {
            var serviceIds = await _smsDataContext.ServiceProviders.Where(r => r.Disabled != true)
                .Select(r => r.Id).ToListAsync();
            var currentDiscounts = await _smsDataContext.Discounts.Where(r => r.GsmDeviceId == gsmId && r.Month == month && r.Year == year)
                .ToListAsync();
            var notIncludeServiceIds = serviceIds.Where(r => !currentDiscounts.Any(c => c.ServiceProviderId == r)).ToList();
            if (notIncludeServiceIds.Any())
            {
                var lastDiscounts = await (from d in _smsDataContext.Discounts
                                           where d.GsmDeviceId == gsmId
                                           orderby d.Year * 12 + d.Month descending
                                           group d by d.ServiceProviderId into gr
                                           select gr.FirstOrDefault()).ToListAsync();
                if (lastDiscounts.Count == 0)
                {
                    var hasValueGsm = await (from d in _smsDataContext.Discounts
                                             orderby d.Year * 12 + d.Month descending
                                             select d).FirstOrDefaultAsync();
                    if (hasValueGsm != null)
                    {
                        lastDiscounts = await (from d in _smsDataContext.Discounts
                                               where d.GsmDeviceId == hasValueGsm.GsmDeviceId
                                               orderby d.Year * 12 + d.Month descending
                                               group d by d.ServiceProviderId into gr
                                               select gr.FirstOrDefault()).ToListAsync();
                    }
                }
                foreach (var id in notIncludeServiceIds)
                {
                    var newDiscount = new Discount()
                    {
                        GsmDeviceId = gsmId,
                        Month = month,
                        Year = year,
                        ServiceProviderId = id,
                        Percent = lastDiscounts.FirstOrDefault(r => r.ServiceProviderId == id)?.Percent ?? 50,
                    };
                    _smsDataContext.Discounts.Add(newDiscount);
                }
                await _smsDataContext.SaveChangesAsync();
                currentDiscounts = await _smsDataContext.Discounts.Where(r => r.GsmDeviceId == gsmId && r.Month == month && r.Year == year)
                    .ToListAsync();
            }
            return new ApiResponseBaseModel<List<Discount>>()
            {
                Results = currentDiscounts
            };
        }

        public async Task BackgroundProcessDiscountTable()
        {
            var gsmIds = await _smsDataContext.GsmDevices.Select(r => r.Id).ToListAsync();
            var month = _dateTimeService.GMT7Now().Month - 1;
            var year = _dateTimeService.GMT7Now().Year;
            foreach (var gsmId in gsmIds)
            {
                await GetDiscountTable(gsmId, month, year);
            }
        }

        public async Task<ApiResponseBaseModel> ApplyDiscountForAll(int templateId)
        {
            var template = await Get(templateId);
            if (template == null) return ApiResponseBaseModel.NotFoundResourceResponse();

            await _smsDataContext.Database.ExecuteSqlCommandAsync(@"update Discounts set [Percent] = {0}
                        where  month = {1} and year = {2} and ServiceProviderId  = {3}",
                        template.Percent,
                        template.Month,
                        template.Year,
                        template.ServiceProviderId
                        );

            return new ApiResponseBaseModel();
        }
    }
}
