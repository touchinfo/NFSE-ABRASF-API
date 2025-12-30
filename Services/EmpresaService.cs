using NFSE_ABRASF.DTOs.Requests;
using NFSE_ABRASF.DTOs.Responses;
using NFSE_ABRASF.Exceptions;
using NFSE_ABRASF.Helpers;
using NFSE_ABRASF.Models;
using NFSE_ABRASF.Repositories.Interfaces;
using NFSE_ABRASF.Services.Interfaces;

namespace NFSE_ABRASF.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly IEmpresaRepository _repository;
        private readonly ICertificadoService _certificadoService;
        private readonly ILogger<EmpresaService> _logger;

        public EmpresaService(
            IEmpresaRepository repository,
            ICertificadoService certificadoService,
            ILogger<EmpresaService> logger)
        {
            _repository = repository;
            _certificadoService = certificadoService;
            _logger = logger;
        }

        public async Task<IEnumerable<EmpresaResponseDto>> ObterTodasAsync(int pagina = 1, int itensPorPagina = 10)
        {
            var empresas = await _repository.ObterTodasAsync(pagina, itensPorPagina);

            if (!empresas.Any())
            {
                _logger.LogInformation("Nenhuma empresa encontrada");
            }

            return empresas.Select(MapToResponseDto);
        }

        public async Task<EmpresaResponseDto?> ObterPorIdAsync(int id)
        {
            var empresa = await _repository.ObterPorIdAsync(id);

            if (empresa == null)
            {
                throw new NotFoundException("Empresa", id);
            }

            return MapToResponseDto(empresa);
        }

        public async Task<EmpresaResponseDto> CriarAsync(CriarEmpresaDto dto)
        {
            // Validar CNPJ duplicado
            if (await _repository.CnpjExisteAsync(dto.Cnpj!))
            {
                throw new BusinessException("Já existe uma empresa cadastrada com este CNPJ.");
            }

            // Processar certificado se enviado
            CertificadoInfo? certInfo = null;
            string? senhaCriptografada = null;

            if (dto.CertificadoArquivo != null && dto.CertificadoArquivo.Length > 0)
            {
                certInfo = await _certificadoService.ProcessarCertificadoAsync(
                    dto.CertificadoArquivo,
                    dto.Senha_Certificado!);

                senhaCriptografada = _certificadoService.CriptografarSenha(dto.Senha_Certificado!);
            }

            // Gerar API Key
            var apiKey = SecurityHelper.GenerateApiKey();

            // Criar entidade
            var empresa = new Empresas
            {
                Cnpj = dto.Cnpj,
                Razao_Social = dto.Razao_Social,
                Nome_Fantasia = dto.Nome_Fantasia,
                Inscricao_Municipal = dto.Inscricao_Municipal,
                Codigo_Municipio = dto.Codigo_Municipio,
                CEP = dto.CEP,
                Logradouro = dto.Logradouro,
                Numero = dto.Numero,
                Complemento = dto.Complemento,
                Bairro = dto.Bairro,
                UF = dto.UF,
                Tipo_Ambiente = "2", // Sempre inicia em Homologação por segurança
                Certificado_Pfx = certInfo?.Bytes,
                Senha_Certificado = senhaCriptografada,
                Certificado_Validade = certInfo?.Validade,
                Certificado_Titular = certInfo?.Titular,
                Certificado_Emissor = certInfo?.Emissor,
                Api_Key_Ativa = apiKey,
                Ativa = true,
                created_At = DateTime.Now
            };

            await _repository.AdicionarAsync(empresa);

            _logger.LogInformation("Empresa criada com sucesso. ID: {EmpresaId}, CNPJ: {Cnpj}",
                empresa.EmpresaId, empresa.Cnpj);

            return new EmpresaResponseDto
            {
                EmpresaId = empresa.EmpresaId,
                Cnpj = empresa.Cnpj!,
                Razao_Social = empresa.Razao_Social!,
                Nome_Fantasia = empresa.Nome_Fantasia,
                Certificado_Validade = empresa.Certificado_Validade,
                Ativa = empresa.Ativa,
                Created_At = empresa.created_At
            };
        }

        public async Task<bool> AtualizarAsync(int id, CriarEmpresaDto dto)
        {
            var empresa = await _repository.ObterPorIdAsync(id);

            if (empresa == null)
            {
                throw new NotFoundException("Empresa", id);
            }

            // Atualizar campos
            empresa.Razao_Social = dto.Razao_Social;
            empresa.Nome_Fantasia = dto.Nome_Fantasia;
            empresa.Inscricao_Municipal = dto.Inscricao_Municipal;
            empresa.Codigo_Municipio = dto.Codigo_Municipio;
            empresa.CEP = dto.CEP;
            empresa.Logradouro = dto.Logradouro;
            empresa.Numero = dto.Numero;
            empresa.Complemento = dto.Complemento;
            empresa.Bairro = dto.Bairro;
            empresa.UF = dto.UF;
            // Tipo_Ambiente não é editável via API - sempre controlado internamente

            return await _repository.AtualizarAsync(empresa);
        }

        public async Task<bool> DeletarAsync(int id)
        {
            return await _repository.DeletarAsync(id);
        }

        public async Task<bool> CnpjJaExisteAsync(string cnpj)
        {
            return await _repository.CnpjExisteAsync(cnpj);
        }

        public async Task<bool> AlterarStatusAsync(int id, bool ativa)
        {
            var empresa = await _repository.ObterPorIdAsync(id);

            if (empresa == null)
            {
                throw new NotFoundException("Empresa", id);
            }

            empresa.Ativa = ativa;
            empresa.updated_At = DateTime.Now;

            var resultado = await _repository.AtualizarAsync(empresa);

            if (resultado)
            {
                _logger.LogInformation("Status da empresa {EmpresaId} alterado para {Status}",
                    id, ativa ? "Ativa" : "Inativa");
            }

            return resultado;
        }

        public async Task<bool> AlterarAmbienteAsync(int id, string tipoAmbiente)
        {
            if (tipoAmbiente != "1" && tipoAmbiente != "2")
            {
                throw new BusinessException("Tipo de ambiente inválido. Use '1' para Produção ou '2' para Homologação.");
            }

            var empresa = await _repository.ObterPorIdAsync(id);

            if (empresa == null)
            {
                throw new NotFoundException("Empresa", id);
            }

            empresa.Tipo_Ambiente = tipoAmbiente;
            empresa.updated_At = DateTime.Now;

            var resultado = await _repository.AtualizarAsync(empresa);

            if (resultado)
            {
                var nomeAmbiente = tipoAmbiente == "1" ? "Produção" : "Homologação";
                _logger.LogInformation("Ambiente da empresa {EmpresaId} alterado para {Ambiente}",
                    id, nomeAmbiente);
            }

            return resultado;
        }

        private static EmpresaResponseDto MapToResponseDto(Empresas empresa)
        {
            return new EmpresaResponseDto
            {
                EmpresaId = empresa.EmpresaId,
                Cnpj = empresa.Cnpj!,
                Razao_Social = empresa.Razao_Social!,
                Nome_Fantasia = empresa.Nome_Fantasia,
                Inscricao_Municipal = empresa.Inscricao_Municipal,
                Codigo_Municipio = empresa.Codigo_Municipio,
                CEP = empresa.CEP,
                Logradouro = empresa.Logradouro,
                Numero = empresa.Numero,
                Complemento = empresa.Complemento,
                Bairro = empresa.Bairro,
                UF = empresa.UF,
                Certificado_Validade = empresa.Certificado_Validade,
                Certificado_Titular = empresa.Certificado_Titular,
                Certificado_Emissor = empresa.Certificado_Emissor,
                Tipo_Ambiente = empresa.Tipo_Ambiente,
                Ativa = empresa.Ativa,
                Created_At = empresa.created_At
            };
        }
    }
}