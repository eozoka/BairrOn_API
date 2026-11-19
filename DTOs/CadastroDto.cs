using System.ComponentModel.DataAnnotations;

namespace BairrOn.Api.DTOs;

// recebe somente os dados que podem vir do cadastro
public class CadastroDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nome { get; set; } = "";

    [Required, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 8)]
    public string Senha { get; set; } = "";
}
