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
    public class BlogController : BaseRestfulController<IBlogService, Blog>
    {
        public BlogController(IBlogService blogService) : base(blogService)
        {
        }
    }
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    public class BlogClientController : ControllerBase
    {
        private readonly IBlogService _blogService;
        public BlogClientController(IBlogService blogService)
        {
            _blogService = blogService;
        }
        [HttpGet]
        public async Task<List<Blog>> GetAllBlogs()
        {
            return await _blogService.getAllAvailableBlogs();
        }
        [HttpGet("{id}")]
        public async Task<Blog> GetBlog(int id)
        {
            return await _blogService.Get(id);
        }
    }
}
