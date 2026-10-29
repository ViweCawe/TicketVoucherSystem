# POS API — MICROS and HRK

The POS consumes **VoucherSystemApp.API**. The Razor Pages application only issues vouchers, administers data, and displays reports. It no longer hosts a redemption endpoint or redemption page. Both applications use the same voucher database, so a voucher issued in the admin app is immediately available to the API.

## Read the code in this order

| Your Section 9 lesson | This solution | What to understand |
| --- | --- | --- |
| Program.cs / DependencyInjectionExtensions | API/Program.cs and API/StartupConfig/DependencyInjectionExtensions.cs | Register controllers, authentication, and data access |
| AuthenticationController | API/Controllers/AuthenticationController.cs | Validate a POS machine credential; issue a 10-minute JWT |
| TodosController | API/Controllers/VouchersController.cs | Receive JSON, use the authenticated till, call data methods, return HTTP responses |
| ITodoData / TodoData | Data/Repositories/IPosVoucherData.cs and PosVoucherData.cs | Interface and implementation, calling named procedures |
| ISqlDataAccess / SqlDataAccess | Data/Sql/IDataAccess.cs and SqlDataAccess.cs | Existing Dapper connection and query code |
| SQL folder | Database/dbo/StoredProcedures/spPosVoucher_*.sql | Validate and persist the redemption atomically |

`API/` above means `VoucherSystemApp.API/`, `Data/` means `TicketVoucherSystem.Data/`, and `Database/` means `TicketVoucherSystem.Database/`. There are no minimal-API voucher handlers. The only minimal health endpoint reports that the process is running, not database readiness.

## Version 1 business rules

- ZAR, fixed-value, single-use vouchers. One voucher per sale, scoped by POS client and transaction ID.
- The authenticated client identifies the outlet, till, and department. The POS cannot supply a different department or outlet in its request.
- A ticket can contain separate Retail and FoodAndBeverage benefits. Only the authenticated department's benefit is looked up or redeemed.
- `saleTotal` is the amount due before this voucher, after other discounts. `eligibleTotal` is the eligible merchandise portion of that total. For an all-food basket they can both be 1500.
- A R300 voucher requires eligible purchases of at least R300. No partial spend, cash change, or retained balance. A R200 eligible basket is rejected, not silently rounded up or forfeited.
- The discount is read from the issued voucher in SQL Server, never supplied by the POS. The API returns `discountAmount = 300` and `amountDue = 1200` for a R1500 basket.
- Lookup is a preview and does not reserve the voucher. Redemption rechecks availability and expiry under a database lock.
- All timestamps are UTC. Monetary amounts have at most two decimal places.
- The POS is responsible for the true eligible total, cashier identity, tax treatment, receipt printing, and collecting payment. This API does not read the POS basket or charge the customer.

These defaults must be agreed with the business and each POS integration partner before production.

## 1. Database setup

For an existing current voucher database, run the additive upgrade (take the normal deployment backup first):

```powershell
sqlcmd -S "YOUR_SERVER" -d "YOUR_DATABASE" -E -b -i ".\TicketVoucherSystem.Database\Scripts\Upgrade-POS-API.sql"
```

This adds `dbo.PosRedemption` plus lookup/redemption procedures. It does not delete issued vouchers. The script can be rerun. For a fresh database use the existing `Scripts/Deploy.sql` from its Scripts directory. The SQL project also includes the new objects for SSDT publishing. Do not use EF migrations for voucher tables.

Use separate SQL identities for the web app and API. The API identity only needs EXECUTE on `dbo.spPosVoucher_Lookup` and `dbo.spPosVoucher_Redeem`; grant those specific permissions after creating the database user. It needs no direct table writes. Remove obsolete redemption procedure permissions from the web identity as part of cutover. Previous database procedures may still exist after an additive upgrade; no web route or web repository method calls them.

## 2. Configure the API in development

