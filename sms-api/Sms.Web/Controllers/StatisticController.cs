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

namespace Sms.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Staff,User")]
    public class StatisticController : ControllerBase
    {
        private readonly IStatisticService _statisticService;
        private readonly IUserService _userService;
        public StatisticController(IStatisticService statisticService, IUserService userService)
        {
            _statisticService = statisticService;
            _userService = userService;
        }
        [HttpPost("daily-report")]
        public async Task<ApiResponseBaseModel<List<DailyReport>>> GenerateReportDaily([FromBody]StatisticRequest request)
        {
            if (request.ClientTimeZone == null) request.ClientTimeZone = 7;
            var id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _userService.Get(id);
            if (user.Role == Helpers.RoleType.Administrator)
            {
                id = 0;
            }
            else if (user.Role == Helpers.RoleType.Staff)
            {
                return await GenerateGsmReport(request);
            }
            return await _statisticService.GenerateDashboard(id);
        }
        private async Task<ApiResponseBaseModel<List<DailyReport>>> GenerateGsmReport([FromBody]StatisticRequest request)
        {
            return await _statisticService.GenerateGsmReport(request);
        }
        [HttpPost("gsm-performance-reports")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<ApiResponseBaseModel<List<GsmReportModel>>> GsmPerformanceReport([FromBody]GsmPerformanceReportRequest request)
        {
            return await _statisticService.GsmPerformanceReport(request);
        }
        [HttpPost("service-available-reports")]
        [Authorize(Roles = "Administrator")]
        public async Task<ApiResponseBaseModel<List<ServiceAvailableReportModel>>> ServiceAvailableReport()
        {
            return await _statisticService.ServiceAvailableReport();
        }
    }
}
