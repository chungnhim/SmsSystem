using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface INewServiceSuggestionService : IServiceBase<NewServiceSuggestion>
    {
    }
    public class NewServiceSuggestionService : ServiceBase<NewServiceSuggestion>, INewServiceSuggestionService
    {
        public NewServiceSuggestionService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(NewServiceSuggestion entity, NewServiceSuggestion model)
        {
            // Don't alow patch this entity
        }
        protected override IQueryable<NewServiceSuggestion> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.Include(r => r.User);
            if (filterRequest != null)
            {
                {
                    if (filterRequest.SearchObject.TryGetValue("SearchText", out object obj))
                    {
                        var searchText = (string)obj;
                        searchText = searchText.ToLower();
                        query = query.Where(r => r.Sender.ToLower().Contains(searchText)
                        || r.Name.ToLower().Contains(searchText)
                        || r.Description.ToLower().Contains(searchText));
                    }
                }
            }
            return query;
        }
    }
}
