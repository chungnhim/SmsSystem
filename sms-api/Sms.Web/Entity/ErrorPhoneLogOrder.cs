using System.ComponentModel.DataAnnotations.Schema;

namespace Sms.Web.Entity
{
    public class ErrorPhoneLogUser : BaseEntity
    {
        public int UserId { get; set; }
        public int ErrorPhoneLogId { get; set; }
        public bool IsIgnored { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("ErrorPhoneLogId")]
        public ErrorPhoneLog ErrorPhoneLog { get; set; }
    }
}
