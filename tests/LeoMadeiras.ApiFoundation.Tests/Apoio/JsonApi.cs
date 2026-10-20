using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeoMadeiras.ApiFoundation.Tests.Apoio;

/// <summary>Mesmas opções de JSON da API (camelCase e enums como texto), para ler as respostas nos testes.</summary>
public static class JsonApi
{
    public static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
