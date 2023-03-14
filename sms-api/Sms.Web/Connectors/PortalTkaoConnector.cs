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
  public interface IPortalTkaoConnector
  {
    Task<PortalUser> CheckUsername(string username);
    Task<ApiResponseBaseModel> SendMoneyToUser(string sender, int toUserId, decimal amount);
  }
  public class PortalTkaoConnector : PortalBaseConnector, IPortalTkaoConnector
  {
    private readonly AppSettings _appSettings;
    public PortalTkaoConnector(IOptions<AppSettings> appSettings) : base(appSettings)
    {
    }

    public async Task<PortalUser> CheckUsername(string username)
    {
      return await WithPortalConnector("Tkao", async connector =>
       {
         return await connector.GetFromPortal<PortalUser>(
           "/api/portal/check-user",
            new Dictionary<string, string>() {
              {"username", username}
            }
          );
       });
    }

    public async Task<ApiResponseBaseModel> SendMoneyToUser(string sender, int toUserId, decimal amount)
    {
      return await WithPortalConnector("Tkao", async connector =>
      {
        return await connector.PostToPortal<ApiResponseBaseModel, PortalTransferMoneyRequest>(
          "/api/portal/transfer-money",
          new PortalTransferMoneyRequest()
          {
            Money = amount,
            ReceiverId = toUserId,
            Sender = sender
          }
        );
      });
    }
  }
}
