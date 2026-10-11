using WebApp.Services;

namespace WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        var productApiBaseUrl = builder.Configuration["ProductApi:BaseUrl"];
        if (!Uri.TryCreate(productApiBaseUrl, UriKind.Absolute, out var productApiBaseUri))
        {
            throw new InvalidOperationException("ProductApi:BaseUrl must be configured as an absolute URI.");
        }

        builder.Services.AddHttpClient<ProductApiClient>(client =>
        {
            client.BaseAddress = productApiBaseUri;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Products}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.Run();
    }
}
