using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sms.Web.Entity;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
    public class BaseRestfulController<TService, TEntity> : ControllerBase where TService : IServiceBase<TEntity> where TEntity : BaseEntity
    {
        protected readonly TService _service;
        public BaseRestfulController(TService service)
        {
            _service = service;
        }

        [HttpGet]
        public virtual async Task<IEnumerable<TEntity>> Get()
        {
            return await _service.GetAlls();
        }

        /// <summary>
        /// For pagination
        /// </summary>
        /// <param name="pageIndex">Start with zero</param>
        /// <param name="pageSize">Size of page (default is 20)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("paging")]
        public virtual async Task<FilterResponse<TEntity>> Paging([FromBody]FilterRequest filterRequest)
        {
            return await _service.Paging(filterRequest);
        }

        // GET: api/BaseRestful/5
        [HttpGet("{id}")]
        public virtual async Task<TEntity> Get(int id)
        {
            return await _service.Get(id);
        }

        // POST: api/BaseRestful
        [HttpPost]
        public virtual async Task<ApiResponseBaseModel<TEntity>> Post([FromBody] TEntity value)
        {
            return await _service.Create(value);
        }

        // PATCH
        [HttpPatch("{id}")]
        public virtual async Task<ApiResponseBaseModel<TEntity>> Patch(int id, [FromBody] JsonPatchDocument<TEntity> patchDoc)
        {
            if (patchDoc == null) throw new Exception("patch doc null");
            var entity = JsonConvert.DeserializeObject<TEntity>(JsonConvert.SerializeObject(await _service.Get(id), Formatting.None, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
            patchDoc.ApplyTo(entity);
            return await _service.Update(entity);
        }

        // PUT: api/BaseRestful/5
        [HttpPut("{id}")]
        public virtual async Task<ApiResponseBaseModel<TEntity>> Put(int id, [FromBody] TEntity value)
        {
            value.Id = id;
            return await _service.Update(value);
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public virtual async Task<ApiResponseBaseModel<int>> Delete(int id)
        {
            return await _service.Delete(id);
        }
    }
}
