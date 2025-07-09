using Microsoft.EntityFrameworkCore;
using ElevatorApp.DataAccess.Context;
using ElevatorApp.Services;
using System.Text.Json.Serialization;
using ElevatorApp.Hubs;
using ElevatorApp.Services.Background;
using ElevatorApp;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;        ;
        options.JsonSerializerOptions.WriteIndented = true;
    });
// Register SignalR services
builder.Services.AddSignalR();
builder.Services.Configure<SimulationSettings>(
    builder.Configuration.GetSection("SimulationSettings"));

// Register elevator background simulation service
builder.Services.AddHostedService<ElevatorSimulationService>();


builder.Services.AddDbContext<ElevatorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BuildingService>();
builder.Services.AddScoped<ElevatorService>();
builder.Services.AddScoped<CallService>();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();
// Map SignalR hub for real-time elevator updates
app.MapHub<ElevatorHub>("/elevatorHub");

app.Run();
