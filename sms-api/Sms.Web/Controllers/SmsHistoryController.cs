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
    public class SmsHistoryController : BaseRestfulController<ISmsHistoryService, SmsHistory>
    {
        public SmsHistoryController(ISmsHistoryService SmsHistoryService) : base(SmsHistoryService)
        {
        }
        [HttpPost("clean-up")]
        [Authorize(Roles = "Administrator")]
        public async Task<ApiResponseBaseModel<int>> CleanUp([FromBody]SmsHistoryCleanUpRequest request)
        {
            return new ApiResponseBaseModel<int>()
            {
                Success = true,
                Results = await _service.CleanUpSmsHistory(request.FromDate, request.ToDate)
            };
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override async Task<ApiResponseBaseModel<SmsHistory>> Patch(int id, [FromBody] JsonPatchDocument<SmsHistory> patchDoc)
        {
            return await base.Patch(id, patchDoc);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<SmsHistory>> Put(int id, [FromBody] SmsHistory value)
        {
            return base.Put(id, value);
        }
        [Obsolete("", true)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<SmsHistory>> Post([FromBody] SmsHistory value)
        {
            return base.Post(value);
        }
        [Obsolete("", true)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<int>> Delete(int id)
        {
            return base.Delete(id);
        }

        [HttpPost("{id}/check-sms-match-service")]
        public async Task<ApiResponseBaseModel<int?>> CheckSmsMatchWithService(int id, int serviceProviderId)
        {
            return await _service.CheckSmsMatchWithService(id, serviceProviderId);
        }

        [HttpPost("{id}/re-match")]
        public async Task<ApiResponseBaseModel<int?>> RematchWithService(int id)
        {
            return await _service.RematchWithService(id);
        }
    }
}
