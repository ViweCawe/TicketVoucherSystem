CREATE TABLE dbo.Outlet
(
    Id              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Outlet PRIMARY KEY,
    Name            nvarchar(120) NOT NULL,
    Code            varchar(20) NOT NULL,
    IsActive        bit NOT NULL CONSTRAINT DF_Outlet_IsActive DEFAULT (1),
    DisplayOrder    int NOT NULL CONSTRAINT DF_Outlet_DisplayOrder DEFAULT (0),
    CreatedUtc      datetime2(0) NOT NULL CONSTRAINT DF_Outlet_CreatedUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Outlet_Name UNIQUE (Name),
    CONSTRAINT UQ_Outlet_Code UNIQUE (Code),
    CONSTRAINT CK_Outlet_DisplayOrder CHECK (DisplayOrder >= 0)
);
