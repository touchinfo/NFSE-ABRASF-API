using Microsoft.AspNetCore.DataProtection;
using NFSE_ABRASF.Exceptions;
using NFSE_ABRASF.Services.Interfaces;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NFSE_ABRASF.Services
{
    public class CertificadoService : ICertificadoService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<CertificadoService> _logger;

        public CertificadoService(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<CertificadoService> logger)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
        }

        public async Task<CertificadoInfo> ProcessarCertificadoAsync(IFormFile arquivo, string senha)
        {
            if (arquivo == null || arquivo.Length == 0)
            {
                throw new BusinessException("Arquivo de certificado inválido.");
            }

            if (string.IsNullOrEmpty(senha))
            {
                throw new BusinessException("Para enviar o certificado digital, é obrigatório informar a senha.");
            }

            byte[] certificadoBytes;

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await arquivo.CopyToAsync(memoryStream);
                    certificadoBytes = memoryStream.ToArray();
                }

                using (var cert = new X509Certificate2(
                    certificadoBytes,
                    senha,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable))
                {
                    var validade = cert.NotAfter;
                    var titular = cert.GetNameInfo(X509NameType.SimpleName, false) ?? string.Empty;
                    var emissor = cert.IssuerName.Name ?? string.Empty;

                    if (!ValidarCertificado(validade))
                    {
                        throw new BusinessException($"O certificado venceu em {validade:dd/MM/yyyy}. Envie um certificado válido.");
                    }

                    _logger.LogInformation("Certificado processado com sucesso. Titular: {Titular}, Validade: {Validade}",
                        titular, validade);

                    return new CertificadoInfo
                    {
                        Bytes = certificadoBytes,
                        Validade = validade,
                        Emissor = emissor,
                        Titular = titular
                    };
                }
            }
            catch (CryptographicException)
            {
                _logger.LogWarning("Tentativa de upload de certificado com senha incorreta");
                throw new BusinessException("A senha do certificado está incorreta ou o arquivo .pfx é inválido.");
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar certificado");
                throw new BusinessException($"Erro ao processar o arquivo do certificado: {ex.Message}");
            }
        }

        public bool ValidarCertificado(DateTime validade)
        {
            return validade >= DateTime.Now;
        }

        public string CriptografarSenha(string senha)
        {
            if (string.IsNullOrEmpty(senha))
                return string.Empty;

            var protector = _dataProtectionProvider.CreateProtector("CertificadoSenhaProtector");
            return protector.Protect(senha);
        }

        public string DescriptografarSenha(string senhaCriptografada)
        {
            if (string.IsNullOrEmpty(senhaCriptografada))
                return string.Empty;

            var protector = _dataProtectionProvider.CreateProtector("CertificadoSenhaProtector");
            return protector.Unprotect(senhaCriptografada);
        }
    }
}