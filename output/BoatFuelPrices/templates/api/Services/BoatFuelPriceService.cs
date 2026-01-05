using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BargeOps.Shared.Dto;
using Admin.Domain.Services;
using Admin.Infrastructure.Repositories;

namespace Admin.Infrastructure.Services
{
    /// <summary>
    /// Service for BoatFuelPrice business logic
    /// Enforces business rules and validation
    /// </summary>
    public class BoatFuelPriceService : IBoatFuelPriceService
    {
        private readonly IBoatFuelPriceRepository _repository;

        public BoatFuelPriceService(IBoatFuelPriceRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IEnumerable<BoatFuelPriceDto>> SearchAsync(BoatFuelPriceSearchRequest criteria)
        {
            return await _repository.SearchAsync(criteria);
        }

        public async Task<BoatFuelPriceDto> GetByIdAsync(int boatFuelPriceID)
        {
            if (boatFuelPriceID <= 0)
                throw new ArgumentException("BoatFuelPriceID must be greater than 0", nameof(boatFuelPriceID));

            return await _repository.GetByIdAsync(boatFuelPriceID);
        }

        public async Task<BoatFuelPriceDto> CreateAsync(BoatFuelPriceDto dto, string userName)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("UserName is required", nameof(userName));

            // Business Rule: Clear InvoiceNumber if FuelVendorBusinessUnitID is empty
            if (!dto.FuelVendorBusinessUnitID.HasValue || dto.FuelVendorBusinessUnitID.Value == 0)
            {
                dto.InvoiceNumber = null;
            }

            // Validate unique constraint
            var isUnique = await _repository.IsUniqueAsync(
                dto.EffectiveDate,
                dto.FuelVendorBusinessUnitID,
                null);

            if (!isUnique)
            {
                throw new InvalidOperationException(
                    $"A boat fuel price already exists for {dto.EffectiveDate:MM/dd/yyyy} " +
                    $"and vendor {dto.FuelVendor ?? "(none)"}.");
            }

            // Create the record
            var newId = await _repository.CreateAsync(dto, userName);

            // Return the created record
            return await _repository.GetByIdAsync(newId);
        }

        public async Task<BoatFuelPriceDto> UpdateAsync(BoatFuelPriceDto dto, string userName)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.BoatFuelPriceID <= 0)
                throw new ArgumentException("BoatFuelPriceID must be greater than 0", nameof(dto.BoatFuelPriceID));

            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("UserName is required", nameof(userName));

            // Business Rule: Clear InvoiceNumber if FuelVendorBusinessUnitID is empty
            if (!dto.FuelVendorBusinessUnitID.HasValue || dto.FuelVendorBusinessUnitID.Value == 0)
            {
                dto.InvoiceNumber = null;
            }

            // Validate unique constraint (excluding current record)
            var isUnique = await _repository.IsUniqueAsync(
                dto.EffectiveDate,
                dto.FuelVendorBusinessUnitID,
                dto.BoatFuelPriceID);

            if (!isUnique)
            {
                throw new InvalidOperationException(
                    $"A boat fuel price already exists for {dto.EffectiveDate:MM/dd/yyyy} " +
                    $"and vendor {dto.FuelVendor ?? "(none)"}.");
            }

            // Update the record
            var updated = await _repository.UpdateAsync(dto, userName);

            if (!updated)
                throw new InvalidOperationException($"BoatFuelPrice with ID {dto.BoatFuelPriceID} not found.");

            // Return the updated record
            return await _repository.GetByIdAsync(dto.BoatFuelPriceID);
        }

        public async Task<bool> DeleteAsync(int boatFuelPriceID)
        {
            if (boatFuelPriceID <= 0)
                throw new ArgumentException("BoatFuelPriceID must be greater than 0", nameof(boatFuelPriceID));

            return await _repository.DeleteAsync(boatFuelPriceID);
        }
    }
}
