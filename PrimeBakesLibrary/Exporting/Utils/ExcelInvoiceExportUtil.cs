using System.Reflection;
using System.Text.RegularExpressions;

using NumericWordsConversion;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Drawing;
using Syncfusion.XlsIO;

namespace PrimeBakesLibrary.Exporting.Utils;

public static class ExcelInvoiceExportUtil
{
    #region Generic Invoice Models

    /// <summary>
    /// Generic invoice header data that works with any transaction type
    /// </summary>
    public class InvoiceData
    {
        public string TransactionNo { get; set; }
        public DateTime TransactionDateTime { get; set; }
        public string ReferenceTransactionNo { get; set; }
        public DateTime? ReferenceDateTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string Remarks { get; set; }
        public bool Status { get; set; } = true; // True = Active, False = Deleted
        /// <summary>
        /// Payment modes breakdown (e.g., "Cash" => 1000.00, "Card" => 500.00)
        /// </summary>
        public Dictionary<string, decimal> PaymentModes { get; set; }
        public CompanyModel Company { get; set; }
        public LedgerModel BillTo { get; set; }
        public string InvoiceType { get; set; } = "INVOICE";
        public string Outlet { get; set; }
    }

    /// <summary>
    /// Column configuration for invoice line items table
    /// </summary>
    public class InvoiceColumnSetting(string propertyName, string displayName, double width,
        ExcelHAlign alignment = ExcelHAlign.HAlignRight, string format = null, bool showOnlyIfHasValue = true)
    {
        public string PropertyName { get; set; } = propertyName;
        public string DisplayName { get; set; } = displayName;
        public double Width { get; set; } = width;
        public ExcelHAlign Alignment { get; set; } = alignment;
        public string Format { get; set; } = format;
        public bool ShowOnlyIfHasValue { get; set; } = showOnlyIfHasValue;
    }

    #endregion

    #region Color Definitions

    private static readonly Color PrimaryBlue = Color.FromArgb(59, 130, 246);
    private static readonly Color HeaderBackground = Color.FromArgb(241, 245, 249);
    private static readonly Color BorderColor = Color.FromArgb(203, 213, 225);
    private static readonly Color AlternateRowColor = Color.FromArgb(249, 250, 251);
    private static readonly Color TotalRowBackground = Color.FromArgb(239, 246, 255);
    private static readonly Color DeletedBadgeColor = Color.FromArgb(220, 38, 38);
    private static readonly Color SuccessColor = Color.FromArgb(16, 185, 129);

    #endregion

    #region Public Methods

