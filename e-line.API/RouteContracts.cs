namespace e_line.Api;

// Represents data the mobile app will SEND to the API
public record RoutePlanRequest(M_GeoPoint Origin, M_GeoPoint Destination, int EvModelId ,double StartSoC);

// Represents the data the API will SEND BACK to the mobile app
public record RoutePlanResponse(string Polyline, List<ChargingStation> Stops, double? TotalDistanceKm, int? TotalTimeMinutes);
// Represents a single charging station
public record ChargingStation(string Name, M_GeoPoint Location, int PowerKw);

// Represents a geographic point
public record M_GeoPoint(double Latitude, double Longitude);