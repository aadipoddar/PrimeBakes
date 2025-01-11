using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Models;

namespace PrimeOrders;

public partial class MainPage : ContentPage
{
	private const string CURRENT_USER_ID_KEY = "user_id";

	public MainPage()
	{
		InitializeComponent();

		var userId = SecureStorage.GetAsync(CURRENT_USER_ID_KEY).GetAwaiter().GetResult();
		if (userId is not null && userId is not "0") Navigation.PushAsync(new OrderPage(int.Parse(userId)), true);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		LoadData();
	}

	private async void LoadData()
	{
		SecureStorage.Remove(CURRENT_USER_ID_KEY);

		userComboBox.ItemsSource = await CommonData.LoadTableData<UserModel>("User");
		userComboBox.DisplayMemberPath = nameof(UserModel.Name);
		userComboBox.SelectedValuePath = nameof(UserModel.Id);
	}

	private async void OnLoginButtonClicked(object sender, EventArgs e)
	{
		if (userComboBox.SelectedItem is UserModel user)
		{
			if (passwordEntry.Text == user.Password)
			{
				await SecureStorage.Default.SetAsync(CURRENT_USER_ID_KEY, user.Id.ToString());

				await Navigation.PushAsync(new OrderPage(user.Id));
			}

			else await DisplayAlert("Error", "Incorrect Password", "OK");
		}

		else await DisplayAlert("Error", "Please select a user", "OK");

		passwordEntry.Text = string.Empty;
	}
}
