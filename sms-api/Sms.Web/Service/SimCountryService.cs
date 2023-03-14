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
  public interface ISimCountryService : IServiceBase<SimCountry>
  {
    Task<List<SimCountry>> GetAllAvailableSimCountries();
  }
  public class SimCountryService : ServiceBase<SimCountry>, ISimCountryService
  {
    public SimCountryService(SmsDataContext smsDataContext) : base(smsDataContext)
    {
    }

    public override void Map(SimCountry entity, SimCountry model)
    {
      entity.PhonePrefix = model.PhonePrefix;
      entity.Price = model.Price;
      entity.CountryCode = model.CountryCode;
      entity.CountryName = model.CountryName;
      entity.IsDisabled = model.IsDisabled;
    }
    protected override IQueryable<SimCountry> GenerateQuery(FilterRequest filterRequest = null)
    {
      var query = base.GenerateQuery(filterRequest);
      if (filterRequest != null)
      {
        {
          if (filterRequest.SearchObject.TryGetValue("CountryName", out object obj))
          {
            var str = obj.ToString().ToLower();
            if (!string.IsNullOrEmpty(str))
            {
              query = query.Where(r => r.CountryName.Contains(str) || r.CountryCode.Contains(str));
            }
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
      }
      return query;
    }

    public async Task<List<SimCountry>> GetAllAvailableSimCountries()
    {
      return await this.GenerateQuery().Where(r => !r.IsDisabled).ToListAsync();
    }

    protected override async Task<string> ValidateEntry(SimCountry entity)
    {
      var duplicateCountryCode = await _smsDataContext.SimCountries.AnyAsync(r => r.CountryCode == entity.CountryCode && r.Id != entity.Id);
      if (duplicateCountryCode)
      {
        return "DuplicateCountryCode";
      }
      return await base.ValidateEntry(entity);
    }
  }
}
