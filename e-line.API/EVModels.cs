namespace e_line.Api;

public record EVModel(
    int Id, 
    string Brand,
    string Name,
    double UsableBatteryKwh,
    double KwhPerKm // Static value for now, represents battery drain
);