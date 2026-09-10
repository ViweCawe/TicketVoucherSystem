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
TicketVoucherSystemApp/       Razor Pages, Identity, barcode export, and IIS
TicketVoucherSystem.Data/     Models, interfaces, Dapper access, and validation
TicketVoucherSystem.Database/ Tables, views, stored procedures, and seed data
TicketVoucherSystemApp.Tests/ Unit tests and browser checks
```

The structure follows the Utility Management approach without coupling the web application to database implementation details. The web project references the Data class library. The SQL project is deployed separately and has no runtime project reference.

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

4. Publish `TicketVoucherSystem.Database` from Visual Studio to create or update the voucher schema. For a new database, the fallback SQLCMD deployment is:

   ```powershell
   cd TicketVoucherSystem.Database\Scripts
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d TicketVoucherSystem -E -i Deploy.sql
   cd ..\..
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
dotnet build TicketVoucherSystemApp\TicketVoucherSystemApp.csproj
dotnet test TicketVoucherSystemApp.Tests\TicketVoucherSystemApp.Tests.csproj
```

On Windows with SQL Server Data Tools installed, build the complete solution in Visual Studio to validate the database project and generate its DACPAC.

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

- installs .NET 10 and builds the Web, Data, and Test projects;
- starts SQL Server 2022 and deploys every table, view, procedure, and seed record;
- applies the existing ASP.NET Core Identity migration;
- publishes an artifact named `TicketVoucherSystem-IIS`;
- starts the real application, signs in, and captures desktop/mobile Chromium screenshots.

The IIS artifact can be downloaded from a successful workflow run and extracted directly into the configured IIS folder.

## Deployment order

1. Back up the target database.
2. Apply the existing Identity migration.
3. Publish `TicketVoucherSystem.Database` to the target database.
4. Deploy the generated IIS folder with its connection string and admin email in IIS app-pool configuration.
5. Issue and redeem a low-value test voucher before enabling operator access.
