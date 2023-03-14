using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
  public abstract class Order : BaseEntity
  {
    public OrderType OrderType { get; set; }
    public DateTime? Expired { get; set; }
    public decimal Price { get; set; }
    public OrderStatus Status { get; set; }
    public int UserId { get; set; }
    public int LockTime { get; set; }
    [MaxLength(15)]
    public string PhoneNumber { get; set; }
    public string RequestPhoneNumber { get; set; }
    [ForeignKey("UserId")]
    public User User { get; set; }
    public int? MaximunSms { get; set; }
    public int? RemainingSms { get; set; }
    public AppSourceType AppSourceType { get; set; }
    public List<OrderResult> OrderResults { get; set; }

    [NotMapped]
    public int ResultsCount { get; set; }
    [NotMapped]
    public bool AlreadyComplain { get; set; }
    public bool PendingReferalCalculate { get; set; }
    public string ProposedPhoneNumber { get; set; }
    public bool NeedProposedProcessing { get; set; }
  }
  public class RentCodeOrder : Order
  {
    public int ServiceProviderId { get; set; }
    public int? NetworkProvider { get; set; }
    [ForeignKey("ServiceProviderId")]
    public ServiceProvider ServiceProvider { get; set; }
    public int? ConnectedGsmId { get; set; }
    public decimal GsmDeviceProfit { get; set; }
    public int? NotMatchServiceId { get; set; }
    public string ProposedGsm512RangeName { get; set; }
    // There are some service that allow a phone number can get more than one OTP
    // We use this flag to say that the user would like to take only the first OTP
    public bool OnlyAcceptFreshOtp { get; set; }
    public bool AllowVoiceSms { get; set; }
    public decimal? VoiceSmsPrice { get; set; }
  }

  public class InternationalSimOrder : Order
  {
    public int SimCountryId { get; set; }

    [ForeignKey("SimCountryId")]
    public SimCountry SimCountry { get; set; }

    public int? ConnectedForwarderId { get; set; }

  }
}
