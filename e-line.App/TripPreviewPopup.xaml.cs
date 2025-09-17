// In Eline.App/TripPreviewPopup.xaml.cs
using CommunityToolkit.Maui.Views;

namespace e_line.App;

public partial class TripPreviewPopup : Popup
{
    public TaskCompletionSource<bool> ResultTaskCompletionSource { get; } = new();

    public TripPreviewPopup()
    {
        InitializeComponent();

        // Use a lambda expression for the Closed event.
        // This avoids the need for a separate method and the 'PopupClosedEventArgs' type name.
        Closed += (sender, args) =>
        {
            // If the popup is closed by any means (e.g., tapping outside),
            // this will set the result to 'false' if it hasn't been set already.
            ResultTaskCompletionSource.TrySetResult(false);
        };
    }

    private async void OnStartNavigationClicked(object sender, EventArgs e)
    {
        // Manually set our task's result to 'true'.
        ResultTaskCompletionSource.TrySetResult(true);
        
        // Close the popup UI.
        await CloseAsync();
    }
}