    /// <summary>
    /// Export invoice to Excel with professional layout (unified method for all transaction types)
    /// </summary>
    /// <typeparam name="T">Type of line item (must be a class)</typeparam>
    /// <param name="invoiceData">Generic invoice header data with all invoice information</param>
    /// <param name="lineItems">Generic invoice line items of any type</param>
    /// <param name="columnSettings">Optional: Custom column settings for line items table</param>
    /// <param name="columnOrder">Optional: Custom column order for line items table</param>
    /// <param name="summaryFields">Optional: Custom summary fields to display (key=label, value=formatted value)</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportInvoiceToExcel<T>(
        InvoiceData invoiceData,
        List<T> lineItems,
        List<InvoiceColumnSetting> columnSettings = null,
        List<string> columnOrder = null,
        Dictionary<string, string> summaryFields = null) where T : class
    {
        MemoryStream ms = new();

        try
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet worksheet = workbook.Worksheets[0];
            worksheet.Name = "Invoice";

            // Set document properties
            workbook.BuiltInDocumentProperties.Title = $"{invoiceData.InvoiceType} - {invoiceData.TransactionNo}";
            workbook.BuiltInDocumentProperties.Subject = invoiceData.InvoiceType;
            workbook.BuiltInDocumentProperties.Author = "PrimeBakes";

            int currentRow = 1;

            // 1. Header Section with Logo and Company Name
            currentRow = await DrawInvoiceHeader(worksheet, currentRow);

            // 2. Invoice Type and Number
            currentRow = DrawInvoiceTitle(worksheet, invoiceData.InvoiceType, invoiceData.TransactionNo, invoiceData.TransactionDateTime, currentRow, invoiceData.Outlet);

            // 2.5. Draw DELETED status badge if Status is false
            if (!invoiceData.Status)
                currentRow = DrawDeletedStatusBadge(worksheet, currentRow);

            // 3. Company and Customer Information (Two Columns)
            currentRow = DrawCompanyInfo(worksheet, invoiceData.Company, invoiceData.BillTo, invoiceData, currentRow);

            // Get column settings dynamically if not provided
            columnSettings ??= GetDefaultInvoiceColumnSettings<T>();

            // Determine column order
            List<string> effectiveColumnOrder = DetermineColumnOrder<T>(columnSettings, columnOrder);

            // Filter out empty columns
            effectiveColumnOrder = FilterEmptyColumns(lineItems, effectiveColumnOrder, columnSettings);

            // 4. Line Items Table
            currentRow = DrawLineItemsTableGeneric(worksheet, lineItems, effectiveColumnOrder, columnSettings, currentRow);

            // 5. Summary Section with Remarks
            currentRow = DrawSummaryAndRemarks(worksheet, invoiceData, summaryFields, currentRow);

            // 6. Amount in Words and Payment Methods
            currentRow = DrawAmountInWordsAndPayment(worksheet, invoiceData, currentRow);

            // 7. Branding Footer
            currentRow = await DrawBrandingFooter(worksheet, currentRow);

            // Apply final formatting
            ApplyFinalFormatting(worksheet);

            workbook.SaveAs(ms);
            ms.Position = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting invoice to Excel: {ex.Message}");
            throw;
        }

        return ms;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Draw invoice header with logo and company name
    /// </summary>
    private static async Task<int> DrawInvoiceHeader(IWorksheet worksheet, int startRow)
    {
        int currentRow = startRow;

        // Try to load and insert logo
        try
        {
            var possibleLogoPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "logo_full.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "wwwroot", "images", "logo_full.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo_full.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "PrimeBakes", "PrimeBakes", "wwwroot", "images", "logo_full.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "PrimeBakes", "PrimeBakes.Web", "wwwroot", "images", "logo_full.png")
            };

            string resolvedLogoPath = possibleLogoPaths.FirstOrDefault(File.Exists);

            if (!string.IsNullOrEmpty(resolvedLogoPath))
            {
                using FileStream imageStream = new(resolvedLogoPath, FileMode.Open, FileAccess.Read);
                IPictureShape logo = worksheet.Pictures.AddPicture(currentRow, 4, imageStream); // Column 4 for centering

                // Calculate logo dimensions maintaining aspect ratio
                double maxLogoHeight = 50; // Slightly bigger for better visibility
                double originalWidth = logo.Width;
                double originalHeight = logo.Height;
                double aspectRatio = originalWidth / originalHeight;

                // Scale proportionally based on max height
                logo.Height = (int)maxLogoHeight;
                logo.Width = (int)(maxLogoHeight * aspectRatio);

                currentRow += 4;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logo loading failed: {ex.Message}");
        }

        // Draw separator line (using bottom border on merged cells)
        worksheet.Range[currentRow, 1, currentRow, 10].Merge();
        worksheet.Range[currentRow, 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Medium;
        worksheet.Range[currentRow, 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Blue;
        currentRow++;

        return currentRow;
    }

    /// <summary>
    /// Draw invoice type and number
    /// </summary>
    private static int DrawInvoiceTitle(IWorksheet worksheet, string invoiceType, string invoiceNumber, DateTime transactionDateTime, int startRow, string outlet = null)
    {
        int currentRow = startRow;

        // Row 1: Invoice Type (left) and Invoice Number (right)
        worksheet.Range[currentRow, 1, currentRow, 5].Merge();
        worksheet.Range[currentRow, 1].Text = invoiceType.ToUpper();
        worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
        worksheet.Range[currentRow, 1].CellStyle.Font.Size = 14;
        worksheet.Range[currentRow, 1].CellStyle.Font.RGBColor = PrimaryBlue;

        worksheet.Range[currentRow, 6, currentRow, 10].Merge();
        worksheet.Range[currentRow, 6].Text = $"Invoice #: {invoiceNumber}";
        worksheet.Range[currentRow, 6].CellStyle.Font.Bold = true;
        worksheet.Range[currentRow, 6].CellStyle.Font.Size = 12;
        worksheet.Range[currentRow, 6].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
        currentRow++;

        // Row 2: Outlet (left) and Invoice Date (right)
        if (!string.IsNullOrWhiteSpace(outlet))
        {
            worksheet.Range[currentRow, 1, currentRow, 5].Merge();
            worksheet.Range[currentRow, 1].Text = $"Outlet: {outlet}";
            worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
            worksheet.Range[currentRow, 1].CellStyle.Font.Size = 10;
            worksheet.Range[currentRow, 1].CellStyle.Font.RGBColor = Color.FromArgb(100, 100, 100);
        }

        worksheet.Range[currentRow, 6, currentRow, 10].Merge();
        worksheet.Range[currentRow, 6].Text = $"Invoice Date: {transactionDateTime:dd-MMM-yyyy hh:mm tt}";
        worksheet.Range[currentRow, 6].CellStyle.Font.Size = 10;
        worksheet.Range[currentRow, 6].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
        currentRow += 2;

        return currentRow;
    }

    /// <summary>
    /// Draw DELETED status badge
    /// </summary>
    private static int DrawDeletedStatusBadge(IWorksheet worksheet, int startRow)
    {
        int currentRow = startRow;

        worksheet.Range[currentRow, 4, currentRow, 6].Merge();
        worksheet.Range[currentRow, 4].Text = "DELETED";
        worksheet.Range[currentRow, 4].CellStyle.Font.Bold = true;
        worksheet.Range[currentRow, 4].CellStyle.Font.Size = 14;
        worksheet.Range[currentRow, 4].CellStyle.Font.Color = ExcelKnownColors.White;
        worksheet.Range[currentRow, 4].CellStyle.Color = DeletedBadgeColor;
        worksheet.Range[currentRow, 4].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
        worksheet.Range[currentRow, 4].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
        worksheet.Range[currentRow, 4].RowHeight = 25;
        currentRow += 2;

        return currentRow;
    }

    /// <summary>
    /// Draw company and customer information in two columns
    /// </summary>
    private static int DrawCompanyInfo(IWorksheet worksheet, CompanyModel company, LedgerModel billTo, InvoiceData invoiceData, int startRow)
    {
        int leftRow = startRow;
        int rightRow = startRow;

        // FROM Section (Left Column)
        worksheet.Range[leftRow, 1].Text = "FROM:";
        worksheet.Range[leftRow, 1].CellStyle.Font.Bold = true;
        worksheet.Range[leftRow, 1].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
        leftRow++;

        // BILL TO Section (Right Column)
        if (billTo != null)
        {
            worksheet.Range[rightRow, 6].Text = "BILL TO:";
            worksheet.Range[rightRow, 6].CellStyle.Font.Bold = true;
            worksheet.Range[rightRow, 6].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
            rightRow++;
        }

        // Company Name (Left)
        worksheet.Range[leftRow, 1, leftRow, 4].Merge();
        worksheet.Range[leftRow, 1].Text = company.Name;
        worksheet.Range[leftRow, 1].CellStyle.Font.Bold = true;
        leftRow++;

        // Bill To Name (Right)
        if (billTo != null)
        {
            worksheet.Range[rightRow, 6, rightRow, 10].Merge();
            worksheet.Range[rightRow, 6].Text = billTo.Name;
            worksheet.Range[rightRow, 6].CellStyle.Font.Bold = true;
            rightRow++;
        }

        // Company Address (Left)
        if (!string.IsNullOrEmpty(company.Address))
        {
            worksheet.Range[leftRow, 1, leftRow, 4].Merge();
            worksheet.Range[leftRow, 1].Text = company.Address;
            worksheet.Range[leftRow, 1].WrapText = true;
            leftRow++;
        }

        // Bill To Address (Right)
        if (billTo != null && !string.IsNullOrEmpty(billTo.Address))
        {
            worksheet.Range[rightRow, 6, rightRow, 10].Merge();
            worksheet.Range[rightRow, 6].Text = billTo.Address;
            worksheet.Range[rightRow, 6].WrapText = true;
            rightRow++;
        }

        // Company Phone (Left)
        if (!string.IsNullOrEmpty(company.Phone))
        {
            worksheet.Range[leftRow, 1, leftRow, 4].Merge();
            worksheet.Range[leftRow, 1].Text = $"Phone: {company.Phone}";
            leftRow++;
        }

        // Bill To Phone (Right)
        if (billTo != null && !string.IsNullOrEmpty(billTo.Phone))
        {
            worksheet.Range[rightRow, 6, rightRow, 10].Merge();
            worksheet.Range[rightRow, 6].Text = $"Phone: {billTo.Phone}";
            rightRow++;
        }

        // Company Email (Left)
        if (!string.IsNullOrEmpty(company.Email))
        {
            worksheet.Range[leftRow, 1, leftRow, 4].Merge();
            worksheet.Range[leftRow, 1].Text = $"Email: {company.Email}";
            leftRow++;
        }

        // Bill To Email (Right)
        if (billTo != null && !string.IsNullOrEmpty(billTo.Email))
        {
            worksheet.Range[rightRow, 6, rightRow, 10].Merge();
            worksheet.Range[rightRow, 6].Text = $"Email: {billTo.Email}";
            rightRow++;
        }

        // Company GST (Left)
        if (!string.IsNullOrEmpty(company.GSTNo))
        {
            worksheet.Range[leftRow, 1, leftRow, 4].Merge();
            worksheet.Range[leftRow, 1].Text = $"GSTIN: {company.GSTNo}";
            leftRow++;
        }

        // Bill To GST (Right)
        if (billTo != null && !string.IsNullOrEmpty(billTo.GSTNo))
        {
            worksheet.Range[rightRow, 6, rightRow, 10].Merge();
            worksheet.Range[rightRow, 6].Text = $"GSTIN: {billTo.GSTNo}";
            rightRow++;
        }

        // Use the maximum row from both columns
        int currentRow = Math.Max(leftRow, rightRow);

        // Linked Transaction Details (Left column) - shows connected order/sale reference
        if (!string.IsNullOrWhiteSpace(invoiceData.ReferenceTransactionNo))
        {
            currentRow++; // Add spacing
            worksheet.Range[currentRow, 1, currentRow, 4].Merge();
            worksheet.Range[currentRow, 1].Text = $"Ref. No: {invoiceData.ReferenceTransactionNo}";
            currentRow++;

            if (invoiceData.ReferenceDateTime.HasValue)
            {
                worksheet.Range[currentRow, 1, currentRow, 4].Merge();
                worksheet.Range[currentRow, 1].Text = $"Ref. Date: {invoiceData.ReferenceDateTime.Value:dd-MMM-yyyy hh:mm tt}";
                currentRow++;
            }
        }

        currentRow++;
        return currentRow;
    }

    /// <summary>
    /// Draw summary and remarks section
    /// </summary>
    private static int DrawSummaryAndRemarks(IWorksheet worksheet, InvoiceData invoiceData, Dictionary<string, string> summaryFields, int startRow)
    {
        int currentRow = startRow;
        int summaryCol = 8;

        // Remarks (Left side)
        if (!string.IsNullOrWhiteSpace(invoiceData.Remarks))
        {
            worksheet.Range[currentRow, 1].Text = "Remarks:";
            worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
            currentRow++;

            worksheet.Range[currentRow, 1, currentRow + 2, 5].Merge();
            worksheet.Range[currentRow, 1].Text = invoiceData.Remarks;
            worksheet.Range[currentRow, 1].WrapText = true;
            worksheet.Range[currentRow, 1].CellStyle.VerticalAlignment = ExcelVAlign.VAlignTop;
        }

        // Summary (Right side)
        int summaryRow = startRow;

        if (summaryFields != null && summaryFields.Count > 0)
        {
            var summaryList = summaryFields.ToList();
            int visibleFieldsDrawn = 0;

            for (int i = 0; i < summaryList.Count; i++)
            {
                var field = summaryList[i];
                bool isLastField = i == summaryList.Count - 1;

                // Skip fields with 0 value, except for the last field (Grand Total)
                if (!isLastField && IsZeroValue(field.Value))
                    continue;

                // Label
                worksheet.Range[summaryRow, summaryCol].Text = field.Key;
                worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                if (isLastField)
                {
                    worksheet.Range[summaryRow, summaryCol].CellStyle.Font.Bold = true;
                    worksheet.Range[summaryRow, summaryCol].CellStyle.Font.Size = 11;
                }

                // Value
                worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
                if (decimal.TryParse(field.Value, out decimal value))
                {
                    worksheet.Range[summaryRow, summaryCol + 1].Number = (double)value;
                    worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
                }
                else
                {
                    worksheet.Range[summaryRow, summaryCol + 1].Text = field.Value;
                }
                worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

                if (isLastField && visibleFieldsDrawn > 0)
                {
                    // Separator line before grand total
                    worksheet.Range[summaryRow, summaryCol, summaryRow, summaryCol + 2].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Medium;
                    worksheet.Range[summaryRow, summaryCol, summaryRow, summaryCol + 2].CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Blue;

                    worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Font.Bold = true;
                    worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Font.Size = 11;
                    worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Color = TotalRowBackground;
                }

                summaryRow++;
                visibleFieldsDrawn++;
            }
        }

        currentRow = Math.Max(currentRow + 3, summaryRow) + 1;
        return currentRow;
    }

    /// <summary>
    /// Draw amount in words and payment methods
    /// </summary>
    private static int DrawAmountInWordsAndPayment(IWorksheet worksheet, InvoiceData invoiceData, int startRow)
    {
        int currentRow = startRow;

        // Skip amount in words if total amount is 0
        if (invoiceData.TotalAmount != 0)
        {
            // Amount in Words (Left side)
            worksheet.Range[currentRow, 1].Text = "Amount in Words:";
            worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
            currentRow++;

            try
            {
                var converter = new CurrencyWordsConverter(new CurrencyWordsConversionOptions
                {
                    Culture = Culture.International,
                    OutputFormat = OutputFormat.English,
                    CurrencyUnit = "Rupees",
                    SubCurrencyUnit = "Paise",
                    CurrencyUnitSeparator = "and"
                });

                string amountInWords = converter.ToWords(invoiceData.TotalAmount);
                amountInWords = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(amountInWords.ToLower());

                worksheet.Range[currentRow, 1, currentRow, 5].Merge();
                worksheet.Range[currentRow, 1].Text = amountInWords;
                worksheet.Range[currentRow, 1].CellStyle.Font.Italic = true;
                worksheet.Range[currentRow, 1].WrapText = true;
            }
            catch
            {
                worksheet.Range[currentRow, 1, currentRow, 5].Merge();
                worksheet.Range[currentRow, 1].Text = $"₹ {invoiceData.TotalAmount:N2}";
            }
        }

        // Payment Methods (Right side)
        int paymentRow = startRow;
        int paymentCol = 7;

        if (invoiceData.PaymentModes != null && invoiceData.PaymentModes.Any(p => p.Value > 0))
        {
            worksheet.Range[paymentRow, paymentCol].Text = "Payment Methods:";
            worksheet.Range[paymentRow, paymentCol].CellStyle.Font.Bold = true;
            paymentRow++;

            foreach (var payment in invoiceData.PaymentModes.Where(p => p.Value > 0))
            {
                worksheet.Range[paymentRow, paymentCol].Text = $"{payment.Key}:";
                worksheet.Range[paymentRow, paymentCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                worksheet.Range[paymentRow, paymentCol + 1, paymentRow, paymentCol + 2].Merge();
                worksheet.Range[paymentRow, paymentCol + 1].Number = (double)payment.Value;
                worksheet.Range[paymentRow, paymentCol + 1].NumberFormat = "#,##0.00";
                worksheet.Range[paymentRow, paymentCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
                paymentRow++;
            }
        }

        currentRow = Math.Max(currentRow + 2, paymentRow) + 1;
        return currentRow;
    }

    /// <summary>
    /// Draw branding footer - matches PDF invoice footer style
    /// </summary>
    private static async Task<int> DrawBrandingFooter(IWorksheet worksheet, int startRow)
    {
        int currentRow = startRow + 1;

        // Blue separator line at top (matching PDF)
        worksheet.Range[currentRow, 1, currentRow, 10].Merge();
        worksheet.Range[currentRow, 1].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
        worksheet.Range[currentRow, 1].CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Blue;
        currentRow++;

        // Get current date/time
        var currentDateTime = await CommonData.LoadCurrentDateTime();

        // Footer row with three sections: Left (branding), Center (export date), Right (empty for Excel - no page numbers needed)
        // Left section: AadiSoft branding
        worksheet.Range[currentRow, 1, currentRow, 3].Merge();
        worksheet.Range[currentRow, 1].Text = $"© {currentDateTime.Year} A Product By aadisoft.vercel.app";
        worksheet.Range[currentRow, 1].CellStyle.Font.Size = 7;
        worksheet.Range[currentRow, 1].CellStyle.Font.Italic = true;
        worksheet.Range[currentRow, 1].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
        worksheet.Range[currentRow, 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;

        // Center section: Export date
        worksheet.Range[currentRow, 4, currentRow, 7].Merge();
        worksheet.Range[currentRow, 4].Text = $"Exported on: {currentDateTime:dd-MMM-yyyy hh:mm tt}";
        worksheet.Range[currentRow, 4].CellStyle.Font.Size = 7;
        worksheet.Range[currentRow, 4].CellStyle.Font.Italic = true;
        worksheet.Range[currentRow, 4].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
        worksheet.Range[currentRow, 4].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

        return currentRow;
    }

    /// <summary>
    /// Apply final formatting to the worksheet
    /// </summary>
    private static void ApplyFinalFormatting(IWorksheet worksheet)
    {
        // Hide gridlines for cleaner invoice appearance (only data grid has borders)
        worksheet.IsGridLinesVisible = false;

        // Set print options
        worksheet.PageSetup.Orientation = ExcelPageOrientation.Portrait;
        worksheet.PageSetup.PaperSize = ExcelPaperSize.PaperA4;
        worksheet.PageSetup.LeftMargin = 0.5;
        worksheet.PageSetup.RightMargin = 0.5;
        worksheet.PageSetup.TopMargin = 0.5;
        worksheet.PageSetup.BottomMargin = 0.5;
        worksheet.PageSetup.FitToPagesTall = 0;
        worksheet.PageSetup.FitToPagesWide = 1;

        // Set default font
        worksheet.UsedRange.CellStyle.Font.FontName = "Calibri";
    }

    #endregion

    #region Generic Column Detection and Helper Methods

    /// <summary>
    /// Get default column settings based on type properties using reflection
    /// </summary>
    private static List<InvoiceColumnSetting> GetDefaultInvoiceColumnSettings<T>()
    {
        var settings = new List<InvoiceColumnSetting>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            string displayName = Regex.Replace(prop.Name, "(\\B[A-Z])", " $1");
            double width = 10;
            ExcelHAlign alignment = ExcelHAlign.HAlignRight;
            string format = null;

            // Determine width and alignment based on property type and name
            if (prop.PropertyType == typeof(string))
            {
                if (prop.Name.Contains("name", StringComparison.CurrentCultureIgnoreCase) || prop.Name.Contains("description", StringComparison.CurrentCultureIgnoreCase))
                    width = 25; // Auto-width for description columns
                else if (prop.Name.Contains("code", StringComparison.CurrentCultureIgnoreCase) || prop.Name.Contains("id", StringComparison.CurrentCultureIgnoreCase))
                    width = 10;
                else
                    width = 15;
                alignment = ExcelHAlign.HAlignLeft;
            }
            else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
            {
                width = 12;
                format = "#,##0.00";
                alignment = ExcelHAlign.HAlignRight;
            }
            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
            {
                width = 8;
                alignment = ExcelHAlign.HAlignCenter;
            }
            else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
            {
                width = 15;
                alignment = ExcelHAlign.HAlignCenter;
            }
            else if (prop.PropertyType == typeof(DateOnly) || prop.PropertyType == typeof(DateOnly?))
            {
                width = 12;
                alignment = ExcelHAlign.HAlignCenter;
            }

            settings.Add(new InvoiceColumnSetting(prop.Name, displayName, width, alignment, format));
        }

        return settings;
    }

    /// <summary>
    /// Determine the effective column order
    /// </summary>
    private static List<string> DetermineColumnOrder<T>(
        List<InvoiceColumnSetting> columnSettings,
        List<string> columnOrder)
    {
        if (columnOrder != null && columnOrder.Count > 0)
            return columnOrder;

        if (columnSettings != null && columnSettings.Count > 0)
            return [.. columnSettings.Select(c => c.PropertyName)];

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return [.. properties.Select(p => p.Name)];
    }

    /// <summary>
    /// Filter out columns that have all null or zero values
    /// </summary>
    private static List<string> FilterEmptyColumns<T>(
        List<T> data,
        List<string> columnOrder,
        List<InvoiceColumnSetting> columnSettings = null)
    {
        if (data == null || data.Count == 0)
            return columnOrder;

        var filteredColumns = new List<string>();

        foreach (var columnName in columnOrder)
        {
            var propInfo = typeof(T).GetProperty(columnName);
            if (propInfo == null)
            {
                filteredColumns.Add(columnName);
                continue;
            }

            // Check if column is marked to show only if has value
            var setting = columnSettings?.FirstOrDefault(c => c.PropertyName == columnName);
            if (setting != null && !setting.ShowOnlyIfHasValue)
            {
                filteredColumns.Add(columnName);
                continue;
            }

            bool hasNonEmptyValue = false;

            foreach (var item in data)
            {
                var value = propInfo.GetValue(item);

                if (value != null)
                {
                    if (value is decimal decValue && decValue != 0)
                    {
                        hasNonEmptyValue = true;
                        break;
                    }
                    else if (value is int intValue && intValue != 0)
                    {
                        hasNonEmptyValue = true;
                        break;
                    }
                    else if (value is double dblValue && dblValue != 0)
                    {
                        hasNonEmptyValue = true;
                        break;
                    }
                    else if (value is string strValue && !string.IsNullOrWhiteSpace(strValue))
                    {
                        hasNonEmptyValue = true;
                        break;
                    }
                    else if (!(value is decimal || value is int || value is double))
                    {
                        hasNonEmptyValue = true;
                        break;
                    }
                }
            }

            if (hasNonEmptyValue)
                filteredColumns.Add(columnName);
        }

        return filteredColumns;
    }

    /// <summary>
    /// Get cell value using reflection
    /// </summary>
    private static string GetCellValueDynamic<T>(T item, InvoiceColumnSetting column, int rowNumber = 0)
    {
        // Special handling for row number column
        if (column.PropertyName == "#" || column.PropertyName == "RowNumber")
            return rowNumber.ToString();

        var propInfo = typeof(T).GetProperty(column.PropertyName);
        if (propInfo == null)
            return "-";

        var value = propInfo.GetValue(item);
        if (value == null)
            return "-";

        // Format based on type
        if (value is decimal decValue)
            return decValue.FormatSmartDecimal();
        else if (value is double dblValue)
            return ((decimal)dblValue).FormatSmartDecimal();
        else if (value is DateTime dtValue)
            return dtValue.ToString(column.Format ?? "dd-MMM-yyyy");
        else if (value is DateOnly doValue)
            return doValue.ToString(column.Format ?? "dd-MMM-yyyy");
        else
            return value.ToString();
    }

    /// <summary>
    /// Check if a string value represents zero (0.00, 0, etc.)
    /// </summary>
    private static bool IsZeroValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        // Remove currency symbols, commas, and whitespace
        string cleanValue = value.Replace("₹", "").Replace(",", "").Trim();

        // Try to parse as decimal
        if (decimal.TryParse(cleanValue, out decimal decValue))
            return decValue == 0;

        return false;
    }

    /// <summary>
    /// Draw line items table with dynamic column detection
    /// </summary>
    private static int DrawLineItemsTableGeneric<T>(IWorksheet worksheet, List<T> lineItems,
        List<string> columnOrder, List<InvoiceColumnSetting> columnSettings, int startRow) where T : class
    {
        int currentRow = startRow;

        // Get column settings list in the correct order
        var orderedColumnSettings = columnOrder.Select(col => columnSettings.First(c => c.PropertyName == col)).ToList();

        // Header row
        for (int i = 0; i < orderedColumnSettings.Count; i++)
        {
            worksheet.Range[currentRow, i + 1].Text = orderedColumnSettings[i].DisplayName;
            worksheet.Range[currentRow, i + 1].CellStyle.Font.Bold = true;
            worksheet.Range[currentRow, i + 1].CellStyle.Font.Size = 10;
            worksheet.Range[currentRow, i + 1].CellStyle.Font.Color = ExcelKnownColors.White;
            worksheet.Range[currentRow, i + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            worksheet.Range[currentRow, i + 1].CellStyle.Color = PrimaryBlue;
            worksheet.Range[currentRow, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Medium;
            worksheet.Range[currentRow, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Blue;
            worksheet.SetColumnWidth(i + 1, orderedColumnSettings[i].Width);
        }
        currentRow++;

        // Data rows
        int rowNumber = 1;
        foreach (var item in lineItems)
        {
            for (int i = 0; i < orderedColumnSettings.Count; i++)
            {
                var column = orderedColumnSettings[i];
                string cellValue = GetCellValueDynamic(item, column, rowNumber);

                worksheet.Range[currentRow, i + 1].Text = cellValue;
                worksheet.Range[currentRow, i + 1].CellStyle.HorizontalAlignment = column.Alignment;
                worksheet.Range[currentRow, i + 1].CellStyle.WrapText = true;
                worksheet.Range[currentRow, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range[currentRow, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Grey_25_percent;
                worksheet.Range[currentRow, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range[currentRow, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].Color = ExcelKnownColors.Grey_25_percent;
                worksheet.Range[currentRow, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range[currentRow, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeRight].Color = ExcelKnownColors.Grey_25_percent;

                if (double.TryParse(cellValue, out var numValue))
                {
                    worksheet.Range[currentRow, i + 1].Number = (double)numValue;
                    worksheet.Range[currentRow, i + 1].NumberFormat = column.Format;
                }
            }

            // Alternating row colors
            if (rowNumber % 2 == 0)
            {
                for (int i = 0; i < orderedColumnSettings.Count; i++)
                {
                    worksheet.Range[currentRow, i + 1].CellStyle.Color = AlternateRowColor;
                }
            }

            currentRow++;
            rowNumber++;
        }

        return currentRow + 1;
    }

    #endregion
}
