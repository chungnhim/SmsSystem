using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Helpers
{
    public class AppSettings
    {
        public string JwtSecret { get; set; }
        public SmtpConfiguration Smtp { get; set; }
        public string FrontEndUrl { get; set; }
        public string ReCaptcharSecretKey { get; set; }
        public bool? IsDevelopment { get; set; }
        public string PortalAuthenticationKey { get; set; }
        public AmazonS3Config S3Config { get; set; }
        public PortalConnections PortalConnections { get; set; }
    }
    public class SmtpConfiguration
    {
        public string Email { get; set; }
        public string Server { get; set; }
        public string Password { get; set; }
    }
    public class PortalConnections
    {
        public string TkaoEndpoint { get; set; }
        public string TkaoKey { get; set; }
    }
    public class AmazonS3Config
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
        public string SubFolder { get; set; }
    }
}
