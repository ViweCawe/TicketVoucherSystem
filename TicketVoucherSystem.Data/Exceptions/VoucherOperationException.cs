namespace TicketVoucherSystem.Data.Exceptions;

public sealed class VoucherOperationException : Exception
{
    public VoucherOperationException(string message, Exception innerException)
        : this(0, message, innerException)
    {
    }

    public VoucherOperationException(int errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public int ErrorCode { get; }
}
