# Ticket Voucher System

A small ASP.NET Core Razor Pages application for issuing, exporting, tracking, and redeeming retail and food-and-beverage vouchers.

## What is included

- Voucher package administration
- Single and batch voucher issuance
- Downloadable Code 128 barcode files (`.svg`) and batch ZIP export
- Paired 10-digit retail and F&B barcode import
- Separate retail and F&B redemption screens
- Atomic, one-time redemption in SQL Server
- Admin voucher register with search, status, redemption details, and event history
- Admin cancellation with an audit reason
- ASP.NET Core Identity and an `Admin` role

## Structure

```text
TicketVoucherSystemApp/           Existing Razor Pages and Identity application
TicketVoucherSystemApp/Vouchers/  Models, interfaces, validation, and Dapper access
TicketVoucherSystemApp.Tests/     Focused validation and browser checks
database/Tables/                  SQL Server tables and indexes
database/Views/                   Read model used by stored procedures
database/StoredProcedures/        All voucher data access
database/Seed/                    Starter voucher packages
```

The implementation extends the Razor Pages and Individual Accounts project already in this repository. Voucher code is grouped under one feature folder so there is only one web project and one Identity setup.

## Setup

Prerequisites: .NET 10 SDK, SQL Server 2019 or newer, and `sqlcmd` or SQL Server Management Studio.

1. Create an empty database named `TicketVoucherSystem`.
2. Point `DefaultConnection` at that database without committing the connection string:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=TicketVoucherSystem;Trusted_Connection=True;TrustServerCertificate=True" --project TicketVoucherSystemApp
   ```

3. Apply the existing Identity migration already committed with the Razor Pages project:

   ```powershell
   dotnet tool install --global dotnet-ef --version 10.0.9
   dotnet ef database update --project TicketVoucherSystemApp
   ```

4. From the `database` folder, run the voucher schema in SQLCMD mode:

   ```powershell
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d TicketVoucherSystem -E -i Deploy.sql
   ```

5. Run the application:

   ```powershell
   dotnet run --project TicketVoucherSystemApp
   ```

6. Register the first user at `/Identity/Account/Register`, then configure that email and restart once to grant the Admin role:

   ```powershell
   dotnet user-secrets set "Admin:Email" "admin@example.com" --project TicketVoucherSystemApp
   ```

Do not store production passwords or connection strings in `appsettings.json`.

## Verification

```powershell
dotnet build TicketVoucherSystem.slnx
dotnet test TicketVoucherSystem.slnx
```

For database verification, attempt two simultaneous redemptions of the same test voucher. Exactly one must succeed; the other must return “already redeemed.”

## IIS folder deployment

Install the .NET 10 Hosting Bundle on the Windows Server before creating the IIS site. The application uses a framework-dependent deployment and the IIS in-process hosting model.

From a machine with the .NET 10 SDK, create the deployment folder:

```powershell
.\Publish-IisFolder.ps1 -Destination 'C:\inetpub\TicketVoucherSystem'
```

Run PowerShell as Administrator to create or update the IIS app pool and site:

```powershell
.\Configure-Iis.ps1 `
  -PhysicalPath 'C:\inetpub\TicketVoucherSystem' `
  -ConnectionString 'Server=SQL01;Database=TicketVoucherSystem;Integrated Security=True;TrustServerCertificate=True' `
  -AdminEmail 'admin@example.com' `
  -Port 8080
```

The setup script:

- creates a `No Managed Code` application pool;
- assigns the publish folder to the IIS site;
- grants the app-pool identity read and execute permission;
- stores the connection string and environment name in IIS app-pool configuration;
- restarts the pool and starts the site.

After registering the email supplied through `-AdminEmail`, recycle the application pool once. The startup initializer will then grant that existing account the `Admin` role.

For Integrated Security, grant the SQL Server database permissions to `IIS AppPool\TicketVoucherSystem` when IIS and SQL Server are on the same machine. For a remote SQL Server, run the app pool under an approved domain service account or use a securely managed SQL credential.

Bind an HTTPS certificate in IIS before production use. Do not expose the HTTP-only example port publicly.

## Automated validation

The included GitHub Actions workflow provisions the dependencies that this restricted workspace cannot download. On every push or pull request it:

- installs .NET 10 and builds/tests the solution;
- starts SQL Server 2022 and deploys every table, view, procedure, and seed record;
- applies the existing ASP.NET Core Identity migration;
- publishes an artifact named `TicketVoucherSystem-IIS`;
- starts the real application, signs in, and captures desktop/mobile Chromium screenshots.

The IIS artifact can be downloaded from a successful workflow run and extracted directly into the configured IIS folder.

## Deployment order

1. Back up the target database.
2. Apply the existing Identity migration.
3. Apply `database/Deploy.sql` to the new database.
4. Deploy the generated IIS folder with its connection string and admin email in IIS app-pool configuration.
5. Issue and redeem a low-value test voucher before enabling operator access.
