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
    public class OrderResultReportController : BaseRestfulController<IOrderResultReportService, OrderResultReport>
    {
        public OrderResultReportController(IOrderResultReportService OrderResultReportService) : base(OrderResultReportService)
        {
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi =true)]
        public override Task<ApiResponseBaseModel<OrderResultReport>> Post([FromBody] OrderResultReport value)
        {
            return base.Post(value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<OrderResultReport>> Put(int id, [FromBody] OrderResultReport value)
        {
            return base.Put(id, value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<IEnumerable<OrderResultReport>> Get()
        {
            return base.Get();
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<int>> Delete(int id)
        {
            return base.Delete(id);
        }
    }
}
