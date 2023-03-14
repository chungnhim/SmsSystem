using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sms.Web.Entity;
using Sms.Web.Models;
using Sms.Web.Service;
using System.Threading;
namespace Sms.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    public class ClientTransferMoneyController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserTransactionService _userTransactionService;
        private readonly IPortalService _portalService;
        public ClientTransferMoneyController(IUserService userService, IUserTransactionService userTransactionService, IPortalService portalService)
        {
            _userService = userService;
            _userTransactionService = userTransactionService;
            _portalService = portalService;
        }
        [HttpGet("search-user")]
        public async Task<List<User>> SearchUser(string username)
        {
            return await _userService.SearchUser(username);
        }
        [HttpGet("check-user")]
        public async Task<ApiResponseBaseModel<User>> CheckUser(string username, string portalName)
        {
            User user = null;
            if (string.IsNullOrEmpty(portalName))
            {
                user = await _userService.CheckUser(username);
            }
            if (portalName == "Tkao")
            {
                var u = await _portalService.CheckTkaoUsername(username);
                if (u != null)
                {
                    user = new User()
                    {
                        Id = u.UserId,
                        Username = u.Username,
                        Role = u.Role,
                        UserProfile = new UserProfile()
                        {
                            Name = $"{u.Username} (taikhoanao.vn)"
                        }
                    };
                }
            }
            return new ApiResponseBaseModel<User>()
            {
                Results = user,
                Success = user != null
            };
        }

        [HttpPost("transfer-money")]
        public async Task<ApiResponseBaseModel<decimal>> TransferMoney([FromBody] ExternalTransferMoneyRequest request)
        {
            if (string.IsNullOrEmpty(request.PortalName))
            {
                return await _userTransactionService.TransferMoney(request);
            }
            if (request.PortalName == "Tkao")
            {
                return await _portalService.TransferMoneyToTkao(request);
            }
            throw new NotImplementedException();
        }
    }
}
