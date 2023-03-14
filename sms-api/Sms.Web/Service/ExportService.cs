using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sms.Web.Entity;
using Sms.Web.Helpers;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IExportService
    {
        //Task<ApiResponseBaseModel<string>> ExportOrders(DateTime? fromDate, DateTime? toDate, List<ServiceType> serviceTypes);
        Task BackgroundProcessOrderExport();

    }
    public class ExportService : IExportService
    {
        private readonly SmsDataContext _smsDataContext;
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IOrderService _orderService;
        private readonly ICacheService _cacheService;
        private readonly IGsmDeviceService _gsmDeviceService;
        private readonly IFileService _fileService;
        private readonly IDateTimeService _dateTimeService;
        //private readonly ILogger<ExportService> _logger;
        private readonly IOrderExportJobService _orderExportJobService;
        public ExportService(SmsDataContext smsDataContext,
            IAuthService authService,
            IUserService userService,
            IOrderService orderService,
            ICacheService cacheService,
            IGsmDeviceService gsmDeviceService,
            IFileService fileService,
            IDateTimeService dateTimeService,
            ILogger<ExportService> logger,
            IOrderExportJobService orderExportJobService
        )
        {
            _smsDataContext = smsDataContext;
            _authService = authService;
            _userService = userService;
            _orderService = orderService;
            _cacheService = cacheService;
            _gsmDeviceService = gsmDeviceService;
            _fileService = fileService;
            _dateTimeService = dateTimeService;
            //_logger = logger;
            _orderExportJobService = orderExportJobService;
        }

        public async Task BackgroundProcessOrderExport()
        {
            await ProcessOrderExport();
        }

        private async Task ProcessOrderExport()
        {
            var currentUser = await _userService.GetCurrentUser();

            if (currentUser == null) return;

            if (currentUser.Role != RoleType.User && currentUser.Role != RoleType.Staff && currentUser.Role != RoleType.Administrator) return;

            var orderExportJobs = await (from o in _smsDataContext.OrderExportJobs
                                              where o.UserId == currentUser.Id
                                                  && o.Status == OrderExportStatus.Waiting
                                              select o).ToListAsync();
            if (orderExportJobs == null) return;

            foreach (var orderExportJob in orderExportJobs)
            {
                var listOrder = new List<RentCodeOrder>();
                if (currentUser.Role == RoleType.Staff)
                {
                    listOrder = await GetAllOrdersToExport(currentUser.Id, orderExportJob.FromDate, orderExportJob.ToDate, null);
                }
                if (currentUser.Role == RoleType.User)
                {
                    //var jsServiveType = JsonConvert.SerializeObject(serviceType);
                    JArray jaServiceType = JArray.Parse(orderExportJob.ServiceType);
                    listOrder = await GetAllOrdersToExport(currentUser.Id, orderExportJob.FromDate, orderExportJob.ToDate, jaServiceType);
                }
                else if (currentUser.Role == RoleType.Administrator)
                {
                    listOrder = await GetAllOrdersToExport(null, orderExportJob.FromDate, orderExportJob.ToDate, null);
                }
                var listOrderAsDataTable = await PublishOrderData(listOrder);
                var excelFile = WriteExcelFileToStream(listOrderAsDataTable, "DonHang");
                var now = _dateTimeService.UtcNow();
                var folderOfFile = string.Format("{0}/{1}/{2}", now.Year, now.Month, now.Day);
                var uploadFileResult = await _fileService.SendMyFileToS3Async(excelFile, folderOfFile, "orders_export_" + Guid.NewGuid().ToString() + ".xlsx");

                // call api update url export
                orderExportJob.Status = OrderExportStatus.Success;
                orderExportJob.UrlExport = uploadFileResult;
                await _orderExportJobService.Update(orderExportJob);
            }
            
            //_logger.LogError("End ProcessOrderExport");
        }

        //public async Task<ApiResponseBaseModel<string>> ExportOrders(DateTime? fromDate, DateTime? toDate, List<ServiceType> serviceType)
        //{
        //    var currentUser = await _userService.GetCurrentUser();

        //    if (currentUser == null) return ApiResponseBaseModel<string>.UnAuthorizedResponse();

        //    if (currentUser.Role != RoleType.User && currentUser.Role != RoleType.Staff && currentUser.Role != RoleType.Administrator) return ApiResponseBaseModel<string>.UnAuthorizedResponse();

        //    var listOrder = new List<RentCodeOrder>();
        //    if (currentUser.Role == RoleType.Staff)
        //    {
        //        listOrder = await GetAllOrdersToExport(currentUser.Id, fromDate, toDate, null);
        //    }
        //    if (currentUser.Role == RoleType.User)
        //    {
        //        var jsServiveType = JsonConvert.SerializeObject(serviceType);
        //        JArray jaServiceType = JArray.Parse(jsServiveType);
        //        listOrder = await GetAllOrdersToExport(currentUser.Id, fromDate, toDate, jaServiceType);
        //    }
        //    else if (currentUser.Role == RoleType.Administrator)
        //    {
        //        listOrder = await GetAllOrdersToExport(null, fromDate, toDate, null);
        //    }
        //    var listOrderAsDataTable = await PublishOrderData(listOrder);
        //    var excelFile = WriteExcelFileToStream(listOrderAsDataTable, "DonHang");
        //    var now = _dateTimeService.UtcNow();
        //    var folderOfFile = string.Format("{0}/{1}/{2}", now.Year, now.Month, now.Day);
        //    var uploadFileResult = await _fileService.SendMyFileToS3Async(excelFile, folderOfFile, "orders_export_" + Guid.NewGuid().ToString() + ".xlsx");

        //    return new ApiResponseBaseModel<string>()
        //    {
        //        Success = true,
        //        Results = uploadFileResult
        //    };
        //}

        private Stream WriteExcelFileToStream(DataTable table, string sheetName)
        {
            var spreadsheetStream = new MemoryStream();
            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.
                Create(spreadsheetStream, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.
                    AppendChild<Sheets>(new Sheets());

                Sheet sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.
                    GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = sheetName
                };
                sheets.Append(sheet);

                Row headerRow = new Row();

                List<String> columns = new List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    columns.Add(column.ColumnName);
                    Cell cell = new Cell();
                    cell.DataType = CellValues.String;
                    cell.CellValue = new CellValue(column.ColumnName);
                    headerRow.AppendChild(cell);
                }
                sheetData.AppendChild(headerRow);

                foreach (DataRow dsrow in table.Rows)
                {
                    Row newRow = new Row();
                    foreach (String col in columns)
                    {
                        Cell cell = new Cell();
                        cell.DataType = CellValues.String;
                        cell.CellValue = new CellValue(dsrow[col].ToString());
                        newRow.AppendChild(cell);
                    }
                    sheetData.AppendChild(newRow);
                }
                workbookpart.Workbook.Save();
                spreadsheetDocument.Close();
            }
            spreadsheetStream.Position = 0;
            return spreadsheetStream;
        }

        private async Task<List<RentCodeOrder>> GetAllOrdersToExport(int? userId, DateTime? startDate, DateTime? endDate, JArray serviceType)
        {
            var maximumOrder = 100000; // 100.000
            var listOrder = new List<RentCodeOrder>();
            var total = maximumOrder;
            var pageIndex = 0;
            while (true)
            {
                var filterRequest = new FilterRequest()
                {
                    PageIndex = pageIndex,
                    PageSize = 1000,
                    SearchObject = new Dictionary<string, object>()
                    {
                        {"IgnoreAdditionalData", true},
                        {"IgnoreComplaint", true}
                    }
                };
                if (userId.HasValue)
                {
                    filterRequest.SearchObject.Add("UserId", userId.Value);
                }
                if (startDate != null)
                {
                    filterRequest.SearchObject.Add("CreatedFrom", startDate.Value);
                }
                if (endDate != null)
                {
                    filterRequest.SearchObject.Add("CreatedTo", endDate.Value);
                }
                if (serviceType != null)
                {
                    filterRequest.SearchObject.Add("ServiceType", serviceType);
                }
                var pagingResult = await _orderService.Paging(filterRequest);
                if (pagingResult.Results.Count == 0) break;
                listOrder.AddRange(pagingResult.Results);
                total = Math.Min(maximumOrder, pagingResult.Total);
                if (listOrder.Count >= total) break;
                pageIndex++;
            }
            return listOrder;
        }

        private async Task<DataTable> PublishOrderData(List<RentCodeOrder> orders)
        {
            var headers = new List<string>()
            {
                "MaDonHang", "KhachHang", "DichVu", "Gia", "ThoiGianSuDung", "NhaMang", "SoDienThoai", "ThietBiGSM", "TrangThai", "NhanCuocGoi", "NgayTao", "CapNhapCuoi"
            };
            var data = new List<List<string>>();

            var allServices = await _cacheService.GetAllServiceProviders();
            var serviceDictionary = allServices.ToDictionary(r => r.Id, r => r.Name);

            var networkProviders = new Dictionary<int, string>();
            networkProviders.Add(1, "Viettel");
            networkProviders.Add(2, "Vinaphone");
            networkProviders.Add(3, "Mobile Phone");
            networkProviders.Add(4, "Vietnam Mobile");
            networkProviders.Add(5, "Cambodia");

            var allGsmDevices = await _gsmDeviceService.GetAlls();
            var gsmDevicesMap = allGsmDevices.ToDictionary(r => r.Id, r => r.Name);
            gsmDevicesMap.Add(0, string.Empty);

            var orderStatusMap = new Dictionary<OrderStatus, string>()
            {
                {OrderStatus.Floating, "Đang xem xét"},
                {OrderStatus.OutOfService, "Quá thời gian"},
                {OrderStatus.Cancelled, "Đã hủy"},
                {OrderStatus.Error, "Lỗi"},
                {OrderStatus.Success, "Thành công"},
                {OrderStatus.Waiting, "Đang chờ đợi"},
            };

            foreach (var order in orders)
            {
                serviceDictionary.TryGetValue(order.ServiceProviderId, out var serviceName);
                networkProviders.TryGetValue(order.NetworkProvider.GetValueOrDefault(), out var network);
                gsmDevicesMap.TryGetValue(order.ConnectedGsmId.GetValueOrDefault(), out var gsmName);
                orderStatusMap.TryGetValue(order.Status, out var orderStatus);
                var dataRow = new List<string>()
                {
                    order.Guid,
                    order.User.Username,
                    serviceName,
                    order.Price.ToString(),
                    order.LockTime.ToString(),
                    network,
                    order.PhoneNumber,
                    gsmName,
                    orderStatus,
                    order.AllowVoiceSms ? "Có" : "Không",
                    order.Created.HasValue ? order.Created.Value.ToString() : string.Empty,
                    order.Updated.HasValue ? order.Updated.Value.ToString() : string.Empty,
                };
                data.Add(dataRow);
            }
            return PopulateDataToDataTable(headers, data);
        }

        private DataTable PopulateDataToDataTable(List<string> headers, List<List<string>> data)
        {
            DataTable table = new DataTable();
            foreach (var h in headers)
            {
                DataColumn tbColumn = new DataColumn();
                tbColumn.ColumnName = h;
                table.Columns.Add(tbColumn);
            }
            DataSet dataSet = new DataSet();
            dataSet.Tables.Add(table);

            foreach (var rowData in data)
            {
                DataRow row = table.NewRow();
                for (int i = 0; i < headers.Count; i++)
                {
                    row[headers[i]] = rowData[i];
                }
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
