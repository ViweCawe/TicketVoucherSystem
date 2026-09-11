CREATE TABLE dbo.TicketVoucherIssue
(
    Id                  bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketVoucherIssue PRIMARY KEY,
    TicketPackageId     int NOT NULL,
    TicketNumber        varchar(32) NOT NULL,
    IssuedUtc           datetime2(0) NOT NULL CONSTRAINT DF_TicketVoucherIssue_IssuedUtc DEFAULT (SYSUTCDATETIME()),
    IssuedBy            nvarchar(256) NOT NULL,
    CONSTRAINT FK_TicketVoucherIssue_TicketPackage FOREIGN KEY (TicketPackageId) REFERENCES dbo.TicketPackage(Id),
    CONSTRAINT UQ_TicketVoucherIssue_TicketNumber UNIQUE (TicketNumber)
);
