using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Utils;

public enum InvoiceExportType
{
    PDF,
    Excel
}

/// <summary>
/// Generic invoice header data that works with any transaction type
/// </summary>
public class InvoiceData
{
    public string TransactionNo { get; set; } = string.Empty;
    public DateTime TransactionDateTime { get; set; }
    public string ReferenceTransactionNo { get; set; } = string.Empty;
    public DateTime? ReferenceDateTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public bool Status { get; set; } = true; // True = Active, False = Deleted
    /// <summary>
    /// Payment modes breakdown (e.g., "Cash" => 1000.00, "Card" => 500.00)
    /// </summary>
    public Dictionary<string, decimal>? PaymentModes { get; set; }
    public CompanyModel? Company { get; set; }
    public LedgerModel? BillTo { get; set; }
    public string InvoiceType { get; set; } = "INVOICE";
    public string Outlet { get; set; } = string.Empty;
}

public enum CellAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Column configuration for invoice line items table
/// </summary>
public class InvoiceColumnSetting(string propertyName, string displayName, InvoiceExportType exportType, CellAlignment alignment = CellAlignment.Right,
    double? pdfWidth = null, double? excelWidth = null, string format = null, bool showOnlyIfHasValue = true)
{
    public string PropertyName { get; set; } = propertyName;
    public string DisplayName { get; set; } = displayName;
    public InvoiceExportType ExportType { get; set; } = exportType;
    public CellAlignment Alignment { get; set; } = alignment;
    public double? PDFWidth { get; set; } = pdfWidth;
    public double? ExcelWidth { get; set; } = excelWidth;
    public string Format { get; set; } = format;
    public bool ShowOnlyIfHasValue { get; set; } = showOnlyIfHasValue;
}