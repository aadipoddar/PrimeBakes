namespace PrimeBakes;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		ConfigurePlatform();
	}

	protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage()) { Title = "PrimeBakes" };

	partial void ConfigurePlatform();
}
