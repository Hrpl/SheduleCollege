using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using UserService.API.Extensions;
using UserService.Infrastructure.Context;

var builder = WebApplication.CreateBuilder(args);
builder.AddDataBase();
builder.AddJwt();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddService();
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();


app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());

app.UseEndpoints(m => m.MapControllers());

app.UseFastEndpoints(conf =>
{
    conf.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
    conf.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    conf.Endpoints.RoutePrefix = "api";
    conf.Endpoints.ShortNames = true;
});
app.UseOpenApi();
app.UseSwaggerUi(c => c.ConfigureDefaults());

app.MapControllers();

app.Run();
