using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BairrOn.Api.Data;
using BairrOn.Api.DTOs;
using BairrOn.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BairrOn.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly BairrOnDbContext _context;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public AuthController(BairrOnDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastro(CadastroDto dados)
    {
        var nome = dados.Nome.Trim();
        var email = dados.Email.Trim();
        var emailNormalizado = email.ToUpperInvariant();

        if (nome.Length < 2 || !new EmailAddressAttribute().IsValid(email) || string.IsNullOrWhiteSpace(dados.Senha))
            return ValidationProblem("Confira o nome, o e-mail e a senha.");

        // compara o e-mail sem diferenciar maiúsculas e minúsculas
        if (await _context.Usuarios.AnyAsync(u => u.EmailNormalizado == emailNormalizado))
            return Conflict(new { mensagem = "Este e-mail já está cadastrado." });

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            EmailNormalizado = emailNormalizado
        };

        // salva o hash, nunca a senha original
        usuario.SenhaHash = _hasher.HashPassword(usuario, dados.Senha);
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Created("/api/usuarios/me", new { usuario.Id, usuario.Nome, usuario.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dados)
    {
        if (!new EmailAddressAttribute().IsValid(dados.Email.Trim()))
            return ValidationProblem("Informe um e-mail válido.");

        var email = dados.Email.Trim().ToUpperInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.EmailNormalizado == email);

        if (usuario == null)
            return Unauthorized(new { mensagem = "E-mail ou senha incorretos." });

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dados.Senha);
        if (resultado == PasswordVerificationResult.Failed)
            return Unauthorized(new { mensagem = "E-mail ou senha incorretos." });

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.SenhaHash = _hasher.HashPassword(usuario, dados.Senha);
            await _context.SaveChangesAsync();
        }

        // o token identifica quem está fazendo as próximas requisições
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim("versao", usuario.VersaoToken.ToString())
        };
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var assinatura = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var expiraEm = DateTime.UtcNow.AddMinutes(60);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiraEm,
            signingCredentials: assinatura);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            expiraEm = new DateTimeOffset(expiraEm),
            tokenType = "Bearer"
        });
    }
}
