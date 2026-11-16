namespace BairrOn.Api.Entities;
public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public TipoPublicacao Tipo { get; set; }
    public int CriadorId { get; set; }
    public Usuario Criador { get; set; } = null!;
    // evita gravar uma publicação enquanto a categoria muda de tipo
    public int Versao { get; set; }
}
