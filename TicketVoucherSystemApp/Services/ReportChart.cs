using System.Globalization;
using TicketVoucherSystem.Data.Models;

namespace TicketVoucherSystemApp.Services;

public static class ReportChart
{
    public static readonly string[] Colors = ["#3367d6", "#00a68a", "#f5a623", "#df5b57", "#7559c7", "#3b8c9e"];

    public static string LinePoints(IReadOnlyList<VoucherTrend> trend)
    {
        if (trend.Count == 0)
        {
            return string.Empty;
        }

        var max = Math.Max(1m, trend.Max(day => day.RedeemedValue));
        var step = trend.Count == 1 ? 0 : 660m / (trend.Count - 1);
        return string.Join(" ", trend.Select((day, index) =>
        {
            var x = 20m + (index * step);
            var y = 190m - (day.RedeemedValue * 155m / max);
            return $"{x.ToString("0.#", CultureInfo.InvariantCulture)},{y.ToString("0.#", CultureInfo.InvariantCulture)}";
        }));
    }

    public static string DonutBackground(IReadOnlyList<VoucherOutletReport> outlets)
    {
        if (outlets.Count == 0)
        {
            return "background:#e5e7eb";
        }

        decimal start = 0;
        var segments = outlets.Select((outlet, index) =>
        {
            var end = Math.Min(100, start + outlet.SharePercent);
            var segment = $"{Colors[index % Colors.Length]} {Percent(start)} {Percent(end)}";
            start = end;
            return segment;
        });

        return $"background:conic-gradient({string.Join(",", segments)})";
    }

    private static string Percent(decimal value) =>
        $"{value.ToString("0.#", CultureInfo.InvariantCulture)}%";
}
