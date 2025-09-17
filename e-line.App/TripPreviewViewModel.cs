// In Eline.App/TripPreviewViewModel.cs
namespace e_line.App;

public class TripPreviewViewModel
{
    public string TotalTime { get; set; } = "";
    public string TotalDistance { get; set; } = "";
    public bool HasChargingStop { get; set; }
    public string ChargingStopSummary { get; set; } = "";
}