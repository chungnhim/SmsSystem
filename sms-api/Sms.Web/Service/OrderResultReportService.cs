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
  public interface IOrderResultReportService : IServiceBase<OrderResultReport>
  {
  }
  public class OrderResultReportService : ServiceBase<OrderResultReport>, IOrderResultReportService
  {
    public OrderResultReportService(SmsDataContext smsDataContext) : base(smsDataContext)
    {
    }
    public override void Map(OrderResultReport entity, OrderResultReport model)
    {
      entity.OrderResultReportStatus = model.OrderResultReportStatus;
    }
    protected override IQueryable<OrderResultReport> GenerateQuery(FilterRequest filterRequest = null)
    {
      var query = base.GenerateQuery(filterRequest);
      query = query.Include("OrderResult.SmsHistory").Include("OrderResult.Order.ServiceProvider");

      if (filterRequest != null)
      {
        var sortColumn = (filterRequest.SortColumnName ?? string.Empty).ToLower();
        var isAsc = filterRequest.IsAsc;
        DateTime? createdFrom = null;
        DateTime? createdTo = null;
        OrderResultReportStatus? status = null;
        List<int> serviceProviderIds = null;
        {
          if (filterRequest.SearchObject.TryGetValue("status", out object obj))
          {
            status = (OrderResultReportStatus)int.Parse(obj.ToString());
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("serviceProviderIds", out object obj))
          {
            serviceProviderIds = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>(); ;
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("createdFrom", out object obj))
          {
            createdFrom = (DateTime)obj;
          }
        }

        {
          if (filterRequest.SearchObject.TryGetValue("createdTo", out object obj))
          {
            createdTo = (DateTime)obj;
          }
        }

        if (status != null)
        {
          query = query.Where(x => x.OrderResultReportStatus == status);
        }
        if (createdTo != null)
        {
          createdTo = createdTo.GetValueOrDefault().AddDays(1);
          query = query.Where(x => x.Created < createdTo);
        }
        if (createdFrom != null)
        {
          query = query.Where(x => x.Created >= createdFrom);
        }
        switch (sortColumn)
        {
          case "created":
            query = isAsc ? query.OrderBy(x => x.Created) : query.OrderByDescending(x => x.Created);
            break;
          case "status":
            query = isAsc ? query.OrderBy(x => x.OrderResultReportStatus) : query.OrderByDescending(x => x.OrderResultReportStatus);
            break;
          default:
            break;
        }
      }

      return query;
    }
  }
}
