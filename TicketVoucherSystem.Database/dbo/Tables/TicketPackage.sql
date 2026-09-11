CREATE TABLE dbo.TicketPackage
(
    Id              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketPackage PRIMARY KEY,
    Name            nvarchar(80) NOT NULL,
    Description     nvarchar(200) NOT NULL CONSTRAINT DF_TicketPackage_Description DEFAULT (N''),
    IsActive        bit NOT NULL CONSTRAINT DF_TicketPackage_IsActive DEFAULT (1),
    CreatedUtc      datetime2(0) NOT NULL CONSTRAINT DF_TicketPackage_CreatedUtc DEFAULT (SYSUTCDATETIME()),
    CreatedBy       nvarchar(256) NOT NULL,
    CONSTRAINT UQ_TicketPackage_Name UNIQUE (Name)
);
