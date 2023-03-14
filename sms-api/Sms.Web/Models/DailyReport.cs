using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sms.Web.Helpers;

namespace Sms.Web.Models
{
    public class StatisticRequest
    {
        public double? ClientTimeZone { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<int> GsmDeviceIds { get; set; }
        public List<int> ServiceProviderIds { get; set; }
    }
    public class GsmReportRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<int> GsmDeviceIds { get; set; }
        public List<int> ServiceProviderIds { get; set; }
    }
    public class GsmPerformanceReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StaffName { get; set; }
        public List<int> GsmDeviveIds { get; set; }
    }
    public class GsmReportModel
    {
        public int FinishedCount { get; set; }
        public int ErrorCount { get; set; }
        public int GsmId { get; set; }
        public int ServiceProviderId { get; set; }
        public decimal Profit { get; set; }
    }
    public class ServiceAvailableReportModel
    {
        public int ServiceProviderId { get; set; }
        public int ErrorCount { get; set; }
        public int UsedCount { get; set; }
        public int AvailableCount { get; set; }
        public int WaitingCount { get; set; }
        public int TotalCount { get; set; }
        [JsonIgnore]
        public ServiceType ServiceType { get; set; }
        [JsonIgnore]
        public int ReceivingThreshold { get; set; }
    }
}
