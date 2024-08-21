using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace FazaBoa_API.Validation
{
    public class UploadProfilePhotoValidator : AbstractValidator<IFormFile>
    {
        public UploadProfilePhotoValidator()
        {
            RuleFor(file => file).NotNull().WithMessage("Nenhum arquivo carregado.");
            RuleFor(file => file.Length).GreaterThan(0).WithMessage("O arquivo não pode estar vazio.");
            //RuleFor(file => file.Length).LessThan(2).WithMessage("Deve ser apenas um arquivo."); verificar se é possível validar pelo front-end
            RuleFor(file => file.ContentType).Must(contentType => contentType.StartsWith("image/"))
                .WithMessage("Tipo de arquivo inválido. Apenas arquivos de imagem são permitidos.");
            RuleFor(file => file.Length).LessThanOrEqualTo(5 * 1024 * 1024) // 5 MB
                .WithMessage("O tamanho do arquivo não pode exceder 5 MB.");
        }
    }
}

