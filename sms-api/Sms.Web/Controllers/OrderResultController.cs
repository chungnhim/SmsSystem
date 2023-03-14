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
    public class OrderResultController : BaseRestfulController<IOrderResultService, OrderResult>
    {
        public OrderResultController(IOrderResultService OrderResultService) : base(OrderResultService)
        {
        }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<OrderResult>> Post([FromBody] OrderResult value)
        {
            return base.Post(value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<OrderResult>> Put(int id, [FromBody] OrderResult value)
        {
            return base.Put(id, value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<IEnumerable<OrderResult>> Get()
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
        public override Task<ApiResponseBaseModel<OrderResult>> Patch(int id, [FromBody] JsonPatchDocument<OrderResult> patchDoc)
        {
            return base.Patch(id, patchDoc);
        }
    }
}
