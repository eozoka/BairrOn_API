using BairrOn.Api.Entities;

namespace BairrOn.Api.DTOs;

// dados públicos da categoria, sem informações sobre o criador
public class CategoriaRespostaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public TipoPublicacao Tipo { get; set; }
}
