using BargeOps.Shared.Dto;
using FluentValidation;

namespace BargeOps.Admin.Infrastructure.Validators;

/// <summary>
/// FluentValidation validator for FacilityBerthDto
/// </summary>
public class FacilityBerthDtoValidator : AbstractValidator<FacilityBerthDto>
{
    public FacilityBerthDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Berth name is required")
            .MaximumLength(50).WithMessage("Berth name cannot exceed 50 characters");

        RuleFor(x => x.LocationID)
            .GreaterThan(0).WithMessage("Location ID is required");
    }
}
