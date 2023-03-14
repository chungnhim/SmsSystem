using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using Sms.Web.Service;

namespace Sms.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    public class ServiceProviderController : BaseRestfulController<IServiceProviderService, ServiceProvider>
    {
        public ServiceProviderController(IServiceProviderService ServiceProviderService) : base(ServiceProviderService)
        {
        }
        [HttpPost("apply-user-error-for-all")]
        public async Task<ApiResponseBaseModel> ApplyUserErrorForAll([FromBody]ApplyUserErrorRequest request)
        {
            var n = request.Count;
            var isSingle = request.IsSingle;
            if (isSingle)
            {
                return await _service.ApplySingleErrorForAll(n);
            }
            return await _service.ApplyUserErrorForAll(n);
        }
        [HttpPost("test-matched-services")]
        public async Task<ApiResponseBaseModel<List<ServiceProviderMatchingTokens>>> TestMatchedServices([FromBody] TestMatchingSmsRequest request)
        {
            var message = request.Message;
            var sender = request.Sender;
            return await _service.TestAllMatchedService(message, sender);
        }
    }
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Staff")]
    public class StaffServiceProviderController : ControllerBase
    {
        private readonly IServiceProviderService _serviceProviderService;
        public StaffServiceProviderController(IServiceProviderService serviceProviderService)
        {
            _serviceProviderService = serviceProviderService;
        }
        [HttpGet]
        public async Task<List<ServiceProvider>> GetAllAvailableServices(int? serviceType)
        {
            return await _serviceProviderService.GetAlls();
        }
    }
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ClientServiceProviderController : ControllerBase
    {
        private readonly IServiceProviderService _serviceProviderService;
        private readonly IStatisticService _statisticService;
        public ClientServiceProviderController(IServiceProviderService serviceProviderService, IStatisticService statisticService)
        {
            _serviceProviderService = serviceProviderService;
            _statisticService = statisticService;
        }
        [HttpGet("available-services")]
        public async Task<List<ServiceProviderWithAvailableCount>> GetAllAvailableServices(int? serviceType)
        {
            var services = await _serviceProviderService.GetAllAvailableServices((ServiceType?)serviceType);
            var reports = await _statisticService.ServiceAvailableReport();
            var servicesWithAvailable = services.Select(r => new ServiceProviderWithAvailableCount(r)
            {
                AvailableCount = (r.ServiceType == ServiceType.Basic || r.ServiceType == ServiceType.Any) ?
                (reports.Results.FirstOrDefault(x => x.ServiceProviderId == r.Id)?.AvailableCount ?? 0) : -1
            }).ToList();
            return servicesWithAvailable;
        }
    }
}
