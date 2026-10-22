namespace TicketVoucherSystemApp.Pos;

public sealed record PosRedemptionRequest(
    string? VoucherCode,
    string? DepartmentType,
    int OutletId,
    string? TillId,
    string? OperatorId);

public sealed record PosRedemptionResponse(
    string Result,
    long VoucherId,
    string VoucherCode,
    string Status,
    string DepartmentType,
    decimal Amount,
    string Currency,
    string PackageName,
    int OutletId,
    string OutletName,
    DateTime? RedeemedUtc,
    string TillId,
    string OperatorId,
    string TraceId);

public sealed record PosApiError(string Code, string Message, string TraceId);
