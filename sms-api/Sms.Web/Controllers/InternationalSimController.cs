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
  [Authorize(Roles = "Administrator,Forwarder")]
  public class InternationalSimController : BaseRestfulOwnerController<IInternationalSimService, InternationalSim>
  {
    public InternationalSimController(IInternationalSimService InternationalSimService, IUserService userService)
     : base(InternationalSimService, userService)
    {
    }

    protected override bool CheckOwnership(InternationalSim entity, int ownerId)
    {
      return entity.ForwarderId == ownerId;
    }

    protected override void SetOwnership(InternationalSim entity, int ownerId)
    {
      entity.ForwarderId = ownerId;
    }
  }
}
