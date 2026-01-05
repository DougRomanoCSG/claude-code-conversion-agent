using BargeOps.Shared.Dto;
using FluentValidation;

namespace BargeOps.Admin.Infrastructure.Validators;

/// <summary>
/// FluentValidation validator for FacilityStatusDto
/// </summary>
public class FacilityStatusDtoValidator : AbstractValidator<FacilityStatusDto>
{
    public FacilityStatusDtoValidator()
    {
        RuleFor(x => x.StartDateTime)
            .NotEmpty().WithMessage("Start date/time is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .MaximumLength(20).WithMessage("Status cannot exceed 20 characters");

        RuleFor(x => x.Note)
            .MaximumLength(4000).WithMessage("Note cannot exceed 4000 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.LocationID)
            .GreaterThan(0).WithMessage("Location ID is required");

        // EndDateTime must be >= StartDateTime
        RuleFor(x => x.EndDateTime)
            .GreaterThanOrEqualTo(x => x.StartDateTime)
            .WithMessage("End date/time must be later than or equal to start date/time")
            .When(x => x.EndDateTime.HasValue);
    }
}
