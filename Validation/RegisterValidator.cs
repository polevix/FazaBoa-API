using FluentValidation;
using FazaBoa_API.Models;

namespace FazaBoa_API.Validation
{
    public class RegisterValidator : AbstractValidator<Register>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("O nome completo é obrigatório");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório")
                .EmailAddress().WithMessage("Um email válido é obrigatório");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("A senha é obrigatória")
                .MinimumLength(8).WithMessage("A senha deve ter pelo menos 8 caracteres")
                .Matches(@"[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula")
                .Matches(@"[a-z]").WithMessage("A senha deve conter pelo menos uma letra minúscula")
                .Matches(@"\d").WithMessage("A senha deve conter pelo menos um dígito")
                .Matches(@"[^\da-zA-Z]").WithMessage("A senha deve conter pelo menos um caractere especial.");

            RuleFor(x => x.MasterUserId)
                .NotEmpty().When(x => x.IsDependent)
                .WithMessage("O ID do Usuário Mestre é obrigatório para dependentes");
        }
    }
}
