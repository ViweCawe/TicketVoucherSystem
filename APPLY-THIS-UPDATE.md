# Apply the group issuance and package management update

This overlay targets `ViweCawe/TicketVoucherSystem` main commit `fa68eae` or later.

1. Back up the SQL database and repository folder.
2. Extract this package over the repository root and replace matching files.
3. From the repository root, run the database upgrade:

   ```powershell
   sqlcmd -S "YOUR_SERVER" -d "YOUR_DATABASE" -E -b `
     -i ".\TicketVoucherSystem.Database\Scripts\Upgrade-GroupIssuance-Packages.sql"
   ```

4. Build and run the tests:

   ```powershell
   dotnet build .\TicketVoucherSystem.slnx
   dotnet test .\TicketVoucherSystemApp.Tests\TicketVoucherSystemApp.Tests.csproj
   ```

5. Confirm `/Admin/Packages` can create a package with Retail, F&B, or both benefits.
6. Confirm `/Operations/Issue` can select that package and issue two test barcodes together.
7. Confirm the resulting page prints two separate ticket sheets.

Commit after the database script and tests succeed:

```powershell
git add .
git commit -m "Add managed packages and group ticket issuance"
git push origin main
```
