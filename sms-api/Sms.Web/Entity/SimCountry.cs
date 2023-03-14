using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
  public class SimCountry : BaseEntity
  {
    [Required]
    public string CountryName { get; set; }
    [Required]
    public string CountryCode { get; set; }
    [Required]
    public string PhonePrefix { get; set; }
    public decimal Price { get; set; }

    public bool IsDisabled { get; set; }
    public int LockTime { get; set; }
  }
}
