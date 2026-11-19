using System.ComponentModel.DataAnnotations;

namespace BairrOn.Api.DTOs;

public class UsuarioDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nome { get; set; } = "";

    [Required, StringLength(254)]
    public string Email { get; set; } = "";

    [StringLength(128)]
    public string? SenhaAtual { get; set; }

    [StringLength(128, MinimumLength = 8)]
    public string? NovaSenha { get; set; }
}
