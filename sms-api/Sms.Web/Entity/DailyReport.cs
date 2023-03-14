using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
  public class DailyReport : BaseEntity
  {
    public int CreatedOrderCount { get; set; }
    public int FinishedOrderCount { get; set; }
    public int CanceledOrderCount { get; set; }
    public int ErrorOrderCount { get; set; }
    public decimal RechargedCredits { get; set; }
    public decimal SpentCredits { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public decimal ReferalFee { get; internal set; }
    public decimal ReferedFee { get; internal set; }
    public decimal AgentDiscount { get; internal set; }
    public decimal AgentCheckout { get; internal set; }
  }
}
