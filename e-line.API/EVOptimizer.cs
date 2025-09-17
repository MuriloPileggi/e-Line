using e_line.Api;

public class EvOptimizer
{
    // Saffety buffer for battery
    private const double SafetyMarginSoC = 0.10; // 10%

    public bool IsStopRequired(double? routeDistanceMeters, EVModel ev, double startSoC)
    {
        // Calculate the energy required for the trip
        var routeDistanceKm = routeDistanceMeters / 1000.0;
        var energyNeededKwh = routeDistanceKm * ev.KwhPerKm;

        // Calculate the available energy in car battery
        var energyAvailableKwh = ev.UsableBatteryKwh * (startSoC - SafetyMarginSoC);

        // Check if trip is possible without charging
        if (energyAvailableKwh >= energyNeededKwh)
        {
            // No stops needed
            return false;
        }

        /* // If a charge is needed, add a stop
        // For now hard-coded stops as place holders
        requiredStops.Add(
            new ChargingStation(
                "ChargeRequired at: Shopping Center Norte",
                new M_GeoPoint(-23.5215, -46.6245),
                150
            )
        ); */

        return true;
    }
}