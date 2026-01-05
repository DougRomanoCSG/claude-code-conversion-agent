using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;
using Admin.Domain.Services;
using Admin.Infrastructure.Abstractions;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service implementation for River business logic
/// Uses DTOs directly from repository (no mapping needed!)
/// </summary>
public class RiverService : IRiverService
{
    private readonly IRiverRepository _riverRepository;

    public RiverService(IRiverRepository riverRepository)
    {
        _riverRepository = riverRepository;
    }

    public async Task<RiverDto?> GetByIdAsync(int riverID)
    {
        if (riverID <= 0)
        {
            throw new ArgumentException("RiverID must be greater than 0", nameof(riverID));
        }

        return await _riverRepository.GetByIdAsync(riverID);
    }

    public async Task<DataTableResponse<RiverDto>> SearchAsync(RiverSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _riverRepository.SearchAsync(request);
    }

    public async Task<IEnumerable<RiverListItemDto>> GetListAsync()
    {
        return await _riverRepository.GetListAsync();
    }

    public async Task<int> CreateAsync(RiverDto river)
    {
        ArgumentNullException.ThrowIfNull(river);

        ValidateRiver(river);

        // Ensure uppercase Code
        river.Code = river.Code?.ToUpper() ?? string.Empty;

        return await _riverRepository.CreateAsync(river);
    }

    public async Task UpdateAsync(RiverDto river)
    {
        ArgumentNullException.ThrowIfNull(river);

        if (river.RiverID <= 0)
        {
            throw new ArgumentException("RiverID must be greater than 0", nameof(river.RiverID));
        }

        ValidateRiver(river);

        // Ensure uppercase Code
        river.Code = river.Code?.ToUpper() ?? string.Empty;

        await _riverRepository.UpdateAsync(river);
    }

    public async Task DeleteAsync(int riverID)
    {
        if (riverID <= 0)
        {
            throw new ArgumentException("RiverID must be greater than 0", nameof(riverID));
        }

        // Soft delete via SetActive
        await _riverRepository.SetActiveAsync(riverID, false);
    }

    private static void ValidateRiver(RiverDto river)
    {
        if (string.IsNullOrWhiteSpace(river.Name))
        {
            throw new ArgumentException("River name is required", nameof(river.Name));
        }

        if (string.IsNullOrWhiteSpace(river.Code))
        {
            throw new ArgumentException("River code is required", nameof(river.Code));
        }

        if (river.Code.Length != 3)
        {
            throw new ArgumentException("River code must be exactly 3 characters", nameof(river.Code));
        }

        if (string.IsNullOrWhiteSpace(river.UpLabel))
        {
            throw new ArgumentException("Upstream label is required", nameof(river.UpLabel));
        }

        if (string.IsNullOrWhiteSpace(river.DownLabel))
        {
            throw new ArgumentException("Downstream label is required", nameof(river.DownLabel));
        }

        // StartMile must be <= EndMile when both have values
        if (river.StartMile.HasValue && river.EndMile.HasValue && river.StartMile > river.EndMile)
        {
            throw new ArgumentException("Start mile must be less than or equal to End mile");
        }

        // Mile values must be >= 0 and <= 5000
        if (river.StartMile.HasValue && (river.StartMile < 0 || river.StartMile > 5000))
        {
            throw new ArgumentException("Start mile must be between 0 and 5000", nameof(river.StartMile));
        }

        if (river.EndMile.HasValue && (river.EndMile < 0 || river.EndMile > 5000))
        {
            throw new ArgumentException("End mile must be between 0 and 5000", nameof(river.EndMile));
        }
    }
}
