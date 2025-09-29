using e_line.Api;
using Azure;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;
using Azure.Maps.Routing;
using Azure.Core.GeoJson;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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

// Register OpenChargeMapService with a dedicated HttpClient
builder.Services.AddHttpClient<OpenChargeMapService>();

// Register Text To Speech Service
builder.Services.AddSingleton<TextToSpeechService>();

// Register DbContext service
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ElineDbContext>(options => options.UseSqlServer(connectionString));

var app = builder.Build();

// Create database and table on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ElineDbContext>();
    dbContext.Database.EnsureCreated();
}

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

// Azure endpoint to get EV models from SQL database
app.MapGet("/api/evmodels", async (ElineDbContext dbContext) =>
{
    var models = await dbContext.EVModels.ToListAsync();
    return Results.Ok(models);
});

// Azure maps geocoding endpoint (Get coordinate from address)
app.MapPost("/api/geocode", async (GeocodeRequest request,  ILogger<Program> logger) =>
{
    //logger.LogInformation("Geocoding request received for address: {Address}", request.Address);

    if (string.IsNullOrWhiteSpace(request.Address))
    {
        return Results.BadRequest("O endereço não pode estar vazio.");
    }

    try
    {
        var searchClient = new MapsSearchClient(credential);

        //logger.LogInformation("Calling Azure Maps geocoding service for: {Address}", request.Address);

        var searchResult = await searchClient.GetGeocodingAsync(request.Address);

        //logger.LogInformation("Azure Maps returned {FeatureCount} features", searchResult.Value.Features?.Count ?? 0);

        if (searchResult.Value.Features == null || searchResult.Value.Features.Count == 0)
        {
            //logger.LogWarning("No geocoding results found for address: {Address}", request.Address);
            return Results.NotFound(new { Message = "Endereço não encontrado." });
        }

        var firstFeature = searchResult.Value.Features[0];

        var coords = firstFeature.Geometry.Coordinates;

        var coordinates = new M_GeoPoint(coords[1], coords[0]);

        var response = new GeocodeResponse(coordinates);

        //logger.LogInformation("Geocoding successful. Address: {Address} -> Coordinates: {Lat}, {Lon}", 
        //    request.Address, coordinates.Latitude, coordinates.Longitude);

        return Results.Ok(response);
    }
    catch (System.Exception ex)
    {
        return Results.Problem($"Erro ao geocodificar endereço: {ex.Message}");
    }
});

// Azure maps route planning endpoint
app.MapPost("/api/route/plan", async (RoutePlanRequest request,
                                      MapsRoutingClient client,
                                      OpenChargeMapService ocmService,
                                      ElineDbContext dbContext,
                                      EvOptimizer optimizer) =>
{
    // Get selected EV Model
    var evModel = await dbContext.EVModels.FindAsync(request.EvModelId);
    if (evModel is null) return Results.BadRequest("Invalid EV Model");

    // Get route from Azure Maps
    var routeQuery = new RouteDirectionQuery(new List<GeoPosition>
    {
        new GeoPosition(request.Origin.Longitude, request.Origin.Latitude),
        new GeoPosition(request.Destination.Longitude, request.Destination.Latitude)
    }, new RouteDirectionOptions
    {
        // Use current time to get a route based on live traffic
        DepartAt = DateTimeOffset.UtcNow,
        // Ensure the travel time calculation includes traffic delays
        TravelTimeType = TravelTimeType.All,
        // Specify that we want the fastest route considering current conditions
        RouteType = RouteType.Fastest
    });

    // Call the Azure Maps Directions API
    var directionsResult = await client.GetDirectionsAsync(routeQuery);

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
    var stopRequired = optimizer.IsStopRequired(pointCoordinates, evModel, request.StartSoC);

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

// Azure voice recognition endpoint
app.MapPost("/api/voice/intent", async (VoiceIntentRequest request, TextToSpeechService ttsService) =>
{
    string responseText = "Desculpe, não entendi o comando.";

    // Simple Intent recognition, for now we just check for keywords
    var normalizedText = request.Text.ToLowerInvariant();
    if (normalizedText.Contains("qual") && normalizedText.Contains("chegada"))
    {
        // If the intent is matched create a dynamic response
        var arrivalTime = DateTime.Now.AddMinutes(45);
        responseText = $"Sua chegada está prevista para às {arrivalTime::HH::mm}.";
    }
    else if (normalizedText.Contains("confirme") && normalizedText.Contains("viagem"))
    {
        responseText = "Sua viagem está confirmada, com origem Aeroporto de Guarulhos e destino FIAP";
    }

    // Text to speech call
    var audioData = await ttsService.SynthesizeSpeechAsync(responseText);

    if (audioData is not null)
    {
        // Return audio data as mp3 file
        return Results.File(audioData, "audio/mpeg", "response.mp3");
    }

    return Results.Problem("Failed to synthesize speech");
});

// Endpoint for generating speech tokens
app.MapGet("/api/auth/speech-token", async (IConfiguration config) =>
{
    var speechKey = config["AzureSpeech:SubscriptionKey"];
    var speechRegion = config["AzureSpeech:Region"];
    if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
    {
        return Results.Problem("Speech service not configured");
    }

    string tokenEndpoint = $"https://{speechRegion}.api.cognitive.microsoft.com/sts/v1.0/issueToken";

    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", speechKey);

    var response = await client.PostAsync(tokenEndpoint, null);
    if (response.IsSuccessStatusCode)
    {
        var token = await response.Content.ReadAsStringAsync();
        return Results.Ok(new { Token = token, Region = speechRegion });
    }

    return Results.Problem("Failed to get speech token from Azure");
});

// Endpoint for generating maps tokens
app.MapGet("/api/config/maps-key", (IConfiguration config) =>
{
    var mapsKey = config["AzureMaps:SubscriptionKey"];

    if (string.IsNullOrEmpty(mapsKey))
    {
        // Return an explicit error so the mobile app knows what's wrong
        return Results.Problem("Azure Maps key is not configured on the server.");
    }
    
    return Results.Ok(new { Key = mapsKey });
});


#endregion


app.Run();

// Define simple record for Voice intent request
public record VoiceIntentRequest(string Text);