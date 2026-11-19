using System.Security.Claims;
using System.Text;
using BairrOn.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BairrOn.Api.Configuration;

// reúne as regras de autenticação para deixar o program mais fácil de acompanhar
public static class AutenticacaoConfig
{
    public static void Configurar(IServiceCollection services, IConfiguration configuration)
    {
        var chaveJwt = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(chaveJwt) || Encoding.UTF8.GetByteCount(chaveJwt) < 32)
            throw new InvalidOperationException("Configure Jwt:Key com pelo menos 32 bytes fora do projeto.");

        // confere a assinatura, o destinatário e a validade do token
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt)),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async contexto =>
                {
                    var idValido = int.TryParse(contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out int id);
                    var versaoValida = int.TryParse(contexto.Principal?.FindFirstValue("versao"), out int versao);
                    if (!idValido || !versaoValida)
                    {
                        contexto.Fail("Token inválido.");
                        return;
                    }

                    // bloqueia tokens antigos depois de trocar a senha ou excluir a conta
                    var banco = contexto.HttpContext.RequestServices.GetRequiredService<BairrOnDbContext>();
                    if (!await banco.Usuarios.AnyAsync(u => u.Id == id && u.VersaoToken == versao))
                        contexto.Fail("Conta ou token inválido.");
                }
            };
        });
        services.AddAuthorization();
    }
}
