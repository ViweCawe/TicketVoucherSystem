using System.Text.RegularExpressions;

namespace TicketVoucherSystem.Data.Validation;

public static partial class VoucherCodeList
{
    public static IReadOnlyList<string> Parse(string input)
    {
        var codes = input
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (codes.Length == 0)
        {
            throw new ArgumentException("Enter at least one barcode.", nameof(input));
        }

        if (codes.Length > 1_000)
        {
            throw new ArgumentException("Import no more than 1,000 barcodes at a time.", nameof(input));
        }

        if (codes.Any(code => !TenDigitBarcode().IsMatch(code)))
        {
            throw new ArgumentException("Every barcode must contain exactly 10 digits.", nameof(input));
        }

        return codes;
    }

    [GeneratedRegex("^[0-9]{10}$")]
    private static partial Regex TenDigitBarcode();
}
