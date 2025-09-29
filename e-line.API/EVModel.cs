namespace e_line.Api;

public class EVModel
{
    public int Id { get; set; } // Primary key for SQL
    public string Make { get; set; } = "";
    public string Name { get; set; } = "";
    public double UsableBatteryKwh { get; set; }
    public double KwhPerKm { get; set; }
}