namespace e_line.App;

public class EVModel
{
    public int Id { get; set; }
    public string Make { get; set; } = "";
    public string Name { get; set; } = "";
    public double UsableBatteryKwh { get; set; }
    public double KwhPerKm { get; set; }
    public string DisplayName => $"{Make} {Name}";
}