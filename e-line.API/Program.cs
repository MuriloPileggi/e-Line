using e_line.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS (Crucial to allow communication with front-end)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAny", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// New test endpoint
app.MapGet("/api/test", () => new { Message = "Hello from local API!" });

// Add our mock route planning endpoint
app.MapPost("/api/route/plan", (RoutePlanRequest request) =>
{
    // For now we ignore request and return hard coded data
    // This polyline represents a path from Guarulhos Airport to Av.Paulista
    var polyline = "[[ -23.4322, -46.4692 ], [ -23.4389, -46.4800 ], [ -23.5215, -46.5218 ], [ -23.5300, -46.5333 ], [ -23.5489, -46.6377 ], [ -23.5613, -46.6565 ]]";

    // This list represents chargins stations to make stops
    var stops = new List<ChargingStation>
    {
        new ChargingStation(
            "Shopping Center Norte",
            new GeoPoint(-23.5215, -46.6565),
            150
        ),
        new ChargingStation(
            "Trianon-Masp",
            new GeoPoint(-23.5613, -46.6565),
            50
        )
    };

    var response = new RoutePlanResponse(polyline, stops);
    return Results.Ok(response);
});

app.Run();