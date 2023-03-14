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
    public class CheckoutRequestController : BaseRestfulController<ICheckoutRequestService, CheckoutRequest>
    {
        public CheckoutRequestController(ICheckoutRequestService checkoutRequestService) : base(checkoutRequestService)
        {
        }

        [HttpPost("{id}/cancel-checkout")]
        public async Task<ApiResponseBaseModel<CheckoutRequest>> CancelCheckout(int id)
        {
            return await _service.CancelCheckout(id);
        }

        [HttpPost("{id}/approve-checkout")]
        public async Task<ApiResponseBaseModel<CheckoutRequest>> ApproveCheckout(int id)
        {
            return await _service.ApproveCheckout(id);
        }
        [HttpPost("{id}/report-error")]
        public async Task<ApiResponseBaseModel<CheckoutRequest>> ReportError(int id, [FromBody]CheckoutRequest checkoutRequest)
        {
            return await _service.ReportError(id, checkoutRequest.Comment);
        }
        [HttpPost("{id}/finish-checkout")]
        public async Task<ApiResponseBaseModel<CheckoutRequest>> FinishCheckout(int id)
        {
            return await _service.FinishCheckout(id);
        }
        public override async Task<ApiResponseBaseModel<CheckoutRequest>> Post([FromBody] CheckoutRequest value)
        {
            return await _service.RequestCheckout(value);
        }
    }
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Staff")]
    public class CheckoutRequestClientController : ControllerBase
    {
        private readonly ICheckoutRequestService _checkoutRequestService;
        private readonly IAuthService _authService;
        public CheckoutRequestClientController(ICheckoutRequestService checkoutRequestService, IAuthService authService)
        {
            _checkoutRequestService = checkoutRequestService;
            _authService = authService;
        }
        [HttpPost("request-checkout")]
        public async Task<ApiResponseBaseModel<CheckoutRequest>> RequestCheckout([FromBody]CheckoutRequest checkoutRequest)
        {
            checkoutRequest.UserId = _authService.CurrentUserId().Value;
            return await _checkoutRequestService.RequestCheckout(checkoutRequest);
        }
        [HttpPost("{id}/cancel-checkout")]
        public async Task<ApiResponseBaseModel<CheckoutRequest>> CancelCheckout(int id)
        {
            return await _checkoutRequestService.CancelCheckout(id);
        }
        [HttpPost]
        [Route("paging")]
        public virtual async Task<FilterResponse<CheckoutRequest>> Paging([FromBody]FilterRequest filterRequest)
        {
            var userId = _authService.CurrentUserId();
            filterRequest.SearchObject = filterRequest.SearchObject ?? new Dictionary<string, object>();
            filterRequest.SearchObject.Add("UserId", userId.GetValueOrDefault());
            return await _checkoutRequestService.Paging(filterRequest);
        }
    }
}
