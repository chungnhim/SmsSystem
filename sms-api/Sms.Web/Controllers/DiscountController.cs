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
    [Authorize(Roles = "Administrator,Staff")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DiscountController : BaseRestfulController<IDiscountService, Discount>
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        public DiscountController(IDiscountService DiscountService, IAuthService authService, IUserService userService) : base(DiscountService)
        {
            _authService = authService;
            _userService = userService;
        }
        [HttpGet("{gsmId}/{month}/{year}")]
        public async Task<ApiResponseBaseModel<List<Discount>>> GetDiscountTable(int gsmId, int month, int year)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel<List<Discount>>.UnAuthorizedResponse();
            var user = await _userService.GetUser(userId.GetValueOrDefault());
            if (user != null && user.Role == Helpers.RoleType.Staff && !user.UserGsmDevices.Any(r => r.GsmDeviceId == gsmId)) return ApiResponseBaseModel<List<Discount>>.UnAuthorizedResponse();
            return await _service.GetDiscountTable(gsmId, month, year);
        }
        public override async Task<ApiResponseBaseModel<Discount>> Patch(int id, [FromBody] JsonPatchDocument<Discount> patchDoc)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel<Discount>.UnAuthorizedResponse();
            var user = await _userService.GetUser(userId.GetValueOrDefault());
            if (user == null || user.Role != Helpers.RoleType.Administrator) return ApiResponseBaseModel<Discount>.UnAuthorizedResponse();
            return await base.Patch(id, patchDoc);
        }
        [HttpPost("apply-for-all")]
        public async Task<ApiResponseBaseModel> ApplyDiscountForAll(int templateId)
        {
            var userId = _authService.CurrentUserId();
            if (userId == null) return ApiResponseBaseModel<Discount>.UnAuthorizedResponse();
            var user = await _userService.GetUser(userId.GetValueOrDefault());
            if (user == null || user.Role != Helpers.RoleType.Administrator) return ApiResponseBaseModel<Discount>.UnAuthorizedResponse();
            return await _service.ApplyDiscountForAll(templateId);

        }
    }
}
