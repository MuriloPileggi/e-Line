using e_line.App.ViewModels;

namespace e_line.App;

public partial class MainPage : ContentPage
{
	private readonly MainViewModel _viewModel;

	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	private async void OnCallApiClicked(object sender, EventArgs e)
	{
		await _viewModel.CallApi();
	}
}
