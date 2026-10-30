using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TicketVoucherSystem.Data.Models;

namespace VoucherSystemApp.API.Authentication;

public sealed class PosAuthenticationOptions
{
    public string Issuer { get; set; } = "VoucherSystemApp.API";
    public string Audience { get; set; } = "VoucherPOS";
    public string SecretKey { get; set; } = "";
    public List<PosClient> Clients { get; set; } = [];

    // Fail closed: incomplete or duplicate till credentials must never enable the API.
    public bool IsValid() => Encoding.UTF8.GetByteCount(SecretKey) >= 32
        && !string.IsNullOrWhiteSpace(Issuer) && !string.IsNullOrWhiteSpace(Audience)
        && Clients.Count > 0 && Clients.All(c => c.IsValid())
        && Clients.Select(c => c.ClientId).Distinct(StringComparer.Ordinal).Count() == Clients.Count;
}

public sealed class PosClient
{
    public string ClientId { get; set; } = "";
    public string SecretHash { get; set; } = "";
    public string TillId { get; set; } = "";
    public int OutletId { get; set; }
    public string DepartmentType { get; set; } = "";

    public bool IsValid() => Regex.IsMatch(ClientId, "^[a-z0-9-]{1,64}$")
        && Regex.IsMatch(SecretHash, "^[A-Fa-f0-9]{64}$")
        && Regex.IsMatch(TillId, "^[A-Za-z0-9-]{1,64}$")
        && OutletId > 0 && VoucherDepartments.All.Contains(DepartmentType);

    // The secret is a random machine credential, not a human password.
    public bool Matches(string secret) => CryptographicOperations.FixedTimeEquals(
        SHA256.HashData(Encoding.UTF8.GetBytes(secret)), Convert.FromHexString(SecretHash));
}
