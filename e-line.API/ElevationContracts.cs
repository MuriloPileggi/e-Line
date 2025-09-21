using System.Text.Json.Serialization;

namespace e_line.Api.Elevation;

public record OpenElevationRequest(
    [property: JsonPropertyName("locations")] List<LocationPoint> Locations
);

public record LocationPoint(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude
);

public record OpenElevationResponse(
    [property: JsonPropertyName("results")] List<ElevationResult> Results
);

public record ElevationResult(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("elevation")] double Elevation
);