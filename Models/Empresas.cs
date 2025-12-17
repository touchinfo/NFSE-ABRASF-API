using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFSE_ABRASF.Models;

[Table("empresas")] 
public class Empresas
{
    [Key] 
    public int EmpresaId { get; set; }

    [Required] 
    [MaxLength(20)] 
    public string? Cnpj { get; set; }

    [Required]
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

    [Column(TypeName = "longblob")]
    public byte[]? Certificado_Pfx { get; set; }

    [MaxLength(255)]
    public string? Senha_Certificado { get; set; }

    public DateTime? Certificado_Validade { get; set; }

    [MaxLength(255)]
    public string? Certificado_Emissor { get; set; }

    [MaxLength(255)]
    public string? Certificado_Titular { get; set; }

    [MaxLength(1)] 
    public string? Tipo_Ambiente { get; set; }

    [MaxLength(100)] 
    public string? Api_Key_Ativa { get; set; }

    public bool Ativa { get; set; } = true; 

    public DateTime created_At { get; set; } = DateTime.Now; 
    public DateTime? updated_At { get; set; }
}