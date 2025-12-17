using NFSE_ABRASF.Models;

namespace NFSE_ABRASF.Repositories.Interfaces
{
    public interface IEmpresaRepository
    {
        Task<IEnumerable<Empresas>> ObterTodasAsync(int pagina, int itensPorPagina);
        Task<Empresas?> ObterPorIdAsync(int id);
        Task<Empresas?> ObterPorCnpjAsync(string cnpj);
        Task<Empresas> AdicionarAsync(Empresas empresa);
        Task<bool> AtualizarAsync(Empresas empresa);
        Task<bool> DeletarAsync(int id);
        Task<bool> CnpjExisteAsync(string cnpj);
        Task<int> ContarAsync();
    }
}