using BairrOn.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BairrOn.Api.Data;

public class BairrOnDbContext : DbContext
{
    public BairrOnDbContext(DbContextOptions<BairrOnDbContext> options) : base(options)
    {
    }

    // cada dbset representa uma tabela do banco
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Publicacao> Publicacoes => Set<Publicacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var usuario = modelBuilder.Entity<Usuario>();
        usuario.ToTable("BRN_USUARIOS");
        usuario.Property(u => u.Nome).HasMaxLength(100).IsRequired();
        usuario.Property(u => u.Email).HasMaxLength(254).IsRequired();
        usuario.Property(u => u.EmailNormalizado).HasMaxLength(254).IsRequired();
        usuario.Property(u => u.SenhaHash).HasMaxLength(512).IsRequired();
        usuario.HasIndex(u => u.EmailNormalizado).IsUnique();

        var categoria = modelBuilder.Entity<Categoria>();
        categoria.ToTable("BRN_CATEGORIAS");
        categoria.Property(c => c.Nome).HasMaxLength(80).IsRequired();
        categoria.Property(c => c.Descricao).HasMaxLength(500).IsRequired();
        categoria.Property(c => c.Versao).IsConcurrencyToken();
        categoria.HasOne(c => c.Criador).WithMany().HasForeignKey(c => c.CriadorId)
            .OnDelete(DeleteBehavior.Restrict);

        var publicacao = modelBuilder.Entity<Publicacao>();
        publicacao.ToTable("BRN_PUBLICACOES");
        publicacao.Property(p => p.Titulo).HasMaxLength(150).IsRequired();
        publicacao.Property(p => p.Descricao).HasMaxLength(2000).IsRequired();
        publicacao.Property(p => p.Endereco).HasMaxLength(300).IsRequired();
        publicacao.Property(p => p.PontoReferencia).HasMaxLength(300);
        publicacao.Property(p => p.Anonima).HasConversion<int>();

        // uma categoria e um usuário podem ter várias publicações
        publicacao.HasOne(p => p.Categoria).WithMany().HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
        publicacao.HasOne(p => p.Autor).WithMany().HasForeignKey(p => p.AutorId)
            .OnDelete(DeleteBehavior.Restrict);

        // mantém os nomes das colunas em maiúsculas no oracle
        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var propriedade in entidade.GetProperties())
                propriedade.SetColumnName(propriedade.Name.ToUpperInvariant());
        }
    }
}
