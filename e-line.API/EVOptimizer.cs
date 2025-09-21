using Azure.Core.GeoJson;
using e_line.Api;

public class EvOptimizer
{
    // Saffety buffer for battery
    private const double SafetyMarginSoC = 0.10; // 10%

    // Method that determines whether or not to add charging stops in route
    public bool IsStopRequired(
        IReadOnlyList<GeoPosition> routePoints,
        List<double> elevations,
        EVModel ev,
        double startSoC)
    {
        double currentSoC = startSoC;

        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            var startPoint = routePoints[i];
            var endPoint = routePoints[i + 1];

            var startElevation = elevations[i];
            var endElevation = elevations[i + 1];

            var energyUsedKwh = CalculateSegmentEnergy(startPoint, endPoint, startElevation, endElevation, ev);

            currentSoC -= energyUsedKwh / ev.UsableBatteryKwh;

            if (currentSoC <= SafetyMarginSoC)
            {
                return true;
            }
        }

        return false;
    }

    // Method to calculate current route segment energy consumption
    private double CalculateSegmentEnergy(
        GeoPosition start,
        GeoPosition end,
        double startElevation,
        double endElevation,
        EVModel ev)
    {
        var distanceKm = Haversine.GetDistance(start, end);
        var elevationChangeM = endElevation - startElevation;

        // Energy for distance (flat ground)
        var flatEnergy = distanceKm * ev.KwhPerKm;

        // Energy for elevation change (potential energy gain/loss)
        // This is a rough approximation. 
        // Vehicle mass and efficiency factor hard coded for now, later we get them from DB
        double vehicleMassKg = 2000;
        double gravity = 9.81;
        double efficiencyFactor = 0.75;
        var potentialEnergyJoules = vehicleMassKg * gravity * elevationChangeM;
        var potentialEnergyKwh = potentialEnergyJoules / (3.6e6 * efficiencyFactor);

        // If we are going downhill, regenerative braking recorver some energy
        if (potentialEnergyKwh < 0)
        {
            potentialEnergyKwh *= 0.6; // Assume 60% regen efficiency
        }

        return flatEnergy + potentialEnergyKwh;
    }

    /* 
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

        // If a charge is needed, add a stop
        // For now hard-coded stops as place holders
        requiredStops.Add(
            new ChargingStation(
                "ChargeRequired at: Shopping Center Norte",
                new M_GeoPoint(-23.5215, -46.6245),
                150
            )
        );

        return true;
    }
    */

}

// Helper class for distance calculations
public static class Haversine
{
    public static double GetDistance(GeoPosition pos1, GeoPosition pos2)
    {
        var R = 6371; // Earth's radius in Km
        var lat1 = pos1.Latitude * (Math.PI / 180.0);
        var lon1 = pos1.Longitude * (Math.PI / 180.0);
        var lat2 = pos2.Latitude * (Math.PI / 180.0);
        var lon2 = pos2.Longitude * (Math.PI / 180.0);

        var dLat = lat2 - lat1;
        var dLon = lon2 - lon1;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}