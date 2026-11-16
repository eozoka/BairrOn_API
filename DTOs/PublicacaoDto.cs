using System.ComponentModel.DataAnnotations;

namespace BairrOn.Api.DTOs;

public class PublicacaoDto
{
    [Required, StringLength(150)]
    public string Titulo { get; set; } = "";

    [Required, StringLength(2000)]
    public string Descricao { get; set; } = "";

    [Required, StringLength(300)]
    public string Endereco { get; set; } = "";

    [StringLength(300)]
    public string? PontoReferencia { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoriaId { get; set; }

    public bool Anonima { get; set; }
    public DateTimeOffset? InicioEm { get; set; }
    public DateTimeOffset? EncerramentoEm { get; set; }
}
