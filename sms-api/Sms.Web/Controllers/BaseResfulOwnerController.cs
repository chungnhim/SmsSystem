using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
  public abstract class BaseRestfulOwnerController<TService, TEntity> : BaseRestfulController<TService, TEntity> where TService : IServiceBase<TEntity> where TEntity : BaseEntity
  {
    protected readonly IUserService _userService;
    public BaseRestfulOwnerController(TService service, IUserService userService) : base(service)
    {
      _userService = userService;
    }

    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override async Task<IEnumerable<TEntity>> Get()
    {
      // do not allow get all in owner controller
      return null;
    }

    /// <summary>
    /// For pagination
    /// </summary>
    /// <param name="pageIndex">Start with zero</param>
    /// <param name="pageSize">Size of page (default is 20)</param>
    /// <returns></returns>
    public override async Task<FilterResponse<TEntity>> Paging([FromBody]FilterRequest filterRequest)
    {
      var currentUser = await _userService.GetCurrentUser();
      if (currentUser == null)
      {
        throw new Exception("Owner resource does not support anonymous user");
      }
      if (currentUser.Role != RoleType.Administrator)
      {
        filterRequest.SearchObject = filterRequest.SearchObject ?? new Dictionary<string, object>();
        filterRequest.SearchObject.Add("ResourceOwnerId", currentUser.Id);
      }

      return await _service.Paging(filterRequest);
    }

    public override async Task<TEntity> Get(int id)
    {
      var currentUser = await _userService.GetCurrentUser();
      if (currentUser == null)
      {
        return null;
      }
      var resource = await _service.Get(id);

      if (currentUser.Role != RoleType.Administrator)
      {
        if (!CheckOwnership(resource, currentUser.Id)) return null;
      }

      return resource;
    }

    public override async Task<ApiResponseBaseModel<TEntity>> Post([FromBody] TEntity value)
    {
      var currentUser = await _userService.GetCurrentUser();
      if (currentUser == null)
      {
        return ApiResponseBaseModel<TEntity>.UnAuthorizedResponse();
      }
      SetOwnership(value, currentUser.Id);
      return await _service.Create(value);
    }

    public override async Task<ApiResponseBaseModel<TEntity>> Patch(int id, [FromBody] JsonPatchDocument<TEntity> patchDoc)
    {
      if (patchDoc == null) throw new Exception("patch doc null");
      var resource = await _service.Get(id);
      var currentUser = await _userService.GetCurrentUser();
      if (currentUser == null)
      {
        return ApiResponseBaseModel<TEntity>.UnAuthorizedResponse();
      }
      if (currentUser.Role != RoleType.Administrator)
      {
        if (!CheckOwnership(resource, currentUser.Id))
        {
          return ApiResponseBaseModel<TEntity>.UnAuthorizedResponse();
        }
      }
      var entity = JsonConvert.DeserializeObject<TEntity>(JsonConvert.SerializeObject(resource, Formatting.None, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
      patchDoc.ApplyTo(entity);
      return await _service.Update(entity);
    }

    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override async Task<ApiResponseBaseModel<TEntity>> Put(int id, [FromBody] TEntity value)
    {
      value.Id = id;
      return await _service.Update(value);
    }

    [HttpDelete("{id}")]
    public override async Task<ApiResponseBaseModel<int>> Delete(int id)
    {
      var resource = await _service.Get(id);
      var currentUser = await _userService.GetCurrentUser();
      if (currentUser == null)
      {
        return ApiResponseBaseModel<int>.UnAuthorizedResponse();
      }
      if (currentUser.Role != RoleType.Administrator)
      {
        if (!CheckOwnership(resource, currentUser.Id))
        {
          return ApiResponseBaseModel<int>.UnAuthorizedResponse();
        }
      }
      return await _service.Delete(id);
    }

    protected abstract void SetOwnership(TEntity entity, int ownerId);
    protected abstract bool CheckOwnership(TEntity entity, int ownerId);
  }
}
