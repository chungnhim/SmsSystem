using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
  public class InternationalSim : BaseEntity
  {
    [Required]
    public int SimCountryId { get; set; }
    [Required]
    public string PhoneNumber { get; set; }
    public int ForwarderId { get; set; }
    public bool IsDisabled { get; set; }
    [ForeignKey("SimCountryId")]
    public SimCountry SimCountry { get; set; }
    [ForeignKey("ForwarderId")]
    public User Forwarder { get; set; }
  }
}
