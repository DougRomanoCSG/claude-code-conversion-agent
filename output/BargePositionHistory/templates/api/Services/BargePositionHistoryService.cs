using BargeOps.Shared.Dto;
using Admin.Domain.Services;
using Admin.Infrastructure.Abstractions;
using Csg.ListQuery;
using System;
using System.Threading.Tasks;

namespace Admin.Infrastructure.Services;

/// <summary>
/// Service implementation for Barge Position History business logic.
/// Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.API\src\Admin.Infrastructure\Services\BargePositionHistoryService.cs
/// </summary>
public class BargePositionHistoryService : IBargePositionHistoryService
{
    private readonly IBargePositionHistoryRepository _repository;

    public BargePositionHistoryService(IBargePositionHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<DataTableResponse<BargePositionHistoryDto>> SearchAsync(BargePositionHistorySearchRequest request)
    {
        // Validate required search criteria
        if (request.FleetID <= 0)
        {
            throw new ArgumentException("FleetID is required.", nameof(request.FleetID));
        }

        if (!request.PositionStartDate.HasValue)
        {
            throw new ArgumentException("PositionStartDate is required.", nameof(request.PositionStartDate));
        }

        if (!request.TierGroupID.HasValue)
        {
            throw new ArgumentException("TierGroupID is required.", nameof(request.TierGroupID));
        }

        return await _repository.SearchAsync(request);
    }

    public async Task<BargePositionHistoryDto> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Invalid FleetPositionHistoryID.", nameof(id));
        }

        return await _repository.GetByIdAsync(id);
    }

    public async Task<int> CreateAsync(BargePositionHistoryDto dto, string modifyUser)
    {
        // Validate required fields
        ValidateDto(dto);

        // Resolve BargeID from BargeNum
        if (!string.IsNullOrEmpty(dto.BargeNum))
        {
            var bargeId = await _repository.GetBargeIdByNumberAsync(dto.BargeNum);
            if (!bargeId.HasValue)
            {
                throw new InvalidOperationException($"Barge number '{dto.BargeNum}' does not match an existing barge record.");
            }
            dto.BargeID = bargeId.Value;
        }
        else if (dto.BargeID <= 0)
        {
            throw new ArgumentException("BargeNum or BargeID is required.", nameof(dto.BargeNum));
        }

        // Apply LeftFleet business rule: clear tier fields when LeftFleet is true
        if (dto.LeftFleet)
        {
            dto.TierID = null;
            dto.TierX = null;
            dto.TierY = null;
        }

        return await _repository.InsertAsync(dto, modifyUser);
    }

    public async Task UpdateAsync(BargePositionHistoryDto dto, string modifyUser)
    {
        // Validate required fields
        ValidateDto(dto);

        if (dto.FleetPositionHistoryID <= 0)
        {
            throw new ArgumentException("FleetPositionHistoryID is required for update.", nameof(dto.FleetPositionHistoryID));
        }

        // Resolve BargeID from BargeNum
        if (!string.IsNullOrEmpty(dto.BargeNum))
        {
            var bargeId = await _repository.GetBargeIdByNumberAsync(dto.BargeNum);
            if (!bargeId.HasValue)
            {
                throw new InvalidOperationException($"Barge number '{dto.BargeNum}' does not match an existing barge record.");
            }
            dto.BargeID = bargeId.Value;
        }
        else if (dto.BargeID <= 0)
        {
            throw new ArgumentException("BargeNum or BargeID is required.", nameof(dto.BargeNum));
        }

        // Apply LeftFleet business rule: clear tier fields when LeftFleet is true
        if (dto.LeftFleet)
        {
            dto.TierID = null;
            dto.TierX = null;
            dto.TierY = null;
        }

        await _repository.UpdateAsync(dto, modifyUser);
    }

    public async Task DeleteAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Invalid FleetPositionHistoryID.", nameof(id));
        }

        await _repository.DeleteAsync(id);
    }

    public async Task<bool> ValidateBargeNumAsync(string bargeNum)
    {
        if (string.IsNullOrWhiteSpace(bargeNum))
        {
            return false;
        }

        var bargeId = await _repository.GetBargeIdByNumberAsync(bargeNum);
        return bargeId.HasValue;
    }

    private void ValidateDto(BargePositionHistoryDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.FleetID <= 0)
        {
            throw new ArgumentException("FleetID is required.", nameof(dto.FleetID));
        }

        if (dto.PositionStartDateTime == default)
        {
            throw new ArgumentException("PositionStartDateTime is required.", nameof(dto.PositionStartDateTime));
        }

        // Validate TierX and TierY are within Int16 range if provided
        if (dto.TierX.HasValue && (dto.TierX.Value < short.MinValue || dto.TierX.Value > short.MaxValue))
        {
            throw new ArgumentException("TierX must be a valid 16-bit integer.", nameof(dto.TierX));
        }

        if (dto.TierY.HasValue && (dto.TierY.Value < short.MinValue || dto.TierY.Value > short.MaxValue))
        {
            throw new ArgumentException("TierY must be a valid 16-bit integer.", nameof(dto.TierY));
        }
    }
}
