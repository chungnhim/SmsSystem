using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using Sms.Web.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Forwarder")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ForwarderInternationalSimOrderController : BaseRestfulController<IInternationalSimOrderService, InternationalSimOrder>
    {
        private readonly IUserService _userService;
        private readonly ISmsHistoryService _smsHistoryService;
        public ForwarderInternationalSimOrderController(IInternationalSimOrderService internationalSimOrderService, IUserService userService, ISmsHistoryService smsHistoryService) 
            : base(internationalSimOrderService)
        {
            _userService = userService;
            _smsHistoryService = smsHistoryService;
        }
        public override async Task<FilterResponse<InternationalSimOrder>> Paging([FromBody] FilterRequest filterRequest)
        {
            var currentUser = await _userService.GetCurrentUser();
            if (currentUser == null) return new FilterResponse<InternationalSimOrder>()
            {
                Results = new List<InternationalSimOrder>()
            };

            if (currentUser.Role == RoleType.Forwarder)
            {
                filterRequest.SearchObject = filterRequest.SearchObject ?? new Dictionary<string, object>();
                filterRequest.SearchObject.Add("UserId", currentUser.Id);
                Newtonsoft.Json.Linq.JArray listStatusIds = new Newtonsoft.Json.Linq.JArray { OrderStatus.Waiting };
                filterRequest.SearchObject.Add("Status", listStatusIds);
            }
            return await base.Paging(filterRequest);
        }
        [HttpPost]
        [Route("put-sms")]
        public async Task<ApiResponseBaseModel<SmsHistory>> PutSms([FromBody] InternationalSimPutSmsRequest request)
        {
            return await _smsHistoryService.Create(new SmsHistory() { Content = request.Content, Sender = request.Sender, PhoneNumber = request.PhoneNumber});
        }
    }
}
