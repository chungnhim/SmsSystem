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
    public interface IOrderComplaintService : IServiceBase<OrderComplaint>
    {
        Task<ApiResponseBaseModel<OrderComplaint>> Complain(int orderId, OrderComplaintRequest request);
    }
    public class OrderComplaintService : ServiceBase<OrderComplaint>, IOrderComplaintService
    {
        private readonly IOrderService _orderService;
        private readonly IAuthService _authService;
        public OrderComplaintService(SmsDataContext smsDataContext, IOrderService orderService, IAuthService authService) : base(smsDataContext)
        {
            _orderService = orderService;
            _authService = authService;
        }
        public override void Map(OrderComplaint entity, OrderComplaint model)
        {
            if (entity.OrderComplaintStatus != model.OrderComplaintStatus && entity.OrderComplaintStatus != OrderComplaintStatus.Refund)
            {
                entity.OrderComplaintStatus = model.OrderComplaintStatus;
            }
            entity.AdminComment = model.AdminComment;
            entity.UserComment = model.UserComment;
        }
        public override async Task<ApiResponseBaseModel<OrderComplaint>> Update(OrderComplaint model)
        {
            var entity = await Get(model.Id);
            if (entity == null)
            {
                return new ApiResponseBaseModel<OrderComplaint>()
                {
                    Success = false,
                    Message = "NotFound"
                };
            }
            if (entity.OrderComplaintStatus != model.OrderComplaintStatus)
            {
                if (model.OrderComplaintStatus == OrderComplaintStatus.Refund)
                {
                    var transactionOfStaff = await _smsDataContext.UserTransactions.Include(r => r.User).Where(r => r.OrderId == entity.OrderId && r.UserTransactionType == UserTransactionType.AgentDiscount).FirstOrDefaultAsync();
                    if (transactionOfStaff.User.Ballance < transactionOfStaff.Amount)
                    {
                        return new ApiResponseBaseModel<OrderComplaint>
                        {
                            Success = false,
                            Message = "Agency does not have enough money to make this transaction"
                        };
                    }
                    await DoRefundMoneyForOrder(entity.OrderId);
                }
            }
            Map(entity, model);
            var validateResult = await ValidateEntry(entity);
            if (!string.IsNullOrEmpty(validateResult))
            {
                return new ApiResponseBaseModel<OrderComplaint>()
                {
                    Success = false,
                    Message = validateResult,
                };
            }
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel<OrderComplaint>() { Results = entity, Success = true };
        }

        private async Task DoRefundMoneyForOrder(int orderId)
        {
            var order = await _smsDataContext.Orders.FirstOrDefaultAsync(r => r.Id == orderId);
            if (order == null) return;
            var transaction = _smsDataContext.UserTransactions.Where(r => r.OrderId == orderId);
            if (transaction == null) return;

            var transactionOfUser = await transaction.Where(r => r.UserTransactionType == UserTransactionType.PaidForService).FirstOrDefaultAsync();
            var transactionOfStaff = await transaction.Where(r => r.UserTransactionType == UserTransactionType.AgentDiscount).FirstOrDefaultAsync();
            var newTrans = new UserTransaction()
            {
                Amount = transactionOfUser.Amount,
                Comment = "Refurn order " + order.Guid,
                IsImport = true,
                UserId = transactionOfUser.UserId,
                OrderId = orderId,
                UserTransactionType = UserTransactionType.PaidForService
            };
            if (transactionOfStaff != null)
            {
                var newTransForStaff = new UserTransaction()
                {
                    Amount = transactionOfStaff.Amount,
                    Comment = "Compensate order " + order.Guid,
                    IsImport = false,
                    UserId = transactionOfStaff.UserId,
                    OrderId = orderId,
                    UserTransactionType = UserTransactionType.AgentDiscount
                };

                _smsDataContext.UserTransactions.Add(newTransForStaff);

                await UpdateUserBallance(transactionOfStaff.UserId, -1 * transactionOfStaff.Amount);
            }

            _smsDataContext.UserTransactions.Add(newTrans);
            await _smsDataContext.SaveChangesAsync();
            await UpdateUserBallance(transactionOfUser.UserId, transactionOfUser.Amount);
        }

        protected override IQueryable<OrderComplaint> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include("Order.ServiceProvider").Include("Order.User");
            query = query.Include(r => r.Order.OrderResults).ThenInclude(r => r.SmsHistory);

            if (filterRequest != null)
            {
                var sortColumn = (filterRequest.SortColumnName ?? string.Empty).ToLower();
                var isAsc = filterRequest.IsAsc;
                DateTime? createdFrom = null;
                DateTime? createdTo = null;
                OrderComplaintStatus? status = null;
                int userId = 0;
                List<int> serviceProviderIds = null;
                {
                    if (filterRequest.SearchObject.TryGetValue("status", out object obj))
                    {
                        status = (OrderComplaintStatus)int.Parse(obj.ToString());
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("serviceProviderIds", out object obj))
                    {
                        serviceProviderIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>(); ;
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("createdFrom", out object obj))
                    {
                        createdFrom = (DateTime)obj;
                    }
                }

                {
                    if (filterRequest.SearchObject.TryGetValue("createdTo", out object obj))
                    {
                        createdTo = (DateTime)obj;
                    }
                }

                {
                    if (filterRequest.SearchObject.TryGetValue("UserId", out object obj))
                    {
                        userId = int.Parse(obj.ToString());
                    }
                }

                if (status != null)
                {
                    query = query.Where(x => x.OrderComplaintStatus == status);
                }
                if (serviceProviderIds != null && serviceProviderIds.Count > 0)
                {
                    query = query.Where(x => x.Order.OrderType == OrderType.RentCode && serviceProviderIds.Contains((x.Order as RentCodeOrder).ServiceProviderId));
                }
                if (createdTo != null)
                {
                    createdTo = createdTo.GetValueOrDefault().AddDays(1);
                    query = query.Where(x => x.Created < createdTo);
                }
                if (createdFrom != null)
                {
                    query = query.Where(x => x.Created >= createdFrom);
                }
                if (userId != 0)
                {
                    query = query.Where(x => x.Order.UserId == userId);
                }
                switch (sortColumn)
                {
                    case "created":
                        query = isAsc ? query.OrderBy(x => x.Created) : query.OrderByDescending(x => x.Created);
                        break;
                    case "status":
                        query = isAsc ? query.OrderBy(x => x.OrderComplaintStatus) : query.OrderByDescending(x => x.OrderComplaintStatus);
                        break;
                    default:
                        break;
                }
            }

            return query;
        }
        public async Task<ApiResponseBaseModel<OrderComplaint>> Complain(int orderId, OrderComplaintRequest request)
        {
            var order = await _orderService.Get(orderId);
            if (order == null) return new ApiResponseBaseModel<OrderComplaint>()
            {
                Success = false,
                Message = "OrderNotFound"
            };

            if ((order.Status & (Helpers.OrderStatus.Success | Helpers.OrderStatus.Cancelled | Helpers.OrderStatus.Error)) == 0)
            {
                return new ApiResponseBaseModel<OrderComplaint>()
                {
                    Success = false,
                    Message = "StatusInvalid"
                };
            }
            var userId = _authService.CurrentUserId().GetValueOrDefault();
            if (order.UserId != userId)
            {

                return new ApiResponseBaseModel<OrderComplaint>()
                {
                    Success = false,
                    Message = "NotYourOrder"
                };
            }
            var newComplaint = new OrderComplaint()
            {
                OrderComplaintStatus = OrderComplaintStatus.Floating,
                UserComment = request.Comment,
                OrderId = orderId
            };
            _smsDataContext.OrderComplaints.Add(newComplaint);
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel<OrderComplaint>()
            {
                Success = true,
                Results = newComplaint
            };
        }
    }
}
