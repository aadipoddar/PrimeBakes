using QuickLook;

using UIKit;

namespace PrimeOrders.Services;

public partial class SaveService
{
	public partial string SaveAndView(string filename, string contentType, MemoryStream stream)
	{
		string exception = string.Empty;
		string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		string filePath = Path.Combine(path, filename);
		try
		{
			FileStream fileStream = File.Open(filePath, FileMode.Create);
			stream.Position = 0;
			stream.CopyTo(fileStream);
			fileStream.Flush();
			fileStream.Close();
		}
		catch (Exception e)
		{
			exception = e.ToString();
		}
		if (contentType != "application/html" || exception == string.Empty)
		{
			UIWindow window = GetKeyWindow();
			if (window != null && window.RootViewController != null)
			{
				UIViewController uiViewController = window.RootViewController;
				if (uiViewController != null)
				{
					QLPreviewController qlPreview = [];
					QLPreviewItem item = new QLPreviewItemBundle(filename, filePath);
					qlPreview.DataSource = new PreviewControllerDS(item);
					uiViewController.PresentViewController(qlPreview, true, null);
				}
			}
		}

		return null;
	}
	public static UIWindow GetKeyWindow()
	{
		foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
		{
			if (scene is UIWindowScene windowScene)
			{
				foreach (var window in windowScene.Windows)
				{
					if (window.IsKeyWindow)
					{
						return window;
					}
				}
			}
		}

		return null;
	}
}
