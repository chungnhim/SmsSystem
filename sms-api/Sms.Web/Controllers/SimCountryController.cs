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
  [Authorize(Roles = "Administrator")]
  public class SimCountryController : BaseRestfulController<ISimCountryService, SimCountry>
  {
    public SimCountryController(ISimCountryService simCountryService) : base(simCountryService)
    {
    }
    [Obsolete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public override Task<ApiResponseBaseModel<int>> Delete(int id)
    {
      return base.Delete(id);
    }
  }
  [Route("api/[controller]")]
  [ApiController]
  [Authorize(Roles = "Administrator,Forwarder,User")]
  public class PublicSimCountryController : ControllerBase
  {
    private readonly ISimCountryService _simCountryService;
    public PublicSimCountryController(ISimCountryService simCountryService)
    {
      _simCountryService = simCountryService;
    }
    [HttpGet]
    public async Task<List<SimCountry>> GetPublicSimCountries()
    {
      return await _simCountryService.GetAllAvailableSimCountries();
    }
  }
}
