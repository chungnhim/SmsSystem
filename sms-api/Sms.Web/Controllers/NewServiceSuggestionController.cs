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
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NewServiceSuggestionController : BaseRestfulController<INewServiceSuggestionService, NewServiceSuggestion>
    {
        public NewServiceSuggestionController(INewServiceSuggestionService NewServiceSuggestionService) : base(NewServiceSuggestionService)
        {
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<NewServiceSuggestion>> Post([FromBody] NewServiceSuggestion value)
        {
            return base.Post(value);
        }
    }
}
