using PrimeBakesLibrary.Accounts.Masters.Models;

using Syncfusion.Drawing;

namespace PrimeBakesLibrary.Utils.Exports;

public enum InvoiceExportType
{
	PDF,
	Excel
}

public enum ReportExportType
{
	PDF,
	Excel
}

/// <summary>
/// Generic invoice header data that works with any transaction type
/// </summary>
internal class InvoiceData
{
	internal string TransactionNo { get; set; } = string.Empty;
	internal DateTime TransactionDateTime { get; set; }
	internal string ReferenceTransactionNo { get; set; } = string.Empty;
	internal DateTime? ReferenceDateTime { get; set; }
	internal decimal TotalAmount { get; set; }
	internal string Remarks { get; set; } = string.Empty;
	internal bool Status { get; set; } = true; // True = Active, False = Deleted
	/// <summary>
	/// Payment modes breakdown (e.g., "Cash" => 1000.00, "Card" => 500.00)
	/// </summary>
	internal Dictionary<string, decimal>? PaymentModes { get; set; }
	internal CompanyModel? Company { get; set; }
	internal LedgerModel? BillTo { get; set; }
	internal string InvoiceType { get; set; } = "INVOICE";
	internal string Outlet { get; set; } = string.Empty;
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
internal class InvoiceColumnSetting(string propertyName, string displayName, InvoiceExportType exportType, CellAlignment alignment = CellAlignment.Right,
	double? pdfWidth = null, double? excelWidth = null, string format = null, bool showOnlyIfHasValue = true)
{
	internal string PropertyName { get; set; } = propertyName;
	internal string DisplayName { get; set; } = displayName;
	internal InvoiceExportType ExportType { get; set; } = exportType;
	internal CellAlignment Alignment { get; set; } = alignment;
	internal double? PDFWidth { get; set; } = pdfWidth;
	internal double? ExcelWidth { get; set; } = excelWidth;
	internal string Format { get; set; } = format;
	internal bool ShowOnlyIfHasValue { get; set; } = showOnlyIfHasValue;
}

/// <summary>
/// Column configuration for report exports (unified for PDF and Excel)
/// </summary>
internal class ReportColumnSetting
{
	internal string DisplayName { get; set; }
	internal CellAlignment Alignment { get; set; } = CellAlignment.Right;
	internal double? PDFWidth { get; set; }
	internal double? ExcelWidth { get; set; } = 15;
	internal string Format { get; set; }
	internal bool IncludeInTotal { get; set; } = true;
	internal bool HighlightNegative { get; set; } = false;
	internal bool IsRequired { get; set; } = false;
	internal bool IsGrandTotal { get; set; } = false;
	internal Func<object, ReportFormatInfo> FormatCallback { get; set; }
}

/// <summary>
/// Format information for report cell formatting
/// </summary>
internal class ReportFormatInfo
{
	internal Color? FontColor { get; set; }
	internal bool Bold { get; set; }
	internal string FormattedText { get; set; }
}