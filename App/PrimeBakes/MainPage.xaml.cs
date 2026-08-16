namespace PrimeBakes;

public partial class MainPage : ContentPage
{
	public MainPage() => InitializeComponent();

	public MainPage(string startPath)
	{
		InitializeComponent();
		blazorWebView.StartPath = startPath;
	}
}
