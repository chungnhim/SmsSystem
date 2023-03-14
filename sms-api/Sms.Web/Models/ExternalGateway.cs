using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Sms.Web.Entity;
using Sms.Web.Helpers;

namespace Sms.Web.Models
{
    public class CreateOrderResponse: ApiResponseBaseModel
    {
        public CreateOrderResponse()
        {

        }
        public CreateOrderResponse(ApiResponseBaseModel<RentCodeOrder> apiResponse)
        {
            Success = apiResponse.Success;
            Message = apiResponse.Message;
            Id = apiResponse.Results?.Id;
        }
        public int? Id { get; set; }
    }
    public class CheckOrderResults: ApiResponseBaseModel
    {
        public string PhoneNumber { get; set; }
        public List<SmsMessage> Messages { get; set; }
    }
    public class SmsMessage
    {
        public string Message { get; set; }
        public string Sender { get; set; }
        public string AudioUrl { get; set; }
        public string MessageType { get; set; }
        public DateTime? Time { get; set; }
    }
}
