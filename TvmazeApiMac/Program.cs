using TvmazeApiMac.Models;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

app.MapGet("api/ping", () =>
    {
        return Results.Ok("pong");
    })
   .WithName("ping")
   .WithOpenApi();

app.MapGet("api/RestartPlex", () =>
    {
        var plexRestart = new RestartPlex();
        plexRestart.Execute();

        return Results.Ok(plexRestart.Execute());
    })
   .WithName("ExecuteRestartPlex")
   .WithOpenApi();

builder.WebHost.UseUrls("http://CA-Server:6002");

app.Run();
