namespace e_line.Api;

// Represents data the mobile app will SEND to the API
public record RoutePlanRequest(GeoPoint Origin, GeoPoint Destination, int EvModelId);

// Represents the data the API will SEND BACK to the mobile app
public record RoutePlanResponse(string Polyline, List<ChargingStation> Stops);

// Represents a single charging station
public record ChargingStation(string Name, GeoPoint Location, int PowerKw);

// Represents a geographic point
public record GeoPoint(double Latitude, double Longitude);