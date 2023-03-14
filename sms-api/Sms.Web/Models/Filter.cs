using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Models
{
    public class FilterRequest
    {
        /// <summary>
        /// start with 0
        /// </summary>
        public int PageIndex { get; set; }
        /// <summary>
        /// Size of page
        /// </summary>
        public int PageSize { get; set; }

        public string SortColumnName { get; set; }
        public bool IsAsc { get; set; }
        public Dictionary<string, object> SearchObject { get; set; }
    }
    public class FilterResponse<T> where T : class
    {
        public int Total { get; set; }
        public List<T> Results { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }
}
