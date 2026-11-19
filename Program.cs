using BairrOn.Api.Configuration;
using System.Text.Json.Serialization;
using BairrOn.Api.Data;
using BairrOn.Api.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
AutenticacaoConfig.Configurar(builder.Services, builder.Configuration);

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

var app = builder.Build();

TratamentoErrosConfig.Configurar(app);

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
