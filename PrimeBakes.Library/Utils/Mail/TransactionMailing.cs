using MailKit.Net.Smtp;

using MimeKit;

using PrimeBakes.Library.Operations.Settings;

namespace PrimeBakes.Library.Utils.Mail;

internal static class TransactionMailing
{
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
		public string Differences { get; set; } // Audit-style "before -> after" change summary (for update actions)
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

	private static async Task SendEmail(string subject, string htmlBody, Dictionary<MemoryStream, string> attachments = null)
	{
		if (SqlDataAccess._databaseConnection != Secrets.AzureConnectionString)
			return; // Do not send emails in local/dev environment

		var notificationEmail = (await SettingsData.LoadSettingsByKey(SettingsKeys.NotificationEmail))?.Value;
		if (string.IsNullOrWhiteSpace(notificationEmail))
			return; // No recipient configured - notifications disabled

		var message = new MimeMessage();
		message.From.Add(new MailboxAddress("AadiSoft", Secrets.Email));
		message.To.Add(new MailboxAddress(Secrets.ToName, notificationEmail));
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

	private static string GenerateTransactionEmailHtml(TransactionEmailData data)
	{
		// Single accent per action — text colour, soft background tint, border tint, label
		var (accent, accentSoft, accentBorder, actionLabel) = data.Action switch
		{
			NotifyType.Created => ("#047857", "#ecfdf5", "#a7f3d0", "Created"),
			NotifyType.Updated => ("#b45309", "#fffbeb", "#fde68a", "Updated"),
			NotifyType.Deleted => ("#b91c1c", "#fef2f2", "#fecaca", "Deleted"),
			NotifyType.Recovered => ("#0369a1", "#eff6ff", "#bae6fd", "Recovered"),
			_ => ("#374151", "#f3f4f6", "#e5e7eb", "Modified")
		};

		var typeLower = data.TransactionType.ToLower();
		var intro = data.Action switch
		{
			NotifyType.Created => $"A new {typeLower} has been recorded in {Secrets.DatabaseName}. A summary is provided below.",
			NotifyType.Updated => $"An existing {typeLower} has been updated. The changes and current details are summarised below.",
			NotifyType.Deleted => $"A {typeLower} has been deleted from {Secrets.DatabaseName}. The details are retained below for your records.",
			NotifyType.Recovered => $"A previously deleted {typeLower} has been restored. A summary is provided below.",
			_ => $"A {typeLower} has been modified. A summary is provided below."
		};

		// Summary rows (last row has no divider)
		var detailCount = data.Details.Count;
		var detailRows = string.Join("\n", data.Details.Select((detail, i) =>
		{
			var divider = i < detailCount - 1 ? "border-bottom: 1px solid #eef0f2;" : "";
			return $@"
                                            <tr>
                                                <td style=""padding: 12px 0; {divider}"">
                                                    <span style=""color: #6b7280; font-size: 13px;"">{Encode(detail.Key)}</span>
                                                </td>
                                                <td style=""padding: 12px 0; {divider} text-align: right;"">
                                                    <span style=""color: #111827; font-size: 14px; font-weight: 600;"">{Encode(detail.Value)}</span>
                                                </td>
                                            </tr>";
		}));

		// Changes section (updates only)
		var changesSection = data.Action == NotifyType.Updated ? BuildDifferencesHtml(data.Differences, accent, accentSoft, accentBorder) : "";

		// Remarks callout
		var remarksSection = string.IsNullOrWhiteSpace(data.Remarks) ? "" : $@"
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 0 0 24px 0; background-color: #f9fafb; border: 1px solid #eef0f2; border-radius: 8px;"">
                                <tr>
                                    <td style=""padding: 16px 20px;"">
                                        <p style=""margin: 0 0 4px 0; color: #6b7280; font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.6px;"">Remarks</p>
                                        <p style=""margin: 0; color: #374151; font-size: 14px; line-height: 1.6;"">{Encode(data.Remarks)}</p>
                                    </td>
                                </tr>
                            </table>";

		// Attachment line (only when something is attached)
		var hasAttachments = (data.Action == NotifyType.Updated && data.BeforeAttachment.HasValue && data.AfterAttachment.HasValue) ||
							(data.Attachments != null && data.Attachments.Count > 0);

		var attachmentLine = !hasAttachments ? "" : $@"
                            <p style=""margin: 0 0 24px 0; color: #6b7280; font-size: 13px; line-height: 1.6;"">
                                {(data.Action == NotifyType.Updated
									? "Before and after copies of the document are attached to this email as PDF."
									: $"A PDF copy of the {typeLower} is attached to this email for your records.")}
                            </p>";

		return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{data.TransactionType} {actionLabel}</title>
    <style>
        @media only screen and (max-width: 600px) {{
            .email-container {{ width: 100% !important; }}
            .pad {{ padding: 24px !important; }}
            .pad-header {{ padding: 22px 24px !important; }}
            .outer-padding {{ padding: 16px 10px !important; }}
        }}
    </style>
</head>
<body style=""margin: 0; padding: 0; -webkit-font-smoothing: antialiased; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: {MailTheme.PageBackground};"">
    <span style=""display: none; max-height: 0; overflow: hidden; opacity: 0;"">{data.TransactionType} {actionLabel} — {data.TransactionNo}</span>
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse; background-color: {MailTheme.PageBackground};"">
        <tr>
            <td align=""center"" class=""outer-padding"" style=""padding: 40px 20px;"">
                <table role=""presentation"" class=""email-container"" style=""width: 680px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border: 1px solid #e6e8eb; border-radius: 10px; overflow: hidden; box-shadow: 0 1px 3px rgba(16, 24, 40, 0.06);"">

                    <!-- Accent bar -->
                    <tr><td style=""height: 4px; background-color: {accent}; font-size: 0; line-height: 0;"">&nbsp;</td></tr>

                    <!-- Header -->
                    <tr>
                        <td class=""pad-header"" style=""padding: 26px 32px; text-align: center; border-bottom: 1px solid #eef0f2;"">
                            <img src=""{Secrets.OnlineFullLogoPath}"" alt=""{Secrets.DatabaseName}"" style=""max-width: 280px; width: 100%; height: auto; display: block; margin: 0 auto;"" />
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td class=""pad"" style=""padding: 32px;"">

                            <span style=""display: inline-block; padding: 4px 12px; border-radius: 999px; background-color: {accentSoft}; color: {accent}; border: 1px solid {accentBorder}; font-size: 11px; font-weight: 600; letter-spacing: 0.6px; text-transform: uppercase;"">{actionLabel}</span>

                            <h1 style=""margin: 16px 0 0 0; color: #111827; font-size: 20px; font-weight: 600; line-height: 1.3;"">{data.TransactionType} {actionLabel}</h1>
                            <p style=""margin: 8px 0 4px 0; color: #6b7280; font-size: 14px; line-height: 1.6;"">{intro}</p>
                            <p style=""margin: 0 0 28px 0; color: #6b7280; font-size: 13px;"">Reference&nbsp;<span style=""color: #111827; font-weight: 600; font-family: 'SF Mono', SFMono-Regular, ui-monospace, Menlo, Consolas, monospace;"">{Encode(data.TransactionNo)}</span></p>

                            <!-- Summary card -->
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 0 0 24px 0; background-color: #ffffff; border: 1px solid #e6e8eb; border-radius: 8px;"">
                                <tr>
                                    <td style=""padding: 8px 20px;"">
                                        <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                                            {detailRows}
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            {changesSection}
                            {remarksSection}
                            {attachmentLine}

                            <!-- CTA -->
                            <table role=""presentation"" style=""border-collapse: collapse; margin: 4px 0 8px 0;"">
                                <tr>
                                    <td style=""border-radius: 6px; background-color: #111827;"">
                                        <a href=""{Secrets.AppWebsite}"" style=""display: inline-block; padding: 11px 22px; color: #ffffff; text-decoration: none; font-size: 14px; font-weight: 600;"">Open in {Secrets.DatabaseName}</a>
                                    </td>
                                </tr>
                            </table>

                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 22px 32px; background-color: #fafafa; border-top: 1px solid #eef0f2;"">
                            <p style=""margin: 0 0 6px 0; color: #9ca3af; font-size: 12px; line-height: 1.6;"">
                                This is an automated notification from {Secrets.DatabaseName}. Please do not reply to this email.
                            </p>
                            <p style=""margin: 0; color: #9ca3af; font-size: 12px; line-height: 1.6;"">
                                © {DateTime.Now.Year} {Secrets.DatabaseName} · Powered by <a href=""{Secrets.AadiSoftWebsite}"" style=""color: #6b7280; text-decoration: none; font-weight: 600;"">AadiSoft</a>
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

	private static string BuildDifferencesHtml(string differences, string accent, string accentSoft, string accentBorder)
	{
		if (string.IsNullOrWhiteSpace(differences))
			return "";

		var rows = new System.Text.StringBuilder();

		foreach (var rawLine in differences.Replace("\r\n", "\n").Split('\n'))
		{
			var line = rawLine.Trim();
			if (line.Length == 0)
				continue;

			var arrowIndex = line.IndexOf(" -> ", StringComparison.Ordinal);
			var colonIndex = line.IndexOf(':');

			if (arrowIndex >= 0)
			{
				// Field change: "Label: old -> new"
				var label = colonIndex >= 0 && colonIndex < arrowIndex ? line[..colonIndex].Trim() : "";
				var rest = colonIndex >= 0 && colonIndex < arrowIndex ? line[(colonIndex + 1)..].Trim() : line;
				var restArrow = rest.IndexOf(" -> ", StringComparison.Ordinal);
				var oldValue = Encode(rest[..restArrow].Trim());
				var newValue = Encode(rest[(restArrow + 4)..].Trim());

				rows.Append($@"
                                            <tr>
                                                <td style=""padding: 12px 0; border-bottom: 1px solid #f1f2f4;"">
                                                    <div style=""color: #6b7280; font-size: 12px; margin-bottom: 5px;"">{Encode(label)}</div>
                                                    <span style=""color: #9ca3af; font-size: 14px; text-decoration: line-through;"">{oldValue}</span>
                                                    <span style=""color: #c4c7cc; font-size: 14px;"">&nbsp;&rarr;&nbsp;</span>
                                                    <span style=""color: #111827; font-size: 14px; font-weight: 600;"">{newValue}</span>
                                                </td>
                                            </tr>");
			}
			else if (line.StartsWith('-'))
			{
				// List item, e.g. "- Card Number: X | Amount: 50.00"
				var text = Encode(line.TrimStart('-', ' '));
				rows.Append($@"
                                            <tr>
                                                <td style=""padding: 4px 0 4px 16px;"">
                                                    <span style=""color: #c4c7cc; font-size: 13px;"">&bull;</span>
                                                    <span style=""color: #6b7280; font-size: 13px;"">&nbsp;{text}</span>
                                                </td>
                                            </tr>");
			}
			else
			{
				// Section header, e.g. "Details:", "Added:", "Removed:"
				var headerColor = line.StartsWith("Added", StringComparison.OrdinalIgnoreCase) ? "#047857"
					: line.StartsWith("Removed", StringComparison.OrdinalIgnoreCase) ? "#b91c1c"
					: "#374151";
				var text = Encode(line.TrimEnd(':'));
				rows.Append($@"
                                            <tr>
                                                <td style=""padding: 16px 0 6px 0;"">
                                                    <span style=""color: {headerColor}; font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;"">{text}</span>
                                                </td>
                                            </tr>");
			}
		}

		return $@"
                            <!-- Changes -->
                            <table role=""presentation"" style=""width: 100%; border-collapse: collapse; margin: 0 0 24px 0; background-color: #ffffff; border: 1px solid #e6e8eb; border-radius: 8px; overflow: hidden;"">
                                <tr>
                                    <td style=""padding: 13px 20px; background-color: {accentSoft}; border-bottom: 1px solid {accentBorder};"">
                                        <span style=""color: {accent}; font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.6px;"">Changes</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 20px 14px 20px;"">
                                        <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">{rows}
                                        </table>
                                    </td>
                                </tr>
                            </table>";
	}

	private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}

public enum NotifyType
{
	Created,
	Updated,
	Recovered,
	Deleted
}