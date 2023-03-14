using Newtonsoft.Json;
using Sms.Web.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Entity
{
    public class User : BaseEntity
    {
        public string Username { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
        public RoleType Role { get; set; }
        public string ApiKey { get; set; }
        public decimal Ballance { get; set; }
        public bool IsBanned { get; set; }
        public UserProfile UserProfile { get; set; }
        public int? ReferalId { get; set; }
        [MaxLength(6)]
        public string ReferalCode { get; set; }
        public bool ReferEnabled { get; set; }
        [JsonIgnore]
        public virtual ICollection<UserToken> UserTokens { get; set; }
        public virtual ICollection<UserGsmDevice> UserGsmDevices { get; set; }
    }
}
