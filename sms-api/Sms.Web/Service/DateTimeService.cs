using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IDateTimeService
    {
        DateTime UtcNow();
        DateTime GMT7Now();
    }

    public class DateTimeService : IDateTimeService
    {
        public DateTime GMT7Now()
        {
            return DateTime.UtcNow.AddHours(7);
        }
        
        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}
