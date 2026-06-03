using System.Text.Json.Serialization;

namespace GestaoProvisao.Api.Models;

/// <summary>
/// Registro de provisao de receita. Espelha o objeto consumido pelo index.html
/// (array DATA) acrescido dos dados de contato (antes em CONTACTS).
/// Os nomes JSON sao mantidos em snake_case para casar com o frontend existente.
/// </summary>
public class ProvisaoRecord
{
    [JsonPropertyName("venc")]
    public string Venc { get; set; } = string.Empty;

    [JsonPropertyName("venc_orig")]
    public string VencOrig { get; set; } = string.Empty;

    [JsonPropertyName("dias")]
    public int Dias { get; set; }

    [JsonPropertyName("filial")]
    public string Filial { get; set; } = string.Empty;

    [JsonPropertyName("apolice")]
    public string Apolice { get; set; } = string.Empty;

    [JsonPropertyName("endosso")]
    public int Endosso { get; set; }

    [JsonPropertyName("grupo")]
    public string Grupo { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("parcela")]
    public int Parcela { get; set; }

    [JsonPropertyName("vl_total")]
    public decimal VlTotal { get; set; }

    [JsonPropertyName("prorr")]
    public int Prorr { get; set; }

    // ----- Contato (antes no objeto CONTACTS) -----

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("fone")]
    public string? Fone { get; set; }

    [JsonPropertyName("resp")]
    public string? Resp { get; set; }

    [JsonPropertyName("cnpj")]
    public string? Cnpj { get; set; }
}
