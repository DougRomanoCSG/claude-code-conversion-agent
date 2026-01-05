using Admin.Domain.Services;
using Admin.Infrastructure.Repositories;
using BargeOps.Shared.Dto;
using Microsoft.Extensions.Configuration;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service for Barge business logic
/// Orchestrates repository calls and applies business rules
/// Handles complex validation, computed fields, and equipment type logic
/// </summary>
public class BargeService : IBargeService
{
    private readonly IBargeRepository _bargeRepository;
    private readonly IConfiguration _configuration;

    // Global settings
    private bool IsTerminalActive => _configuration.GetValue<bool>("Licenses:Terminal");
    private bool IsFreightActive => _configuration.GetValue<bool>("Licenses:Freight");
    private bool EnableCoverTypeSpecialLogic => _configuration.GetValue<bool>("GlobalSettings:EnableCoverTypeSpecialLogic");
    private bool RequireBargeCoverType => _configuration.GetValue<bool>("GlobalSettings:RequireBargeCoverType");

    public BargeService(
        IBargeRepository bargeRepository,
        IConfiguration configuration)
    {
        _bargeRepository = bargeRepository ?? throw new ArgumentNullException(nameof(bargeRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    #region Search and Get

    public async Task<PagedResult<BargeDto>> SearchAsync(
        BargeSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        // Parse boat/facility/ship search types into boolean flags
        if (!string.IsNullOrWhiteSpace(request.BoatSearchType))
        {
            request.IsInTow = request.BoatSearchType.Contains("In Tow");
            request.IsScheduledIn = request.BoatSearchType.Contains("Scheduled In");
            request.IsScheduledOut = request.BoatSearchType.Contains("Scheduled Out");
        }

        if (!string.IsNullOrWhiteSpace(request.FacilitySearchType))
        {
            request.IsAtFacility = request.FacilitySearchType.Contains("At Facility");
            request.IsConsignedToFacility = request.FacilitySearchType.Contains("Consigned");
            request.IsDestinationIn = request.FacilitySearchType.Contains("Destination In");
            request.IsDestinationOut = request.FacilitySearchType.Contains("Destination Out");
            request.IsOnOrderToFacility = request.FacilitySearchType.Contains("On Order");
        }

        if (!string.IsNullOrWhiteSpace(request.ShipSearchType))
        {
            request.IsConsignedToShip = request.ShipSearchType.Contains("Consigned");
            request.IsOnOrderToShip = request.ShipSearchType.Contains("On Order");
        }

        return await _bargeRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<BargeDto?> GetByIdAsync(
        int bargeId,
        CancellationToken cancellationToken = default)
    {
        var barge = await _bargeRepository.GetByIdAsync(bargeId, cancellationToken);

        if (barge != null)
        {
            // Calculate computed fields for display
            CalculateSizeCategory(barge);
        }

        return barge;
    }

    #endregion

    #region Create and Update

    public async Task<ServiceResult<int>> CreateAsync(
        BargeDto barge,
        string userName,
        CancellationToken cancellationToken = default)
    {
        // Validate business rules
        var validationResult = await ValidateAsync(barge);
        if (!validationResult.IsValid)
        {
            return ServiceResult<int>.Failure(validationResult.Errors);
        }

        // Check for duplicate barge number
        var exists = await _bargeRepository.BargeNumExistsAsync(barge.BargeNum, null, cancellationToken);
        if (exists)
        {
            return ServiceResult<int>.Failure($"Barge number '{barge.BargeNum}' already exists.");
        }

        // Apply business logic
        ApplyBusinessLogic(barge);

        // Clear unused fields based on EquipmentType
        ClearUnusedFields(barge);

        // Calculate computed fields
        CalculateSizeCategory(barge);

        // Create barge
        var bargeId = await _bargeRepository.CreateAsync(barge, userName, cancellationToken);

        return ServiceResult<int>.Success(bargeId);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(
        BargeDto barge,
        string userName,
        CancellationToken cancellationToken = default)
    {
        // Validate business rules
        var validationResult = await ValidateAsync(barge);
        if (!validationResult.IsValid)
        {
            return ServiceResult<bool>.Failure(validationResult.Errors);
        }

        // Check for duplicate barge number (excluding current barge)
        var exists = await _bargeRepository.BargeNumExistsAsync(barge.BargeNum, barge.BargeID, cancellationToken);
        if (exists)
        {
            return ServiceResult<bool>.Failure($"Barge number '{barge.BargeNum}' already exists.");
        }

        // Apply business logic
        ApplyBusinessLogic(barge);

        // Clear unused fields based on EquipmentType
        ClearUnusedFields(barge);

        // Calculate computed fields
        CalculateSizeCategory(barge);

        // Update barge
        var success = await _bargeRepository.UpdateAsync(barge, userName, cancellationToken);

        return success
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure("Failed to update barge.");
    }

    #endregion

    #region Delete

    public async Task<ServiceResult<bool>> DeleteAsync(
        int bargeId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        // Check if barge exists
        var barge = await _bargeRepository.GetByIdAsync(bargeId, cancellationToken);
        if (barge == null)
        {
            return ServiceResult<bool>.Failure("Barge not found.");
        }

        // TODO: Add additional validation rules:
        // - Check if barge has open tickets
        // - Check if barge is in active tow
        // - Check if barge has active cargo

        var success = await _bargeRepository.DeleteAsync(bargeId, userName, cancellationToken);

        return success
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure("Failed to delete barge.");
    }

    #endregion

    #region Barge Charters

    public async Task<List<BargeCharterDto>> GetBargeChartersAsync(
        int bargeId,
        CancellationToken cancellationToken = default)
    {
        return await _bargeRepository.GetBargeChartersAsync(bargeId, cancellationToken);
    }

    public async Task<ServiceResult<int>> CreateCharterAsync(
        BargeCharterDto charter,
        string userName,
        CancellationToken cancellationToken = default)
    {
        // Validate charter
        var validationResult = ValidateCharter(charter);
        if (!validationResult.IsValid)
        {
            return ServiceResult<int>.Failure(validationResult.Errors);
        }

        // Validate date range overlaps
        var charters = await _bargeRepository.GetBargeChartersAsync(charter.BargeID, cancellationToken);
        var overlapError = ValidateCharterDateOverlaps(charter, charters);
        if (overlapError != null)
        {
            return ServiceResult<int>.Failure(overlapError);
        }

        var charterId = await _bargeRepository.CreateCharterAsync(charter, userName, cancellationToken);

        return ServiceResult<int>.Success(charterId);
    }

    public async Task<ServiceResult<bool>> UpdateCharterAsync(
        BargeCharterDto charter,
        string userName,
        CancellationToken cancellationToken = default)
    {
        // Validate charter
        var validationResult = ValidateCharter(charter);
        if (!validationResult.IsValid)
        {
            return ServiceResult<bool>.Failure(validationResult.Errors);
        }

        // Validate date range overlaps (excluding current charter)
        var charters = await _bargeRepository.GetBargeChartersAsync(charter.BargeID, cancellationToken);
        var otherCharters = charters.Where(c => c.BargeCharterID != charter.BargeCharterID).ToList();
        var overlapError = ValidateCharterDateOverlaps(charter, otherCharters);
        if (overlapError != null)
        {
            return ServiceResult<bool>.Failure(overlapError);
        }

        var success = await _bargeRepository.UpdateCharterAsync(charter, userName, cancellationToken);

        return success
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure("Failed to update charter.");
    }

    public async Task<ServiceResult<bool>> DeleteCharterAsync(
        int charterId,
        CancellationToken cancellationToken = default)
    {
        var success = await _bargeRepository.DeleteCharterAsync(charterId, cancellationToken);

        return success
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure("Failed to delete charter.");
    }

    #endregion

    #region Update Location

    public async Task<ServiceResult<bool>> UpdateLocationAsync(
        int bargeId,
        int locationId,
        DateTime locationDateTime,
        short? tierX = null,
        short? tierY = null,
        short? facilityBerthX = null,
        short? facilityBerthY = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Calculate Status based on LocationID
        // This would typically involve looking up location type and applying business rules

        var success = await _bargeRepository.UpdateLocationAsync(
            bargeId, locationId, locationDateTime,
            tierX, tierY, facilityBerthX, facilityBerthY,
            userName, cancellationToken);

        return success
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure("Failed to update location.");
    }

    #endregion

    #region Validation

    public async Task<ValidationResult> ValidateAsync(BargeDto barge)
    {
        var errors = new List<string>();

        // Rule: BargeNum is required
        if (string.IsNullOrWhiteSpace(barge.BargeNum))
        {
            errors.Add("Barge number is required.");
        }

        // Rule: EquipmentType is required
        if (string.IsNullOrWhiteSpace(barge.EquipmentType))
        {
            errors.Add("Equipment type is required.");
        }

        var isFleetOwned = barge.EquipmentType?.ToLower() == "fleet-owned";

        // Rule: CustomerID required for non-fleet-owned equipment when not in terminal mode
        if (!IsTerminalActive && !isFleetOwned && !barge.CustomerID.HasValue)
        {
            errors.Add("Customer (operator) is required for non-fleet-owned equipment.");
        }

        // Rule: FleetID required for fleet-owned equipment
        if (isFleetOwned && !barge.FleetID.HasValue)
        {
            errors.Add("Fleet is required for fleet-owned equipment.");
        }

        // Rule: SizeCategory required for non-fleet-owned equipment when not in terminal mode
        if (!IsTerminalActive && !isFleetOwned && string.IsNullOrWhiteSpace(barge.SizeCategory))
        {
            errors.Add("Size category is required for non-fleet-owned equipment.");
        }

        // Rule: Range validations
        if (barge.ExternalLength.HasValue && (barge.ExternalLength.Value < 0 || barge.ExternalLength.Value > 50000))
        {
            errors.Add("External length must be between 0 and 50,000 feet.");
        }

        if (barge.ExternalWidth.HasValue && (barge.ExternalWidth.Value < 0 || barge.ExternalWidth.Value > 20000))
        {
            errors.Add("External width must be between 0 and 20,000 feet.");
        }

        if (barge.ExternalDepth.HasValue && (barge.ExternalDepth.Value < 0 || barge.ExternalDepth.Value > 10000))
        {
            errors.Add("External depth must be between 0 and 10,000 feet.");
        }

        if (barge.Draft.HasValue && (barge.Draft.Value < 0 || barge.Draft.Value > 99.999m))
        {
            errors.Add("Draft must be between 0 and 99.999 feet.");
        }

        // Rule: Draft must be >= all corner drafts
        if (barge.Draft.HasValue)
        {
            var cornerDrafts = new[] {
                barge.DraftPortBow,
                barge.DraftPortStern,
                barge.DraftStarboardBow,
                barge.DraftStarboardStern
            }.Where(d => d.HasValue).Select(d => d!.Value).ToList();

            if (cornerDrafts.Any() && cornerDrafts.Max() > barge.Draft.Value)
            {
                errors.Add("Overall draft must be greater than or equal to all corner drafts.");
            }
        }

        // Cover Type Special Logic
        var isCompanyOperated = IsCompanyOperatedBarge(barge);

        // Rule: CoverType required based on global settings
        if (RequireBargeCoverType && string.IsNullOrWhiteSpace(barge.CoverType))
        {
            errors.Add("Cover type is required.");
        }

        // Rule: CoverConfig required when EnableCoverTypeSpecialLogic and specific conditions
        if (EnableCoverTypeSpecialLogic &&
            isCompanyOperated &&
            !string.IsNullOrWhiteSpace(barge.CoverType) &&
            barge.CoverType.ToUpper() != "OT" &&
            string.IsNullOrWhiteSpace(barge.CoverConfig))
        {
            errors.Add("Cover configuration is required for company-operated barges with cover type.");
        }

        // Rule: CoverSubType required when commodity requires cover
        // TODO: This would require looking up commodity settings
        // if (IsRequiredCoverForCommodity(barge.CommodityID) && !barge.CoverSubTypeID.HasValue)
        // {
        //     errors.Add("Cover sub-type is required for this commodity.");
        // }

        // Rule: FreeboardRange required when HasInsufficientFreeboard
        if (barge.HasInsufficientFreeboard && string.IsNullOrWhiteSpace(barge.FreeboardRange))
        {
            errors.Add("Freeboard range is required when insufficient freeboard is indicated.");
        }

        // Rule: DamageNote required when IsDamaged
        // Note: This is cleared automatically in ClearUnusedFields, but we validate for completeness
        if (barge.IsDamaged && string.IsNullOrWhiteSpace(barge.DamageNote))
        {
            errors.Add("Damage note is required when barge is marked as damaged.");
        }

        return await Task.FromResult(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
    }

    private ValidationResult ValidateCharter(BargeCharterDto charter)
    {
        var errors = new List<string>();

        // Rule: BargeID required
        if (charter.BargeID <= 0)
        {
            errors.Add("Barge ID is required.");
        }

        // Rule: ChartererCustomerID required
        if (charter.ChartererCustomerID <= 0)
        {
            errors.Add("Charter company is required.");
        }

        // Rule: StartDate required
        if (charter.StartDate == default)
        {
            errors.Add("Start date is required.");
        }

        // Rule: EndDate must be after StartDate
        if (charter.EndDate.HasValue && charter.EndDate.Value < charter.StartDate)
        {
            errors.Add("End date must be after start date.");
        }

        // Rule: Rate must be positive
        if (charter.Rate.HasValue && charter.Rate.Value < 0)
        {
            errors.Add("Daily rate must be a positive number.");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private string? ValidateCharterDateOverlaps(BargeCharterDto charter, List<BargeCharterDto> existingCharters)
    {
        // From validation.json rule #24: "BargeCharters: Date ranges must not overlap"
        foreach (var existing in existingCharters)
        {
            var charterStart = charter.StartDate;
            var charterEnd = charter.EndDate ?? DateTime.MaxValue;
            var existingStart = existing.StartDate;
            var existingEnd = existing.EndDate ?? DateTime.MaxValue;

            // Check for overlap: (StartA <= EndB) AND (EndA >= StartB)
            if (charterStart <= existingEnd && charterEnd >= existingStart)
            {
                return $"Charter date range overlaps with existing charter from {existing.StartDate:d} to {(existing.EndDate.HasValue ? existing.EndDate.Value.ToString("d") : "present")}.";
            }
        }

        return null;
    }

    #endregion

    #region Business Logic Helpers

    private void ApplyBusinessLogic(BargeDto barge)
    {
        // Auto-set FleetID for fleet-owned equipment
        // Note: This would require SelectedFleetID from user context
        // if (barge.EquipmentType?.ToLower() == "fleet-owned" && !barge.FleetID.HasValue)
        // {
        //     barge.FleetID = _selectedFleetID;
        // }

        // Auto-calculate SizeCategory from dimensions
        CalculateSizeCategory(barge);

        // Auto-clear CoverSubTypeID if CoverType changes
        // This would be handled in the UI, but we can enforce here as well
    }

    private void ClearUnusedFields(BargeDto barge)
    {
        var isFleetOwned = barge.EquipmentType?.ToLower() == "fleet-owned";
        var isFleetOrCustomerOwned = isFleetOwned || barge.EquipmentType?.ToLower() == "customer-owned";

        // Clear fields for fleet-owned equipment
        if (isFleetOwned)
        {
            barge.CommodityID = null;
            barge.CleanStatus = null;
            barge.RepairStatus = null;
            barge.IsCargoDamaged = false;
            barge.DamageLevel = null;
            barge.IsLeaker = false;
            barge.IsDamaged = false;
            barge.DamageNote = null;
            barge.IsDryDocked = false;
            barge.IsRepairScheduled = false;

            // CoverConfig handling with special logic
            if (EnableCoverTypeSpecialLogic)
            {
                barge.CoverConfig = null;
            }
        }

        // Clear fields for non-fleet-owned equipment
        if (!isFleetOwned)
        {
            barge.FleetID = null;
        }

        // Clear ColorPairID if not fleet-owned or customer-owned
        if (!isFleetOrCustomerOwned)
        {
            barge.ColorPairID = null;
        }

        // Clear FreeboardRange if HasInsufficientFreeboard is false
        if (!barge.HasInsufficientFreeboard)
        {
            barge.FreeboardRange = null;
        }

        // Clear DamageNote if IsDamaged is false
        if (!barge.IsDamaged)
        {
            barge.DamageNote = null;
        }

        // Clear CoverConfig if disabled by special logic
        if (EnableCoverTypeSpecialLogic &&
            (string.IsNullOrWhiteSpace(barge.CoverType) || barge.CoverType.ToUpper() == "OT"))
        {
            barge.CoverConfig = null;
        }
    }

    private void CalculateSizeCategory(BargeDto barge)
    {
        // Auto-calculate SizeCategory from ExternalLength and ExternalWidth
        // This is a simplified example - actual logic would be more complex
        if (barge.ExternalLength.HasValue && barge.ExternalWidth.HasValue)
        {
            var length = barge.ExternalLength.Value;
            var width = barge.ExternalWidth.Value;

            // Example size category logic (would need actual business rules)
            if (length >= 300 && width >= 54)
            {
                barge.SizeCategory = "J"; // Jumbo
            }
            else if (length >= 195 && width >= 35)
            {
                barge.SizeCategory = "S"; // Standard
            }
            else
            {
                barge.SizeCategory = "M"; // Mini
            }
        }
    }

    private bool IsCompanyOperatedBarge(BargeDto barge)
    {
        // TODO: This would check if the customer (operator) is the company itself
        // For now, return true if CustomerID matches a specific company customer ID
        // This would come from configuration or database lookup
        return barge.CustomerID.HasValue;
    }

    #endregion
}

#region Service Result Classes

/// <summary>
/// Service result wrapper for operations
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string error)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Errors = new List<string> { error }
        };
    }

    public static ServiceResult<T> Failure(List<string> errors)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Errors = errors
        };
    }
}

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion
