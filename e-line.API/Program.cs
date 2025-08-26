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
app.UseCors("AllowAny");

// Define a simple test endpoint
app.MapGet("/api/test", () =>
{
    return Results.Ok(new { Message = "Hello from Azure!" });
});

// Define the mock route planning endpoint
app.MapPost("/api/route/plan", () =>
{
    // Hard coded response for now
    var mockResponse = new
    {
        routeId = "mock_123",
        polyline = "yxrpA~}hbOA?@A@?@?@A@?@A?@A?@A?@A?@?@?@?@?@A?@A?@A@?@A@??@A?@A?",
        etaSec = 7200, // 2 hours
        distanceKm = 150.5,
        stops = new[] {
            new {
                stationId = "mock_charger_1",
                arriveSoc = 15.0,
                departSoc = 80.0,
                minutes = 45,
                connectorType = "CCS2",
                powerKw = 50
            }
        }
    };

    return Results.Ok(mockResponse);
});

app.Run();