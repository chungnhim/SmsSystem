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
    public interface ICheckoutRequestService : IServiceBase<CheckoutRequest>
    {
        Task<ApiResponseBaseModel<CheckoutRequest>> ApproveCheckout(int id);
        Task<ApiResponseBaseModel<CheckoutRequest>> CancelCheckout(int id);
        Task<ApiResponseBaseModel<CheckoutRequest>> ReportError(int id, string comment);
        Task<ApiResponseBaseModel<CheckoutRequest>> FinishCheckout(int id);
        Task<ApiResponseBaseModel<CheckoutRequest>> RequestCheckout(CheckoutRequest checkoutRequest);
    }
    public class CheckoutRequestService : ServiceBase<CheckoutRequest>, ICheckoutRequestService
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        public CheckoutRequestService(SmsDataContext smsDataContext, IAuthService authService, IUserService userService) : base(smsDataContext)
        {
            _authService = authService;
            _userService = userService;
        }

        public async Task<ApiResponseBaseModel<CheckoutRequest>> CancelCheckout(int id)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel<CheckoutRequest>.UnAuthorizedResponse();
            var checkout = await _smsDataContext.CheckoutRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (checkout == null) return ApiResponseBaseModel<CheckoutRequest>.NotFoundResourceResponse();
            if (checkout.UserId != userId.Value && (await _userService.GetUser(userId.Value))?.Role != RoleType.Administrator)
            {
                return ApiResponseBaseModel<CheckoutRequest>.NotFoundResourceResponse();
            }
            if(checkout.Status != CheckoutRequestStatus.Floating)
            {
                return new ApiResponseBaseModel<CheckoutRequest>()
                {
                    Success = false,
                    Message = "InvalidStatus"
                };
            }
            checkout.Status = CheckoutRequestStatus.Cancel;
            var staffId = checkout.UserId;
            var userTransaction = new UserTransaction()
            {
                Amount = checkout.Amount,
                IsImport = true,
                UserId = staffId,
                OrderId = null,
                Comment = $"Huy lenh rut tien <{checkout.Guid}>",
                UserTransactionType = UserTransactionType.AgentCheckout
            };
            _smsDataContext.UserTransactions.Add(userTransaction);
            await _smsDataContext.SaveChangesAsync();
            await UpdateUserBallance(staffId, checkout.Amount);
            return new ApiResponseBaseModel<CheckoutRequest>()
            {
                Results = checkout
            };
        }

        public async Task<ApiResponseBaseModel<CheckoutRequest>> ApproveCheckout(int id)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel<CheckoutRequest>.UnAuthorizedResponse();
            var checkout = await _smsDataContext.CheckoutRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (checkout == null) return ApiResponseBaseModel<CheckoutRequest>.NotFoundResourceResponse();
            if (checkout.UserId != userId.Value && (await _userService.GetUser(userId.Value))?.Role != RoleType.Administrator)
            {
                return ApiResponseBaseModel<CheckoutRequest>.NotFoundResourceResponse();
            }
            checkout.Status = CheckoutRequestStatus.Approve;
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel<CheckoutRequest>()
            {
                Results = checkout
            };
        }

        public async Task<ApiResponseBaseModel<CheckoutRequest>> FinishCheckout(int id)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel<CheckoutRequest>.UnAuthorizedResponse();
            var checkout = await _smsDataContext.CheckoutRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (checkout == null) return ApiResponseBaseModel<CheckoutRequest>.NotFoundResourceResponse();
            if (checkout.UserId != userId.Value && (await _userService.GetUser(userId.Value))?.Role != RoleType.Administrator)
            {
                return ApiResponseBaseModel<CheckoutRequest>.NotFoundResourceResponse();
            }
            checkout.Status = CheckoutRequestStatus.Finish;
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel<CheckoutRequest>()
            {
                Results = checkout
            };
        }

        public async Task<ApiResponseBaseModel<CheckoutRequest>> ReportError(int id, string comment)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel<CheckoutRequest>.UnAuthorizedResponse();
            var checkout = await _smsDataContext.CheckoutRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (checkout == null) return ApiResponseBaseModel<CheckoutRequest>.NotFoundResourceResponse();
            if (checkout.UserId != userId.Value && (await _userService.GetUser(userId.Value))?.Role != RoleType.Administrator)
            {
                return ApiResponseBaseModel<CheckoutRequest>.NotFoundResourceResponse();
            }
            checkout.Status = CheckoutRequestStatus.Error;
            checkout.Comment = comment;
            await _smsDataContext.SaveChangesAsync();
            return new ApiResponseBaseModel<CheckoutRequest>()
            {
                Results = checkout
            };
        }

        public override void Map(CheckoutRequest entity, CheckoutRequest model)
        {
        }
        protected override IQueryable<CheckoutRequest> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include(r => r.User);
            query = query.OrderByDescending(r => r.Created);

            if (filterRequest.SearchObject != null)
            {
                {
                    if (filterRequest.SearchObject.TryGetValue("UserId", out object obj))
                    {
                        var userId = (int)obj;
                        query = query.Where(r => r.UserId == userId);
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("Guid", out object obj))
                    {
                        var guid = obj.ToString();
                        query = query.Where(r => r.Guid == guid);
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("Status", out object obj))
                    {
                        var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<CheckoutRequestStatus>>();
                        if (ts.Count > 0)
                        {
                            query = query.Where(r => ts.Contains(r.Status));
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("username", out object obj))
                    {
                        var username = ((string)obj);
                        if (!string.IsNullOrEmpty(username))
                        {
                            username = username.ToLower();
                            if (username.StartsWith("[", StringComparison.OrdinalIgnoreCase) && username.EndsWith("]", StringComparison.OrdinalIgnoreCase))
                            {
                                username = username.TrimStart('[');
                                username = username.TrimEnd(']');
                                query = query.Where(r => r.User != null && r.User.Username == username);
                            }
                            else
                            {
                                query = query.Where(r => r.User != null && r.User.Username.ToLower().Contains(username));
                            }
                        }
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("CreatedFrom", out object obj))
                    {
                        var fromDate = (DateTime)obj;
                        query = query.Where(r => r.Created >= fromDate);
                    }
                }
                {
                    if (filterRequest.SearchObject.TryGetValue("CreatedTo", out object obj))
                    {
                        var toDate = (DateTime)obj;
                        toDate = toDate.AddDays(1);
                        query = query.Where(r => r.Created < toDate);
                    }
                }
            }
            return query;
        }

        public async Task<ApiResponseBaseModel<CheckoutRequest>> RequestCheckout(CheckoutRequest checkout)
        {
            var staffId = checkout.UserId;
            var user = await _userService.GetUser(staffId);
            if (user == null) return ApiResponseBaseModel<CheckoutRequest>.UnAuthorizedResponse();
            if (checkout.Amount > user.Ballance) return new ApiResponseBaseModel<CheckoutRequest>()
            {
                Success = false,
                Message = "NotEnoughCredit"
            };
            _smsDataContext.Add(checkout);
            await _smsDataContext.SaveChangesAsync();
            var userTransaction = new UserTransaction()
            {
                Amount = checkout.Amount,
                IsImport = false,
                UserId = staffId,
                OrderId = null,
                Comment = $"Yeu cau rut tien <{checkout.Guid}>",
                UserTransactionType = UserTransactionType.AgentCheckout
            };
            _smsDataContext.UserTransactions.Add(userTransaction);
            await _smsDataContext.SaveChangesAsync();
            await UpdateUserBallance(staffId, -1 * checkout.Amount);
            return new ApiResponseBaseModel<CheckoutRequest>()
            {
                Results = checkout
            };
        }
    }
}
