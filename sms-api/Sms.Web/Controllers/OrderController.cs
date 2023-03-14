using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
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
    [ApiExplorerSettings(IgnoreApi = true)]
    public class OrderController : BaseRestfulController<IOrderService, RentCodeOrder>
    {
        private readonly IExportService _exportService;
        private readonly IOrderExportJobService _orderExportJobService;
        private readonly IAuthService _authService;
        public OrderController(IOrderService OrderService,
            IExportService exportService,
            IOrderExportJobService orderExportJobService,
            IAuthService authService) : base(OrderService)
        {
            _exportService = exportService;
            _orderExportJobService = orderExportJobService;
            _authService = authService;
        }
        [HttpPost("{id}/close-order")]
        public async Task<ApiResponseBaseModel> CloseOrder(int id)
        {
            return await _service.CloseOrder(id);
        }

        [HttpPost("export-orders")]
        public async Task<ApiResponseBaseModel<OrderExportJob>> ExportOrders(ExportOrdersRequest request)
        {
            //return await _exportService.ExportOrders(request.FromDate, request.ToDate, request.ServiceType);
            OrderExportJob orderExportJobLast = await _orderExportJobService.GetOrderExportByStatus(OrderExportStatus.Waiting);
            if (orderExportJobLast != null)
            {
                return new ApiResponseBaseModel<OrderExportJob>()
                {
                    Success = false,
                    Message = "ProcessExport"
                };
            }
            OrderExportJob orderExportJob = new OrderExportJob()
            {
                UserId = _authService.CurrentUserId().GetValueOrDefault(),
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Status = OrderExportStatus.Waiting,
            };
            return await _orderExportJobService.Create(orderExportJob);
        }

        [HttpPost("cancel-export-orders")]
        public async Task<ApiResponseBaseModel<OrderExportJob>> CancelExportOrders()
        {
            //return await _exportService.ExportOrders(request.FromDate, request.ToDate, request.ServiceType);
            OrderExportJob orderExportJob = await _orderExportJobService.GetOrderExportByStatus(OrderExportStatus.Waiting);
            orderExportJob.Status = OrderExportStatus.Cancelled;
            return await _orderExportJobService.Update(orderExportJob);
        }

        [HttpPost("export-orders-success")]
        public async Task<ApiResponseBaseModel<OrderExportJob>> ExportOrdersSuccess()
        {
            return new ApiResponseBaseModel<OrderExportJob>()
            {
                Results = await _orderExportJobService.GetOrderExportByStatus(OrderExportStatus.Success)
            };
        }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<RentCodeOrder>> Post([FromBody] RentCodeOrder value)
        {
            return base.Post(value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<RentCodeOrder>> Put(int id, [FromBody] RentCodeOrder value)
        {
            return base.Put(id, value);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<IEnumerable<RentCodeOrder>> Get()
        {
            return base.Get();
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<int>> Delete(int id)
        {
            return base.Delete(id);
        }
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        public override Task<ApiResponseBaseModel<RentCodeOrder>> Patch(int id, [FromBody] JsonPatchDocument<RentCodeOrder> patchDoc)
        {
            return base.Patch(id, patchDoc);
        }
    }
}
