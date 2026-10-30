# Public voucher lookup and redemption

The separate `VoucherSystemApp.API` project exposes two business endpoints. No authentication, authorization, token, API key, operator, outlet, till or transaction reference is required. HRK and MICROS retain transaction reporting. Anyone who can reach the API and knows a valid barcode and department can redeem it.

## Test locally

1. Apply `TicketVoucherSystem.Database/Scripts/Upgrade-POS-API.sql` to your existing voucher database using SQLCMD or SSMS. Existing vouchers and historic receipts are preserved; the new API no longer writes POS receipt records.
2. Configure `ConnectionStrings:DefaultConnection` for `VoucherSystemApp.API` using user secrets or environment variables. Authentication settings are no longer used.
3. Run `dotnet run --project VoucherSystemApp.API --no-launch-profile --urls https://localhost:7240 --environment Development`.
4. Open `https://localhost:7240/swagger`, or import `POS-API.postman_collection.json`. Choose **No Auth** in Postman.
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

Publish this API as its own IIS application using HTTPS. Enable IIS Anonymous Authentication for it; there is no application login requirement. Configure only the database connection. Swagger is available in Development. `/health` is an infrastructure database health check, not a third voucher operation. Update both procedures before deploying the new API because their signatures have changed.

Run `dotnet test VoucherSystemApp.API.Tests/VoucherSystemApp.API.Tests.csproj`. SQL tests require the disposable SQL Server connection in `POS_TEST_SQL`; HTTP-only tests use `--filter Category!=SqlServer`.
