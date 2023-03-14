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
  public interface IInternationalSimService : IServiceBase<InternationalSim>
  {
  }
  public class InternationalSimService : ServiceBase<InternationalSim>, IInternationalSimService
  {
    public InternationalSimService(SmsDataContext smsDataContext) : base(smsDataContext)
    {
    }

    public override void Map(InternationalSim entity, InternationalSim model)
    {
      entity.PhoneNumber = model.PhoneNumber;
      entity.IsDisabled = model.IsDisabled;
      entity.SimCountryId = model.SimCountryId;
    }
    protected override IQueryable<InternationalSim> GenerateQuery(FilterRequest filterRequest = null)
    {
      var query = base.GenerateQuery(filterRequest);
      if (filterRequest != null)
      {
        {
          if (filterRequest.SearchObject.TryGetValue("PhoneNumber", out object obj))
          {
            var str = obj.ToString().ToLower();
            if (!string.IsNullOrEmpty(str))
            {
              query = query.Where(r => r.PhoneNumber.Contains(str));
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("Forwarder", out object obj))
          {
            var str = obj.ToString().ToLower();
            if (!string.IsNullOrEmpty(str))
            {
              query = query.Where(r => r.Forwarder.Username.Contains(str));
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("ForwarderId", out object obj))
          {
            var str = (int)obj;
            query = query.Where(r => r.ForwarderId == str);
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("ResourceOwnerId", out object obj))
          {
            var str = (int)obj;
            query = query.Where(r => r.ForwarderId == str);
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("Disabled", out object obj))
          {
            var str = (bool?)obj;
            if (str.HasValue)
            {
              query = query.Where(r => r.IsDisabled == str);
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("CountryName", out object obj))
          {
            var str = obj.ToString().ToLower();
            if (!string.IsNullOrEmpty(str))
            {
              query = query.Where(r => r.SimCountry.CountryName.Contains(str) || r.SimCountry.CountryCode.Contains(str));
            }
          }
        }
      }
      return query;
    }

    protected override async Task<string> ValidateEntry(InternationalSim entity)
    {
      var duplicateCountryCode = await _smsDataContext.InternationalSims
        .AnyAsync(r =>
        r.SimCountryId == entity.SimCountryId
        && r.PhoneNumber == entity.PhoneNumber
        && r.Id != entity.Id);
      if (duplicateCountryCode)
      {
        return "DuplicatePhoneNumber";
      }
      return await base.ValidateEntry(entity);
    }
  }
}
