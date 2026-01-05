using BargeOps.Shared.Dto;
using FluentValidation;

namespace BargeOps.Admin.Infrastructure.Validators;

/// <summary>
/// FluentValidation validator for FacilityDto
/// Implements business rules and field validation
/// </summary>
public class FacilityDtoValidator : AbstractValidator<FacilityDto>
{
    public FacilityDtoValidator()
    {
        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Facility name is required")
            .MaximumLength(100).WithMessage("Facility name cannot exceed 100 characters");

        // ShortName validation
        RuleFor(x => x.ShortName)
            .MaximumLength(50).WithMessage("Short name cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.ShortName));

        // Note validation
        RuleFor(x => x.Note)
            .MaximumLength(255).WithMessage("Note cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));

        // Mile validation
        RuleFor(x => x.Mile)
            .LessThanOrEqualTo(2000.0m).WithMessage("Mile must be less than or equal to 2000")
            .When(x => x.Mile.HasValue);

        // River/Mile must both be present or both absent
        RuleFor(x => x)
            .Must(x => (string.IsNullOrEmpty(x.River) && !x.Mile.HasValue) ||
                      (!string.IsNullOrEmpty(x.River) && x.Mile.HasValue))
            .WithMessage("Cannot have River value without Mile value, or vice versa");

        // BargeExCode validation
        RuleFor(x => x.BargeExCode)
            .MaximumLength(10).WithMessage("BargeEx code cannot exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.BargeExCode));

        // Bank validation
        RuleFor(x => x.Bank)
            .MaximumLength(50).WithMessage("Bank cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Bank));

        // BargeExLocationType validation
        RuleFor(x => x.BargeExLocationType)
            .MaximumLength(20).WithMessage("Facility type cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.BargeExLocationType));

        // Lock/Gauge field validation - must be blank if type is not Lock or Gauge Location
        RuleFor(x => x.LockUsaceName)
            .Empty().WithMessage("USACE name must be blank if Facility type is not 'Lock' or 'Gauge Location'")
            .When(x => x.BargeExLocationType != "Lock" && x.BargeExLocationType != "Gauge Location" && !string.IsNullOrEmpty(x.LockUsaceName));

        RuleFor(x => x.LockFloodStage)
            .Null().WithMessage("Flood stage must be blank if Facility type is not 'Lock' or 'Gauge Location'")
            .When(x => x.BargeExLocationType != "Lock" && x.BargeExLocationType != "Gauge Location" && x.LockFloodStage.HasValue);

        RuleFor(x => x.LockPoolStage)
            .Null().WithMessage("Pool stage must be blank if Facility type is not 'Lock' or 'Gauge Location'")
            .When(x => x.BargeExLocationType != "Lock" && x.BargeExLocationType != "Gauge Location" && x.LockPoolStage.HasValue);

        RuleFor(x => x.LockLowWater)
            .Null().WithMessage("Low water must be blank if Facility type is not 'Lock' or 'Gauge Location'")
            .When(x => x.BargeExLocationType != "Lock" && x.BargeExLocationType != "Gauge Location" && x.LockLowWater.HasValue);

        RuleFor(x => x.LockNormalCurrent)
            .Null().WithMessage("Normal current must be blank if Facility type is not 'Lock' or 'Gauge Location'")
            .When(x => x.BargeExLocationType != "Lock" && x.BargeExLocationType != "Gauge Location" && x.LockNormalCurrent.HasValue);

        RuleFor(x => x.LockHighFlow)
            .Null().WithMessage("High flow must be blank if Facility type is not 'Lock' or 'Gauge Location'")
            .When(x => x.BargeExLocationType != "Lock" && x.BargeExLocationType != "Gauge Location" && x.LockHighFlow.HasValue);

        RuleFor(x => x.LockHighWater)
            .Null().WithMessage("High water must be blank if Facility type is not 'Lock' or 'Gauge Location'")
            .When(x => x.BargeExLocationType != "Lock" && x.BargeExLocationType != "Gauge Location" && x.LockHighWater.HasValue);

        RuleFor(x => x.LockCatastrophicLevel)
            .Null().WithMessage("Catastrophic level must be blank if Facility type is not 'Lock' or 'Gauge Location'")
            .When(x => x.BargeExLocationType != "Lock" && x.BargeExLocationType != "Gauge Location" && x.LockCatastrophicLevel.HasValue);

        // Lock/Gauge field string length validation
        RuleFor(x => x.LockUsaceName)
            .MaximumLength(50).WithMessage("USACE name cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.LockUsaceName));
    }
}
