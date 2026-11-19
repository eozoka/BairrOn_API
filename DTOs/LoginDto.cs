using System.ComponentModel.DataAnnotations;

namespace BairrOn.Api.DTOs;

public class LoginDto
{
    [Required, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(128)]
    public string Senha { get; set; } = "";
}
