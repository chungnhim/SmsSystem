using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IRecaptcharService
    {
        bool Validate(string encodedResponse);
    }
    public class RecaptcharService : IRecaptcharService
    {
        private readonly AppSettings _appSettings;
        public RecaptcharService(IOptions<AppSettings> appSettings)
        {
            this._appSettings = appSettings.Value;
        }
        public bool Validate(string encodedResponse)
        {
            //return true;
            if (string.IsNullOrEmpty(encodedResponse)) return false;

            var secret = this._appSettings.ReCaptcharSecretKey;
            if (string.IsNullOrEmpty(secret)) return false;

            var client = new System.Net.WebClient();
            var googleReply = client.DownloadString(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={encodedResponse}");

            return JsonConvert.DeserializeObject<RecaptchaResponse>(googleReply).Success;
        }
    }
}
