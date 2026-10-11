using Microsoft.EntityFrameworkCore;
using ProductAPI.Models;

namespace ProductAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var dbDirectory = Path.Combine(builder.Environment.ContentRootPath, "Database");
        Directory.CreateDirectory(dbDirectory);
        var dbPath = Path.Combine(dbDirectory, "ProductList.db");

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddDbContext<ProductContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ProductContext>();
            dbContext.Database.EnsureCreated();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.MapGet("/", () => "Hello world!");

        app.Run();
    }
}
