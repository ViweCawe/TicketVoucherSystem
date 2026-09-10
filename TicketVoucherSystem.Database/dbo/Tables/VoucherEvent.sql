CREATE TABLE dbo.VoucherEvent
(
    Id           bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_VoucherEvent PRIMARY KEY,
    VoucherId    bigint NOT NULL,
    EventType    varchar(30) NOT NULL,
    EventUtc     datetime2(0) NOT NULL CONSTRAINT DF_VoucherEvent_EventUtc DEFAULT (SYSUTCDATETIME()),
    PerformedBy  nvarchar(256) NOT NULL,
    Location     nvarchar(120) NULL,
    Notes        nvarchar(500) NULL,
    CONSTRAINT FK_VoucherEvent_Voucher FOREIGN KEY (VoucherId) REFERENCES dbo.Voucher(Id),
    CONSTRAINT CK_VoucherEvent_Type CHECK (EventType IN ('Issued', 'Imported', 'Redeemed', 'Expired', 'Cancelled'))
);
GO
CREATE INDEX IX_VoucherEvent_VoucherId_EventUtc
    ON dbo.VoucherEvent (VoucherId, EventUtc DESC);
