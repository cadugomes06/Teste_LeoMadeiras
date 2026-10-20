using System.Text.Json.Serialization;
using LeoMadeiras.ApiFoundation.Api.Workers;
using LeoMadeiras.ApiFoundation.Application;
using LeoMadeiras.ApiFoundation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<ReprocessamentoOptions>()
    .Bind(builder.Configuration.GetSection(ReprocessamentoOptions.Secao))
    .Validate(o => o.IntervaloSegundos > 0, $"{ReprocessamentoOptions.Secao}:IntervaloSegundos deve ser maior que zero.")
    .ValidateOnStart();
builder.Services.AddHostedService<ReprocessamentoNotificacoesWorker>();

var app = builder.Build();

app.Services.InicializarBanco();

// Exceção não tratada vira 500 em ProblemDetails, sem stack trace na resposta (o detalhe vai só para o log).
app.UseExceptionHandler();
// Erros que saem sem corpo (rota inexistente, método não permitido...) também respondem em ProblemDetails.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// Expõe a classe gerada pelos top-level statements para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;
