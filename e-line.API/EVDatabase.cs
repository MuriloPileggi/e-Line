namespace e_line.Api;

public static class EVDatabase
{
    private static readonly List<EVModel> _models = new()
    {
        // Mock data for Chevrolet Bolt
        new EVModel(
            Id: 1,
            Brand: "Chevrolet",
            Name: "Bolt EV",
            UsableBatteryKwh: 60.0,
            KwhPerKm: 0.17 // Approx. 170 Wh/Km
        ),
        // Mock data for BYD Dolphin
        new EVModel(
            Id: 2,
            Brand: "BYD",
            Name: "Dolphin",
            UsableBatteryKwh: 44.9,
            KwhPerKm: 0.15 // Approx. 150 Wh/Km
        )
    };

    public static EVModel? GetModelById(int id)
    {
        return _models.FirstOrDefault(m => m.Id == id);
    }
}