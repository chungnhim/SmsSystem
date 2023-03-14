using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sms.Web.Models;

public static class PortalPayloadHelpers
{
  public static string GenerateKey(string portalKey)
  {
    var nowAsHHDDMMYYYY = DateTime.UtcNow.ToString("HHDDMMYYYY");
    return ComputeHmacSha256(nowAsHHDDMMYYYY, portalKey);
  }
  public static bool VerifyPortalPayload(string payload, string portalKey)
  {
    var nowAsHHDDMMYYYY = DateTime.UtcNow.ToString("HHDDMMYYYY");
    var isOkay = ComputeHmacSha256(nowAsHHDDMMYYYY, portalKey) == payload;
    if (isOkay) return isOkay;
    var lastHour = DateTime.UtcNow.AddHours(-1).ToString("HHDDMMYYYY");
    return ComputeHmacSha256(lastHour, portalKey) == payload;
  }
  public static string ComputeHmacSha256(string payload, string key)
  {
    var keyBytes = Encoding.UTF8.GetBytes(key);

    using (var hmac = new HMACSHA256(keyBytes))
    {
      // ComputeHash - returns byte array
      byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

      // Convert byte array to a string
      StringBuilder builder = new StringBuilder();
      for (int i = 0; i < bytes.Length; i++)
      {
        builder.Append(bytes[i].ToString("x2"));
      }
      return builder.ToString();
    }
  }

}