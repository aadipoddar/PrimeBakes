namespace PrimeBakesLibrary.Models.Operations;

public enum CodeType
{
	Accounting,
	Ledger,
	Purchase,
	PurchaseReturn,
	KitchenIssue,
	KitchenProduction,
	RawMaterial,
	FinishedProduct,
	Order,
	Sale,
	SaleReturn,
	StockTransfer,
	Bill,
}

public class DecodeTransactionNoModel
{
	public object TransactionModel { get; set; }
	public CodeType CodeType { get; set; }
	public string PageRouteName { get; set; }
	public (MemoryStream stream, string fileName) PDFStream { get; set; }
	public (MemoryStream stream, string fileName) ExcelStream { get; set; }
}