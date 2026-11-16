using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BairrOn.Api.Data;

// permite usar os comandos de migrations sem iniciar a api
public class BairrOnDbContextFactory : IDesignTimeDbContextFactory<BairrOnDbContext>
{
    public BairrOnDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<BairrOnDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // a conexão de exemplo só permite gerar migrations, não acessar o banco real
        var conexao = config.GetConnectionString("BairrOnOracle")
            ?? "User Id=placeholder;Password=placeholder;Data Source=localhost/FREEPDB1";

        var options = new DbContextOptionsBuilder<BairrOnDbContext>()
            .UseOracle(conexao, oracle => oracle
                .UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)
                .MigrationsHistoryTable("BRN_MIGRATIONS"))
            .Options;

        return new BairrOnDbContext(options);
    }
}
