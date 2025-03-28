using DataInfo.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddDistributedMemoryCache();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("https://task2-rzvm.onrender.com/",
                                "https://datainfo-9w7m.onrender.com/",
                                "https://localhost:44387/")
                             .AllowAnyMethod()
                             .AllowAnyHeader();
        });
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add IWebHostEnvironment
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);


var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];
var supabaseClient = new Supabase.Client(supabaseUrl??"", supabaseKey);
await supabaseClient.InitializeAsync();

builder.Services.AddSingleton(supabaseClient);


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

app.UseRouting();
app.UseCors();
app.UseSession();
app.UseAuthentication();  
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.MapStaticAssets();

app.Use(async (context, next) =>
{

    var loginPath = "/Home/Login";
    var currentPath = context.Request.Path.Value?.ToLower();


    if (currentPath == loginPath.ToLower() || context.Request.Path.StartsWithSegments("/Uploads"))
    {
        await next(context);
        return;
    }
    var authToken = context.Session.GetString("AuthToken") ?? "";
    if (string.IsNullOrEmpty(authToken))
    {

        context.Response.Redirect(loginPath);
       
    }
    await next(context);
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