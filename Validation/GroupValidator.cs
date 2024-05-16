using FluentValidation;
using FazaBoa_API.Models;

namespace FazaBoa_API.Validation
{
    public class GroupValidator : AbstractValidator<Group>
    {
        public GroupValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome do grupo é obrigatório")
                .MaximumLength(100).WithMessage("O nome do grupo deve ter no máximo 100 caracteres");

            RuleFor(x => x.PhotoUrl)
                .MaximumLength(200).WithMessage("A URL da foto deve ter no máximo 200 caracteres");
        }
    }
}
