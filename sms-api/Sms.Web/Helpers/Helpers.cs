using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sms.Web.Models;

namespace Sms.Web.Helpers
{
    public static class Helpers
    {
        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string GenerateApiKey()
        {
            var key = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
                generator.GetBytes(key);
            var stringKey = Convert.ToBase64String(key);
            stringKey = Regex.Replace(stringKey, @"[^0-9a-zA-Z]+", "v");
            return stringKey;
        }
    }

    public class RandomStringGenerator
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public static class MessageMatchingHelpers
    {
        public static bool CheckMessageIsMatchWithServiceProviderPattern(string pattern, string message, string sender)
        {
            var token = BuildSmsMatchingTokens(pattern, message, sender);
            return token.SenderTokens.Any() || token.ContentTokens.Any();
        }

        public static SmsMatchingTokens BuildSmsMatchingTokens(string pattern, string message, string sender)
        {
            if (string.IsNullOrEmpty(message)) return new SmsMatchingTokens();
            if (string.IsNullOrEmpty(pattern)) return new SmsMatchingTokens();
            var result = new SmsMatchingTokens();
            pattern = pattern.ToLower();
            var arrs = pattern.Split("|");
            pattern = arrs[0];
            result.ContentTokens = MatchedContentTokens(pattern, message);
            var senderPattern = "";
            if (arrs.Length > 1)
            {
                senderPattern = arrs[1];
            }
            result.SenderTokens = MatchedSenderTokens(senderPattern, sender);
            return result;
        }

        public static List<string> MatchedContentTokens(string pattern, string message)
        {
            var tokens = pattern.Split(",").Select(r => r.Trim()).Where(r => !string.IsNullOrEmpty(r)).ToList();
            message = message.ToLower();
            return tokens.Where(r => message.Contains(r)).ToList();
        }

        public static List<string> MatchedSenderTokens(string senderPattern, string sender)
        {
            return senderPattern.Split(",")
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .Where(r => sender.ToLower() == r).ToList();
        }
    }

    public class AsyncLocker
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> SemaphoreSlims = new ConcurrentDictionary<string, SemaphoreSlim>();

        public SemaphoreSlim GetOrCreateSlim(string key, int threshold)
        {
            if (SemaphoreSlims.TryGetValue(key, out var obj))
            {
                return obj;
            }
            var slim = new SemaphoreSlim(threshold);
            SemaphoreSlims.TryAdd(key, slim);
            return slim;
        }

        public SemaphoreSlim DedicatedPutSmsLocker(int userId)
        {
            return GetOrCreateSlim($"PUT_SMS_{userId}", 1);
        }
    }
}
