using System.ComponentModel.DataAnnotations;
using BairrOn.Api.Entities;

namespace BairrOn.Api.DTOs;

public class CategoriaDto
{
    [Required, StringLength(80)]
    public string Nome { get; set; } = "";

    [Required, StringLength(500)]
    public string Descricao { get; set; } = "";

    [EnumDataType(typeof(TipoPublicacao))]
    public TipoPublicacao Tipo { get; set; }
}
