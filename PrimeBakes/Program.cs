using PrimeBakesLibrary.DataAccess;

namespace PrimeBakes;

internal static class Program
{
	/// <summary>
	///  The main entry point for the application.
	/// </summary>
	[STAThread]
	static void Main()
	{
		// To customize application configuration such as set high DPI settings or default font,
		// see https://aka.ms/applicationconfiguration.
		Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncfusionLicense);
		ApplicationConfiguration.Initialize();
		Application.Run(new Dashboard());
	}
}