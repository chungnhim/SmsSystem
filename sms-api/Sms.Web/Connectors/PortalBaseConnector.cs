using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
  public interface IPortalBaseConnector
  {
  }
  public class PortalBaseConnector : IPortalBaseConnector
  {
    private readonly AppSettings _appSettings;
    public PortalBaseConnector(IOptions<AppSettings> appSettings)
    {
      _appSettings = appSettings.Value;
    }
    private PortalConnection GetPortalConnection(string portalName)
    {
      switch (portalName)
      {
        case "Tkao":
          return new PortalConnection()
          {
            PortalEndPoint = _appSettings.PortalConnections.TkaoEndpoint,
            PortalKey = _appSettings.PortalConnections.TkaoKey
          };
      }
      return null;
    }
    protected async Task<T> WithPortalConnector<T>(string portalName, Func<PortalConnector, Task<T>> func)
    {
      var connection = GetPortalConnection(portalName);
      using (var portalConnector = new PortalConnector(connection))
      {
        return await func(portalConnector);
      }
    }
  }
}
