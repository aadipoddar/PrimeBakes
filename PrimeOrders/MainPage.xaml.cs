using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Models;

namespace PrimeOrders;

public partial class MainPage : ContentPage
{
	public MainPage() => InitializeComponent();

	protected override void OnAppearing()
	{
		base.OnAppearing();

		LoadComboBox();
	}

	private async void LoadComboBox()
	{
		userComboBox.ItemsSource = await CommonData.LoadTableData<UserModel>("UserTable");
		userComboBox.DisplayMemberPath = nameof(UserModel.Name);
		userComboBox.SelectedValuePath = nameof(UserModel.Id);
	}

	private void OnLoginButtonClicked(object sender, EventArgs e)
	{
		if (userComboBox.SelectedItem is UserModel user)
			if (passwordEntry.Text == user.Password) Navigation.PushAsync(new OrderPage(user.Id));
			else DisplayAlert("Error", "Incorrect Password", "OK");
		else DisplayAlert("Error", "Please select a user", "OK");

		passwordEntry.Text = string.Empty;
	}
}
