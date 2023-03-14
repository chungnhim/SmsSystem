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
    [Authorize(Roles = "Administrator")]
    public class ErrorPhoneLogController : BaseRestfulController<IErrorPhoneLogService, ErrorPhoneLog>
    {
        public ErrorPhoneLogController(IErrorPhoneLogService errorPhoneLogService) : base(errorPhoneLogService)
        {
        }

        [HttpPost("{id}/reopen")]
        public async Task<ApiResponseBaseModel> ReopenError(int id)
        {
            return await _service.ReopenError(id);
        }
    }
}
