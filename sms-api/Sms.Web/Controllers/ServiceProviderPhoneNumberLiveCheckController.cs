using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Staff")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ServiceProviderPhoneNumberLiveCheckController : BaseRestfulController<IServiceProviderPhoneNumberLiveCheckService, ServiceProviderPhoneNumberLiveCheck>
    {
        private readonly IUserService _userService;
        public ServiceProviderPhoneNumberLiveCheckController(IServiceProviderPhoneNumberLiveCheckService serviceProviderPhoneNumberLiveCheckService,
             IUserService userService) : base(serviceProviderPhoneNumberLiveCheckService)
        {
            _userService = userService;
        }
        public override async Task<FilterResponse<ServiceProviderPhoneNumberLiveCheck>> Paging([FromBody] FilterRequest filterRequest)
        {
            var currentUser = await _userService.GetCurrentUser();
            if (currentUser == null) return new FilterResponse<ServiceProviderPhoneNumberLiveCheck>()
            {
                Results = new List<ServiceProviderPhoneNumberLiveCheck>()
            };

            if (currentUser.Role == RoleType.Staff)
            {
                filterRequest.SearchObject = filterRequest.SearchObject ?? new Dictionary<string, object>();
                filterRequest.SearchObject.Add("staffId", currentUser.Id);
            }
            return await base.Paging(filterRequest);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<ServiceProviderPhoneNumberLiveCheck>> Post([FromBody] ServiceProviderPhoneNumberLiveCheck value)
        {
            return base.Post(value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<ServiceProviderPhoneNumberLiveCheck>> Put(int id, [FromBody] ServiceProviderPhoneNumberLiveCheck value)
        {
            return base.Put(id, value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<IEnumerable<ServiceProviderPhoneNumberLiveCheck>> Get()
        {
            return base.Get();
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<int>> Delete(int id)
        {
            return base.Delete(id);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<ServiceProviderPhoneNumberLiveCheck>> Patch(int id, [FromBody] JsonPatchDocument<ServiceProviderPhoneNumberLiveCheck> patchDoc)
        {
            return base.Patch(id, patchDoc);
        }
    }
}
