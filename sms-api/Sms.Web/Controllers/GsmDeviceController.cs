using System;
using System.Collections.Generic;
using System.Linq;
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
    [Authorize(Roles = "Administrator,Staff")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class GsmDeviceController : BaseRestfulController<IGsmDeviceService, GsmDevice>
    {
        public GsmDeviceController(IGsmDeviceService gsmDeviceService) : base(gsmDeviceService)
        {
        }

        [HttpPost("{gsmId}/reset-specified-services")]
        public async Task<ApiResponseBaseModel> ResetSpecifiedServices(int gsmId)
        {
            return await _service.ResetSpecifiedServices(gsmId);
        }

        [HttpPost("{gsmId}/turn-on-serving-third-service")]
        [Authorize(Roles = "Administrator")]
        public async Task<ApiResponseBaseModel> TurnOnServingThirdService(int gsmId)
        {
            return await _service.ToggleServingThirdService(gsmId, true);
        }
        [HttpPost("{gsmId}/turn-off-serving-third-service")]
        [Authorize(Roles = "Administrator")]
        public async Task<ApiResponseBaseModel> TurnOffServingThirdService(int gsmId)
        {
            return await _service.ToggleServingThirdService(gsmId, false);
        }

        [HttpGet("get-alls")]
        public async Task<List<GsmDevice>> GetAlls()
        {
            return await _service.GetAlls();
        }
        [HttpPost("{gsmId}/toggle-maintenance-status")]
        public async Task<ApiResponseBaseModel> ToggleMaintenanceStatus(int gsmId, bool status)
        {
            return await _service.ToggleMaintenanceStatus(gsmId, status);
        }
        [HttpGet("{gsmId}/count-active-order")]
        public async Task<ApiResponseBaseModel<int>> CountActiveOrder(int gsmId)
        {
            return await _service.CountActiveOrder(gsmId);
        }
        [HttpPost("{gsmId}/priority")]
        [Authorize(Roles = "Administrator")]
        public async Task<ApiResponseBaseModel> UpdateGsmDevicePriority(int gsmId, int priority)
        {
            return await _service.UpdateGsmDevicePriority(gsmId, priority);
        }
        [HttpPost("specified-services")]
        public async Task<ApiResponseBaseModel> SpecifiedServices([FromBody]SpecifiedServiceForGsmRequest request)
        {
            var gsmIds = (request.ApplyFor ?? new List<int>()).ToList();
            return await _service.SpecifiedServices(gsmIds, request);
        }
        [HttpPost("{gsmId}/toggle-web-only")]
        public async Task<ApiResponseBaseModel> ToggleWebOnly(int gsmId, bool webOnly)
        {
            return await _service.ToggleWebOnly(gsmId, webOnly);
        }
    }
}
