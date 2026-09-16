CREATE TABLE dbo.TicketVoucherIssue
(
    Id                  bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketVoucherIssue PRIMARY KEY,
    BatchId             uniqueidentifier NOT NULL CONSTRAINT DF_TicketVoucherIssue_BatchId DEFAULT (NEWSEQUENTIALID()),
    TicketPackageId     int NOT NULL,
    TicketNumber        varchar(32) NOT NULL,
    IssuedUtc           datetime2(0) NOT NULL CONSTRAINT DF_TicketVoucherIssue_IssuedUtc DEFAULT (SYSUTCDATETIME()),
    IssuedBy            nvarchar(256) NOT NULL,
    CONSTRAINT FK_TicketVoucherIssue_TicketPackage FOREIGN KEY (TicketPackageId) REFERENCES dbo.TicketPackage(Id),
    CONSTRAINT UQ_TicketVoucherIssue_TicketNumber UNIQUE (TicketNumber)
);
GO
CREATE INDEX IX_TicketVoucherIssue_BatchId ON dbo.TicketVoucherIssue (BatchId, Id);
