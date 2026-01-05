using Admin.Domain.Services;
using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service implementation for BargeSeries business logic.
/// Uses DTOs directly from repository - NO mapping needed!
/// </summary>
public class BargeSeriesService : IBargeSeriesService
{
    private readonly IBargeSeriesRepository _repository;

    public BargeSeriesService(IBargeSeriesRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<PagedResult<BargeSeriesDto>> SearchAsync(
        BargeSeriesSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Repository returns DTOs directly - no mapping!
        return await _repository.SearchAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BargeSeriesDto>> GetListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("BargeSeries ID must be greater than zero.", nameof(id));

        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto> CreateAsync(
        BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bargeSeries);

        // Validate business rules
        ValidateBargeSeriesDto(bargeSeries);

        // Ensure IsActive is true for new records
        bargeSeries.IsActive = true;

        // Create via repository (returns DTO directly)
        return await _repository.CreateAsync(bargeSeries, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BargeSeriesDto> UpdateAsync(
        BargeSeriesDto bargeSeries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bargeSeries);

        if (bargeSeries.BargeSeriesID <= 0)
            throw new ArgumentException("BargeSeries ID must be greater than zero.", nameof(bargeSeries));

        // Validate business rules
        ValidateBargeSeriesDto(bargeSeries);

        // Verify entity exists
        var existing = await _repository.GetByIdAsync(bargeSeries.BargeSeriesID, cancellationToken);
        if (existing == null)
            throw new InvalidOperationException($"BargeSeries with ID {bargeSeries.BargeSeriesID} not found.");

        // Update via repository (returns DTO directly)
        return await _repository.UpdateAsync(bargeSeries, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("BargeSeries ID must be greater than zero.", nameof(id));

        // Verify entity exists before deactivating
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new InvalidOperationException($"BargeSeries with ID {id} not found.");

        return await _repository.SetActiveAsync(id, false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ReactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("BargeSeries ID must be greater than zero.", nameof(id));

        // Verify entity exists before reactivating
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new InvalidOperationException($"BargeSeries with ID {id} not found.");

        return await _repository.SetActiveAsync(id, true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BargeSeriesDraftDto>> UpdateDraftsAsync(
        int bargeSeriesId,
        IEnumerable<BargeSeriesDraftDto> drafts,
        CancellationToken cancellationToken = default)
    {
        if (bargeSeriesId <= 0)
            throw new ArgumentException("BargeSeries ID must be greater than zero.", nameof(bargeSeriesId));

        ArgumentNullException.ThrowIfNull(drafts);

        // Verify parent entity exists
        var existing = await _repository.GetByIdAsync(bargeSeriesId, cancellationToken);
        if (existing == null)
            throw new InvalidOperationException($"BargeSeries with ID {bargeSeriesId} not found.");

        // Validate draft records
        var draftList = drafts.ToList();
        foreach (var draft in draftList)
        {
            ValidateBargeSeriesDraftDto(draft);
        }

        // Update via repository
        return await _repository.UpsertDraftsAsync(bargeSeriesId, draftList, cancellationToken);
    }

    #region Private Validation Methods

    private static void ValidateBargeSeriesDto(BargeSeriesDto dto)
    {
        var errors = new List<string>();

        // Required fields
        if (dto.CustomerID <= 0)
            errors.Add("Customer is required.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add("Series is required.");
        else if (dto.Name.Length > 50)
            errors.Add("Series exceeds maximum length of 50.");

        if (string.IsNullOrWhiteSpace(dto.HullType))
            errors.Add("Hull type is required.");
        else if (dto.HullType.Length > 1)
            errors.Add("Hull type exceeds maximum length of 1.");

        if (string.IsNullOrWhiteSpace(dto.CoverType))
            errors.Add("Cover type is required.");
        else if (dto.CoverType.Length > 3)
            errors.Add("Cover type exceeds maximum length of 3.");

        // Numeric validations
        if (!dto.Length.HasValue)
            errors.Add("Length is required.");
        else if (dto.Length.Value < 0)
            errors.Add("Length must be non-negative.");

        if (!dto.Width.HasValue)
            errors.Add("Width is required.");
        else if (dto.Width.Value < 0)
            errors.Add("Width must be non-negative.");

        if (!dto.Depth.HasValue)
            errors.Add("Depth is required.");
        else if (dto.Depth.Value < 0)
            errors.Add("Depth must be non-negative.");

        if (!dto.TonsPerInch.HasValue)
            errors.Add("Tons/inch is required.");
        else if (dto.TonsPerInch.Value < 0)
            errors.Add("Tons/inch must be non-negative.");

        if (!dto.DraftLight.HasValue)
            errors.Add("Light draft is required.");
        else if (dto.DraftLight.Value < 0 || dto.DraftLight.Value > 99.999m)
            errors.Add("Light draft must be between 0 and 99.999.");

        // Validate child draft records
        if (dto.Drafts?.Any() == true)
        {
            foreach (var draft in dto.Drafts)
            {
                ValidateBargeSeriesDraftDto(draft, errors);
            }
        }

        if (errors.Any())
            throw new ValidationException(string.Join(" ", errors));
    }

    private static void ValidateBargeSeriesDraftDto(BargeSeriesDraftDto dto, List<string>? errors = null)
    {
        errors ??= new List<string>();

        if (!dto.DraftFeet.HasValue)
            errors.Add("Draft feet is required.");
        else if (dto.DraftFeet.Value < 0)
            errors.Add("Draft feet must be non-negative.");

        // Validate all tonnage values are non-negative if specified
        var tonnageProperties = new[]
        {
            (dto.Tons00, "Tons00"), (dto.Tons01, "Tons01"), (dto.Tons02, "Tons02"),
            (dto.Tons03, "Tons03"), (dto.Tons04, "Tons04"), (dto.Tons05, "Tons05"),
            (dto.Tons06, "Tons06"), (dto.Tons07, "Tons07"), (dto.Tons08, "Tons08"),
            (dto.Tons09, "Tons09"), (dto.Tons10, "Tons10"), (dto.Tons11, "Tons11")
        };

        foreach (var (value, name) in tonnageProperties)
        {
            if (value.HasValue && value.Value < 0)
                errors.Add($"{name} must be non-negative.");
        }

        if (errors.Any() && errors == null)
            throw new ValidationException(string.Join(" ", errors));
    }

    #endregion
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
