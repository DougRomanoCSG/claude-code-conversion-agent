using BargeOps.Shared.Dto;
using Admin.Domain.Services;
using Admin.Infrastructure.Abstractions;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service implementation for BoatMaintenanceLog business logic
/// ⭐ Uses DTOs directly (no mapping needed)
/// ⭐ Implements conditional validation and field clearing logic
/// </summary>
public class BoatMaintenanceLogService : IBoatMaintenanceLogService
{
    private readonly IBoatMaintenanceLogRepository _repository;

    public BoatMaintenanceLogService(IBoatMaintenanceLogRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<BoatMaintenanceLogDto?> GetByIdAsync(int boatMaintenanceLogId)
    {
        return await _repository.GetByIdAsync(boatMaintenanceLogId);
    }

    public async Task<IEnumerable<BoatMaintenanceLogDto>> GetByLocationIdAsync(int locationId)
    {
        return await _repository.GetByLocationIdAsync(locationId);
    }

    public async Task<int> CreateAsync(BoatMaintenanceLogDto log, string currentUser)
    {
        // Set audit fields
        log.ModifyUser = currentUser;

        // Clear unused fields based on MaintenanceType
        ClearUnusedFields(log);

        // Validate business rules
        var validation = await ValidateAsync(log, isUpdate: false);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        return await _repository.CreateAsync(log);
    }

    public async Task<BoatMaintenanceLogDto> UpdateAsync(BoatMaintenanceLogDto log, string currentUser)
    {
        // Get existing record to validate MaintenanceType hasn't changed
        var existing = await _repository.GetByIdAsync(log.BoatMaintenanceLogID);
        if (existing == null)
        {
            throw new NotFoundException($"BoatMaintenanceLog {log.BoatMaintenanceLogID} not found");
        }

        // ⭐ CRITICAL: MaintenanceType cannot be changed once created
        if (existing.MaintenanceType != log.MaintenanceType)
        {
            throw new BusinessRuleException("Maintenance Type cannot be changed once the record is created");
        }

        // Set audit fields
        log.ModifyUser = currentUser;

        // Clear unused fields based on MaintenanceType
        ClearUnusedFields(log);

        // Validate business rules
        var validation = await ValidateAsync(log, isUpdate: true);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        await _repository.UpdateAsync(log);
        return log;
    }

    public async Task DeleteAsync(int boatMaintenanceLogId)
    {
        await _repository.DeleteAsync(boatMaintenanceLogId);
    }

    public async Task<IEnumerable<BoatMaintenanceLogDto>> SearchAsync(BoatMaintenanceLogSearchRequest request)
    {
        return await _repository.SearchAsync(request);
    }

    public async Task<ValidationResult> ValidateAsync(BoatMaintenanceLogDto log, bool isUpdate = false)
    {
        var errors = new List<string>();

        // Required fields
        if (log.LocationID <= 0)
        {
            errors.Add("LocationID is required");
        }

        if (string.IsNullOrWhiteSpace(log.MaintenanceType))
        {
            errors.Add("Type is required");
        }

        if (log.StartDateTime == default)
        {
            errors.Add("Start date/time is required");
        }

        // MaxLength validations
        if (log.MaintenanceType?.Length > 50)
        {
            errors.Add("Type cannot exceed maximum length of 50");
        }

        if (log.Division?.Length > 14)
        {
            errors.Add("Division cannot exceed maximum length of 14");
        }

        if (log.Status?.Length > 50)
        {
            errors.Add("Status cannot exceed maximum length of 50");
        }

        if (log.Note?.Length > 500)
        {
            errors.Add("Note cannot exceed maximum length of 500");
        }

        // ⭐ Conditional validation based on MaintenanceType
        switch (log.MaintenanceType)
        {
            case "Boat Status":
                // Status is required, others must be blank
                if (string.IsNullOrWhiteSpace(log.Status))
                {
                    errors.Add("If Type is 'Boat Status', then Status is required");
                }
                if (!string.IsNullOrWhiteSpace(log.Division) || log.PortFacilityID.HasValue)
                {
                    errors.Add("Division and Port Facility must be blank when Type is 'Boat Status'");
                }
                if (log.BoatRoleID.HasValue)
                {
                    errors.Add("Boat Role must be blank when Type is 'Boat Status'");
                }
                break;

            case "Change Division/Facility":
                // Division is required, Status and BoatRole must be blank
                if (string.IsNullOrWhiteSpace(log.Division))
                {
                    errors.Add("If Type is 'Change Division/Facility', then Division is required");
                }
                if (!string.IsNullOrWhiteSpace(log.Status))
                {
                    errors.Add("Status must be blank when Type is 'Change Division/Facility'");
                }
                if (log.BoatRoleID.HasValue)
                {
                    errors.Add("Boat Role must be blank when Type is 'Change Division/Facility'");
                }
                break;

            case "Change Boat Role":
                // BoatRole is required, others must be blank
                if (!log.BoatRoleID.HasValue || log.BoatRoleID.Value <= 0)
                {
                    errors.Add("If Type is 'Change Boat Role', then Boat Role is required");
                }
                if (!string.IsNullOrWhiteSpace(log.Status))
                {
                    errors.Add("Status must be blank when Type is 'Change Boat Role'");
                }
                if (!string.IsNullOrWhiteSpace(log.Division) || log.PortFacilityID.HasValue)
                {
                    errors.Add("Division and Port Facility must be blank when Type is 'Change Boat Role'");
                }
                break;

            default:
                errors.Add("Invalid Maintenance Type. Must be 'Boat Status', 'Change Division/Facility', or 'Change Boat Role'");
                break;
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Clear fields that are not applicable to the selected MaintenanceType
    /// ⭐ CRITICAL: This ensures data integrity by clearing unused fields before save
    /// </summary>
    private void ClearUnusedFields(BoatMaintenanceLogDto log)
    {
        switch (log.MaintenanceType)
        {
            case "Boat Status":
                // Keep Status, clear others
                log.Division = null;
                log.PortFacilityID = null;
                log.BoatRoleID = null;
                break;

            case "Change Division/Facility":
                // Keep Division and PortFacilityID, clear others
                log.Status = null;
                log.BoatRoleID = null;
                break;

            case "Change Boat Role":
                // Keep BoatRoleID, clear others
                log.Status = null;
                log.Division = null;
                log.PortFacilityID = null;
                break;
        }
    }
}

/// <summary>
/// Validation result for business rule validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(List<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when a record is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
