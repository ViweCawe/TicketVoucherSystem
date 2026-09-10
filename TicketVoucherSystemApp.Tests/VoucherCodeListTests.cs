using TicketVoucherSystemApp.Vouchers.Validation;
using Xunit;

namespace TicketVoucherSystemApp.Vouchers.Tests;

public sealed class VoucherCodeListTests
{
    [Fact]
    public void Parse_accepts_new_lines_and_removes_duplicates()
    {
        var result = VoucherCodeList.Parse("1234567890\n0987654321\n1234567890");

        Assert.Equal(["1234567890", "0987654321"], result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefghij")]
    public void Parse_rejects_invalid_input(string input)
    {
        Assert.Throws<ArgumentException>(() => VoucherCodeList.Parse(input));
    }
}
