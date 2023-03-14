using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "UsernameRequired")]
        public string Username { get; set; }
        [Required(ErrorMessage = "PasswordRequired")]
        [MinLength(6, ErrorMessage = "PasswordTooShort")]
        public string Password { get; set; }
        public bool? RememberMe { get; set; }
        //[Required()]
        public string Captchar { get; set; }
        public int? Totp { get; set; }
    }
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
    }

    public class RegisterRequest : RegisterNoCaptcharRequest
    {
        [Required()]
        public string Captchar { get; set; }
    }
    public class RegisterNoCaptcharRequest
    {
        [Required(ErrorMessage = "UsernameRequired")]
        public string Username { get; set; }
        [Required(ErrorMessage = "PasswordRequired")]
        [MinLength(6, ErrorMessage = "PasswordTooShort")]
        public string Password { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ReferalCode { get; set; }
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
    }

    public class GenerateApiKeyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ApiKey { get; set; }

    }
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "PasswordRequired")]
        [MinLength(6, ErrorMessage = "PasswordTooShort")]
        public string Password { get; set; }

        [Required]
        [MinLength(6)]
        public string CurrentPassword { get; set; }
    }

    public class ResetUserPasswordRequest
    {
        [Required]
        public int? UserId { get; set; }
    }
    public class ChangeUserStatusRequest
    {
        [Required]
        public int? UserId { get; set; }
        [Required]
        public bool? IsBanned { get; set; }
    }
    public class ChangeUserReferEnabled
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public bool ReferEnabled { get; set; }
    }
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Url]
        public string CallbackUrl { get; set; }
    }

    public class SubmitForgotPasswordRequest
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }

        public string ForgotPasswordToken { get; set; }

    }
}
