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
    public class PaymentMethodConfigurationController : BaseRestfulController<IPaymentMethodConfigurationService, PaymentMethodConfiguration>
    {
        public PaymentMethodConfigurationController(IPaymentMethodConfigurationService PaymentMethodConfigurationService) : base(PaymentMethodConfigurationService)
        {
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User,Administrator,Staff")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ClientPaymentMethodConfigurationController
    {
        private IPaymentMethodConfigurationService _paymentMethodConfigurationService;
        public ClientPaymentMethodConfigurationController(IPaymentMethodConfigurationService PaymentMethodConfigurationService)
        {
            _paymentMethodConfigurationService = PaymentMethodConfigurationService;
        }
        [HttpGet("available-payment-methods")]
        public async Task<List<PaymentMethodConfiguration>> GetAvailablePaymentMethods()
        {
            var all = await _paymentMethodConfigurationService.GetAlls();
            return all.Where(r => r.IsDisabled != true).ToList();
        }
    }

}
