using DevExpress.AspNetCore;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using reporting.Services;

var builder = WebApplication.CreateBuilder(args);

// Register reporting services in the application's dependency injection container.
builder.Services.AddDevExpressControls();
// Use the AddMvc (or AddMvcCore) method to add MVC services.
builder.Services.AddMvc();
// Register the storage after the AddDevExpressControls method call.
builder.Services.AddScoped<ReportStorageWebExtension, CustomReportStorageWebExtension>();
builder.Services.AddDbContext<ReportDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("ReportsDataConnectionString")));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseDevExpressControls();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
