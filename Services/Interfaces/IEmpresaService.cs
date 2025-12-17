using NFSE_ABRASF.DTOs.Requests;
using NFSE_ABRASF.DTOs.Responses;
using NFSE_ABRASF.Models;

namespace NFSE_ABRASF.Services.Interfaces
{
    public interface IEmpresaService
    {
        Task<IEnumerable<EmpresaResponseDto>> ObterTodasAsync(int pagina = 1, int itensPorPagina = 10);
        Task<EmpresaResponseDto?> ObterPorIdAsync(int id);
        Task<EmpresaResponseDto> CriarAsync(CriarEmpresaDto dto);
        Task<bool> AtualizarAsync(int id, CriarEmpresaDto dto);
        Task<bool> DeletarAsync(int id);
        Task<bool> CnpjJaExisteAsync(string cnpj);
    }
}