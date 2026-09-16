namespace TicketVoucherSystem.Data.Validation;

public static class TicketNumberList
{
    private static readonly char[] Separators = ['\r', '\n', ',', ';', '\t', ' '];

    public static IReadOnlyList<string> Parse(string? input, int maximum = 100)
    {
        var ticketNumbers = (input ?? string.Empty)
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (ticketNumbers.Length == 0)
        {
            throw new ArgumentException("Scan or enter at least one ticket barcode.");
        }

        if (ticketNumbers.Length > maximum)
        {
            throw new ArgumentException($"A maximum of {maximum} ticket barcodes can be issued at once.");
        }

        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ticketNumber in ticketNumbers)
        {
            if (ticketNumber.Length is < 6 or > 32 || ticketNumber.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
            {
                throw new ArgumentException($"'{ticketNumber}' is not a valid ticket barcode. Use 6 to 32 letters, numbers, or hyphens.");
            }

            if (!unique.Add(ticketNumber))
            {
                throw new ArgumentException($"'{ticketNumber}' appears more than once.");
            }
        }

        return ticketNumbers;
    }
}