The API deliberately refuses to start without a connection string, a signing key of at least 32 UTF-8 bytes, and valid client credentials. Do not use the examples below as real credentials. Configure secrets for **VoucherSystemApp.API**, not the Razor Pages project.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=YOUR_DATABASE;Integrated Security=true;Encrypt=true;TrustServerCertificate=true" --project VoucherSystemApp.API
dotnet user-secrets set "Authentication:Issuer" "VoucherSystemApp.API" --project VoucherSystemApp.API
dotnet user-secrets set "Authentication:Audience" "VoucherPOS" --project VoucherSystemApp.API
```

Use the helper to generate strong machine credentials locally (PowerShell 7):

```powershell
.\scripts\New-PosClientSecret.ps1
```

It prints a random `Secret` and its `SecretHash`. Give the secret securely to the POS; store only its hash in the API settings. Generate a separate random value for `Authentication:SecretKey` (the JWT signing secret); never give that signing key to the POS.

```powershell
dotnet user-secrets set "Authentication:SecretKey" "PASTE_A_SEPARATELY_GENERATED_RANDOM_SECRET" --project VoucherSystemApp.API
dotnet user-secrets set "Authentication:Clients:0:ClientId" "micros-fnb-01" --project VoucherSystemApp.API
dotnet user-secrets set "Authentication:Clients:0:SecretHash" "PASTE_THE_64_CHARACTER_SHA256_HASH" --project VoucherSystemApp.API
dotnet user-secrets set "Authentication:Clients:0:TillId" "MICROS-01" --project VoucherSystemApp.API
dotnet user-secrets set "Authentication:Clients:0:OutletId" "1" --project VoucherSystemApp.API
dotnet user-secrets set "Authentication:Clients:0:DepartmentType" "FoodAndBeverage" --project VoucherSystemApp.API
```

Replace outlet 1 with an actual active outlet ID from your database. For HRK, configure another client using index `1`, a separate secret, an actual outlet ID, and `Retail` or `FoodAndBeverage`. Client IDs must be lowercase letters, digits, or hyphens; use a permanent unique client per till. Do not recycle transaction IDs or client IDs. Secret hashes assume high-entropy random machine secrets, not memorable human passwords.

```powershell
dotnet dev-certs https --trust
dotnet run --project VoucherSystemApp.API --no-launch-profile --urls "https://localhost:7240" --environment Development
```

Use `VoucherSystemApp.API.http` or import `docs/POS-API.postman_collection.json` into Postman. Enter the local secret only in your private Postman environment; do not commit it. The native OpenAPI JSON is at `/openapi/v1.json` in Development. There is no Swagger UI installed.

## 3. Authenticate

`POST https://localhost:7240/api/auth/token`

```json
{ "userName": "micros-fnb-01", "password": "YOUR_RANDOM_POS_MACHINE_SECRET" }
```

Response:

```json
{ "accessToken": "eyJ...", "tokenType": "Bearer", "expiresIn": 600 }
```

Send `Authorization: Bearer <accessToken>` and `Content-Type: application/json` to the next two endpoints. Use HTTPS with valid certificate validation. Tokens last 10 minutes; obtain a new token after expiry and retain the original redemption request if retrying. These are service credentials, not the admin app's Identity accounts.

## 4. Look up the voucher

`POST /api/pos/v1/vouchers/lookup`

```json
{ "voucherCode": "1605729254", "saleTotal": 1500.00, "eligibleTotal": 1500.00, "currency": "ZAR" }
```

Example response for an issued R300 voucher (IDs, name, expiry are examples):

```json
{
  "voucherId": 42, "voucherCode": "1605729254", "packageName": "Food R300",
  "departmentType": "FoodAndBeverage", "voucherAmount": 300.00, "currency": "ZAR",
  "status": "Issued", "expiresUtc": "2026-12-01T12:00:00Z", "canRedeem": true,
  "reason": null, "saleTotal": 1500.00, "eligibleTotal": 1500.00,
  "discountAmount": 300.00, "amountDue": 1200.00
}
```

The operator displays the voucher value and status. `canRedeem` is false for used, cancelled, expired, or too-small eligible baskets; lookup still returns 200 for a found voucher so the operator can see why it is unavailable. A missing barcode/department returns 404. No Webtickets upstream call is needed here: the admin app has already validated and issued the benefit.

## 5. Redeem once

`POST /api/pos/v1/vouchers/redeem`

```json
{
  "voucherCode": "1605729254", "saleTotal": 1500.00, "eligibleTotal": 1500.00,
  "currency": "ZAR", "idempotencyKey": "8dc21e5d-834f-4ae9-9260-f9602c6b1f39",
  "transactionId": "MICROS-20261029-000123", "operatorId": "EMP-42"
}
```

The POS creates a fresh UUID **once for this sale's redemption** and persists the full request before sending it. The server returns a receipt including `id`, `voucherId`, `transactionId`, `discountAmount`, `saleTotal`, `eligibleTotal`, `amountDue`, `currency`, `redeemedUtc`, `outletId`, `tillId`, `operatorId`, and `isReplay`.

After a successful response, the POS applies the R300 voucher benefit exactly once to that sale and collects the R1200 balance. Persist the returned receipt ID against the POS sale. Each vendor must decide the correct discount/tender mapping and tax treatment with your finance team.

### Lost responses and double clicks

