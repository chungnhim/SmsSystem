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
    public class OfflinePaymentNoticeController : BaseRestfulController<IOfflinePaymentNoticeService, OfflinePaymentNotice>
    {
        private readonly IUserOfflinePaymentReceiptService _userOfflinePaymentReceiptService;
        public OfflinePaymentNoticeController(IOfflinePaymentNoticeService OfflinePaymentNoticeService, IUserOfflinePaymentReceiptService userOfflinePaymentReceiptService) : base(OfflinePaymentNoticeService)
        {
            _userOfflinePaymentReceiptService = userOfflinePaymentReceiptService;
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override async Task<ApiResponseBaseModel<OfflinePaymentNotice>> Patch(int id, [FromBody] JsonPatchDocument<OfflinePaymentNotice> patchDoc)
        {
            return await base.Patch(id, patchDoc);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<OfflinePaymentNotice>> Put(int id, [FromBody] OfflinePaymentNotice value)
        {
            return base.Put(id, value);
        }
        [Obsolete("", true)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<OfflinePaymentNotice>> Post([FromBody] OfflinePaymentNotice value)
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
            return await _userOfflinePaymentReceiptService.HandleOfflinePaymentNotice(request, true);
        }
    }
}
