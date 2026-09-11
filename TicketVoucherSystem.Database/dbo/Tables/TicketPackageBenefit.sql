CREATE TABLE dbo.TicketPackageBenefit
(
    TicketPackageId     int NOT NULL,
    VoucherPackageId    int NOT NULL,
    DisplayOrder        int NOT NULL CONSTRAINT DF_TicketPackageBenefit_DisplayOrder DEFAULT (0),
    CONSTRAINT PK_TicketPackageBenefit PRIMARY KEY (TicketPackageId, VoucherPackageId),
    CONSTRAINT FK_TicketPackageBenefit_TicketPackage FOREIGN KEY (TicketPackageId) REFERENCES dbo.TicketPackage(Id),
    CONSTRAINT FK_TicketPackageBenefit_VoucherPackage FOREIGN KEY (VoucherPackageId) REFERENCES dbo.VoucherPackage(Id)
);
