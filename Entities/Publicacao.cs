namespace BairrOn.Api.Entities;
public class Publicacao
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string? PontoReferencia { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public int AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;
    public bool Anonima { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? InicioEm { get; set; }
    public DateTime? EncerramentoEm { get; set; }
    public DateTime? ResolvidaEm { get; set; }

    public string ObterSituacao(DateTime agora)
    {
        // problemas dependem da confirmação do autor
        if (Categoria.Tipo == TipoPublicacao.Problema)
        {
            if (ResolvidaEm == null)
                return "Aberto";
            return "Resolvido";
        }

        // acontecimentos mudam de situação conforme a data
        if (agora < InicioEm)
            return "Programado";
        if (agora < EncerramentoEm)
            return "Acontecendo";

        return "Encerrado";
    }
}
