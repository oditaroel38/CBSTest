using ACDI.DataContext;
using CBS.Common;
using CBS.Services.Remittance;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/cbs.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container

// Register HTTP Client
builder.Services.AddHttpClient();

// Set EPPlus License context
ExcelPackage.LicenseContext = LicenseContext.Commercial;

// Register DbContext with SQL Server connection
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

// Register custom services
builder.Services.AddScoped<IRemittanceServices, RemittanceServices>();

// Configure CORS policy to allow your React app to make requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
        builder.WithOrigins("http://localhost:3000") // Allow React app (localhost:3000)
               .AllowAnyMethod()                   // Allow all methods (GET, POST, etc.)
               .AllowAnyHeader());                 // Allow all headers
});

// Configure JSON options for API responses
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

var app = builder.Build();

// Apply the CORS policy globally
app.UseCors("AllowReactApp");  // Allow React app's domain

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Remittance/Error");
}

// Enable middleware for serving static files, routing, and authorization
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Map controller routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Remittance}/{action=Index}/{id?}");

// Redirect root request to Remittance page
app.MapGet("/", async context =>
{
    var page = context.Request.Query["page"];
    var redirectUrl = $"/Remittance/?page={page}";
    context.Response.Redirect(redirectUrl);
    await Task.CompletedTask;
});

app.Run();
