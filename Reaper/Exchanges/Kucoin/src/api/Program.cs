using Reaper.Exchanges.Kucoin.Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

Serilog.Debugging.SelfLog.Enable(Console.Error);
Log.Logger = Dependencies.GetLogger();
builder.Services.AddReaperServices();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var app = builder.Build();
Reaper.Exchanges.Kucoin.Services.RLogger.AppLog.Information("APP STARTED...............");

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
