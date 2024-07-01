using FluentValidation;
using FazaBoa_API.Models;

namespace FazaBoa_API.Validation
{
    public class ResetPasswordValidator : AbstractValidator<ResetPassword>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório")
                .EmailAddress().WithMessage("Um email válido é obrigatório");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("O token é obrigatório");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("A nova senha é obrigatória")
                .MinimumLength(8).WithMessage("A senha deve ter pelo menos 8 caracteres")
                .Matches(@"[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula")
                .Matches(@"[a-z]").WithMessage("A senha deve conter pelo menos uma letra minúscula")
                .Matches(@"\d").WithMessage("A senha deve conter pelo menos um dígito")
                .Matches(@"[!@#$%^&*(),.?\"":{ }|<>]").WithMessage("A senha deve conter pelo menos um caractere especial");
        }
    }
}
