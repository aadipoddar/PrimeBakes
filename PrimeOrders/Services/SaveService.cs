namespace PrimeOrders.Services;

public partial class SaveService
{
	//Method to save document as a file and view the saved document.
	public partial string SaveAndView(string filename, string contentType, MemoryStream stream);
}