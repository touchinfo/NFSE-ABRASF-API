using NFSE_ABRASF.DTOs;

namespace NFSE_ABRASF.Services.Interfaces
{
    public class CertificadoInfo
    {
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public DateTime Validade { get; set; }
        public string Emissor { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
    }

    public interface ICertificadoService
    {
        Task<CertificadoInfo> ProcessarCertificadoAsync(IFormFile arquivo, string senha);
        bool ValidarCertificado(DateTime validade);
        string CriptografarSenha(string senha);
        string DescriptografarSenha(string senhaCriptografada);
    }
}