using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using BairrOn.Api.Data;
using BairrOn.Api.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oracle.ManagedDataAccess.Client;

var builder = WebApplication.CreateBuilder(args);
var chaveJwt = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(chaveJwt) || Encoding.UTF8.GetByteCount(chaveJwt) < 32)
    throw new InvalidOperationException("Configure Jwt:Key com pelo menos 32 bytes fora do projeto.");

// conecta o entity framework ao oracle
builder.Services.AddDbContext<BairrOnDbContext>(options => options.UseOracle(
    builder.Configuration.GetConnectionString("BairrOnOracle"),
    oracle => oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)
        .MigrationsHistoryTable("BRN_MIGRATIONS")));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    options.JsonSerializerOptions.Converters.Add(new DataComFusoConverter());
});

// cria a página de testes e o botão para informar o token do login
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BairrOn API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Faça login e cole somente o accessToken, sem escrever Bearer."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
// confere a assinatura, o destinatário e a validade do token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
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
builder.Services.AddAuthorization();

var app = builder.Build();

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

// mostra o swagger durante o desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
