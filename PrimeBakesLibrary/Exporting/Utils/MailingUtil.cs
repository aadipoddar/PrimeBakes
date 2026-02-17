using MailKit.Net.Smtp;

using MimeKit;

namespace PrimeBakesLibrary.Exporting.Utils;

internal static class MailingUtil
{
    private static async Task SendEmail(string subject, string htmlBody, Dictionary<MemoryStream, string>? attachments = null)
    {
        if (SqlDataAccess._databaseConnection == Secrets.LocalConnectionString)
            return; // Do not send emails in local/dev environment

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("AadiSoft", Secrets.Email));
        message.To.Add(new MailboxAddress(Secrets.ToName, Secrets.ToEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        if (attachments is not null)
            foreach (var attachment in attachments)
            {
                attachment.Key.Position = 0;
                bodyBuilder.Attachments.Add(attachment.Value, attachment.Key, ContentType.Parse("application/pdf"));
            }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync("smtp.gmail.com", 465, true);
        await client.AuthenticateAsync(Secrets.Email, Secrets.EmailPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    /// <summary>
    /// Transaction email data for sending emails about transaction actions (create, update, delete, recover).
    /// For UPDATE actions, use BeforeAttachment and AfterAttachment to provide comparison invoices.
    /// For other actions, use Attachments dictionary.
    /// </summary>
    internal class TransactionEmailData
    {
        public string TransactionType { get; set; } // "Order", "Sale", "Purchase", etc.
        public string TransactionNo { get; set; }
        public NotifyType Action { get; set; }
        public string LocationName { get; set; }
        public Dictionary<string, string> Details { get; set; } // Key-value pairs for transaction details
        public string Remarks { get; set; }
        public Dictionary<MemoryStream, string> Attachments { get; set; } // PDF attachments (for non-update actions)
        public (MemoryStream stream, string fileName)? BeforeAttachment { get; set; } // For update emails - before state
        public (MemoryStream stream, string fileName)? AfterAttachment { get; set; } // For update emails - after state
    }

    internal static async Task SendTransactionEmail(TransactionEmailData emailData)
    {
        var actionText = emailData.Action switch
        {
            NotifyType.Created => "Created",
            NotifyType.Updated => "Updated",
            NotifyType.Deleted => "Deleted",
            NotifyType.Recovered => "Recovered",
            _ => "Modified"
        };

        var subject = $"{emailData.TransactionType} {actionText}: {emailData.TransactionNo} | {emailData.LocationName}";
        var htmlBody = GenerateTransactionEmailHtml(emailData);

        // Handle before/after attachments for update emails
        Dictionary<MemoryStream, string> attachments;
        if (emailData.Action == NotifyType.Updated && emailData.BeforeAttachment.HasValue && emailData.AfterAttachment.HasValue)
            attachments = new Dictionary<MemoryStream, string>
            {
                { emailData.BeforeAttachment.Value.stream, emailData.BeforeAttachment.Value.fileName },
                { emailData.AfterAttachment.Value.stream, emailData.AfterAttachment.Value.fileName }
            };
        else
            attachments = emailData.Attachments;

        await SendEmail(subject, htmlBody, attachments);
    }

    private static string GenerateTransactionEmailHtml(TransactionEmailData data)
    {
        var actionText = data.Action switch
        {
            NotifyType.Created => "CREATED",
            NotifyType.Updated => "UPDATED",
            NotifyType.Deleted => "DELETED",
            NotifyType.Recovered => "RECOVERED",
            _ => "MODIFIED"
        };

        var actionColor = data.Action switch
        {
            NotifyType.Created => "#28a745",
            NotifyType.Updated => "#ffc107",
            NotifyType.Deleted => "#dc3545",
            NotifyType.Recovered => "#17a2b8",
            _ => "#6c757d"
        };

        var actionEmoji = data.Action switch
        {
            NotifyType.Created => "✅",
            NotifyType.Updated => "✏️",
            NotifyType.Deleted => "⚠️",
            NotifyType.Recovered => "♻️",
            _ => "ℹ️"
        };

        var actionMessage = data.Action switch
        {
            NotifyType.Created => $"A new {data.TransactionType.ToLower()} has been created in the system. Please review the details below:",
            NotifyType.Updated => $"A {data.TransactionType.ToLower()} has been updated in the system. Please review the details below and compare the attached before/after invoices:",
            NotifyType.Deleted => $"A {data.TransactionType.ToLower()} has been deleted from the system. Please review the details below:",
            NotifyType.Recovered => $"A {data.TransactionType.ToLower()} has been recovered in the system. Please review the details below:",
            _ => $"A {data.TransactionType.ToLower()} has been modified. Please review the details below:"
        };

        // Generate detail rows
        var detailRows = string.Join("\n", data.Details.Select(detail => $@"
                                            <tr class=""detail-row"">
                                                <td class=""detail-label"" style=""padding: 10px 0; border-bottom: 1px solid #fce4ec;"">
                                                    <span style=""color: #666666; font-size: 14px;"">{detail.Key}</span>
                                                </td>
                                                <td class=""detail-value"" style=""padding: 10px 0; border-bottom: 1px solid #fce4ec; text-align: right;"">
                                                    <strong style=""color: #333333; font-size: 14px;"">{detail.Value}</strong>
                                                </td>
                                            </tr>"));

        // Fix the last row border
        var lastDetailKey = data.Details.Last().Key;
        detailRows = detailRows.Replace(
            $@"<span style=""color: #666666; font-size: 14px;"">{lastDetailKey}</span>
                                                </td>
                                                <td class=""detail-value"" style=""padding: 10px 0; border-bottom: 1px solid #fce4ec;",
            $@"<span style=""color: #666666; font-size: 14px;"">{lastDetailKey}</span>
                                                </td>
                                                <td class=""detail-value"" style=""padding: 10px 0;"
        );

        // Generate remarks section (if any)
        var remarksSection = string.IsNullOrWhiteSpace(data.Remarks) ? "" : $@"
                            <!-- Remarks Section -->
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 15px 20px; background-color: #fff8e1; border-left: 4px solid #ffc107; border-radius: 4px;"">
                                        <p style=""margin: 0; color: #8a6d3b; font-size: 14px;"">
                                            <strong>📝 Remarks:</strong> {data.Remarks}
                                        </p>
                                    </td>
                                </tr>
                            </table>";

        // Generate attachment notice (only if attachments exist)
        var hasAttachments = (data.Action == NotifyType.Updated && data.BeforeAttachment.HasValue && data.AfterAttachment.HasValue) ||
                            (data.Attachments != null && data.Attachments.Count > 0);

        var attachmentNotice = !hasAttachments ? "" : $@"<strong>📎 Attachment:</strong> {(data.Action == NotifyType.Updated ? "Before/After comparison invoices are" : "The " + data.TransactionType.ToLower() + " invoice PDF is")} attached to this email for your records.<br>";

        var websiteLinkSection = $@"
                            <!-- Website Link Notice -->
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 25px 0;"">
                                <tr>
                                    <td style=""padding: 15px 20px; background-color: #e8f4fd; border-left: 4px solid #2196F3; border-radius: 4px; text-align: center;"">
                                        <p style=""margin: 0; color: #1565c0; font-size: 14px;"">
                                            {attachmentNotice}
                                            <a href=""{Secrets.AppWebsite}"" style=""color: #ec407a; text-decoration: none; font-weight: 600; margin-top: 8px; display: inline-block;"">Visit Prime Bakes</a>
                                        </p>
                                    </td>
                                </tr>
                            </table>";

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{data.TransactionType} {actionText} Notification</title>
    <style>
        @media only screen and (max-width: 600px) {{
            .email-container {{
                width: 100% !important;
                margin: 0 !important;
            }}
            .content-padding {{
                padding: 20px !important;
            }}
            .header-padding {{
                padding: 20px !important;
            }}
            .alert-padding {{
                padding: 12px 20px !important;
            }}
            .details-padding {{
                padding: 15px !important;
            }}
            .detail-row {{
                display: block !important;
                width: 100% !important;
            }}
            .detail-label {{
                display: block !important;
                width: 100% !important;
                padding: 8px 0 4px 0 !important;
                text-align: left !important;
            }}
            .detail-value {{
                display: block !important;
                width: 100% !important;
                padding: 0 0 8px 0 !important;
                text-align: left !important;
                border-bottom: 1px solid #fce4ec !important;
            }}
            .footer-padding {{
                padding: 20px !important;
            }}
            .outer-padding {{
                padding: 20px 10px !important;
            }}
        }}
    </style>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #fef5f7;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse; background-color: #fef5f7;"">
        <tr>
            <td align=""center"" class=""outer-padding"" style=""padding: 40px 20px;"">
                <table role=""presentation"" class=""email-container"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 20px rgba(236, 64, 122, 0.15);"">
                    
                    <!-- Header -->
                    <tr>
                        <td class=""header-padding"" style=""padding: 30px; text-align: center; background-color: #E91E63; border-radius: 12px 12px 0 0;"">
                            <img src=""{Secrets.OnlineFullLogoPath}"" alt=""Prime Bakes"" style=""max-width: 400px; width: 100%; height: auto; display: block; margin: 0 auto;"" />
                        </td>
                    </tr>
                    
                    <!-- Alert Banner -->
                    <tr>
                        <td style=""padding: 0;"">
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; background-color: {actionColor};"">
                                <tr>
                                    <td class=""alert-padding"" style=""padding: 15px 40px; text-align: center;"">
                                        <span style=""color: #ffffff; font-size: 14px; font-weight: 600; letter-spacing: 0.5px;"">{actionEmoji} {data.TransactionType.ToUpper()} {actionText}</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td class=""content-padding"" style=""padding: 40px;"">
                            <p style=""margin: 0 0 25px 0; color: #333333; font-size: 16px; line-height: 1.6;"">
                                {actionMessage}
                            </p>
                            
                            <!-- Transaction Details Card -->
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 25px 0; background-color: #fff8fa; border-radius: 8px; border: 1px solid #fce4ec;"">
                                <tr>
                                    <td class=""details-padding"" style=""padding: 25px;"">
                                        <h3 style=""margin: 0 0 20px 0; color: #c2185b; font-size: 16px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;"">{data.TransactionType} Details</h3>
                                        
                                        <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                                            {detailRows}
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            {remarksSection}
                            {websiteLinkSection}
                            
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td class=""footer-padding"" style=""padding: 30px 40px; background-color: #fff8fa; border-radius: 0 0 12px 12px; border-top: 1px solid #fce4ec;"">
                            <p style=""margin: 0 0 10px 0; color: #999999; font-size: 14px; line-height: 1.6;"">
                                Best regards,<br>
                                <strong style=""color: #c2185b;"">The AadiSoft Team</strong>
                            </p>
                            <p style=""margin: 20px 0 0 0; color: #999999; font-size: 12px; line-height: 1.6;"">
                                This is an automated message, please do not reply to this email.<br>
                                <a href=""https://aadisoft.tech/"" style=""color: #ec407a; text-decoration: none;"">Visit our website</a>
                            </p>
                        </td>
                    </tr>
                </table>
                
                <!-- Footer Text -->
                <table role=""presentation"" class=""email-container"" style=""width: 600px; max-width: 100%; border-collapse: collapse; margin-top: 20px;"">
                    <tr>
                        <td align=""center"" style=""padding: 0 20px;"">
                            <p style=""margin: 0; color: #999999; font-size: 12px; line-height: 1.6;"">
                                © {DateTime.Now.Year} AadiSoft. All Rights Reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}