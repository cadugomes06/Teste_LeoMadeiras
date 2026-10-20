using System.Text.Json.Serialization;
using LeoMadeiras.ApiFoundation.Application;
using LeoMadeiras.ApiFoundation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.Services.InicializarBanco();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// Expõe a classe gerada pelos top-level statements para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;
