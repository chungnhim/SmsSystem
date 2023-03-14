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
  public class InternationalSimOrderController : BaseRestfulController<IInternationalSimOrderService, InternationalSimOrder>
  {
    public InternationalSimOrderController(IInternationalSimOrderService internationalSimOrderService) : base(internationalSimOrderService)
    {
    }
    [HttpPost("{id}/close-order")]
    public async Task<ApiResponseBaseModel> CloseOrder(int id)
    {
      return await _service.CloseOrder(id);
    }

    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ApiResponseBaseModel<InternationalSimOrder>> Post([FromBody] InternationalSimOrder value)
    {
      return base.Post(value);
    }
    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ApiResponseBaseModel<InternationalSimOrder>> Put(int id, [FromBody] InternationalSimOrder value)
    {
      return base.Put(id, value);
    }
    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<IEnumerable<InternationalSimOrder>> Get()
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
    public override Task<ApiResponseBaseModel<InternationalSimOrder>> Patch(int id, [FromBody] JsonPatchDocument<InternationalSimOrder> patchDoc)
    {
      return base.Patch(id, patchDoc);
    }
  }
}
