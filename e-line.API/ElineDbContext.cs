using Microsoft.EntityFrameworkCore;

namespace e_line.Api;

public class ElineDbContext : DbContext
{
    public ElineDbContext(DbContextOptions<ElineDbContext> options) : base(options) { }

    // EVModels table 
    public DbSet<EVModel> EVModels { get; set; }

    // This method is used to seed database with initial data
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EVModel>().HasData(
            new EVModel { Id = 1, Make = "Volvo", Name = "EX30", UsableBatteryKwh = 64.0, KwhPerKm = 0.165 },
            new EVModel { Id = 2, Make = "BYD", Name = "Dolphin", UsableBatteryKwh = 44.9, KwhPerKm = 0.150 },
            new EVModel { Id = 3, Make = "Fiat", Name = "500e", UsableBatteryKwh = 37.3, KwhPerKm = 0.145 },
            new EVModel { Id = 4, Make = "Volkswagen", Name = "ID.4", UsableBatteryKwh = 77.0, KwhPerKm = 0.180 },
            new EVModel { Id = 5, Make = "GWM", Name = "Ora 03", UsableBatteryKwh = 48.0, KwhPerKm = 0.160 } 
        );
    }
}