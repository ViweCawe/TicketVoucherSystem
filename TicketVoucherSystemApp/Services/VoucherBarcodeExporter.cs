using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using TicketVoucherSystem.Data.Models;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace TicketVoucherSystemApp.Services;

public sealed class VoucherBarcodeExporter : IVoucherBarcodeExporter
{
    public byte[] CreateTicketSvg(string ticketNumber)
    {
        var document = CreateDocument(ticketNumber, height: 180);
        return Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting));
    }

    public byte[] CreateSvg(Voucher voucher)
    {
        var document = CreateDocument(voucher.Code, height: 230);
        var root = document.Root ?? throw new InvalidOperationException("Barcode SVG was empty.");
        var svg = root.Name.Namespace;

        root.Add(
            Label(svg, 190, $"{voucher.PackageName} - R{voucher.Amount:N2}", 18, bold: true),
            Label(svg, 215, $"Valid until {voucher.ExpiresUtc:dd MMM yyyy} UTC", 13));

        return Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting));
    }

    public byte[] CreateZip(IReadOnlyList<Voucher> vouchers)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var voucher in vouchers)
            {
                var entry = archive.CreateEntry($"voucher-{voucher.Code}.svg", CompressionLevel.Fastest);
                using var stream = entry.Open();
                stream.Write(CreateSvg(voucher));
            }
        }

        return output.ToArray();
    }

    private static XElement Label(XNamespace svg, int y, string text, int size, bool bold = false) =>
        new(
            svg + "text",
            new XAttribute("x", "50%"),
            new XAttribute("y", y),
            new XAttribute("text-anchor", "middle"),
            new XAttribute("font-family", "Arial, sans-serif"),
            new XAttribute("font-size", size),
            bold ? new XAttribute("font-weight", "700") : null,
            text);

    private static XDocument CreateDocument(string value, int height)
    {
        var writer = new BarcodeWriterSvg
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions { Height = 150, Width = 600, Margin = 20, PureBarcode = false }
        };

        var document = XDocument.Parse(writer.Write(value).Content);
        var root = document.Root ?? throw new InvalidOperationException("Barcode SVG was empty.");
        root.SetAttributeValue("height", height);
        var viewBox = root.Attribute("viewBox");
        if (viewBox is not null)
        {
            var values = viewBox.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            values[^1] = height.ToString();
            viewBox.Value = string.Join(' ', values);
        }

        return document;
    }
}
