using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using src.Data;
using src.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<LLMService>();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks()
    .AddCheck("SubNodeHealthCheck", () => HealthCheckResult.Healthy("SubNode is healthy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapBlazorHub();
    endpoints.MapHub<LLMService>("/llmHub");
    endpoints.MapHealthChecks("/health");
    endpoints.MapFallbackToPage("/_Host");
});

app.Run();
