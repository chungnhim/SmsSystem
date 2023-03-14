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
    public class ClientPerfectMoneyController : ControllerBase
    {
        private readonly IPerfectMoneyPaymentService _PerfectMoneyPaymentService;
        public ClientPerfectMoneyController(IPerfectMoneyPaymentService PerfectMoneyPaymentService)
        {
            _PerfectMoneyPaymentService = PerfectMoneyPaymentService;
        }

        [HttpPost("request-a-payment")]
        public async Task<ApiResponseBaseModel<GeneralPaymentResponse>> RequestAPayment([FromBody]GeneralPaymentRequest PerfectMoneyPaymentRequest)
        {
            return await _PerfectMoneyPaymentService.RequestPerfectMoneyPayment(PerfectMoneyPaymentRequest);
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PerfectMoneyController : ControllerBase
    {
        private readonly IPerfectMoneyPaymentService _PerfectMoneyPaymentService;
        public PerfectMoneyController(IPerfectMoneyPaymentService PerfectMoneyPaymentService)
        {
            _PerfectMoneyPaymentService = PerfectMoneyPaymentService;
        }

        [HttpPost("ReturnCallback")]
        public async Task<ApiResponseBaseModel> ReturnCallback([FromForm]PerfectMoneyNotifyReturnRawModel returnModel)
        {
            var model = new PerfectMoneyNotifyReturnModel()
            {
                PayeeAccount = returnModel.PAYEE_ACCOUNT,
                PaymentAmount = returnModel.PAYMENT_AMOUNT,
                PaymentBatchNum = returnModel.PAYMENT_BATCH_NUM,
                PaymentId = returnModel.PAYMENT_ID,
                PayerAccount = returnModel.PAYER_ACCOUNT,
                PaymentUnits = returnModel.PAYMENT_UNITS,
                Timestamp = returnModel.TIMESTAMPGMT,
                V2Hash = returnModel.V2_HASH,
            };
            await _PerfectMoneyPaymentService.ProcessPerfectMoneyCallback(model);
            return new ApiResponseBaseModel()
            {
                Success = true
            };
        }
    }

}
