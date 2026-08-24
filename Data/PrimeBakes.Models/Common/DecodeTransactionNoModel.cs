namespace PrimeBakes.Models.Common;

public enum CodeType
{
	Accounting,
	Ledger,

	Purchase,
	PurchaseOrder,
	PurchaseReturn,
	KitchenIssue,
	KitchenIssueReturn,
	KitchenProductionReturn,
	KitchenProduction,
	RawMaterial,

	Order,
	Sale,
	SaleReturn,
	StockTransfer,
	FinishedProduct,

	Bill,

	Employee,
	Department,
	Designation,
	Payroll,
}

public class DecodeTransactionNoModel
{
	public object TransactionModel { get; set; }
	public CodeType CodeType { get; set; }
	public string PageRouteName { get; set; }
	public (MemoryStream stream, string fileName) PDFStream { get; set; }
	public (MemoryStream stream, string fileName) ExcelStream { get; set; }
}

public class DecodeTransactionNoResult
{
	public CodeType CodeType { get; set; }
	public string PageRouteName { get; set; }
	public FileResult Pdf { get; set; }
	public FileResult Excel { get; set; }
}

public class FileResult
{
	public byte[] Bytes { get; set; }
	public string FileName { get; set; }
}