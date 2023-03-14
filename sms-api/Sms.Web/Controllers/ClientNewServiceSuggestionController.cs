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
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ClientNewServiceSuggestionController : ControllerBase
    {
        private readonly INewServiceSuggestionService _newServiceSuggestionService;
        private readonly IAuthService _authService;
        public ClientNewServiceSuggestionController(INewServiceSuggestionService NewServiceSuggestionService, IAuthService authService)
        {
            _newServiceSuggestionService = NewServiceSuggestionService;
            _authService = authService;
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<ApiResponseBaseModel<NewServiceSuggestion>> Post([FromBody] NewServiceSuggestion value)
        {
            value.UserId = _authService.CurrentUserId().GetValueOrDefault();
            return await _newServiceSuggestionService.Create(value);
        }
    }
}
