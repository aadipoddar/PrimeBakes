using System.Reflection;
using System.Text.RegularExpressions;

using NumericWordsConversion;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting.Utils;

public static class PDFInvoiceExportUtil
{
    private static PdfLayoutFormat _layoutFormat;

    #region Public Methods

    /// <summary>
    /// Export invoice to PDF with professional layout (unified method for all transaction types)
    /// </summary>
    /// <typeparam name="T">Type of line item (must be a class)</typeparam>
    /// <param name="invoiceData">Generic invoice header data with all invoice information</param>
    /// <param name="lineItems">Generic invoice line items of any type</param>
    /// <param name="columnSettings">Optional: Custom column settings for line items table</param>
    /// <param name="columnOrder">Optional: Custom column order for line items table</param>
    /// <param name="summaryFields">Optional: Custom summary fields to display (key=label, value=formatted value)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportInvoiceToPdf<T>(
        InvoiceData invoiceData,
        List<T> lineItems,
        List<InvoiceColumnSetting> columnSettings = null,
        List<string> columnOrder = null,
        Dictionary<string, string> summaryFields = null) where T : class
    {
        MemoryStream ms = new();

        try
        {
            using PdfDocument pdfDocument = new();
            // Add page
            PdfPage page = pdfDocument.Pages.Add();
            PdfGraphics graphics = page.Graphics;

            // Add footer template first
            await AddBrandingFooter(pdfDocument);

            // Initialize layout format for proper pagination
            _layoutFormat = new()
            {
                Layout = PdfLayoutType.Paginate,
                Break = PdfLayoutBreakType.FitPage
            };

            float pageWidth = page.GetClientSize().Width;
            float leftMargin = 20;
            float rightMargin = 20;
            float currentY = 15;

            // 1. Header Section with Logo and Company Info
            currentY = DrawInvoiceHeader(graphics, leftMargin, pageWidth, currentY);

            // 2. Invoice Type and Number
            currentY = DrawInvoiceTitle(graphics, invoiceData.InvoiceType, invoiceData.TransactionNo, invoiceData.TransactionDateTime, leftMargin, pageWidth, currentY, invoiceData.Outlet);

            // 2.5. Draw DELETED status badge if Status is false
            if (!invoiceData.Status)
                currentY = DrawDeletedStatusBadge(graphics, pageWidth, currentY);

            // 3. Company and Customer Information (Two Columns)
            currentY = DrawCompanyInfo(graphics, invoiceData.Company, invoiceData.BillTo, invoiceData, leftMargin, pageWidth, currentY);

            // Get column settings dynamically if not provided
            columnSettings ??= GetDefaultInvoiceColumnSettings<T>();

            // Determine column order
            List<string> effectiveColumnOrder = DetermineColumnOrder<T>(columnSettings, columnOrder);

            // Filter out columns that have no data
            effectiveColumnOrder = FilterEmptyColumns(lineItems, effectiveColumnOrder, columnSettings);

            // 4. Line Items Table
            PdfGridLayoutResult gridResult = DrawLineItemsTableWithResult(page, lineItems, effectiveColumnOrder, columnSettings, leftMargin, rightMargin, pageWidth, currentY);

            // Get the last page where the grid ended
            PdfPage lastPage = gridResult.Page;
            PdfGraphics lastPageGraphics = lastPage.Graphics;
            currentY = gridResult.Bounds.Bottom + 8;

            // Calculate estimated heights for sections
            float pageHeight = lastPage.GetClientSize().Height;
            float footerHeight = 30; // Footer template height
            float availableSpace = pageHeight - currentY - footerHeight;

            // Estimate space needed for summary and remarks (minimum)
            float estimatedSummaryHeight = summaryFields != null ? summaryFields.Count(f => !IsZeroValue(f.Value)) * 12 + 20 : 40;
            float estimatedRemarksHeight = string.IsNullOrWhiteSpace(invoiceData.Remarks) ? 0 : 50;
            float estimatedWordsHeight = invoiceData.TotalAmount > 0 ? 30 : 0;
            float estimatedPaymentHeight = (invoiceData.PaymentModes != null && invoiceData.PaymentModes.Any(p => p.Value > 0)) ? 40 : 0;
            float totalEstimatedHeight = Math.Max(estimatedSummaryHeight, estimatedRemarksHeight) + Math.Max(estimatedWordsHeight, estimatedPaymentHeight);

            // If not enough space, add a new page
            if (availableSpace < totalEstimatedHeight)
            {
                lastPage = pdfDocument.Pages.Add();
                lastPageGraphics = lastPage.Graphics;
                currentY = 15; // Start from top margin
            }

            // 5. Summary Section with Remarks (two-column layout on the last page)
            currentY = DrawSummaryAndRemarks(lastPageGraphics, invoiceData, summaryFields, leftMargin, pageWidth, currentY);

            // Check space before amount in words section (recalculate pageHeight in case we added a new page)
            pageHeight = lastPage.GetClientSize().Height;
            availableSpace = pageHeight - currentY - footerHeight;
            if (availableSpace < Math.Max(estimatedWordsHeight, estimatedPaymentHeight) && (estimatedWordsHeight > 0 || estimatedPaymentHeight > 0))
            {
                lastPage = pdfDocument.Pages.Add();
                lastPageGraphics = lastPage.Graphics;
                currentY = 15;
            }

            // 6. Amount in Words and Payment Methods side by side (on the last page)
            float amountInWordsStartY = currentY;
            float amountInWordsEndY = DrawAmountInWords(lastPageGraphics, invoiceData.TotalAmount, leftMargin, pageWidth, currentY);
            float paymentMethodsEndY = DrawPaymentMethods(lastPageGraphics, invoiceData, leftMargin, pageWidth, amountInWordsStartY);
            currentY = Math.Max(amountInWordsEndY, paymentMethodsEndY);

            // Save PDF document to stream
            pdfDocument.Save(ms);
            ms.Position = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting invoice to PDF: {ex.Message}");
            throw;
        }

        return ms;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Draw invoice header with logo and company name
    /// </summary>
    private static float DrawInvoiceHeader(PdfGraphics graphics, float leftMargin, float pageWidth, float startY)
    {
        float currentY = startY;

        // Try to load logo - use same logic as PDFReportExportUtil
        try
        {
            // Try multiple possible logo paths
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
                PdfBitmap logoBitmap = new(imageStream);

                // Calculate logo dimensions (compact for better space utilization)
                float maxLogoHeight = 42;
                float logoWidth = logoBitmap.Width;
                float logoHeight = logoBitmap.Height;
                float aspectRatio = logoWidth / logoHeight;

                if (logoHeight > maxLogoHeight)
                {
                    logoHeight = maxLogoHeight;
                    logoWidth = logoHeight * aspectRatio;
                }

                // Center the logo horizontally at the top
                float logoX = (pageWidth - logoWidth) / 2;
                graphics.DrawImage(logoBitmap, new PointF(logoX, currentY), new SizeF(logoWidth, logoHeight));
                currentY += logoHeight + 5;
            }
        }
        catch (Exception ex)
        {
            // Log error but continue without logo
            Console.WriteLine($"Logo loading failed: {ex.Message}");
        }

        // Draw a separator line
        PdfPen separatorPen = new(new PdfColor(59, 130, 246), 2f);
        graphics.DrawLine(separatorPen, new PointF(leftMargin, currentY), new PointF(pageWidth - 20, currentY));
        currentY += 4;

        return currentY;
    }

    /// <summary>
    /// Draw invoice type and number
    /// </summary>
    private static float DrawInvoiceTitle(PdfGraphics graphics, string invoiceType, string invoiceNumber, DateTime transactionDateTime, float leftMargin, float pageWidth, float startY, string outlet = null)
    {
        float currentY = startY;

        // Invoice Type (e.g., "TAX INVOICE")
        PdfStandardFont titleFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
        PdfBrush titleBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
        graphics.DrawString(invoiceType.ToUpper(), titleFont, titleBrush, new PointF(leftMargin, currentY));

        // Invoice Number (right aligned)
        PdfStandardFont numberFont = new(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        string invoiceNumberText = $"Invoice #: {invoiceNumber}";
        SizeF numberSize = numberFont.MeasureString(invoiceNumberText);
        graphics.DrawString(invoiceNumberText, numberFont, new PdfSolidBrush(new PdfColor(0, 0, 0)),
            new PointF(pageWidth - 20 - numberSize.Width, currentY));

        currentY += 16;

        // Draw Outlet label and Invoice Date on same line
        if (!string.IsNullOrWhiteSpace(outlet))
        {
            PdfStandardFont outletFont = new(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
            PdfBrush outletBrush = new PdfSolidBrush(new PdfColor(100, 100, 100));
            string outletText = $"Outlet: {outlet}";
            graphics.DrawString(outletText, outletFont, outletBrush, new PointF(leftMargin, currentY));
        }

        // Invoice Date (right aligned, on same line as outlet)
        PdfStandardFont dateFont = new(PdfFontFamily.Helvetica, 10);
        string invoiceDateText = $"Invoice Date: {transactionDateTime:dd-MMM-yyyy hh:mm tt}";
        SizeF dateSize = dateFont.MeasureString(invoiceDateText);
        graphics.DrawString(invoiceDateText, dateFont, PdfBrushes.Black,
            new PointF(pageWidth - 20 - dateSize.Width, currentY));

        currentY += 14;

        return currentY;
    }

    /// <summary>
    /// Draw DELETED status badge when invoice is deleted
    /// </summary>
    private static float DrawDeletedStatusBadge(PdfGraphics graphics, float pageWidth, float startY)
    {
        // Draw a prominent red badge/box with "DELETED" text
        float badgeWidth = 120;
        float badgeHeight = 30;
        float badgeX = (pageWidth - badgeWidth) / 2; // Center horizontally
        float badgeY = startY + 5;

        // Draw red background rectangle with rounded corners
        PdfBrush redBrush = new PdfSolidBrush(new PdfColor(220, 38, 38)); // Red-600
        graphics.DrawRectangle(redBrush, new RectangleF(badgeX, badgeY, badgeWidth, badgeHeight));

        // Draw white "DELETED" text centered in the badge
        PdfStandardFont badgeFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
        PdfBrush whiteBrush = new PdfSolidBrush(new PdfColor(255, 255, 255));
        string deletedText = "DELETED";
        SizeF textSize = badgeFont.MeasureString(deletedText);
        float textX = badgeX + (badgeWidth - textSize.Width) / 2;
        float textY = badgeY + (badgeHeight - textSize.Height) / 2;
        graphics.DrawString(deletedText, badgeFont, whiteBrush, new PointF(textX, textY));

        // Return updated Y position (badge Y + badge height + spacing)
        return badgeY + badgeHeight + 10;
    }

    /// <summary>
    /// Draw company and customer information in two columns
    /// </summary>
    private static float DrawCompanyInfo(PdfGraphics graphics, CompanyModel company,
        LedgerModel billTo, InvoiceData invoiceData, float leftMargin, float pageWidth, float startY)
    {
        float currentY = startY;
        float columnWidth = (pageWidth - 40 - 10) / 2; // 10px gap between columns
        float rightColumnX = leftMargin + columnWidth + 10;

        PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
        PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
        PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(100, 100, 100));
        PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

        float padding = 5;
        float leftTextY = currentY + padding;
        float rightTextY = currentY + padding;
        float leftStartY = leftTextY;
        float rightStartY = rightTextY;

        // Left Column - From (Company)
        graphics.DrawString("FROM:", labelFont, labelBrush, new PointF(leftMargin + padding, leftTextY));
        leftTextY += 10;

        if (company != null)
        {
            graphics.DrawString(company.Name ?? string.Empty, valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
            leftTextY += 9;

            if (!string.IsNullOrEmpty(company.Address))
            {
                // Calculate actual address height dynamically
                int addressLines = Math.Max(1, company.Address.Length / 50);
                float addressHeight = addressLines * 9;
                DrawWrappedText(graphics, company.Address, valueFont, valueBrush,
                    new RectangleF(leftMargin + padding, leftTextY, columnWidth - 2 * padding, addressHeight + 5));
                leftTextY += addressHeight + 2;
            }

            if (!string.IsNullOrEmpty(company.Phone))
            {
                graphics.DrawString($"Phone: {company.Phone}", valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
                leftTextY += 9;
            }

            if (!string.IsNullOrEmpty(company.Email))
            {
                graphics.DrawString($"Email: {company.Email}", valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
                leftTextY += 9;
            }

            if (!string.IsNullOrEmpty(company.GSTNo))
            {
                graphics.DrawString($"GSTIN: {company.GSTNo}", valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
                leftTextY += 9;
            }
        }

        // Right Column - To (Customer/Party) - skip entire section if null
        if (billTo != null)
        {
            graphics.DrawString("BILL TO:", labelFont, labelBrush, new PointF(rightColumnX + padding, rightTextY));
            rightTextY += 10;

            graphics.DrawString(billTo.Name, valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
            rightTextY += 9;

            if (!string.IsNullOrEmpty(billTo.Address))
            {
                // Calculate actual address height dynamically
                int addressLines = Math.Max(1, billTo.Address.Length / 50);
                float addressHeight = addressLines * 9;
                DrawWrappedText(graphics, billTo.Address, valueFont, valueBrush,
                    new RectangleF(rightColumnX + padding, rightTextY, columnWidth - 2 * padding, addressHeight + 5));
                rightTextY += addressHeight + 2;
            }

            if (!string.IsNullOrEmpty(billTo.Phone))
            {
                graphics.DrawString($"Phone: {billTo.Phone}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
                rightTextY += 9;
            }

            if (!string.IsNullOrEmpty(billTo.Email))
            {
                graphics.DrawString($"Email: {billTo.Email}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
                rightTextY += 9;
            }

            if (!string.IsNullOrEmpty(billTo.GSTNo))
            {
                graphics.DrawString($"GSTIN: {billTo.GSTNo}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
                rightTextY += 9;
            }
        }

        // Calculate dynamic box height based on content
        float leftContentHeight = leftTextY - leftStartY;
        float rightContentHeight = rightTextY - rightStartY;
        float boxHeight = Math.Max(leftContentHeight, rightContentHeight) + padding;

        currentY += boxHeight + 5;

        // Linked Transaction Details (if present) - shows connected order/sale reference
        if (!string.IsNullOrWhiteSpace(invoiceData.ReferenceTransactionNo))
        {
            graphics.DrawString($"Ref. No: {invoiceData.ReferenceTransactionNo}", valueFont, valueBrush,
                new PointF(leftMargin, currentY));
            currentY += 12;

            if (invoiceData.ReferenceDateTime.HasValue)
            {
                graphics.DrawString($"Ref. Date: {invoiceData.ReferenceDateTime.Value:dd-MMM-yyyy hh:mm tt}", valueFont, valueBrush,
                    new PointF(leftMargin, currentY));
                currentY += 12;
            }
        }

        return currentY;
    }

    /// <summary>
    /// Draw line items table with dynamic column detection
    /// </summary>
    private static PdfGridLayoutResult DrawLineItemsTableWithResult<T>(PdfPage page, List<T> lineItems,
        List<string> columnOrder, List<InvoiceColumnSetting> columnSettings,
        float leftMargin, float rightMargin, float pageWidth, float startY) where T : class
    {
        PdfGrid pdfGrid = new();

        // Get column settings list in the correct order
        var orderedColumnSettings = columnOrder.Select(col => columnSettings.First(c => c.PropertyName == col)).ToList();

        // Calculate available width and adjust column widths to fit within page
        float availableWidth = pageWidth - leftMargin - rightMargin;
        float fixedWidths = orderedColumnSettings.Where(c => c.PDFWidth.HasValue && c.PDFWidth.Value > 0).Sum(c => (float)c.PDFWidth.Value);

        // Find first column with width 0 (auto-size column, typically description)
        var autoSizeColumn = orderedColumnSettings.FirstOrDefault(c => c.PDFWidth.HasValue && c.PDFWidth.Value == 0);
        if (autoSizeColumn != null)
        {
            // Calculate remaining width for auto-size column
            float remainingWidth = availableWidth - fixedWidths;
            autoSizeColumn.PDFWidth = Math.Max(remainingWidth, 80); // Minimum 80 points
        }

        // Check if total width exceeds available width - if so, scale proportionally
        float totalWidth = orderedColumnSettings.Where(c => c.PDFWidth.HasValue).Sum(c => (float)c.PDFWidth.Value);
        if (totalWidth > availableWidth)
        {
            // Scale all columns proportionally to fit within available width
            float scaleFactor = availableWidth / totalWidth;
            foreach (var column in orderedColumnSettings)
            {
                if (column.PDFWidth.HasValue)
                    column.PDFWidth = column.PDFWidth.Value * scaleFactor;
            }
        }

        // Add columns to grid
        pdfGrid.Columns.Add(orderedColumnSettings.Count);
        for (int i = 0; i < orderedColumnSettings.Count; i++)
        {
            if (orderedColumnSettings[i].PDFWidth.HasValue)
                pdfGrid.Columns[i].Width = (float)orderedColumnSettings[i].PDFWidth.Value;
        }

        pdfGrid.Style.AllowHorizontalOverflow = false;
        pdfGrid.RepeatHeader = true;
        pdfGrid.AllowRowBreakAcrossPages = true;

        // Add header row
        PdfGridRow headerRow = pdfGrid.Headers.Add(1)[0];
        for (int i = 0; i < orderedColumnSettings.Count; i++)
        {
            headerRow.Cells[i].Value = orderedColumnSettings[i].DisplayName;
            headerRow.Cells[i].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
            headerRow.Cells[i].Style.TextBrush = PdfBrushes.White;
            headerRow.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 9f, PdfFontStyle.Bold);
            headerRow.Cells[i].Style.StringFormat = new PdfStringFormat
            {
                Alignment = PdfTextAlignment.Center,
                LineAlignment = PdfVerticalAlignment.Middle,
                WordWrap = PdfWordWrapType.Word
            };
            headerRow.Cells[i].Style.CellPadding = new PdfPaddings(1f, 1f, 1f, 1f);
        }

        // Add data rows
        int rowNumber = 1;
        foreach (var item in lineItems)
        {
            PdfGridRow row = pdfGrid.Rows.Add();

            // Populate cells based on column settings
            for (int i = 0; i < orderedColumnSettings.Count; i++)
            {
                var column = orderedColumnSettings[i];
                string cellValue = GetCellValueDynamic(item, column, rowNumber);
                row.Cells[i].Value = cellValue;

                // Apply styling
                row.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 9f);
                row.Cells[i].Style.Borders.All = new PdfPen(new PdfColor(220, 220, 220), 0.5f);
                row.Cells[i].Style.CellPadding = new PdfPaddings(1f, 1f, 1f, 1f);
                // Convert CellAlignment to PdfTextAlignment
                PdfTextAlignment pdfAlign = column.Alignment switch
                {
                    CellAlignment.Left => PdfTextAlignment.Left,
                    CellAlignment.Center => PdfTextAlignment.Center,
                    CellAlignment.Right => PdfTextAlignment.Right,
                    _ => PdfTextAlignment.Right
                };
                row.Cells[i].Style.StringFormat = new PdfStringFormat
                {
                    Alignment = pdfAlign,
                    LineAlignment = PdfVerticalAlignment.Middle,
                    WordWrap = PdfWordWrapType.Word
                };
            }

            // Alternating row colors
            if (rowNumber % 2 == 0)
            {
                for (int i = 0; i < orderedColumnSettings.Count; i++)
                {
                    row.Cells[i].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(249, 250, 251));
                }
            }

            rowNumber++;
        }

        // Draw grid with proper pagination layout similar to reference code
        // The footer template bounds are automatically handled by the document template
        PdfLayoutResult result = pdfGrid.Draw(page, new PointF(leftMargin, startY), _layoutFormat);

        // Return the layout result so caller can get the last page
        // Cast to PdfGridLayoutResult for compatibility
        return new PdfGridLayoutResult(result.Page, result.Bounds);
    }

    /// <summary>
    /// Draw invoice summary (subtotal, taxes, total) on the right and remarks on the left
    /// </summary>
    private static float DrawSummaryAndRemarks(PdfGraphics graphics, InvoiceData invoiceData, Dictionary<string, string> summaryFields, float leftMargin, float pageWidth, float startY)
    {
        float summaryStartY = startY;
        float remarksStartY = startY;

        // Define column boundaries
        float remarksColumnWidth = pageWidth - 240; // Left side for remarks
        float summaryColumnX = pageWidth - 200; // Right side for summary
        float rightMargin = 20;

        PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
        PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
        PdfStandardFont totalFont = new(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
        PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(80, 80, 80));
        PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

        // ===== LEFT SIDE: REMARKS =====
        if (!string.IsNullOrWhiteSpace(invoiceData.Remarks))
        {
            PdfStandardFont remarksLabelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
            PdfStandardFont remarksValueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
            PdfBrush remarksBrush = new PdfSolidBrush(new PdfColor(60, 60, 60));

            graphics.DrawString("Remarks:", remarksLabelFont, new PdfSolidBrush(new PdfColor(0, 0, 0)), new PointF(leftMargin, remarksStartY));
            remarksStartY += 12;

            // Calculate dynamic height based on text content
            float remarksBoxWidth = remarksColumnWidth - 30;
            float textWidth = remarksBoxWidth - 10; // Account for padding

            // Measure the text height with word wrapping
            PdfStringFormat format = new()
            {
                LineAlignment = PdfVerticalAlignment.Top,
                Alignment = PdfTextAlignment.Left,
                WordWrap = PdfWordWrapType.Word
            };

            SizeF textSize = remarksValueFont.MeasureString(invoiceData.Remarks, textWidth, format);
            float remarksBoxHeight = textSize.Height + 10; // Add padding (5px top + 5px bottom)

            // Ensure minimum height
            if (remarksBoxHeight < 30)
                remarksBoxHeight = 30;

            // Draw remarks text with padding (no box)
            RectangleF remarksTextRect = new(leftMargin + 5, remarksStartY + 5, remarksBoxWidth - 10, remarksBoxHeight - 10);
            DrawWrappedText(graphics, invoiceData.Remarks, remarksValueFont, remarksBrush, remarksTextRect);

            remarksStartY += remarksBoxHeight + 5;
        }

        // ===== RIGHT SIDE: SUMMARY =====
        // Use custom summary fields if provided, otherwise show just total
        if (summaryFields != null && summaryFields.Count > 0)
        {
            bool isLastField;
            int fieldIndex = 0;
            int visibleFieldsDrawn = 0;

            foreach (var field in summaryFields)
            {
                fieldIndex++;
                isLastField = fieldIndex == summaryFields.Count;

                // Skip fields with 0.00 value, except for the last field (Grand Total)
                if (!isLastField && IsZeroValue(field.Value))
                    continue;

                // Draw line before last field (typically Grand Total) and after at least one field has been drawn
                if (isLastField && visibleFieldsDrawn > 0)
                {
                    PdfPen linePen = new(new PdfColor(59, 130, 246), 1f);
                    graphics.DrawLine(linePen, new PointF(summaryColumnX - 10, summaryStartY), new PointF(pageWidth - 20, summaryStartY));
                    summaryStartY += 4;
                }

                // Use bold font and blue color for last field (total)
                var currentLabelFont = isLastField ? totalFont : labelFont;
                var currentValueFont = isLastField ? totalFont : valueFont;
                var currentBrush = isLastField ? new PdfSolidBrush(new PdfColor(59, 130, 246)) : valueBrush;

                graphics.DrawString($"{field.Key}:", currentLabelFont, isLastField ? currentBrush : labelBrush, new PointF(summaryColumnX, summaryStartY));
                SizeF valueSize = currentValueFont.MeasureString(field.Value);
                graphics.DrawString(field.Value, currentValueFont, currentBrush, new PointF(pageWidth - rightMargin - valueSize.Width, summaryStartY));
                summaryStartY += isLastField ? 15 : 10;
                visibleFieldsDrawn++;
            }
        }
        else
        {
            // Fallback: Just show total amount
            PdfPen linePen = new(new PdfColor(59, 130, 246), 1f);
            graphics.DrawLine(linePen, new PointF(summaryColumnX - 10, summaryStartY), new PointF(pageWidth - 20, summaryStartY));
            summaryStartY += 4;

            PdfBrush totalBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
            graphics.DrawString("TOTAL:", totalFont, totalBrush, new PointF(summaryColumnX, summaryStartY));
            string totalText = invoiceData.TotalAmount.FormatIndianCurrency();
            SizeF totalSize = totalFont.MeasureString(totalText);
            graphics.DrawString(totalText, totalFont, totalBrush, new PointF(pageWidth - rightMargin - totalSize.Width, summaryStartY));
            summaryStartY += 15;
        }

        // Return the maximum Y position of both columns
        return Math.Max(remarksStartY, summaryStartY);
    }

    /// <summary>
    /// Draw amount in words and payment methods side by side
    /// </summary>
    private static float DrawAmountInWords(PdfGraphics graphics, decimal amount, float leftMargin, float pageWidth, float startY)
    {
        // Skip if amount is 0
        if (amount == 0)
            return startY;

        float currentY = startY;

        PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
        PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Italic);
        PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

        string amountInWords = ConvertAmountToWords(amount);

        // Left column width for amount in words
        float leftColumnWidth = (pageWidth - 40) * 0.6f; // 60% for amount in words

        graphics.DrawString("Amount in Words:", labelFont, labelBrush, new PointF(leftMargin, currentY));
        currentY += 10;

        DrawWrappedText(graphics, amountInWords, valueFont, labelBrush,
            new RectangleF(leftMargin, currentY, leftColumnWidth, 20));
        currentY += 18;

        return currentY;
    }

    /// <summary>
    /// Draw payment methods breakdown from dictionary
    /// </summary>
    private static float DrawPaymentMethods(PdfGraphics graphics, InvoiceData invoiceData, float leftMargin, float pageWidth, float startY)
    {
        // Check if payment modes dictionary has any values
        if (invoiceData.PaymentModes == null || !invoiceData.PaymentModes.Any(p => p.Value > 0))
            return startY;

        PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
        PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
        PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));
        PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(37, 99, 235)); // Blue for amounts

        // Position payment methods on the right side (40% of width)
        float rightColumnX = leftMargin + (pageWidth - 40) * 0.6f + 20; // Start after amount in words
        float currentY = startY;

        graphics.DrawString("Payment Methods:", labelFont, labelBrush, new PointF(rightColumnX, currentY));
        currentY += 12;

        // Draw each payment method that has a value > 0
        float methodX = rightColumnX + 10;
        float amountX = rightColumnX + 80;

        foreach (var paymentMode in invoiceData.PaymentModes.Where(p => p.Value > 0))
        {
            graphics.DrawString($"{paymentMode.Key}:", valueFont, labelBrush, new PointF(methodX, currentY));
            graphics.DrawString(paymentMode.Value.FormatIndianCurrency(), valueFont, valueBrush, new PointF(amountX, currentY));
            currentY += 10;
        }

        currentY += 5; // Add spacing after payment methods
        return currentY;
    }

    /// <summary>
    /// Add branding footer to all pages
    /// </summary>
    private static async Task AddBrandingFooter(PdfDocument document)
    {
        try
        {
            // Create footer template with proper bounds
            RectangleF footerRect = new(0, 0, document.Pages[0].GetClientSize().Width, 30);
            PdfPageTemplateElement footer = new(footerRect);

            PdfStandardFont footerFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Italic);
            PdfBrush footerBrush = new PdfSolidBrush(new PdfColor(107, 114, 128)); // Gray

            // Draw separator line at top
            PdfPen separatorPen = new(new PdfColor(59, 130, 246), 0.5f);
            footer.Graphics.DrawLine(separatorPen, new PointF(0, 0), new PointF(footer.Width, 0));

            // Left: AadiSoft branding
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            string branding = $"© {currentDateTime.Year} A Product By aadisoft.vercel.app";
            footer.Graphics.DrawString(branding, footerFont, footerBrush, new PointF(15, 8));

            // Center: Export date
            string exportDate = $"Exported on: {currentDateTime:dd-MMM-yyyy hh:mm tt}";
            SizeF exportDateSize = footerFont.MeasureString(exportDate);
            float centerX = (document.Pages[0].GetClientSize().Width - exportDateSize.Width) / 2;
            footer.Graphics.DrawString(exportDate, footerFont, footerBrush, new PointF(centerX, 8));

            // Right: Page numbers
            PdfPageNumberField pageNumber = new();
            PdfPageCountField pageCount = new();
            PdfCompositeField pageInfo = new(
                footerFont,
                footerBrush,
                "Page {0} of {1}",
                pageNumber,
                pageCount);

            string pageText = "Page 999 of 999"; // Max width for alignment
            SizeF pageInfoSize = footerFont.MeasureString(pageText);
            float rightX = document.Pages[0].GetClientSize().Width - pageInfoSize.Width - 15;
            pageInfo.Draw(footer.Graphics, new PointF(rightX, 8));          // Add footer to document
            document.Template.Bottom = footer;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding branding footer: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to draw wrapped text within a rectangle
    /// </summary>
    private static void DrawWrappedText(PdfGraphics graphics, string text, PdfFont font, PdfBrush brush, RectangleF bounds)
    {
        if (string.IsNullOrEmpty(text))
            return;

        PdfStringFormat format = new()
        {
            LineAlignment = PdfVerticalAlignment.Top,
            Alignment = PdfTextAlignment.Left,
            WordWrap = PdfWordWrapType.Word
        };

        graphics.DrawString(text, font, brush, bounds, format);
    }

    /// <summary>
    /// Convert amount to words (Indian format)
    /// </summary>
    private static string ConvertAmountToWords(decimal amount)
    {
        try
        {
            // Use NumericWordsConversion package for Indian currency
            var converter = new CurrencyWordsConverter(new CurrencyWordsConversionOptions
            {
                Culture = Culture.Hindi,
                CurrencyUnit = "Rupee",
                SubCurrencyUnit = "Paise",
                EndOfWordsMarker = "Only"
            });

            string words = converter.ToWords(amount);
            return words;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting amount to words: {ex.Message}");
            return "Amount in Words Not Available";
        }
    }

    #endregion

    #region Dynamic Column Detection

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
            float width = 50;
            PdfTextAlignment alignment = PdfTextAlignment.Right;
            string format = null;

            // Determine width and alignment based on property type and name
            if (prop.PropertyType == typeof(string))
            {
                // Name/Description columns get auto-width (0), others get smaller fixed width
                if (prop.Name.Contains("name", StringComparison.CurrentCultureIgnoreCase) || prop.Name.Contains("description", StringComparison.CurrentCultureIgnoreCase))
                {
                    width = 0; // Auto-width for description columns
                }
                else if (prop.Name.Contains("code", StringComparison.CurrentCultureIgnoreCase) || prop.Name.Contains("id", StringComparison.CurrentCultureIgnoreCase))
                {
                    width = 50; // Compact for codes/IDs
                }
                else
                {
                    width = 70; // Moderate width for other strings
                }
                alignment = PdfTextAlignment.Left;
            }
            else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
            {
                // Smaller width for decimal columns to fit more
                width = 50;
                format = "#,##0.00";
                alignment = PdfTextAlignment.Right;
            }
            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
            {
                width = 35;
                alignment = PdfTextAlignment.Center;
            }
            else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
            {
                width = 70;
                alignment = PdfTextAlignment.Center;
            }
            else if (prop.PropertyType == typeof(DateOnly) || prop.PropertyType == typeof(DateOnly?))
            {
                width = 60;
                alignment = PdfTextAlignment.Center;
            }

            // Convert PdfTextAlignment to CellAlignment
            CellAlignment cellAlignment = alignment switch
            {
                PdfTextAlignment.Left => CellAlignment.Left,
                PdfTextAlignment.Center => CellAlignment.Center,
                PdfTextAlignment.Right => CellAlignment.Right,
                _ => CellAlignment.Right
            };

            settings.Add(new InvoiceColumnSetting(prop.Name, displayName, InvoiceExportType.PDF, cellAlignment, width, null, format));
        }

        return settings;
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
    /// Determine the effective column order
    /// </summary>
    private static List<string> DetermineColumnOrder<T>(
        List<InvoiceColumnSetting> columnSettings,
        List<string> columnOrder)
    {
        if (columnOrder != null && columnOrder.Count > 0)
            return columnOrder;

        // Use column order from settings if available
        if (columnSettings != null && columnSettings.Count > 0)
            return [.. columnSettings.Select(c => c.PropertyName)];

        // Use all properties if no order specified
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
            return decValue.ToString(column.Format ?? "#,##0.00");
        else if (value is double dblValue)
            return dblValue.ToString(column.Format ?? "#,##0.00");
        else if (value is DateTime dtValue)
            return dtValue.ToString(column.Format ?? "dd-MMM-yyyy");
        else if (value is DateOnly doValue)
            return doValue.ToString(column.Format ?? "dd-MMM-yyyy");
        else
            return value.ToString();
    }

    #endregion
}
