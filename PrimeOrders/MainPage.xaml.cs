namespace PrimeOrders;

public partial class MainPage : ContentPage
{
	private const string CURRENT_USER_ID_KEY = "user_id";
	private int _userId;

	public MainPage()
	{
		InitializeComponent();

		var userId = SecureStorage.GetAsync(CURRENT_USER_ID_KEY).GetAwaiter().GetResult();
		if (userId is not null && userId is not "0") Navigation.PushAsync(new OrderPage(int.Parse(userId)), true);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		SecureStorage.Remove(CURRENT_USER_ID_KEY);
	}

	private async Task<bool> ValidateForm()
	{
		if (userCodeEntry.Text is null) return false;
		if (string.IsNullOrEmpty(passwordEntry.Text)) return false;

		var user = await CommonData.LoadTableDataByCodeActive<UserModel>(Table.User, userCodeEntry.Text);
		if (user is null) return false;
		if (passwordEntry.Text != user.Password) return false;

		_userId = user.Id;
		return true;
	}

	private async void OnLoginButtonClicked(object sender, EventArgs e)
	{
		if (!await ValidateForm())
		{
			await DisplayAlert("Error", "Please Enter Correct User Id and Password", "OK");
			passwordEntry.Text = string.Empty;
			return;
		}

		await SecureStorage.Default.SetAsync(CURRENT_USER_ID_KEY, _userId.ToString());

		await Navigation.PushAsync(new OrderPage(_userId));

		userCodeEntry.Text = string.Empty;
		passwordEntry.Text = string.Empty;
	}
}
