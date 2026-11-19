using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace BairrOn.Api.Configuration;

// centraliza as respostas de erro sem expor detalhes internos do banco
public static class TratamentoErrosConfig
{
    public static void Configurar(WebApplication app)
    {
        // trata falhas do banco sem mostrar senhas ou comandos sql na resposta
        app.UseExceptionHandler(tratamento => tratamento.Run(async contexto =>
        {
            var erro = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
            var erroOracle = erro?.InnerException as OracleException;
            int status = 500;
            var mensagem = "Não foi possível concluir a operação.";

            if (erro is DbUpdateConcurrencyException)
            {
                status = 409;
                mensagem = "O registro foi alterado. Consulte os dados e tente novamente.";
            }
            else if (erroOracle != null && (erroOracle.Number == 1 || erroOracle.Number == 2291 || erroOracle.Number == 2292))
            {
                status = 409;
                mensagem = "A operação conflita com um registro existente ou relacionado.";
            }

            contexto.Response.StatusCode = status;
            await contexto.Response.WriteAsJsonAsync(new { mensagem });
        }));
    }
}
