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
    [Authorize(Roles = "Administrator,Staff,User")]
    public class UserReferalFeeController : BaseRestfulController<IUserReferalFeeService, UserReferalFee>
    {
        private readonly IUserService _userService;
        public UserReferalFeeController(IUserReferalFeeService service, IUserService userService) : base(service)
        {
            _userService = userService;
        }

        public override async Task<FilterResponse<UserReferalFee>> Paging([FromBody] FilterRequest filterRequest)
        {
            var currentUser = await _userService.GetCurrentUser();
            if (currentUser == null) return new FilterResponse<UserReferalFee>()
            {
                Results = new List<UserReferalFee>()
            };

            if(currentUser.Role!= Helpers.RoleType.Administrator)
            {
                filterRequest.SearchObject = filterRequest.SearchObject ?? new Dictionary<string, object>();
                filterRequest.SearchObject.Add("UserId", currentUser.Id);
            }
            return await base.Paging(filterRequest);
        }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<UserReferalFee>> Post([FromBody] UserReferalFee value)
        {
            return base.Post(value);
        }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<UserReferalFee>> Put(int id, [FromBody] UserReferalFee value)
        {
            return base.Put(id, value);
        }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<UserReferalFee>> Patch(int id, [FromBody] JsonPatchDocument<UserReferalFee> patchDoc)
        {
            return base.Patch(id, patchDoc);
        }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<int>> Delete(int id)
        {
            return base.Delete(id);
        }
    }
}
