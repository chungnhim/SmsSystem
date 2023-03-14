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
    public class UserReferredFeeController : BaseRestfulController<IUserReferredFeeService, UserReferredFee>
    {
        private readonly IUserService _userService;
        public UserReferredFeeController(IUserReferredFeeService service, IUserService userService) : base(service)
        {
            _userService = userService;
        }

        public override async Task<FilterResponse<UserReferredFee>> Paging([FromBody] FilterRequest filterRequest)
        {
            var currentUser = await _userService.GetCurrentUser();
            if (currentUser == null) return new FilterResponse<UserReferredFee>()
            {
                Results = new List<UserReferredFee>()
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
        public override Task<ApiResponseBaseModel<UserReferredFee>> Post([FromBody] UserReferredFee value)
        {
            return base.Post(value);
        }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<UserReferredFee>> Put(int id, [FromBody] UserReferredFee value)
        {
            return base.Put(id, value);
        }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<UserReferredFee>> Patch(int id, [FromBody] JsonPatchDocument<UserReferredFee> patchDoc)
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
