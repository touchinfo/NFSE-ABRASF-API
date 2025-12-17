using Microsoft.EntityFrameworkCore;
using NFSE_ABRASF.Data.Context;
using NFSE_ABRASF.Models;
using NFSE_ABRASF.Repositories.Interfaces;

namespace NFSE_ABRASF.Repositories
{
    public class EmpresaRepository : IEmpresaRepository
    {
        private readonly AppDbContext _context;

        public EmpresaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Empresas>> ObterTodasAsync(int pagina, int itensPorPagina)
        {
            return await _context.Empresas!
                .AsNoTracking()
                .OrderByDescending(e => e.created_At)
                .Skip((pagina - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToListAsync();
        }

        public async Task<Empresas?> ObterPorIdAsync(int id)
        {
            return await _context.Empresas!
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmpresaId == id);
        }

        public async Task<Empresas?> ObterPorCnpjAsync(string cnpj)
        {
            return await _context.Empresas!
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Cnpj == cnpj);
        }

        public async Task<Empresas> AdicionarAsync(Empresas empresa)
        {
            await _context.Empresas!.AddAsync(empresa);
            await _context.SaveChangesAsync();
            return empresa;
        }

        public async Task<bool> AtualizarAsync(Empresas empresa)
        {
            empresa.updated_At = DateTime.Now;
            _context.Empresas!.Update(empresa);
            var resultado = await _context.SaveChangesAsync();
            return resultado > 0;
        }

        public async Task<bool> DeletarAsync(int id)
        {
            var empresa = await _context.Empresas!.FindAsync(id);
            if (empresa == null)
                return false;

            _context.Empresas.Remove(empresa);
            var resultado = await _context.SaveChangesAsync();
            return resultado > 0;
        }

        public async Task<bool> CnpjExisteAsync(string cnpj)
        {
            return await _context.Empresas!
                .AsNoTracking()
                .AnyAsync(e => e.Cnpj == cnpj);
        }

        public async Task<int> ContarAsync()
        {
            return await _context.Empresas!.CountAsync();
        }
    }
}