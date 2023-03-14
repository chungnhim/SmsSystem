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
    public class OrderComplaintController : BaseRestfulController<IOrderComplaintService, OrderComplaint>
    {
        public OrderComplaintController(IOrderComplaintService OrderComplaintService) : base(OrderComplaintService)
        {
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<OrderComplaint>> Post([FromBody] OrderComplaint value)
        {
            return base.Post(value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<OrderComplaint>> Put(int id, [FromBody] OrderComplaint value)
        {
            return base.Put(id, value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<IEnumerable<OrderComplaint>> Get()
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


    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ClientOrderComplaintController
    {
        private readonly IOrderComplaintService _orderComplaintService;
        private readonly IAuthService _authService;
        public ClientOrderComplaintController(IOrderComplaintService orderComplaintService, IAuthService authService)
        {
            _orderComplaintService = orderComplaintService;
            _authService = authService;
        }
        [HttpPost("my-complaints")]
        public async Task<FilterResponse<OrderComplaint>> MyComplaints([FromBody]FilterRequest filterRequest)
        {
            var userId = _authService.CurrentUserId().GetValueOrDefault();
            if (userId == 0) return new FilterResponse<OrderComplaint>()
            {
                Total = 0,
                Results = new List<OrderComplaint>()
            };
            filterRequest.SearchObject.Add("UserId", userId);
            return await _orderComplaintService.Paging(filterRequest);
        }
    }
}
