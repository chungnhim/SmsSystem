using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Helpers
{
    public enum ServiceType
    {
        Basic = 1, // basic type, for Google, Facebook ...
        Any = 2, // service for all type, no need to provide message format
        ByTime = 3, // service for rent in one day
        Callback = 4, // for callback sim service
    }
    public enum RoleType
    {
        User = 1,
        Administrator = 2,
        Staff = 3,
        Forwarder = 4,
        UtilityTool = 5
    }

    public enum OrderStatus
    {
        Floating = 1,
        Waiting = 2,
        Success = 4,
        Error = 8,
        Cancelled = 16,
        OutOfService = 32
    }
    public enum OrderResultReportStatus
    {
        Floating = 1,
        Refund = 2,
        Cancelled = 4
    }
    public enum OrderComplaintStatus
    {
        Floating = 1,
        Refund = 2,
        Cancelled = 4
    }
    public enum PaymentMethodType
    {
        Ebanking = 1,
        Momo = 2
    }
    public enum OfflinePaymentNoticeStatus
    {
        Success = 1,
        Error = 2
    }
    public enum OfflinePaymentNoticeErrorReason
    {
        None = 0,
        CodeNotFound = 1,
        CodeIsExpired = 2,
        AmountTooSmall = 3,
        AmountTooLarge = 4
    }
    public enum HoldSimDurationUnit
    {
        ByDay = 0,
        ByHour = 1
    }
    public enum CheckoutRequestStatus
    {
        Floating = 0,
        Approve = 1,
        Finish = 2,
        Error = 3,
        Cancel = 4
    }
    public enum UserTransactionType
    {
        PaidForService = 0,
        UserRecharge = 1,
        AgentDiscount = 2,
        AgentCheckout = 3,
        ReferalFee = 4,
        ReferedFee = 5,
        TransferMoney = 6,
        TransferFee = 7,
        PaidForInternationalSim = 8
    }
    public enum OfflinePaymentMethodType
    {
        ManualAdmin = 0,
        Vietcombank = 1,
        Techcombank = 2,
        Momo = 3,
        Vietinbank = 4,
        BIDV = 5,
        MBBank = 6,
        NganLuong = 7,
        PerfectMoney = 8
    }

    public enum AppSourceType
    {
        Web = 0,
        Api = 1
    }
    public enum SmsType
    {
        Text = 0,
        Audio = 1
    }

    public enum OrderType
    {
        RentCode = 0,
        InternationalSim = 1,
    }
    public enum LiveCheckStatus
    {
        None = 0,
        Checking = 1,
        Ok = 2,
        Failed = 3
    }
    public enum OrderExportStatus
    {
        Waiting = 2,
        Success = 4,
        Cancelled = 16
    }
}
