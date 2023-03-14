using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Sms.Web.Entity;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UserController : BaseRestfulController<IUserService, User>
    {
        private readonly IUserTransactionService _userTransactionService;
        public UserController(IUserService UserService, IUserTransactionService userTransactionService) : base(UserService)
        {
            _userTransactionService = userTransactionService;
        }
        [HttpPost("reset-user-password")]
        public async Task<ApiResponseBaseModel<string>> ResetPasswrod([FromBody]ResetUserPasswordRequest request)
        {
            var user = await _service.Get(request.UserId.Value);
            if (user == null) return new ApiResponseBaseModel<string>()
            {
                Success = false,
                Message = "NotFound"
            };
            if (user.Role == Helpers.RoleType.Administrator)
            {
                return new ApiResponseBaseModel<string>()
                {
                    Success = false,
                    Message = "CannotBanAdmin"
                };
            }
            user.Password = Guid.NewGuid().ToString().Split("-")[0];

            await _service.Update(user);

            return new ApiResponseBaseModel<string>()
            {
                Success = true,
                Results = user.Password
            };
        }
        [HttpPost("change-user-status")]
        public async Task<ApiResponseBaseModel> ChangeUserStatus([FromBody] ChangeUserStatusRequest request)
        {
            var user = await _service.Get(request.UserId.Value);
            if (user == null) return new ApiResponseBaseModel()
            {
                Success = false,
                Message = "NotFound"
            };
            if (user.Role == Helpers.RoleType.Administrator)
            {
                return new ApiResponseBaseModel()
                {
                    Success = false,
                    Message = "CannotBanAdmin"
                };
            }
            user.IsBanned = request.IsBanned.GetValueOrDefault();

            _service.ClearLoginCache(user);
            await _service.Update(user);

            return new ApiResponseBaseModel()
            {
                Success = true
            };
        }
        [HttpPost("change-user-refer-enabled")]
        public async Task<ApiResponseBaseModel> ChangeUserReferalEnabled([FromBody] ChangeUserReferEnabled request)
        {
            var user = await _service.Get(request.UserId);
            if (user == null) return new ApiResponseBaseModel()
            {
                Success = false,
                Message = "NotFound"
            };
            if (user.Role == Helpers.RoleType.Administrator)
            {
                return new ApiResponseBaseModel()
                {
                    Success = false,
                    Message = "CannotEnableReferalForAdmin"
                };
            }
            await _service.ToggleReferEnabled(request.UserId, request.ReferEnabled);

            return new ApiResponseBaseModel()
            {
                Success = true
            };
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<User>> Put(int id, [FromBody] User value)
        {
            return base.Put(id, value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<User>> Patch(int id, [FromBody] JsonPatchDocument<User> patchDoc)
        {
            return base.Patch(id, patchDoc);
        }
        [HttpPost("{id}/recharge")]
        public async Task<ApiResponseBaseModel<UserTransaction>> Recharge(int id, [FromBody]RechargeRequestModel request)
        {
            var transaction = new UserTransaction()
            {
                UserId = id,
                Comment = request.Comment,
                Amount = request.Amount,
                IsImport = request.IsImport,
                UserTransactionType = Helpers.UserTransactionType.UserRecharge,
                OfflinePaymentMethodType = Helpers.OfflinePaymentMethodType.ManualAdmin
            };
            return await _userTransactionService.Create(transaction);
        }
        [HttpPost("{id}/transaction/paging")]
        public async Task<FilterResponse<UserTransaction>> PagingTransaction(int id, [FromBody]FilterRequest filterRequest)
        {
            filterRequest.SearchObject = filterRequest.SearchObject ?? new Dictionary<string, object>();
            filterRequest.SearchObject["UserId"] = id;
            return await _userTransactionService.Paging(filterRequest);
        }
        [HttpPost("add-a-staff")]
        public async Task<ApiResponseBaseModel<User>> AddAStaff(RegisterNoCaptcharRequest registerRequest)
        {
            var createResult = await _service.CreateStaff(registerRequest);
            return createResult;
        }

        [HttpPost("{userId}/assign-gsm")]
        public async Task<ApiResponseBaseModel<User>> AssignGsm(int userId, [FromBody]AssignGsmToUserRequest request)
        {
            return await _service.AssignGsmsToUser(userId, request);
        }
    }
}
