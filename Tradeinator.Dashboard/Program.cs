using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Tradeinator.Configuration;
using Tradeinator.Dashboard.Data;
using Tradeinator.Shared;

DotEnv.LoadEnvFiles(Path.Join(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var host = configuration["Rabbit:Host"];
var exchangeName = configuration["Rabbit:Exchange"];
if(host is null || exchangeName is null)
{
    throw new ArgumentNullException("RabbitMQ host or exchange name was null in config file");
}

var exchange = new EventService(host, exchangeName);
await exchange.StartConsuming();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<EventService>(exchange);

// register default http client, required for fluent ui
builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();

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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();