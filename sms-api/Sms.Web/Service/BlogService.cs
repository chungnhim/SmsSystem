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
    public interface IBlogService : IServiceBase<Blog>
    {
        Task<List<Blog>> getAllAvailableBlogs();
    }
    public class BlogService : ServiceBase<Blog>, IBlogService
    {
        public BlogService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(Blog entity, Blog model)
        {
            entity.Content = model.Content;
            entity.Title = model.Title;
            entity.IsDisabled = model.IsDisabled;
        }
        protected override IQueryable<Blog> GenerateQuery(FilterRequest filterRequest = null)
        {
            var query = base.GenerateQuery(filterRequest);
            query = query.OrderBy(r => r.Priority).ThenByDescending(r => r.Created);
            return query;
        }

        public async Task<List<Blog>> getAllAvailableBlogs()
        {
            var list = await _smsDataContext.Blogs.Where(r => r.IsDisabled != true).OrderByDescending(r => r.Created).ToListAsync();

            foreach (var item in list)
            {
                item.Content = null;
            }

            return list;
        }
    }
}
