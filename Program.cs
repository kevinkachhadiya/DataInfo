using DataInfo.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ??
                       builder.Configuration.GetConnectionString("DefaultConnection");

var usePostgreSql = Environment.GetEnvironmentVariable("USE_POSTGRESQL") == "true" ||
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
  // if (usePostgreSql)
    {
       
        options.UseNpgsql(connectionString);
    }
 
  /*  else
    {

        options.UseSqlServer(connectionString);
    }
  */
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

string uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}")
    .WithStaticAssets();

if (app.Environment.IsDevelopment())
{
    
    app.Run(); 
}
else
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    app.Run($"http://0.0.0.0:{port}");
}