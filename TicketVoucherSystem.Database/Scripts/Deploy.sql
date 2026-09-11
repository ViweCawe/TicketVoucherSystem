:r ../dbo/Tables/VoucherPackage.sql
GO
:r ../dbo/Tables/Outlet.sql
GO
:r ../dbo/Tables/TicketPackage.sql
GO
:r ../dbo/Tables/TicketPackageBenefit.sql
GO
:r ../dbo/Tables/TicketVoucherIssue.sql
GO
:r ../dbo/Tables/Voucher.sql
GO
:r ../dbo/Tables/VoucherEvent.sql
GO
:r ../dbo/Views/vVoucherDetails.sql
GO
:r ../dbo/StoredProcedures/spVoucherPackage_GetAll.sql
GO
:r ../dbo/StoredProcedures/spVoucherPackage_GetActive.sql
GO
:r ../dbo/StoredProcedures/spVoucherPackage_Save.sql
GO
:r ../dbo/StoredProcedures/spVoucherPackage_SetActive.sql
GO
:r ../dbo/StoredProcedures/spVoucher_Redeem.sql
GO
:r ../dbo/StoredProcedures/spVoucher_Cancel.sql
GO
:r ../dbo/StoredProcedures/spVoucher_GetRecent.sql
GO
:r ../dbo/StoredProcedures/spVoucher_Search.sql
GO
:r ../dbo/StoredProcedures/spVoucher_GetById.sql
GO
:r ../dbo/StoredProcedures/spVoucherEvent_GetByVoucherId.sql
GO
:r ../dbo/StoredProcedures/spVoucher_GetDashboard.sql
GO
:r ../dbo/StoredProcedures/spTicketPackage_GetActive.sql
GO
:r ../dbo/StoredProcedures/spTicketVoucher_Issue.sql
GO
:r ../dbo/StoredProcedures/spTicketVoucherIssue_GetById.sql
GO
:r ../dbo/StoredProcedures/spTicketVoucherIssue_GetVouchers.sql
GO
:r ../dbo/StoredProcedures/spOutlet_GetActive.sql
GO
:r ../dbo/StoredProcedures/spOutlet_GetAll.sql
GO
:r ../dbo/StoredProcedures/spOutlet_Save.sql
GO
:r ../dbo/StoredProcedures/spOutlet_SetActive.sql
GO
:r ../dbo/StoredProcedures/spReport_GetExecutiveSummary.sql
GO
:r ../dbo/StoredProcedures/spReport_GetVoucherTrend.sql
GO
:r ../dbo/StoredProcedures/spReport_GetDepartmentPerformance.sql
GO
:r ../dbo/StoredProcedures/spReport_GetOutletPerformance.sql
GO
:r ./PostDeployment.sql
GO
