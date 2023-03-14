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
    public class ComHistoryController : BaseRestfulController<IComHistoryService, ComHistory>
    {
        public ComHistoryController(IComHistoryService comHistoryService) : base(comHistoryService)
        {
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<ComHistory>> Post([FromBody] ComHistory value)
        {
            return base.Post(value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<ComHistory>> Put(int id, [FromBody] ComHistory value)
        {
            return base.Put(id, value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<IEnumerable<ComHistory>> Get()
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
        public override Task<ApiResponseBaseModel<ComHistory>> Patch(int id, [FromBody] JsonPatchDocument<ComHistory> patchDoc)
        {
            return base.Patch(id, patchDoc);
        }
    }
}
