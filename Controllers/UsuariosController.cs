using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BairrOn.Api.Data;
using BairrOn.Api.DTOs;
using BairrOn.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BairrOn.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios/me")]
public class UsuariosController : ControllerBase
{
    private readonly BairrOnDbContext _context;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public UsuariosController(BairrOnDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Obter()
    {
        // o id vem do login, não de um campo enviado pelo usuário
        int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return NotFound(new { mensagem = "Usuário não encontrado." });

        return Ok(new { usuario.Id, usuario.Nome, usuario.Email });
    }

    [HttpPut]
    public async Task<IActionResult> Atualizar(UsuarioDto dados)
    {
        int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return NotFound(new { mensagem = "Usuário não encontrado." });

        var nome = dados.Nome.Trim();
        var email = dados.Email.Trim();
        var emailNormalizado = email.ToUpperInvariant();
        if (nome.Length < 2 || !new EmailAddressAttribute().IsValid(email))
            return BadRequest(new { mensagem = "Confira o nome e o e-mail." });

        if (await _context.Usuarios.AnyAsync(u => u.Id != id && u.EmailNormalizado == emailNormalizado))
            return Conflict(new { mensagem = "Este e-mail já está cadastrado." });

        if (dados.NovaSenha != null)
        {
            // só permite trocar a senha depois de conferir a atual
            if (string.IsNullOrWhiteSpace(dados.NovaSenha) || string.IsNullOrEmpty(dados.SenhaAtual)
                || _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dados.SenhaAtual) == PasswordVerificationResult.Failed)
                return BadRequest(new { mensagem = "Confira a senha atual e a nova senha." });

            usuario.SenhaHash = _hasher.HashPassword(usuario, dados.NovaSenha);
            usuario.VersaoToken++;
        }

        usuario.Nome = nome;
        usuario.Email = email;
        usuario.EmailNormalizado = emailNormalizado;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Excluir()
    {
        int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return NotFound(new { mensagem = "Usuário não encontrado." });

        // evita deixar publicações e categorias sem um responsável
        bool temConteudo = await _context.Categorias.AnyAsync(c => c.CriadorId == id)
            || await _context.Publicacoes.AnyAsync(p => p.AutorId == id);
        if (temConteudo)
            return Conflict(new { mensagem = "Remova suas publicações e categorias antes de excluir a conta." });

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
