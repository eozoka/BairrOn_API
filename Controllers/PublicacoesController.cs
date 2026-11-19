using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BairrOn.Api.Data;
using BairrOn.Api.DTOs;
using BairrOn.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BairrOn.Api.Controllers;

[ApiController]
[Route("api/publicacoes")]
public class PublicacoesController : ControllerBase
{
    private readonly BairrOnDbContext _context;

    public PublicacoesController(BairrOnDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([Range(1, int.MaxValue)] int? categoriaId, TipoPublicacao? tipo, string? situacao)
    {
        var consulta = Consultar().AsNoTracking();
        if (categoriaId != null)
            consulta = consulta.Where(p => p.CategoriaId == categoriaId);

        if (tipo != null)
        {
            if (!Enum.IsDefined(tipo.Value))
                return BadRequest(new { mensagem = "Tipo inválido." });
            consulta = consulta.Where(p => p.Categoria.Tipo == tipo);
        }

        // o horário atual define quais acontecimentos ainda estão ativos
        var agora = DateTime.UtcNow;
        if (situacao != null)
        {
            switch (situacao.Trim().ToLowerInvariant())
            {
                case "aberto":
                    consulta = consulta.Where(p => p.Categoria.Tipo == TipoPublicacao.Problema && p.ResolvidaEm == null);
                    break;
                case "resolvido":
                    consulta = consulta.Where(p => p.Categoria.Tipo == TipoPublicacao.Problema && p.ResolvidaEm != null);
                    break;
                case "programado":
                    consulta = consulta.Where(p => p.Categoria.Tipo == TipoPublicacao.Acontecimento && p.InicioEm > agora);
                    break;
                case "acontecendo":
                    consulta = consulta.Where(p => p.Categoria.Tipo == TipoPublicacao.Acontecimento && p.InicioEm <= agora && p.EncerramentoEm > agora);
                    break;
                case "encerrado":
                    consulta = consulta.Where(p => p.Categoria.Tipo == TipoPublicacao.Acontecimento && p.EncerramentoEm <= agora);
                    break;
                default:
                    return BadRequest(new { mensagem = "Situação inválida." });
            }
        }

        var publicacoes = await consulta.OrderByDescending(p => p.CriadaEm).ThenByDescending(p => p.Id).ToListAsync();
        return Ok(publicacoes.Select(p => MontarResposta(p, agora)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id)
    {
        var publicacao = await Consultar().FirstOrDefaultAsync(p => p.Id == id);
        if (publicacao == null)
            return NotFound(new { mensagem = "Publicação não encontrada." });
        return Ok(MontarResposta(publicacao, DateTime.UtcNow));
    }

    [Authorize]
    [HttpGet("minhas")]
    public async Task<IActionResult> Minhas()
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var publicacoes = await Consultar().Where(p => p.AutorId == usuarioId)
            .OrderByDescending(p => p.CriadaEm).ThenByDescending(p => p.Id).ToListAsync();
        var agora = DateTime.UtcNow;
        return Ok(publicacoes.Select(p => MontarResposta(p, agora)));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Criar(PublicacaoDto dados)
    {
        var categoria = await _context.Categorias.FindAsync(dados.CategoriaId);
        if (categoria == null)
            return NotFound(new { mensagem = "Categoria não encontrada." });

        var erro = ValidarDatas(dados, categoria.Tipo);
        if (erro != null)
            return BadRequest(new { mensagem = erro });

        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return Unauthorized();

        // a publicação continua ligada ao autor, mesmo quando é anônima
        var publicacao = new Publicacao
        {
            AutorId = usuarioId,
            Autor = usuario,
            CriadaEm = DateTime.UtcNow
        };
        AplicarDados(publicacao, dados, categoria);
        _context.Publicacoes.Add(publicacao);
        categoria.Versao++;
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = publicacao.Id }, MontarResposta(publicacao, DateTime.UtcNow));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, PublicacaoDto dados)
    {
        var publicacao = await Consultar().FirstOrDefaultAsync(p => p.Id == id);
        if (publicacao == null)
            return NotFound(new { mensagem = "Publicação não encontrada." });

        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (publicacao.AutorId != usuarioId)
            return StatusCode(403, new { mensagem = "Somente o autor pode alterar esta publicação." });

        var categoria = await _context.Categorias.FindAsync(dados.CategoriaId);
        if (categoria == null)
            return NotFound(new { mensagem = "Categoria não encontrada." });
        if (categoria.Tipo != publicacao.Categoria.Tipo)
            return Conflict(new { mensagem = "A publicação não pode mudar de tipo." });

        var erro = ValidarDatas(dados, categoria.Tipo);
        if (erro != null)
            return BadRequest(new { mensagem = erro });

        // editar o texto não apaga a data de resolução
        AplicarDados(publicacao, dados, categoria);
        categoria.Versao++;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var publicacao = await _context.Publicacoes.FindAsync(id);
        if (publicacao == null)
            return NotFound(new { mensagem = "Publicação não encontrada." });

        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (publicacao.AutorId != usuarioId)
            return StatusCode(403, new { mensagem = "Somente o autor pode excluir esta publicação." });

        _context.Publicacoes.Remove(publicacao);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPut("{id:int}/resolucao")]
    public async Task<IActionResult> Resolver(int id)
    {
        var publicacao = await Consultar().FirstOrDefaultAsync(p => p.Id == id);
        if (publicacao == null)
            return NotFound(new { mensagem = "Publicação não encontrada." });

        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (publicacao.AutorId != usuarioId)
            return StatusCode(403, new { mensagem = "Somente o autor pode resolver esta publicação." });
        if (publicacao.Categoria.Tipo != TipoPublicacao.Problema)
            return BadRequest(new { mensagem = "Somente problemas podem ser marcados como resolvidos." });

        // mantém a primeira data se o problema já estiver resolvido
        if (publicacao.ResolvidaEm == null)
            publicacao.ResolvidaEm = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Publicacao> Consultar()
    {
        // carrega a categoria e o autor junto com a publicação
        return _context.Publicacoes.Include(p => p.Categoria).Include(p => p.Autor);
    }

    private string? ValidarDatas(PublicacaoDto dados, TipoPublicacao tipo)
    {
        if (tipo == TipoPublicacao.Acontecimento)
        {
            if (dados.InicioEm == null || dados.EncerramentoEm == null || dados.EncerramentoEm <= dados.InicioEm)
                return "Informe início e encerramento, com encerramento posterior ao início.";
        }
        else if (dados.InicioEm != null || dados.EncerramentoEm != null)
            return "Problemas não possuem datas de início e encerramento de acontecimento.";
        return null;
    }

    private void AplicarDados(Publicacao publicacao, PublicacaoDto dados, Categoria categoria)
    {
        publicacao.Titulo = dados.Titulo.Trim();
        publicacao.Descricao = dados.Descricao.Trim();
        publicacao.Endereco = dados.Endereco.Trim();
        publicacao.PontoReferencia = string.IsNullOrWhiteSpace(dados.PontoReferencia) ? null : dados.PontoReferencia.Trim();
        publicacao.CategoriaId = categoria.Id;
        publicacao.Categoria = categoria;
        publicacao.Anonima = dados.Anonima;
        publicacao.InicioEm = dados.InicioEm?.UtcDateTime;
        publicacao.EncerramentoEm = dados.EncerramentoEm?.UtcDateTime;
    }

    private PublicacaoRespostaDto MontarResposta(Publicacao publicacao, DateTime agora)
    {
        var nomeAutor = publicacao.Autor.Nome;
        if (publicacao.Anonima)
            nomeAutor = "Anônimo";

        // não retorna o id, o e-mail ou a senha do autor
        return new PublicacaoRespostaDto
        {
            Id = publicacao.Id,
            Titulo = publicacao.Titulo,
            Descricao = publicacao.Descricao,
            Endereco = publicacao.Endereco,
            PontoReferencia = publicacao.PontoReferencia,
            Categoria = new CategoriaRespostaDto
            {
                Id = publicacao.Categoria.Id,
                Nome = publicacao.Categoria.Nome,
                Descricao = publicacao.Categoria.Descricao,
                Tipo = publicacao.Categoria.Tipo
            },
            Anonima = publicacao.Anonima,
            Autor = nomeAutor,
            Situacao = publicacao.ObterSituacao(agora),
            CriadaEm = ComoUtc(publicacao.CriadaEm),
            InicioEm = ComoUtc(publicacao.InicioEm),
            EncerramentoEm = ComoUtc(publicacao.EncerramentoEm),
            ResolvidaEm = ComoUtc(publicacao.ResolvidaEm)
        };
    }

    private DateTimeOffset? ComoUtc(DateTime? data)
    {
        // informa que a data salva no banco está em utc
        if (data == null)
            return null;
        return new DateTimeOffset(DateTime.SpecifyKind(data.Value, DateTimeKind.Utc));
    }
}
