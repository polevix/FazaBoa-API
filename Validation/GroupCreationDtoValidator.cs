using FazaBoa_API.Dtos;
using FluentValidation;

namespace APP.Validation
{
    public class GroupCreationDtoValidator : AbstractValidator<GroupCreationDto>
    {
        public GroupCreationDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("O nome do grupo é obrigatório.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("A descrição do grupo é obrigatória.");
            RuleFor(x => x.Photo).NotNull().When(x => x.Photo != null).WithMessage("O upload de uma imagem válida é obrigatório.")
                .Must(BeAValidImage).WithMessage("O arquivo deve ser uma imagem válida.")
                .Must(BeOfValidSize).WithMessage("O tamanho da imagem não pode exceder 2MB.");
        }

        private bool BeAValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        private bool BeOfValidSize(IFormFile file)
        {
            return file.Length <= 2 * 1024 * 1024; // 2MB
        }
    }

}