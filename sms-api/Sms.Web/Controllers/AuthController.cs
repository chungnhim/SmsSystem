using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Sms.Web.Entity;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IForgotPasswordService _forgotPasswordService;
        private readonly IUserProfileService _userProfileService;
        private readonly IUserTokenService _userTokenService;
        public AuthController(IUserService userService, IUserProfileService userProfileService, IUserTokenService userTokenService, IForgotPasswordService forgotPasswordService)
        {
            this._userService = userService;
            _userProfileService = userProfileService;
            _userTokenService = userTokenService;
            _forgotPasswordService = forgotPasswordService;
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<LoginResult> Login(LoginRequest loginRequest)
        {
            return await _userService.Authenticate(loginRequest.Username, loginRequest.Password, loginRequest.Captchar, loginRequest.Totp);
        }
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<RegisterResponse> Register(RegisterRequest registerRequest)
        {
            var createResult = await _userService.CreateUser(registerRequest);
            if (!createResult.Success)
            {
                return createResult;
            }
            var loginResult = await _userService.Authenticate(registerRequest.Username, registerRequest.Password, null, null, true);
            createResult.Token = loginResult.Token;
            return createResult;
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ApiResponseBaseModel<string>> ForgotPassword([FromBody]ForgotPasswordRequest request)
        {
            return await _forgotPasswordService.ForgotPassword(request.Email, request.CallbackUrl);
        }
        [AllowAnonymous]
        [HttpGet("check-forgot-password")]
        public async Task<ApiResponseBaseModel<string>> CheckForgotPassword(string token)
        {
            return await _forgotPasswordService.CheckForgotPassword(token);
        }
        [AllowAnonymous]
        [HttpPost("submit-forgot-password")]
        public async Task<ApiResponseBaseModel<string>> SubmitForgotPassword([FromBody]SubmitForgotPasswordRequest request)
        {
            return await _forgotPasswordService.SubmitForgotPassword(request.ForgotPasswordToken, request.NewPassword);
        }

        [HttpPost("generate-api-key")]
        public async Task<GenerateApiKeyResponse> GenerateApiKey()
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return await _userService.GenerateNewApiKey(id);
        }

        [HttpGet("me")]
        public async Task<User> GetMe()
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _userService.Get(id);
            user.Password = "******";
            user.UserTokens = null;
            return user;
        }
        [HttpPatch("update-profile")]
        public async Task<ApiResponseBaseModel<UserProfile>> UpdateProfile([FromBody] JsonPatchDocument<UserProfile> patchDoc)
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            if (patchDoc == null) throw new Exception("patch doc null");
            var user = await _userService.GetUser(id);
            if (user == null) return new ApiResponseBaseModel<UserProfile>()
            {
                Success = false,
                Message = "UserNotFound"
            };
            var entity = user.UserProfile ?? new UserProfile();
            if (entity.Id != 0)
            {
                patchDoc.ApplyTo(entity);
                var updateResult = await _userProfileService.Update(entity);
                return updateResult;
            }
            else
            {
                entity = new UserProfile()
                {
                    UserId = id
                };
                patchDoc.ApplyTo(entity);
                entity.Id = 0;
                return await _userProfileService.Create(entity);
            }
        }
        [HttpPost("change-password")]
        public async Task<ApiResponseBaseModel<int>> ChangePassword([FromBody]ChangePasswordRequest request)
        {
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _userService.GetUser(id);
            if (user == null) return new ApiResponseBaseModel<int>()
            {
                Message = "NotFound",
                Success = false,
            };
            if(user.Password != request.CurrentPassword)
            {
                return new ApiResponseBaseModel<int>()
                {
                    Success = false,
                    Message = "WrongPassword"
                };
            }
            user.Password = request.Password;
            await _userService.Update(user);
            if(Guid.TryParse(User.FindFirst(ClaimTypes.Hash).Value, out Guid guid))
            {
                await _userTokenService.RemoveAllUserToken(id, guid);
            }
            return new ApiResponseBaseModel<int>()
            {
                Success = true
            };
        }
    }
}