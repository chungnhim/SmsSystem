using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Models
{
  public class RequestInternaltionSimOrderRequest
  {
    [Required]
    public int SimCountryId { get; set; }
    public int? MaximumSms { get; set; }
  }

    public class InternationalSimPutSmsRequest
    {
        public string Sender { get; set; }
        [Required(ErrorMessage = "ContentRequired")]
        public string Content { get; set; }
        [Required(ErrorMessage = "FromPhoneNumberRequired")]
        public string PhoneNumber { get; set; }
    }
}