If the response times out, the voucher may already be redeemed. Retry the **identical body with the same key and authenticated client**. A saved success returns 200 with the original receipt ID and amounts and `isReplay: true`. Do not apply the benefit again. Never create a new key to recover from a timeout.

Using the same key for changed amounts, a different voucher, sale, cashier, or till is 409. Using another key for the same completed sale is also 409. Another till trying the same voucher gets `already_redeemed`. Re-authentication is allowed during retry, provided it is still the same configured client.

Redemption is final in this version. Payment failure, abandoned sales, refunds and voids need a supervisor reconciliation procedure; there is no automatic reservation, reversal, or payment rollback endpoint. Agree the point in the MICROS/HRK sale lifecycle at which to redeem and how failed sales are recovered **before going live**. Never reset a redeemed voucher manually while a POS retry may still be in flight. If either vendor requires reserve/commit/void, that is a separate workflow to implement and test.

## HTTP errors

| Status | Meaning / action |
| --- | --- |
| 400 | Invalid JSON, missing fields, invalid identifiers, currency, precision, or totals; fix request |
| 401 | Missing/invalid/expired token or wrong machine credentials; authenticate |
| 403 | Configured outlet inactive or insufficient token claims |
| 404 | Voucher absent for barcode and authenticated department |
| 409 | Already redeemed/cancelled, changed retry payload, or sale already used; do not apply discount |
| 410 | Voucher expired |
| 422 | Eligible total below full voucher value |
| 429 | Request limit reached; back off and retry the same redemption request |
| 500 / 503 / timeout | Outcome may be unknown; retry the original request/key, then reconcile if persistent |

Business errors have `code`, `message`, `traceId`. Framework validation and unexpected failures use Problem Details. Do not parse English messages for program decisions. Never log credentials, bearer tokens or whole request bodies.

## MICROS and HRK handoff

Give each integration partner this contract, its HTTPS test URL, its own client credentials, an issued test ticket, and the assigned department/outlet. Confirm the exact product/version and available extension or middleware mechanism with that vendor; no vendor SDK or proprietary connector is included here. An API by itself does not add a button to either POS.

Acceptance flow: authenticate → look up → operator confirms → redeem → apply returned amount once → collect balance → record receipt. Test wrong department, expired/cancelled/used vouchers, mixed eligible items, double click, two tills, lost response, token expiry, failed payment and cancelled sales. The POS transaction ID must be unique for the lifetime of that client, including business dates and terminal restarts.

## IIS: separate deployment

Publish the API separately from the admin app:

```powershell
dotnet publish VoucherSystemApp.API/VoucherSystemApp.API.csproj -c Release -r win-x64 --self-contained true -o ./artifacts/pos-api
```

Create a separate IIS site/application and application pool (No Managed Code), configure its own HTTPS binding and DNS, and copy only the API publish output to its folder. Configure production secrets as environment variables or an approved secret store. User secrets are Development-only. Use a trusted SQL certificate in production (`Encrypt=true;TrustServerCertificate=false`). Configure the API database identity with the two EXECUTE grants described above.

Nested environment variables use double underscores, for example `Authentication__SecretKey`, `Authentication__Clients__0__SecretHash`, and `ConnectionStrings__DefaultConnection`. Restart the pool after credential/configuration changes. Do not expose HTTP as a working integration URL; have IIS require TLS. Confirm the .NET ASP.NET Core Hosting Bundle is installed for IIS even when self-contained.

Cutover order: upgrade the database → configure/deploy/test the separate API → deploy the admin app removal → switch MICROS/HRK to the API host. The old web `/Operations/Redeem` and `/api/pos/v1/vouchers/redeem` routes are unavailable after the new web build is deployed (404, or 405 for an unmatched POST handled by framework fallback routing). Remove the old API-key configuration from the hosting environment. Existing role records and redemption history are retained; they grant no redemption route in the web app.

## Run the tests

```powershell
dotnet test VoucherSystemApp.API.Tests/VoucherSystemApp.API.Tests.csproj --filter "Category!=SqlServer"
```

HTTP tests use an in-memory web host and a fake database. Database tests use real SQL Server and should run only against a disposable database deployed from this repository:

```powershell
$env:POS_TEST_SQL = "Server=localhost;Database=VoucherTest;Integrated Security=true;TrustServerCertificate=true"
dotnet test VoucherSystemApp.API.Tests/VoucherSystemApp.API.Tests.csproj
```

SQL tests check real arithmetic, expiry/cancellation, department isolation, exact-value baskets, identical retry, changed payload, reused sale, simultaneous retries, competing tills, and one audit event. GitHub Actions deploys a disposable SQL Server database and runs both sets. The separate POS workflow also builds the admin project after removal.
