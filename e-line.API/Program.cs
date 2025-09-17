using e_line.Api;
using Azure;
using Azure.Maps.Routing;
using Azure.Core.GeoJson;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get the maps key from configuration
var mapsKey = builder.Configuration["AzureMaps:SubscriptionKey"];
if (string.IsNullOrEmpty(mapsKey))
{
    throw new InvalidOperationException("Azure Maps Subscription Key is not configured");
}
var credential = new AzureKeyCredential(mapsKey);

// Register the Maps routing client for dependency injection
builder.Services.AddSingleton(new MapsRoutingClient(credential));

// Add CORS (Crucial to allow communication with front-end)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAny", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Register Optimizer for dependency injection
builder.Services.AddSingleton<EvOptimizer>();

// Registor OpenChargeMapService with a dedicated HttpClient
builder.Services.AddHttpClient<OpenChargeMapService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region API Endpoints

// New test endpoint
app.MapGet("/api/test", () => new { Message = "Hello from local API!" });

// Azure maps route planning endpoint
app.MapPost("/api/route/plan", async (RoutePlanRequest request, MapsRoutingClient client, EvOptimizer optimizer, OpenChargeMapService ocmService) =>
{
    // Get selected EV Model
    var evModel = EVDatabase.GetModelById(request.EvModelId);
    if (evModel is null)
    {
        return Results.BadRequest("Invalid EV Model");
    }
    
    // Define the route points from the incoming request
    var routePoints = new List<GeoPosition>
    {
        new GeoPosition(request.Origin.Longitude, request.Origin.Latitude),
        new GeoPosition(request.Destination.Longitude, request.Destination.Latitude)
    };

    // Call the Azure Maps Directions API
    var directionsResult = await client.GetDirectionsAsync(new RouteDirectionQuery(routePoints));

    // Extract the coordinates from the first leg of the first route
    var routeLeg = directionsResult.Value.Routes.First().Legs.First();
    var pointCoordinates = routeLeg.Points;

    // Convert the coordinates into the simple [[lat, lon], ...] format our JS expects 
    var polylineForJS = pointCoordinates.Select(p => new[] { p.Latitude, p.Longitude }).ToList();
    // Serialize it into JSON string format (nested array)
    var polyline = JsonSerializer.Serialize(polylineForJS);

    // Use optimizer to calculate charging stops
    var requiredStops = new List<ChargingStation>();
    var routeDistanceMeters = routeLeg.Summary.LengthInMeters;
    var stopRequired = optimizer.IsStopRequired(routeDistanceMeters, evModel, request.StartSoC);

    if (stopRequired)
    {
        // If a stop is needed find chargers near the middle of the route
        var midpointIndex = pointCoordinates.Count / 2;
        var midpoint = pointCoordinates[midpointIndex];
        var midpointGeoPoint = new M_GeoPoint(midpoint.Latitude, midpoint.Longitude);

        requiredStops = await ocmService.GetChargersNearPoint(midpointGeoPoint);
    }

    // Create and return response
        var response = new RoutePlanResponse(
        polyline, 
        requiredStops,
        TotalDistanceKm: routeDistanceMeters / 1000.0,
        TotalTimeMinutes: (int)Math.Ceiling(routeLeg.Summary.TravelTimeInSeconds.GetValueOrDefault() / 60.0)
    );

    return Results.Ok(response);
});

#endregion

app.Run();