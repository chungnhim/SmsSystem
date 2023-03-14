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
    [Authorize(Roles = "User")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UserOfflinePaymentReceiptController : ControllerBase
    {
        private readonly IUserOfflinePaymentReceiptService _userOfflinePaymentReceiptService;
        public UserOfflinePaymentReceiptController(IUserOfflinePaymentReceiptService userOfflinePaymentReceiptService)
        {
            _userOfflinePaymentReceiptService = userOfflinePaymentReceiptService;
        }

        [HttpPost("my-receipt")]
        public async Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> GenerateOrGetMyLatestPaymentReceipt(int? methodId)
        {
            return await _userOfflinePaymentReceiptService.GenerateOrGetMyLatestPaymentReceipt(methodId);
        }
        [HttpPost("confirm-receipt")]
        public async Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> ConfirmReceipt([FromBody]UserReceiptConfirmRequest confirmRequest)
        {
            return await _userOfflinePaymentReceiptService.UserConfirmReceipt(confirmRequest);
        }
    }
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class UserOfflinePaymentReceiptAdminController : BaseRestfulController<IUserOfflinePaymentReceiptService, UserOfflinePaymentReceipt>
    {
        public UserOfflinePaymentReceiptAdminController(IUserOfflinePaymentReceiptService userOfflinePaymentReceiptService) : base(userOfflinePaymentReceiptService)
        {
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override async Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> Patch(int id, [FromBody] JsonPatchDocument<UserOfflinePaymentReceipt> patchDoc)
        {
            return await base.Patch(id, patchDoc);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> Put(int id, [FromBody] UserOfflinePaymentReceipt value)
        {
            return base.Put(id, value);
        }
        [Obsolete("", true)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> Post([FromBody] UserOfflinePaymentReceipt value)
        {
            return base.Post(value);
        }
        [Obsolete("", true)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<int>> Delete(int id)
        {
            return base.Delete(id);
        }

        [HttpPost("offline-payment-notice-manual")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ApiResponseBaseModel> OfflinePaymentNotice([FromBody]OfflinePaymentNoticeModel request)
        {
            return await _service.HandleOfflinePaymentNotice(request, true);
        }
    }
}
