using TicketVoucherSystem.Data.Exceptions;
using TicketVoucherSystem.Data.Models;
using TicketVoucherSystem.Data.Repositories;

namespace TicketVoucherSystemApp.Pos;

public static class PosApiEndpoints
{
    private const int MaxVoucherCodeLength = 32;
    private const int MaxTerminalIdentifierLength = 64;

    public static IEndpointRouteBuilder MapPosApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/pos/v1")
            .WithTags("POS voucher redemption")
            .AddEndpointFilter<PosApiKeyFilter>();

        group.MapPost("/vouchers/redeem", RedeemAsync)
            .WithName("RedeemPosVoucher")
            .Accepts<PosRedemptionRequest>("application/json")
            .Produces<PosRedemptionResponse>()
            .Produces<PosApiError>(StatusCodes.Status400BadRequest)
            .Produces<PosApiError>(StatusCodes.Status401Unauthorized)
            .Produces<PosApiError>(StatusCodes.Status404NotFound)
            .Produces<PosApiError>(StatusCodes.Status409Conflict)
            .Produces<PosApiError>(StatusCodes.Status410Gone)
            .Produces<PosApiError>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    public static async Task<IResult> RedeemAsync(
        PosRedemptionRequest request,
        IVoucherData vouchers,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;
        var validationError = Validate(request, traceId);
        if (validationError is not null)
        {
            return Results.Json(validationError, statusCode: StatusCodes.Status400BadRequest);
        }

        var department = VoucherDepartments.All.First(value =>
            string.Equals(value, request.DepartmentType, StringComparison.OrdinalIgnoreCase));
        var voucherCode = request.VoucherCode!.Trim();
        var tillId = request.TillId!.Trim();
        var operatorId = request.OperatorId!.Trim();
        var performedBy = $"POS:{tillId}:{operatorId}";

        try
        {
            var voucher = await vouchers.RedeemAsync(
                voucherCode,
                department,
                request.OutletId,
                performedBy,
                cancellationToken);

            if (voucher is null)
            {
                return Results.Json(
                    new PosApiError(
                        "redemption_failed",
                        "The voucher could not be redeemed.",
                        traceId),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return Results.Ok(new PosRedemptionResponse(
                "redeemed",
                voucher.Id,
                voucher.Code,
                voucher.Status,
                voucher.DepartmentType,
                voucher.Amount,
                "ZAR",
                voucher.PackageName,
                voucher.RedeemedOutletId ?? request.OutletId,
                voucher.RedeemedAt ?? string.Empty,
                voucher.RedeemedUtc,
                tillId,
                operatorId,
                traceId));
        }
        catch (VoucherOperationException exception)
        {
            var (statusCode, errorCode, publicMessage) = MapError(exception);
            return Results.Json(
                new PosApiError(errorCode, publicMessage, traceId),
                statusCode: statusCode);
        }
    }

    private static PosApiError? Validate(PosRedemptionRequest request, string traceId)
    {
        if (string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            return new PosApiError("validation_error", "voucherCode is required.", traceId);
        }

        if (request.VoucherCode.Trim().Length > MaxVoucherCodeLength)
        {
            return new PosApiError(
                "validation_error",
                $"voucherCode cannot exceed {MaxVoucherCodeLength} characters.",
                traceId);
        }

        if (string.IsNullOrWhiteSpace(request.DepartmentType) ||
            !VoucherDepartments.All.Any(value =>
                string.Equals(value, request.DepartmentType, StringComparison.OrdinalIgnoreCase)))
        {
            return new PosApiError(
                "validation_error",
                "departmentType must be Retail or FoodAndBeverage.",
                traceId);
        }

        if (request.OutletId <= 0)
        {
            return new PosApiError("validation_error", "outletId must be greater than zero.", traceId);
        }

        if (string.IsNullOrWhiteSpace(request.TillId) ||
            request.TillId.Trim().Length > MaxTerminalIdentifierLength)
        {
            return new PosApiError(
                "validation_error",
                $"tillId is required and cannot exceed {MaxTerminalIdentifierLength} characters.",
                traceId);
        }

        if (string.IsNullOrWhiteSpace(request.OperatorId) ||
            request.OperatorId.Trim().Length > MaxTerminalIdentifierLength)
        {
            return new PosApiError(
                "validation_error",
                $"operatorId is required and cannot exceed {MaxTerminalIdentifierLength} characters.",
                traceId);
        }

        return null;
    }

    private static (int StatusCode, string ErrorCode, string Message) MapError(
        VoucherOperationException exception) =>
        exception.ErrorCode switch
        {
            50001 => (StatusCodes.Status404NotFound, "voucher_not_found", exception.Message),
            50002 => (StatusCodes.Status409Conflict, "already_redeemed", exception.Message),
            50003 => (StatusCodes.Status409Conflict, "voucher_cancelled", exception.Message),
            50004 => (StatusCodes.Status410Gone, "voucher_expired", exception.Message),
            50005 => (StatusCodes.Status409Conflict, "redemption_conflict", exception.Message),
            50006 => (StatusCodes.Status400BadRequest, "invalid_outlet", exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "redemption_failed",
                "The voucher could not be redeemed.")
        };
}
