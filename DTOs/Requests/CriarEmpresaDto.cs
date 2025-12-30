using System.ComponentModel.DataAnnotations;

namespace NFSE_ABRASF.DTOs.Requests
{
    public class CriarEmpresaDto
    {
        [Required(ErrorMessage = "O CNPJ é obrigatório")]
        [MaxLength(20)]
        public string? Cnpj { get; set; }

        [Required(ErrorMessage = "A Razão Social é obrigatória")]
        [MaxLength(255)]
        public string? Razao_Social { get; set; }

        [MaxLength(255)]
        public string? Nome_Fantasia { get; set; }

        [MaxLength(20)]
        public string? Inscricao_Municipal { get; set; }

        [MaxLength(7)]
        public string? Codigo_Municipio { get; set; }

        [MaxLength(10)]
        public string? CEP { get; set; }

        [MaxLength(150)]
        public string? Logradouro { get; set; }

        [MaxLength(20)]
        public string? Numero { get; set; }

        [MaxLength(100)]
        public string? Complemento { get; set; }

        [MaxLength(100)]
        public string? Bairro { get; set; }

        [MaxLength(2)]
        public string? UF { get; set; }

        public IFormFile? CertificadoArquivo { get; set; }

        [MaxLength(255)]
        public string? Senha_Certificado { get; set; }
    }
}