namespace TicketVoucherSystem.Data.Exceptions;

public sealed class VoucherOperationException(string message, Exception innerException)
    : Exception(message, innerException);
