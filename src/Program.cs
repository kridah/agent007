using Agent007.Data;
using Agent007.LLM;
using Agent007.Models;
using Agent007.Tools;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddSingleton<OllamaService>();
builder.Services.AddSingleton<IModelRepository, OllamaModelRepository>();
builder.Services.AddScoped<LLMBackendFactory>();
builder.Services.AddTransient<OllamaApiClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<OllamaSettings>>().Value;
    return new OllamaApiClient(settings.BaseUrl);
});

builder.Services.AddDbContext<ChatDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHttpContextAccessor();

// Tools available for the LLMs
builder.Services.AddTransient<DiceRollTool>();
builder.Services.AddTransient<AssistantTool>();


// Authentication - simple and standard
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Initialize models on startup
using (var scope = app.Services.CreateScope())
{
    var modelRepository = scope.ServiceProvider.GetRequiredService<IModelRepository>();
    await modelRepository.RefreshModelsAsync();
}

// Order matters here!
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages(); // This is important for Razor Pages to work
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();