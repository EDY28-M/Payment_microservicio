namespace PaymentMicroservicio.Domain.Enums;

/// <summary>
/// Estados posibles de un pago
/// </summary>
public static class PaymentStatus
{
    public const string Pending = "pending";
    public const string RequiresPaymentMethod = "requires_payment_method";
    public const string RequiresConfirmation = "requires_confirmation";
    public const string RequiresAction = "requires_action";
    public const string Processing = "processing";
    public const string Succeeded = "succeeded";
    public const string Canceled = "canceled";
    public const string Failed = "failed";

    public static bool IsSuccessful(string status) => status == Succeeded;
    public static bool IsFailed(string status) => status == Failed || status == Canceled;
    public static bool IsPending(string status) => status == Pending || status == Processing;
}
