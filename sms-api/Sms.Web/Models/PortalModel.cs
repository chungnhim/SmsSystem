using Sms.Web.Helpers;

namespace Sms.Web.Models
{
  public class PortalUser
  {
    public int UserId { get; set; }
    public string Username { get; set; }
    public RoleType Role { get; set; }
  }

  public class PortalTransferMoneyRequest
  {
    public int ReceiverId { get; set; }
    public decimal Money { get; set; }
    public string Sender { get; set; }
  }

  public class PortalConnection
  {
    public string PortalEndPoint { get; set; }
    public string PortalKey { get; set; }
  }
}