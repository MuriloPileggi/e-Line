using System.ComponentModel;
using System.Runtime.CompilerServices;
using AndroidX.Media3.Common;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace e_line.App;

// Simple class to store results from the popup 
public class RouteInputResult : INotifyPropertyChanged
{
    private string _originAddress = "";
    private string _destinationAddress = "";
    private double _startSoC = 80;

    public string OriginAddress
    {
        get => _originAddress;
        set
        {
            if (_originAddress != value)
            {
                _originAddress = value;
                OnPropertyChanged();
            }
        }
    }

    public string DestinationAddress
    {
        get => _destinationAddress;
        set
        {
            if (_destinationAddress != value)
            {
                _destinationAddress = value;
                OnPropertyChanged();
            }
        }
    }

    public double StartSoC
    {
        get => _startSoC;
        set
        {
            if (Math.Abs(_startSoC - value) > 0.1) // Small tolerance for double comparison
            {
                _startSoC = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class RouteInputPopup : Popup<RouteInputResult>
{
    // Create a public property to hold the data
    public RouteInputResult Result { get; } = new();

    public RouteInputPopup()
    {
        InitializeComponent();
        // Set default value for starting Soc
        Result.StartSoC = 80;
        this.BindingContext = Result;
    }

    private async void OnFindRouteClicked(object sender, EventArgs e)
    {
        await CloseAsync(Result);
    }
}