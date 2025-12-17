namespace NFSE_ABRASF.DTOs.Responses
{
    public class EmpresaResponseDto
    {
        public int EmpresaId { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string Razao_Social { get; set; } = string.Empty;
        public string? Nome_Fantasia { get; set; }
        public string? Inscricao_Municipal { get; set; }
        public string? Codigo_Municipio { get; set; }
        public string? CEP { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? UF { get; set; }
        public DateTime? Certificado_Validade { get; set; }
        public string? Certificado_Titular { get; set; }
        public string? Certificado_Emissor { get; set; }
        public string? Tipo_Ambiente { get; set; }
        public bool Ativa { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class CriarEmpresaResponseDto
    {
        public string Mensagem { get; set; } = "Empresa criada com sucesso!";
        public int Id { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public DateTime? ValidadeCertificado { get; set; }
        public string ApiKey { get; set; } = string.Empty;
    }
}