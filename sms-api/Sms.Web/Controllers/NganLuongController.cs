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
    public class ClientNganLuongController : ControllerBase
    {
        private readonly INganLuongPaymentService _NganLuongPaymentService;
        public ClientNganLuongController(INganLuongPaymentService NganLuongPaymentService)
        {
            _NganLuongPaymentService = NganLuongPaymentService;
        }

        [HttpPost("request-a-payment")]
        public async Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestAPayment([FromBody]GeneralPaymentRequest NganLuongPaymentRequest)
        {
            return await _NganLuongPaymentService.RequestNganLuongPayment(NganLuongPaymentRequest);
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NganLuongController : ControllerBase
    {
        private readonly INganLuongPaymentService _NganLuongPaymentService;
        public NganLuongController(INganLuongPaymentService NganLuongPaymentService)
        {
            _NganLuongPaymentService = NganLuongPaymentService;
        }

        [HttpGet("ReturnCallback")]
        public async Task ReturnCallback(string transaction_info, int price, int payment_id, int payment_type, string error_text, string secure_code, string token_nl, string order_code)
        {
            var model = new NganLuongNotifyReturnModel()
            {
                transaction_info = transaction_info,
                price = price,
                payment_id = payment_id,
                error_text = error_text,
                order_code = order_code,
                payment_type = payment_type,
                secure_code = secure_code,
                token_nl = token_nl,
            };
            await _NganLuongPaymentService.ProcessNganLuongCallback(model);
        }
    }

}
