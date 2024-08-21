using FluentValidation;
using FazaBoa_API.Models;

namespace FazaBoa_API.Validation
{
    public class ChallengeValidator : AbstractValidator<Challenge>
    {
        public ChallengeValidator()
        {
            RuleFor(c => c.Name).NotEmpty().WithMessage("O nome do desafio é obrigatório.");
            RuleFor(c => c.Description).NotEmpty().WithMessage("A descrição do desafio é obrigatória.");
            RuleFor(c => c.CoinValue).GreaterThan(0).WithMessage("O valor da moeda deve ser maior que zero.");
            RuleFor(c => c.StartDate).LessThan(c => c.EndDate).When(c => c.StartDate.HasValue && c.EndDate.HasValue)
                .WithMessage("A data de início deve ser menor que a data de término.");
        }
    }
}
