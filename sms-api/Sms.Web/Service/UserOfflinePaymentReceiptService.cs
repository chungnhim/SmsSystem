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
  public interface IUserOfflinePaymentReceiptService : IServiceBase<UserOfflinePaymentReceipt>
  {
    Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> GenerateOrGetMyLatestPaymentReceipt(int? methodId);
    Task<ApiResponseBaseModel> HandleOfflinePaymentNotice(OfflinePaymentNoticeModel request, bool isAdminConfirm);
    Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> UserConfirmReceipt(UserReceiptConfirmRequest confirmRequest);
  }
  public class UserOfflinePaymentReceiptService : ServiceBase<UserOfflinePaymentReceipt>, IUserOfflinePaymentReceiptService
  {
    private readonly IAuthService _authService;
    private readonly IUserTransactionService _userTransactionService;
    public UserOfflinePaymentReceiptService(SmsDataContext smsDataContext, IAuthService authService, IUserTransactionService userTransactionService) : base(smsDataContext)
    {
      _authService = authService;
      _userTransactionService = userTransactionService;
    }

    public async Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> GenerateOrGetMyLatestPaymentReceipt(int? methodId)
    {
      var userId = _authService.CurrentUserId();
      if (userId == null) return new ApiResponseBaseModel<UserOfflinePaymentReceipt>()
      {
        Message = "Unauthorized",
        Success = false
      };
      if (methodId == null)
      {
        methodId = (await _smsDataContext.PaymentMethodConfigurations.FirstOrDefaultAsync())?.Id;
      }
      var last = await _smsDataContext.UserOfflinePaymentReceipts
          .Where(r => r.IsExpired != true && r.UserId == userId && r.PaymentMethodConfigurationId == methodId)
          .OrderByDescending(r => r.Created).LastOrDefaultAsync();
      if (last != null) return new ApiResponseBaseModel<UserOfflinePaymentReceipt>()
      {
        Message = null,
        Success = true,
        Results = last
      };
      last = new UserOfflinePaymentReceipt()
      {
        UserId = userId.Value,
        PaymentMethodConfigurationId = methodId,
        ReceiptCode = await this.GenerateRandomPinCode()
      };
      _smsDataContext.UserOfflinePaymentReceipts.Add(last);
      await _smsDataContext.SaveChangesAsync();
      return new ApiResponseBaseModel<UserOfflinePaymentReceipt>()
      {
        Message = null,
        Success = true,
        Results = last
      };
    }

    private async Task<string> GenerateRandomPinCode()
    {
      var random = new Random(Guid.NewGuid().GetHashCode());
      var number = random.Next(0, 999999999);
      var str = number.ToString("D9");
      var code = $"NT{str}TN";
      if (await _smsDataContext.UserOfflinePaymentReceipts.AnyAsync(r => r.ReceiptCode == code))
      {
        return await GenerateRandomPinCode();
      }
      return code;
    }

    public override void Map(UserOfflinePaymentReceipt entity, UserOfflinePaymentReceipt model)
    {
    }

    public async Task<ApiResponseBaseModel> HandleOfflinePaymentNotice(OfflinePaymentNoticeModel request, bool isAdminConfirm)
    {
      var receipt = await _smsDataContext.UserOfflinePaymentReceipts.Include(r => r.PaymentMethodConfiguration)
          .FirstOrDefaultAsync(r => r.ReceiptCode == request.ReceiptCode);
      var validateResult = await ValidatePaymentNotice(request, receipt);
      if (validateResult != null) return validateResult;
      var notice = new OfflinePaymentNotice()
      {
        Amount = request.Amount,
        OptionalMessage = request.OptionalMessage,
        ReceiptCode = request.ReceiptCode,
        ServiceProvider = request.ServiceProvider,
        Status = OfflinePaymentNoticeStatus.Success,
        SolvedReceiptId = receipt.Id
      };
      _smsDataContext.OfflinePaymentNotices.Add(notice);
      receipt.IsExpired = true;
      receipt.Amount = request.Amount;
      var transaction = new UserTransaction()
      {
        UserId = receipt.UserId,
        Comment = $"Nap tien {request.ServiceProvider}, GD: {receipt.ReceiptCode}",
        Amount = receipt.Amount,
        IsImport = true,
        UserTransactionType = UserTransactionType.UserRecharge,
        OfflinePaymentMethodType = PaymentMethodToPaymentMethodType(receipt.PaymentMethodConfiguration),
        IsAdminConfirm = isAdminConfirm
      };
      await _smsDataContext.SaveChangesAsync();
      await _userTransactionService.Create(transaction);
      return new ApiResponseBaseModel()
      {
        Success = true
      };
    }

    private OfflinePaymentMethodType? PaymentMethodToPaymentMethodType(PaymentMethodConfiguration method)
    {
      if (method == null) return null;
      switch (method.BankCode)
      {
        case "VCB":
          return OfflinePaymentMethodType.Vietcombank;
        case "TCB":
          return OfflinePaymentMethodType.Techcombank;
        case "Momo":
          return OfflinePaymentMethodType.Momo;
        case "VTB":
          return OfflinePaymentMethodType.Vietinbank;
        case "BIDV":
          return OfflinePaymentMethodType.BIDV;
        case "MBB":
          return OfflinePaymentMethodType.MBBank;
      }
      return null;
    }

    private async Task<ApiResponseBaseModel> ValidatePaymentNotice(OfflinePaymentNoticeModel request, UserOfflinePaymentReceipt receipt)
    {
      if (receipt == null)
      {
        var notice = new OfflinePaymentNotice()
        {
          Amount = request.Amount,
          OptionalMessage = request.OptionalMessage,
          ReceiptCode = request.ReceiptCode,
          ServiceProvider = request.ServiceProvider,
          Status = OfflinePaymentNoticeStatus.Error,
          ErrorReason = OfflinePaymentNoticeErrorReason.CodeNotFound
        };
        _smsDataContext.OfflinePaymentNotices.Add(notice);
        await _smsDataContext.SaveChangesAsync();
        return new ApiResponseBaseModel()
        {
          Success = false,
          Message = "CodeNotFound"
        };
      }
      if (receipt.IsExpired)
      {
        var notice = new OfflinePaymentNotice()
        {
          Amount = request.Amount,
          OptionalMessage = request.OptionalMessage,
          ReceiptCode = request.ReceiptCode,
          ServiceProvider = request.ServiceProvider,
          Status = OfflinePaymentNoticeStatus.Error,
          ErrorReason = OfflinePaymentNoticeErrorReason.CodeIsExpired
        };
        _smsDataContext.OfflinePaymentNotices.Add(notice);
        await _smsDataContext.SaveChangesAsync();
        return new ApiResponseBaseModel()
        {
          Success = false,
          Message = "CodeIsExpired"
        };
      }
      var amount = request.Amount;
      if (amount < 10000)
      {
        var notice = new OfflinePaymentNotice()
        {
          Amount = request.Amount,
          OptionalMessage = request.OptionalMessage,
          ReceiptCode = request.ReceiptCode,
          ServiceProvider = request.ServiceProvider,
          Status = OfflinePaymentNoticeStatus.Error,
          ErrorReason = OfflinePaymentNoticeErrorReason.AmountTooSmall
        };
        _smsDataContext.OfflinePaymentNotices.Add(notice);
        await _smsDataContext.SaveChangesAsync();
        return new ApiResponseBaseModel()
        {
          Success = false,
          Message = "AmountTooSmall"
        };
      }
      if (amount > 100000000)
      {
        var notice = new OfflinePaymentNotice()
        {
          Amount = request.Amount,
          OptionalMessage = request.OptionalMessage,
          ReceiptCode = request.ReceiptCode,
          ServiceProvider = request.ServiceProvider,
          Status = OfflinePaymentNoticeStatus.Error,
          ErrorReason = OfflinePaymentNoticeErrorReason.AmountTooLarge
        };
        _smsDataContext.OfflinePaymentNotices.Add(notice);
        await _smsDataContext.SaveChangesAsync();
        return new ApiResponseBaseModel()
        {
          Success = false,
          Message = "AmountTooLarge"
        };
      }
      return null;
    }

    protected override IQueryable<UserOfflinePaymentReceipt> GenerateQuery(FilterRequest filterRequest = null)
    {
      var query = base.GenerateQuery(filterRequest);
      query = query.OrderByDescending(r => r.Updated);
      query = query.Include(r => r.PaymentMethodConfiguration);
      query = query.Include(r => r.User);

      if (filterRequest != null)
      {
        var sortColumn = (filterRequest.SortColumnName ?? string.Empty).ToLower();
        var isAsc = filterRequest.IsAsc;
        {
          if (filterRequest.SearchObject.TryGetValue("searchReceiptCode", out object obj))
          {
            var c = ((string)obj).ToLower();
            if (!string.IsNullOrEmpty(c))
            {
              query = query.Where(r => r.ReceiptCode.ToLower().Contains(c));
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("userConfirmedAccountOwner", out object obj))
          {
            var c = ((string)obj).ToLower();
            if (!string.IsNullOrEmpty(c))
            {
              query = query.Where(r => r.UserConfirmedAccountOwner.ToLower().Contains(c));
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("userConfirmedComment", out object obj))
          {
            var c = ((string)obj).ToLower();
            if (!string.IsNullOrEmpty(c))
            {
              query = query.Where(r => r.UserConfirmedComment.ToLower().Contains(c));
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("user", out object obj))
          {
            var c = ((string)obj).ToLower();
            if (!string.IsNullOrEmpty(c))
            {
              query = query.Where(r => r.User != null && r.User.Username.ToLower().Contains(c));
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("searchStatus", out object obj))
          {
            var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int>>();
            if (ts.Count > 0)
            {
              var tsBools = ts.Select(r => r == 0);
              query = query.Where(r => tsBools.Contains(r.IsExpired));
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("PaymentMethodIds", out object obj))
          {
            var ts = ((Newtonsoft.Json.Linq.JArray)obj).ToObject<List<int?>>();
            if (ts != null && ts.Count > 0)
            {
              query = query.Where(r => ts.Contains(r.PaymentMethodConfigurationId));
            }
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("onlyConfirmed", out object obj))
          {
            var c = (bool)obj;
            if (c)
            {
              query = query.Where(r => r.UserConfirmedAmount.HasValue);
            }
          }
        }
        DateTime? fromDate = null;
        DateTime? toDate = null;
        {
          if (filterRequest.SearchObject.TryGetValue("searchStartDate", out object obj))
          {
            fromDate = (DateTime)obj;
          }
        }
        {
          if (filterRequest.SearchObject.TryGetValue("searchEndDate", out object obj))
          {
            toDate = (DateTime)obj;
            toDate = toDate.Value.AddDays(1).AddSeconds(-1);
          }
        }
        if (fromDate != null)
        {
          query = query.Where(r => r.Created >= fromDate);
        }
        if (toDate != null)
        {
          query = query.Where(r => r.Created < toDate);
        }
        switch (sortColumn)
        {
            case "created":
                query = isAsc ? query.OrderBy(x => x.Created) : query.OrderByDescending(x => x.Created);
                break;
            case "updated":
                query = isAsc ? query.OrderBy(x => x.Updated) : query.OrderByDescending(x => x.Updated);
                break;
            default:
                break;
        }
      }
      return query;
    }

    public async Task<ApiResponseBaseModel<UserOfflinePaymentReceipt>> UserConfirmReceipt(UserReceiptConfirmRequest confirmRequest)
    {
      var userId = _authService.CurrentUserId();
      if (userId == null) return new ApiResponseBaseModel<UserOfflinePaymentReceipt>()
      {
        Success = false,
        Message = "Unauthorized"
      };
      var receipt = await _smsDataContext.UserOfflinePaymentReceipts
          .FirstOrDefaultAsync(r => r.ReceiptCode == confirmRequest.ReceiptCode && r.IsExpired != true && r.UserId == userId.Value);
      if (receipt == null) return new ApiResponseBaseModel<UserOfflinePaymentReceipt>()
      {
        Success = false,
        Message = "ReceiptNotFound"
      };
      receipt.UserConfirmedAmount = confirmRequest.Amount;
      receipt.UserConfirmedComment = confirmRequest.Comment;
      receipt.UserConfirmedAccountOwner = confirmRequest.AccountOwner;
      await _smsDataContext.SaveChangesAsync();

      return new ApiResponseBaseModel<UserOfflinePaymentReceipt>()
      {
        Results = receipt
      };
    }
  }
}
