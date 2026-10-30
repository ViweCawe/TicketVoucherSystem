# Voucher lookup and redemption with Basic authentication

The separate `VoucherSystemApp.API` project exposes two business endpoints. HTTP Basic authentication is required for both operations and /health. No bearer token or token endpoint is used. No operator, outlet, till or transaction reference is required. HRK and MICROS retain transaction reporting. Only callers with the configured shared integration username and password can access voucher operations.

## Test locally

1. Apply `TicketVoucherSystem.Database/Scripts/Upgrade-POS-API.sql` to your existing voucher database using SQLCMD or SSMS. Existing vouchers and historic receipts are preserved; the new API no longer writes POS receipt records.
2. Configure `ConnectionStrings:DefaultConnection` for `VoucherSystemApp.API` using user secrets or environment variables. Configure BasicAuth:Username and BasicAuth:Password in the API's secrets store.
3. Run `dotnet run --project VoucherSystemApp.API --no-launch-profile --urls https://localhost:7240 --environment Development`.
4. Open `https://localhost:7240/swagger`, or import `POS-API.postman_collection.json`. Choose **Basic Auth** in Postman and enter the configured username and password. In Swagger, click **Authorize**, enter those credentials, then test either operation.
5. Issue a voucher through the admin app, then send its exact barcode and department to lookup and redeem.

Both endpoints accept the same JSON:

```json
{"voucherCode":"1605729254","departmentType":"FoodAndBeverage"}
```

`departmentType` is `Retail` or `FoodAndBeverage`. It selects the benefit because one barcode can contain both.

| Method | Path | Behavior |
|---|---|---|
| POST | /api/pos/v1/vouchers/lookup | Returns amount, status, isValid, isRedeemed, isExpired and expiry. Does not change the voucher. |
| POST | /api/pos/v1/vouchers/redeem | Marks an issued, unexpired voucher Redeemed atomically and returns its value and redemption time. |

Lookup returns 200 for found vouchers even when redeemed, expired or cancelled, with the corresponding flags. Missing vouchers return 404. Redemption returns 200 on success, 409 for already redeemed/cancelled, 410 for expired, 404 for absent, and 400 for invalid input. A second redemption cannot succeed, even from concurrent callers. Errors contain `code` and `message`.

For a R300 voucher, the response includes `voucherAmount: 300` and `currency: ZAR`. HRK/MICROS applies that discount to its sale and collects the difference (R1200 on R1500). No basket, payment or cashier data is sent to this API. Voucher redemption is full and single-use. No payment processing, reservation, refund or reversal endpoint is included.

A timeout has an unknown outcome: use lookup to check voucher status and reconcile with the POS report. A repeated redeem returns 409 if the first attempt succeeded. Lookup is not a reservation.

## Maintain the code

`VouchersController` receives JSON → `IPosVoucherData` / `PosVoucherData` calls existing Dapper `IDataAccess` → `spPosVoucher_Lookup` or `spPosVoucher_Redeem` runs SQL. The redemption procedure locks the voucher and updates status with one audit event in a single transaction. The legacy audit table requires a PerformedBy value, so it stores the fixed system label `Public POS API`, not a customer, cashier or till identity. The admin app remains separate and contains no redemption screen.

Publish this API as its own IIS application using HTTPS. Enable IIS Anonymous Authentication for it; ASP.NET Core validates Basic credentials, so do not enable IIS's own Basic or Windows authentication for this application. Configure the database connection and BasicAuth credentials. Swagger is available in Development. `/health` is an infrastructure database health check, not a third voucher operation. Update both procedures before deploying the new API because their signatures have changed.

Run `dotnet test VoucherSystemApp.API.Tests/VoucherSystemApp.API.Tests.csproj`. SQL tests require the disposable SQL Server connection in `POS_TEST_SQL`; HTTP-only tests use `--filter Category!=SqlServer`.

## Configure the shared Basic credentials

In Visual Studio, right-click VoucherSystemApp.API > Manage User Secrets, or run:

```powershell
dotnet user-secrets set "BasicAuth:Username" "YOUR_POS_USERNAME" --project VoucherSystemApp.API
dotnet user-secrets set "BasicAuth:Password" "YOUR_STRONG_SHARED_PASSWORD" --project VoucherSystemApp.API
```

Use an actual strong password, not the illustrative UserName/Password values. The API refuses to start when either setting is missing. Never commit live credentials. Share the selected pair securely with HRK and MICROS; their server-side client configuration or secret store holds the same pair. Do not embed it in browser JavaScript or a public client bundle. No cashier-specific login is introduced.

Production IIS settings use BasicAuth__Username and BasicAuth__Password. Recycle the application pool after changing credentials. Both vendors must update their stored credentials when you rotate this shared pair. The credentials are separate from Webtickets credentials and the admin app's Identity accounts.

Every HTTPS request sends Authorization: Basic BASE64(username:password). Base64 is encoding, not encryption, so TLS certificate validation is required. Missing, malformed or incorrect credentials return HTTP 401 with WWW-Authenticate: Basic. A bearer token or API key is not accepted. The request body and database procedures are unchanged; this update requires no SQL migration. Development Swagger documentation loads without credentials so its Authorize dialog is usable; actual voucher requests still require authentication. Existing PDFs describing public access are superseded by these instructions.
