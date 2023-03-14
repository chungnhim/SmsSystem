using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class ComController : BaseRestfulController<IComService, Com>
    {
        public ComController(IComService comService) : base(comService)
        {
        }
        [HttpPost("check-phone-number-efficiency")]
        public async Task<ApiResponseBaseModel<List<PhoneNumberEfficiency>>> CheckPhoneNumberEfficiency([FromBody]CheckPhoneNumberEfficiencyRequest request)
        {
            return await _service.CheckPhoneNumberEfficiency(request);
        }
    }
}
