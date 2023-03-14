using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IPaymentMethodConfigurationService : IServiceBase<PaymentMethodConfiguration>
    {
    }
    public class PaymentMethodConfigurationService : ServiceBase<PaymentMethodConfiguration>, IPaymentMethodConfigurationService
    {
        public PaymentMethodConfigurationService(SmsDataContext smsDataContext) : base(smsDataContext)
        {
        }

        public override void Map(PaymentMethodConfiguration entity, PaymentMethodConfiguration model)
        {
            entity.BankAccount = model.BankAccount;
            entity.BankName = model.BankName;
            entity.IsDisabled = model.IsDisabled;
            entity.MessageFromAdmin = model.MessageFromAdmin;
            entity.Name = model.Name;
            entity.OwnerName = model.OwnerName;
            entity.Sender = model.Sender;
        }

    }
}
