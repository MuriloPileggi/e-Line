namespace e_line.Api;

public record GeocodeRequest(string Address);
public record GeocodeResponse(M_GeoPoint? Coordinates);