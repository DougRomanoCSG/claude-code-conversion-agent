using Admin.Domain.Services;
using Admin.Infrastructure.Abstractions;
using BargeOps.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for RiverArea business logic.
    /// Handles validation, business rules, and orchestration.
    ///
    /// Business Rules:
    /// 1. Name required (max 50 characters)
    /// 2. Mutually exclusive area types (IsPriceZone, IsPortalArea, IsHighWaterArea)
    /// 3. CustomerID required if IsHighWaterArea = true
    /// 4. Pricing zones cannot have overlapping river segments
    /// 5. River segment validation: River (3 chars), StartMile < EndMile
    ///
    /// Pattern Reference: FacilityService.cs
    /// </summary>
    public class RiverAreaService : IRiverAreaService
    {
        private readonly IRiverAreaRepository _repository;

        public RiverAreaService(IRiverAreaRepository repository)
        {
            _repository = repository;
        }

        public async Task<DataTableResponse<RiverAreaListDto>> SearchAsync(RiverAreaSearchRequest request)
        {
            return await _repository.SearchAsync(request);
        }

        public async Task<RiverAreaDto> GetByIdAsync(int riverAreaId)
        {
            return await _repository.GetByIdAsync(riverAreaId);
        }

        public async Task<int> CreateAsync(RiverAreaDto riverArea)
        {
            // Validate business rules
            var errors = await ValidateAsync(riverArea);
            if (errors.Any())
            {
                throw new ValidationException($"Validation failed: {string.Join("; ", errors)}");
            }

            // Create river area
            return await _repository.CreateAsync(riverArea);
        }

        public async Task<int> CreateAsync(RiverAreaDto riverArea)
        {
            // Validate business rules
            var errors = await ValidateAsync(riverArea);
            if (errors.Any())
            {
                throw new ValidationException($"Validation failed: {string.Join("; ", errors)}");
            }

            // Create river area
            return await _repository.CreateAsync(riverArea);
        }

        public async Task UpdateAsync(RiverAreaDto riverArea)
        {
            // Check if exists
            var existing = await _repository.GetByIdAsync(riverArea.RiverAreaID);
            if (existing == null)
            {
                throw new NotFoundException($"River area with ID {riverArea.RiverAreaID} not found");
            }

            // Validate business rules
            var errors = await ValidateAsync(riverArea);
            if (errors.Any())
            {
                throw new ValidationException($"Validation failed: {string.Join("; ", errors)}");
            }

            // Update river area
            await _repository.UpdateAsync(riverArea);
        }

        public async Task DeleteAsync(int riverAreaId)
        {
            // Check if exists
            var existing = await _repository.GetByIdAsync(riverAreaId);
            if (existing == null)
            {
                throw new NotFoundException($"River area with ID {riverAreaId} not found");
            }

            // Hard delete (legacy pattern)
            await _repository.DeleteAsync(riverAreaId);
        }

        public async Task<IEnumerable<RiverAreaSegmentDto>> GetSegmentsByRiverAreaIdAsync(int riverAreaId)
        {
            return await _repository.GetSegmentsByRiverAreaIdAsync(riverAreaId);
        }

        public async Task<List<string>> ValidateAsync(RiverAreaDto riverArea)
        {
            var errors = new List<string>();

            // 1. Name required and max length
            if (string.IsNullOrWhiteSpace(riverArea.Name))
            {
                errors.Add("Name is required");
            }
            else if (riverArea.Name.Length > 50)
            {
                errors.Add("Name cannot exceed maximum length of 50 characters");
            }

            // 2. Mutually exclusive area types
            var areaTypeCount = new[] { riverArea.IsPriceZone, riverArea.IsPortalArea, riverArea.IsHighWaterArea }
                .Count(x => x);

            if (areaTypeCount > 1)
            {
                errors.Add("Only one of these may be checked: Pricing zone, Portal area, or High water area");
            }

            // 3. CustomerID validation with IsHighWaterArea
            if (!riverArea.IsHighWaterArea && riverArea.CustomerID.HasValue)
            {
                errors.Add("High water customer must be blank if High water area is not checked");
            }

            if (riverArea.IsHighWaterArea && !riverArea.CustomerID.HasValue)
            {
                errors.Add("High water customer is required when High water area is checked");
            }

            // 4. River segment validation
            if (riverArea.Segments != null && riverArea.Segments.Any())
            {
                foreach (var segment in riverArea.Segments)
                {
                    // River required and max length
                    if (string.IsNullOrWhiteSpace(segment.River))
                    {
                        errors.Add("River is required for all segments");
                    }
                    else if (segment.River.Length > 3)
                    {
                        errors.Add("River cannot exceed maximum length of 3 characters");
                    }

                    // StartMile and EndMile required
                    if (!segment.StartMile.HasValue)
                    {
                        errors.Add("Start mile is required for all segments");
                    }

                    if (!segment.EndMile.HasValue)
                    {
                        errors.Add("End mile is required for all segments");
                    }

                    // StartMile < EndMile
                    if (segment.StartMile.HasValue && segment.EndMile.HasValue
                        && segment.StartMile.Value >= segment.EndMile.Value)
                    {
                        errors.Add($"Start mile ({segment.StartMile}) must be less than end mile ({segment.EndMile}) for river {segment.River}");
                    }
                }
            }

            // 5. Pricing zone overlap validation
            if (riverArea.IsPriceZone)
            {
                var overlappingZones = await CheckOverlappingPricingZonesAsync(riverArea);
                if (overlappingZones.Any())
                {
                    errors.Add($"Another pricing zone has river segments that overlap this zone. The other zone(s) are: {string.Join(", ", overlappingZones)}");
                }
            }

            return errors;
        }

        public async Task<List<string>> CheckOverlappingPricingZonesAsync(RiverAreaDto riverArea)
        {
            var overlappingZones = new List<string>();

            // Only check if this is a pricing zone with segments
            if (!riverArea.IsPriceZone || riverArea.Segments == null || !riverArea.Segments.Any())
            {
                return overlappingZones;
            }

            // Get all other pricing zones
            var otherPricingZones = await _repository.SearchAsync(new RiverAreaSearchRequest
            {
                PricingZonesOnly = true,
                ActiveOnly = false,
                Length = int.MaxValue
            });

            // Check each segment against other pricing zones
            foreach (var mySegment in riverArea.Segments)
            {
                if (!mySegment.StartMile.HasValue || !mySegment.EndMile.HasValue || string.IsNullOrWhiteSpace(mySegment.River))
                {
                    continue;
                }

                foreach (var otherZone in otherPricingZones.Data.Where(z => z.RiverAreaID != riverArea.RiverAreaID))
                {
                    // Get segments for this other pricing zone
                    var otherSegments = await _repository.GetSegmentsByRiverAreaIdAsync(otherZone.RiverAreaID);

                    foreach (var otherSegment in otherSegments.Where(s => s.River == mySegment.River))
                    {
                        if (!otherSegment.StartMile.HasValue || !otherSegment.EndMile.HasValue)
                        {
                            continue;
                        }

                        // Check for overlap
                        // Ranges overlap if: NOT (myEnd <= otherStart OR myStart >= otherEnd)
                        var noOverlap = mySegment.EndMile.Value <= otherSegment.StartMile.Value
                                     || mySegment.StartMile.Value >= otherSegment.EndMile.Value;

                        if (!noOverlap)
                        {
                            if (!overlappingZones.Contains(otherZone.Name))
                            {
                                overlappingZones.Add(otherZone.Name);
                            }
                        }
                    }
                }
            }

            return overlappingZones;
        }
    }

    /// <summary>
    /// Custom exception for validation errors.
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Custom exception for not found errors.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
