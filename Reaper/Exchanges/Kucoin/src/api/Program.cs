using Reaper.CommonLib.Interfaces;
using Reaper.Exchanges.Kucoin.Interfaces;
using Reaper.Exchanges.Kucoin.Services;
using Reaper.Exchanges.Kucoin.Services.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Configuration.AddUserSecrets<Program>();
builder.Services.Configure<KucoinOptions>(builder.Configuration.GetSection("Kucoin"));
builder.Services.AddScoped<IBrokerService, BrokerService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();
builder.Services.AddScoped<IMarketDataService, FuturesMarketDataService>();
builder.Services.AddScoped<IBackTestService, BackTestService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<ITilsonService, TilsonService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
