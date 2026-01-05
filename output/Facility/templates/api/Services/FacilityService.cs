using BargeOps.Admin.Infrastructure.Repositories;
using BargeOps.Shared.Dto;
using FluentValidation;

namespace BargeOps.Admin.Infrastructure.Services;

/// <summary>
/// Service for Facility business logic
/// Works with DTOs from repository (no AutoMapper needed!)
/// </summary>
public class FacilityService : IFacilityService
{
    private readonly IFacilityRepository _repository;
    private readonly IValidator<FacilityDto> _facilityValidator;
    private readonly IValidator<FacilityBerthDto> _berthValidator;
    private readonly IValidator<FacilityStatusDto> _statusValidator;

    public FacilityService(
        IFacilityRepository repository,
        IValidator<FacilityDto> facilityValidator,
        IValidator<FacilityBerthDto> berthValidator,
        IValidator<FacilityStatusDto> statusValidator)
    {
        _repository = repository;
        _facilityValidator = facilityValidator;
        _berthValidator = berthValidator;
        _statusValidator = statusValidator;
    }

    public async Task<PagedResult<FacilityDto>> SearchAsync(
        FacilitySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _repository.SearchAsync(request, cancellationToken);
    }

    public async Task<FacilityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<FacilityDto> CreateAsync(FacilityDto facility, CancellationToken cancellationToken = default)
    {
        // Validate
        var validationResult = await _facilityValidator.ValidateAsync(facility, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Apply business rules
        ApplyBusinessRules(facility);

        return await _repository.CreateAsync(facility, cancellationToken);
    }

    public async Task<FacilityDto> UpdateAsync(FacilityDto facility, CancellationToken cancellationToken = default)
    {
        // Validate
        var validationResult = await _facilityValidator.ValidateAsync(facility, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Apply business rules
        ApplyBusinessRules(facility);

        return await _repository.UpdateAsync(facility, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<FacilityBerthDto>> GetBerthsAsync(
        int facilityId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetBerthsAsync(facilityId, cancellationToken);
    }

    public async Task<IEnumerable<FacilityStatusDto>> GetStatusesAsync(
        int facilityId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetStatusesAsync(facilityId, cancellationToken);
    }

    public async Task<FacilityBerthDto> CreateBerthAsync(
        FacilityBerthDto berth,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _berthValidator.ValidateAsync(berth, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        return await _repository.CreateBerthAsync(berth, cancellationToken);
    }

    public async Task<FacilityBerthDto> UpdateBerthAsync(
        FacilityBerthDto berth,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _berthValidator.ValidateAsync(berth, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        return await _repository.UpdateBerthAsync(berth, cancellationToken);
    }

    public async Task<bool> DeleteBerthAsync(int berthId, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteBerthAsync(berthId, cancellationToken);
    }

    public async Task<FacilityStatusDto> CreateStatusAsync(
        FacilityStatusDto status,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _statusValidator.ValidateAsync(status, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Business rule: EndDateTime must be >= StartDateTime
        if (status.EndDateTime.HasValue && status.EndDateTime < status.StartDateTime)
        {
            throw new ValidationException("End date/time must be later than or equal to start date/time");
        }

        return await _repository.CreateStatusAsync(status, cancellationToken);
    }

    public async Task<FacilityStatusDto> UpdateStatusAsync(
        FacilityStatusDto status,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _statusValidator.ValidateAsync(status, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Business rule: EndDateTime must be >= StartDateTime
        if (status.EndDateTime.HasValue && status.EndDateTime < status.StartDateTime)
        {
            throw new ValidationException("End date/time must be later than or equal to start date/time");
        }

        return await _repository.UpdateStatusAsync(status, cancellationToken);
    }

    public async Task<bool> DeleteStatusAsync(int statusId, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteStatusAsync(statusId, cancellationToken);
    }

    private void ApplyBusinessRules(FacilityDto facility)
    {
        // Business Rule: Lock/Gauge fields must be blank if facility type is not 'Lock' or 'Gauge Location'
        if (facility.BargeExLocationType != "Lock" && facility.BargeExLocationType != "Gauge Location")
        {
            facility.LockUsaceName = null;
            facility.LockFloodStage = null;
            facility.LockPoolStage = null;
            facility.LockLowWater = null;
            facility.LockNormalCurrent = null;
            facility.LockHighFlow = null;
            facility.LockHighWater = null;
            facility.LockCatastrophicLevel = null;
        }

        // Business Rule: Mile must be reasonable
        if (facility.Mile.HasValue && facility.Mile.Value > 2000.0m)
        {
            throw new ValidationException($"Mile {facility.Mile.Value} is not reasonable for Mile.");
        }

        // Business Rule: Cannot have River without Mile, or vice versa
        if ((string.IsNullOrEmpty(facility.River) && facility.Mile.HasValue) ||
            (!string.IsNullOrEmpty(facility.River) && !facility.Mile.HasValue))
        {
            throw new ValidationException("Cannot have River value without Mile value, or vice versa.");
        }
    }
}
