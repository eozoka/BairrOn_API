using System.Security.Claims;
using BairrOn.Api.Data;
using BairrOn.Api.DTOs;
using BairrOn.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BairrOn.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly BairrOnDbContext _context;

    public CategoriasController(BairrOnDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        // retorna só os campos públicos, sem os dados do criador
        var categorias = await _context.Categorias.OrderBy(c => c.Nome).ThenBy(c => c.Id)
            .Select(c => new { c.Id, c.Nome, c.Descricao, c.Tipo }).ToListAsync();
        return Ok(categorias);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null)
            return NotFound(new { mensagem = "Categoria não encontrada." });

        return Ok(new { categoria.Id, categoria.Nome, categoria.Descricao, categoria.Tipo });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Criar(CategoriaDto dados)
    {
        var categoria = new Categoria
        {
            Nome = dados.Nome.Trim(),
            Descricao = dados.Descricao.Trim(),
            Tipo = dados.Tipo,
            CriadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = categoria.Id },
            new { categoria.Id, categoria.Nome, categoria.Descricao, categoria.Tipo });
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, CategoriaDto dados)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null)
            return NotFound(new { mensagem = "Categoria não encontrada." });

        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (categoria.CriadorId != usuarioId)
            return StatusCode(403, new { mensagem = "Somente o criador pode alterar esta categoria." });

        // mudar o tipo de uma categoria em uso afetaria as publicações dela
        if (categoria.Tipo != dados.Tipo && await _context.Publicacoes.AnyAsync(p => p.CategoriaId == id))
            return Conflict(new { mensagem = "Uma categoria em uso não pode mudar de tipo." });

        categoria.Nome = dados.Nome.Trim();
        categoria.Descricao = dados.Descricao.Trim();
        categoria.Tipo = dados.Tipo;
        categoria.Versao++;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null)
            return NotFound(new { mensagem = "Categoria não encontrada." });

        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (categoria.CriadorId != usuarioId)
            return StatusCode(403, new { mensagem = "Somente o criador pode excluir esta categoria." });

        if (await _context.Publicacoes.AnyAsync(p => p.CategoriaId == id))
            return Conflict(new { mensagem = "A categoria possui publicações vinculadas." });

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
