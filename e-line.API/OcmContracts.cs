using System.Text.Json.Serialization;

namespace e_line.Api.Ocm;

// We only map the fields we care about from the large OCM JSON response.
public record OcmPoi
{
    [JsonPropertyName("AddressInfo")]
    public AddressInfo? AddressInfo { get; init; }

    [JsonPropertyName("Connections")]
    public List<Connection>? Connections { get; init; }
}

public record AddressInfo
{
    [JsonPropertyName("Title")]
    public string? Title { get; init; }

    [JsonPropertyName("Latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("Longitude")]
    public double Longitude { get; init; }
}

public record Connection
{
    [JsonPropertyName("PowerKW")]
    public double? PowerKw { get; init; }
}