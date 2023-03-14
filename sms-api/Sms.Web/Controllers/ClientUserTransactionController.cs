using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
    [Authorize(Roles = "User,Staff")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ClientUserTransactionController : ControllerBase
    {
        private readonly IUserTransactionService _userTransactionService;
        private readonly IAuthService _authService;
        public ClientUserTransactionController(IUserTransactionService userTransactionService, IAuthService authService)
        {
            _userTransactionService = userTransactionService;
            _authService = authService;
        }

        [HttpPost("my-transactions")]

        public async Task<FilterResponse<UserTransaction>> PagingTransaction([FromBody]FilterRequest filterRequest)
        {
            filterRequest.SearchObject = filterRequest.SearchObject ?? new Dictionary<string, object>();
            filterRequest.SearchObject["UserId"] = _authService.CurrentUserId().GetValueOrDefault();
            return await _userTransactionService.Paging(filterRequest);
        }
    }
}
