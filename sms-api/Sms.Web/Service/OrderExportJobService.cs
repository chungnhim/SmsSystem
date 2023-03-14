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
    public interface IOrderExportJobService : IServiceBase<OrderExportJob>
    {
        Task<OrderExportJob> GetOrderExportByStatus(OrderExportStatus status);
    }
    public class OrderExportJobService : ServiceBase<OrderExportJob>, IOrderExportJobService
    {
        private readonly IUserService _userService;
        public OrderExportJobService(SmsDataContext smsDataContext,
            IUserService userService) : base(smsDataContext)
        {
            _userService = userService;
        }
        public override void Map(OrderExportJob entity, OrderExportJob model)
        {
            entity.Status = model.Status;
            entity.UrlExport = model.UrlExport;
        }
        public async Task<OrderExportJob> GetOrderExportByStatus(OrderExportStatus status)
        {
            var currentUser = await _userService.GetCurrentUser();
            var orderExportJobLasted = await (from o in _smsDataContext.OrderExportJobs
                                              where o.UserId == currentUser.Id
                                                  && o.Status == status
                                              select o).FirstOrDefaultAsync();
            return orderExportJobLasted;
        }
    }
}