namespace ITValet.HelpingClasses
{
    public enum EnumActiveStatus
    {
        Deleted = 0,
        Active = 1,
        AdminVerificationPending = 2,
        EmailVerificationPending = 3,
        AccountOnHold = 4,
    }
    public enum EnumRoles
    {
        Admin = 1,
        Employee = 2,
        Customer = 3,
        Valet = 4,
    }

    public enum PaymentStatusCheckResult
    {
        Success,
        ItemNotFound,
        UnexpectedStatusCode,
        Exception
    }

    public enum FundTransferResult
    {
        Success,
        ApiError,
        UnclaimedTransaction,
        Exception
    }

    public enum OrderReasonType
    {
        Extend = 1,
        Revision = 2,
        Cancel = 3,
    }
    public enum NotificationType
    {
        TimeAvailabilityNotification = 1,
        OrderMessage = 2,
        InboxMessage = 3,
        OrderCancellationRequested = 4,
        OrderCancellationRejected = 5,
        OrderCancelled = 6,        
        DateExtensionRequested = 7,
        DateExtensionRejected = 8,
        DateExtended = 9,
        ZoomMeetingCreated = 10,
        OrderDelivered = 11,
        RevisionRequested =12,
        DelivertAccepted = 13,
    }

    public enum StripePaymentStatus
    {
        PaymentReceived = 1,
        Refunded = 2,
        SessionUsed = 3,
        SessionReverted = 4,
        SentToValet = 5,
        PaymentFailedToSend = 6,
        PaymentNotReceived = 7,
    }
}
