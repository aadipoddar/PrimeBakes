using System.Net;
using System.Net.Mail;

namespace PrimeBakesLibrary.Printing;

public static class Mailing
{
	public static void MailPDF(string customerEmail, string filePath)
	{
		string fromMail = Secrets.EmailId;
		string fromPassword = Secrets.EmailPassword;

		MailMessage message = new()
		{
			From = new MailAddress(fromMail),
			Subject = "Your Prime Bakes Order",
			Body = "<html><body> Thank You For Ordering <html><body> ",
			IsBodyHtml = true
		};

		message.Attachments.Add(new Attachment(filePath));
		message.To.Add(customerEmail);

		var smtpClient = new SmtpClient("smtp.gmail.com", 587)
		{
			Credentials = new NetworkCredential(fromMail, fromPassword),
			EnableSsl = true
		};

		smtpClient.Send(message);
	}
}