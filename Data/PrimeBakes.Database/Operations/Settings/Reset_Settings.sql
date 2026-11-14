CREATE PROCEDURE [dbo].[Reset_Settings]
AS
BEGIN
	DELETE FROM [Settings]

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PrimaryCompanyLinkingId'			, N'1'		, N'Company Id for the Primary Company Account')

	-- Code Prefixes
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'RawMaterialCodePrefix'			, N'RM'		, N'Added to the start of every new raw material code')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'FinishedProductCodePrefix'		, N'FP'		, N'Added to the start of every new finished product code')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'LedgerCodePrefix'				, N'LD'		, N'Added to the start of every new ledger code')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'EmployeeCodePrefix'			, N'EMP'	, N'Added to the start of every new employee code')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'DepartmentCodePrefix'			, N'DEPT'	, N'Added to the start of every new department code')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'DesignationCodePrefix'		, N'DESG'	, N'Added to the start of every new designation code')
	
	-- Transaction Prefixes
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'AccountingTransactionPrefix'	, N'ACCT'	, N'Added to the start of every new accounting transaction number')

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseTransactionPrefix'		, N'PUR'	, N'Added to the start of every new purchase transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseReturnTransactionPrefix'	, N'PURRET'	, N'Added to the start of every new purchase return transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseOrderTransactionPrefix'	, N'PURORD'	, N'Added to the start of every new purchase order transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'KitchenIssueTransactionPrefix'		, N'KITIS'	, N'Added to the start of every new kitchen issue transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'KitchenIssueReturnTransactionPrefix'	, N'KITISRET'	, N'Added to the start of every new kitchen issue return transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'KitchenProductionTransactionPrefix'		, N'KITPRD'	, N'Added to the start of every new kitchen production transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'KitchenProductionReturnTransactionPrefix'	, N'KITPRDRET'	, N'Added to the start of every new kitchen production return transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'RawMaterialStockAdjustmentTransactionPrefix'	, N'RMADJ'	, N'Added to the start of every new raw material stock adjustment transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'ProductStockAdjustmentTransactionPrefix' 	, N'PRDADJ'	, N'Added to the start of every new product stock adjustment transaction number')
	
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleTransactionPrefix' 		, N'SAL'	, N'Added to the start of every new sale transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleReturnTransactionPrefix'		, N'SALRET'	, N'Added to the start of every new sale return transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'StockTransferTransactionPrefix'	, N'STTRF'	, N'Added to the start of every new stock transfer transaction number')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'OrderTransactionPrefix'		, N'ORD'	, N'Added to the start of every new order transaction number')

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'BillTransactionPrefix' 		, N'BL'	, N'Added to the start of every new bill transaction number')

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PayrollTransactionPrefix' 		, N'PAY'	, N'Added to the start of every new payroll transaction number')

	-- Vouchers
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseVoucherId', N'4', N'Voucher type for Purchase transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseReturnVoucherId', N'6', N'Voucher type for Purchase Return transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleVoucherId', N'3', N'Voucher type for Sales transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleReturnVoucherId', N'5', N'Voucher type for Sale Return transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'StockTransferVoucherId', N'7', N'Voucher type for Stock Transfer transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'BillVoucherId', N'9', N'Voucher type for Bill transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'BillDayCloseVoucherId', N'10', N'Voucher type for Bill Day Close transactions')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleDayCloseVoucherId', N'11', N'Voucher type for Sale Day Close transactions')

	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'DefaultSelectedVoucherId', N'8', N'Default selected voucher type in transactions')
	
	-- Ledgers
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'PurchaseLedgerId', N'155', N'Ledger account for Purchase entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'SaleLedgerId', N'144', N'Ledger account for Sales entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'StockTransferLedgerId', N'156', N'Ledger account for Stock Transfer entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'BillLedgerId', N'144', N'Ledger account for Bill entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'CashLedgerId', N'1', N'Cash ledger account for Cash Entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'CashSalesLedgerId', N'170', N'Cash ledger account for Cash Sales Entries')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'GSTLedgerId', N'1751', N'GST ledger account for GST Tax Entries')

	-- Bank Reconciliation
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'BankAccountTypeId', N'2', N'Account Type that identifies Bank ledgers for reconciliation')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'ShowBRSFields', N'false', N'When on, the Accounting Entry page shows reference and instrument fields for bank reconciliation')

	-- Purchase Behavior
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'UpdateRawMaterialMasterRateOnPurchase'	, N'true'	, N'When on, saving a purchase updates the raw material master rate')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'UpdateRawMaterialMasterUOMOnPurchase'	, N'true'	, N'When on, saving a purchase updates the raw material master unit of measurement')

	-- Kitchen Production
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'KitchenProductionDiscountRate', N'25', N'Discount rate for kitchen production')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'KitchenProductionReturnDiscountRate', N'25', N'Discount rate for kitchen production returns')

	-- Report Settings
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'AutoRefreshReportTimer', N'30', N'Auto refresh interval for reports in minutes')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'ReportWarningDays', N'30', N'Days threshold used to highlight due items in reports')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'HeavyQuerySpanDays', N'30', N'Reports covering more than this many days are treated as heavy reports')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'HeavyQueryMaxLoadPercent', N'50', N'Heavy reports are blocked while database load is above this percentage')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'LocationRadiusMeters', N'1000', N'Distance in metres within which a transaction counts as done on site')

	-- Login Settings
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'MaxLoginTimeHours', N'12', N'Hours after login before a user is signed out automatically')

	-- Notification Settings
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'NotificationEmail', N'primebakesght@gmail.com', N'Recipient email for transaction notifications; leave blank to disable emails')

	-- Cache Settings
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'CacheTimeoutMinutes', N'60', N'How long the API keeps master data cached, in minutes')

	-- Backup Settings
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'BackupReminderDays', N'7', N'Days after the last backup before the dashboard reminds an admin to run one')

	-- Dashboard Settings
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'ShowPageRecommendations', N'true', N'Show suggested pages on the dashboard, based on recent visits')
	INSERT INTO [dbo].[Settings] ([Key], [Value], [Description]) VALUES (N'RecommendationHistoryDays', N'5', N'Number of days of page visits used for dashboard recommendations')

	SELECT 1 AS Success;

END