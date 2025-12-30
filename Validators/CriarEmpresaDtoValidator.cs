using FluentValidation;
using NFSE_ABRASF.DTOs.Requests;
using NFSE_ABRASF.Helpers;

namespace NFSE_ABRASF.Validators
{
    public class CriarEmpresaDtoValidator : AbstractValidator<CriarEmpresaDto>
    {
        public CriarEmpresaDtoValidator()
        {
            RuleFor(x => x.Cnpj)
                .NotEmpty().WithMessage("O CNPJ é obrigatório")
                .MaximumLength(20).WithMessage("O CNPJ deve ter no máximo 20 caracteres")
                .Must(CnpjValidator.Validar).WithMessage("CNPJ inválido");

            RuleFor(x => x.Razao_Social)
                .NotEmpty().WithMessage("A Razão Social é obrigatória")
                .MaximumLength(255).WithMessage("A Razão Social deve ter no máximo 255 caracteres");

            RuleFor(x => x.Nome_Fantasia)
                .MaximumLength(255).WithMessage("O Nome Fantasia deve ter no máximo 255 caracteres");

            RuleFor(x => x.Inscricao_Municipal)
                .MaximumLength(20).WithMessage("A Inscrição Municipal deve ter no máximo 20 caracteres");

            RuleFor(x => x.Codigo_Municipio)
                .MaximumLength(7).WithMessage("O Código do Município deve ter no máximo 7 caracteres");

            RuleFor(x => x.CEP)
                .MaximumLength(10).WithMessage("O CEP deve ter no máximo 10 caracteres")
                .Matches(@"^\d{5}-?\d{3}$")
                .When(x => !string.IsNullOrEmpty(x.CEP))
                .WithMessage("CEP inválido. Use o formato 00000-000");

            RuleFor(x => x.UF)
                .MaximumLength(2).WithMessage("A UF deve ter 2 caracteres")
                .Matches(@"^[A-Z]{2}$")
                .When(x => !string.IsNullOrEmpty(x.UF))
                .WithMessage("UF inválida. Use 2 letras maiúsculas (ex: RJ)");

            RuleFor(x => x.Senha_Certificado)
                .NotEmpty()
                .When(x => x.CertificadoArquivo != null && x.CertificadoArquivo.Length > 0)
                .WithMessage("A senha do certificado é obrigatória quando um arquivo é enviado");

            RuleFor(x => x.CertificadoArquivo)
                .Must(arquivo => arquivo == null || arquivo.Length <= 10 * 1024 * 1024)
                .WithMessage("O arquivo do certificado deve ter no máximo 10MB")
                .Must(arquivo => arquivo == null || arquivo.FileName.EndsWith(".pfx") || arquivo.FileName.EndsWith(".p12"))
                .When(x => x.CertificadoArquivo != null)
                .WithMessage("O arquivo deve ser um certificado .pfx ou .p12");
        }
    }
}