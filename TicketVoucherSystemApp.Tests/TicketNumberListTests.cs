using TicketVoucherSystem.Data.Validation;
using Xunit;

namespace TicketVoucherSystemApp.Tests;

public sealed class TicketNumberListTests
{
    [Fact]
    public void Parse_accepts_new_lines_commas_and_semicolons() =>
        Assert.Equal(
            new[] { "ABC-123456", "ABC-123457", "ABC-123458" },
            TicketNumberList.Parse("ABC-123456\nABC-123457, ABC-123458;"));

    [Fact]
    public void Parse_rejects_duplicate_ticket_numbers()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            TicketNumberList.Parse("ABC-123456\nabc-123456"));

        Assert.Contains("appears more than once", exception.Message);
    }

    [Fact]
    public void Parse_rejects_more_than_one_hundred_tickets()
    {
        var input = string.Join('\n', Enumerable.Range(100000, 101).Select(number => $"VIP-{number}"));

        var exception = Assert.Throws<ArgumentException>(() => TicketNumberList.Parse(input));

        Assert.Contains("maximum of 100", exception.Message);
    }
}
