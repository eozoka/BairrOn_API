namespace BairrOn.Api.DTOs;

// define os campos devolvidos nas consultas e na criação de publicações
public class PublicacaoRespostaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string? PontoReferencia { get; set; }
    public CategoriaRespostaDto Categoria { get; set; } = new();
    public bool Anonima { get; set; }
    public string Autor { get; set; } = "";
    public string Situacao { get; set; } = "";
    public DateTimeOffset? CriadaEm { get; set; }
    public DateTimeOffset? InicioEm { get; set; }
    public DateTimeOffset? EncerramentoEm { get; set; }
    public DateTimeOffset? ResolvidaEm { get; set; }
}
