namespace PrimeBakes;

internal static class Program
{
	[STAThread]
	static void Main()
	{
		Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncfusionLicense);
		ApplicationConfiguration.Initialize();
		Application.Run(new Dashboard());
	}
}