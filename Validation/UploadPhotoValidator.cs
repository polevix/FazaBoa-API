using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace FazaBoa_API.Validation
{
    public class UploadPhotoValidator : AbstractValidator<IFormFile>
    {
        public UploadPhotoValidator()
        {
            RuleFor(photo => photo)
                .NotNull().WithMessage("Nenhum arquivo carregado")
                .Must(photo => photo.Length > 0).WithMessage("O arquivo está vazio")
                .Must(photo => photo.ContentType.StartsWith("image/")).WithMessage("Tipo de arquivo inválido. Apenas arquivos de imagem são permitidos")
                .Must(photo => new[] { ".jpg", ".jpeg", ".png" }.Contains(Path.GetExtension(photo.FileName).ToLower())).WithMessage("Apenas arquivos JPG, JPEG e PNG são permitidos")
                .Must(photo => photo.Length <= 2 * 1024 * 1024).WithMessage("O tamanho do arquivo não pode exceder 2MB");
        }
    }
}
