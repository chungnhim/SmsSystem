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
    [Authorize(Roles = "User")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ClientMomoController : ControllerBase
    {
        private readonly IMomoPaymentService _momoPaymentService;
        public ClientMomoController(IMomoPaymentService momoPaymentService)
        {
            _momoPaymentService = momoPaymentService;
        }
        
        [HttpPost("request-a-payment")]
        public async Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestAPayment([FromBody]GeneralPaymentRequest momoPaymentRequest)
        {
            return await _momoPaymentService.RequestMomoPayment(momoPaymentRequest);
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MomoController : ControllerBase
    {
        private readonly IMomoPaymentService _momoPaymentService;
        public MomoController(IMomoPaymentService momoPaymentService)
        {
            _momoPaymentService = momoPaymentService;
        }

        [HttpPost("ReturnCallback")]
        public async Task ReturnCallback([FromForm]MomoNotifyReturnModel returnModel)
        {
            await _momoPaymentService.ProcessMomoCallback(returnModel);
        }
    }

}
