using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BargeOps.Shared.Dto;
using BargeOpsAdmin.Services;
using BargeOpsAdmin.ViewModels;

namespace BargeOpsAdmin.Controllers
{
    /// <summary>
    /// MVC Controller for BoatFuelPrice search and CRUD operations
    /// Uses IdentityConstants.ApplicationScheme for authentication
    /// </summary>
    [Authorize(Policy = "BoatFuelPrices.View")]
    public class BoatFuelPriceSearchController : Controller
    {
        private readonly IBoatFuelPriceService _boatFuelPriceService;
        private readonly IVendorService _vendorService; // Assume this exists for loading vendors

        public BoatFuelPriceSearchController(
            IBoatFuelPriceService boatFuelPriceService,
            IVendorService vendorService)
        {
            _boatFuelPriceService = boatFuelPriceService ?? throw new ArgumentNullException(nameof(boatFuelPriceService));
            _vendorService = vendorService ?? throw new ArgumentNullException(nameof(vendorService));
        }

        /// <summary>
        /// Main search page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new BoatFuelPriceSearchViewModel
            {
                EffectiveDateSearch = DateTime.Today,
                FuelVendors = await GetFuelVendorSelectListAsync()
            };

            return View(model);
        }

        /// <summary>
        /// DataTables AJAX endpoint for server-side processing
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BoatFuelPriceTable([FromForm] BoatFuelPriceSearchRequest request)
        {
            try
            {
                var results = await _boatFuelPriceService.SearchAsync(request);
                var resultList = results.ToList();

                return Json(new DataTableResponse<BoatFuelPriceDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = resultList.Count,
                    RecordsFiltered = resultList.Count,
                    Data = resultList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading boat fuel prices", error = ex.Message });
            }
        }

        /// <summary>
        /// Get boat fuel price for editing (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBoatFuelPrice(int id)
        {
            try
            {
                var dto = await _boatFuelPriceService.GetByIdAsync(id);

                if (dto == null)
                    return NotFound(new { message = "Boat fuel price not found" });

                var model = new BoatFuelPriceEditViewModel
                {
                    BoatFuelPriceID = dto.BoatFuelPriceID,
                    EffectiveDate = dto.EffectiveDate,
                    Price = dto.Price,
                    FuelVendorBusinessUnitID = dto.FuelVendorBusinessUnitID,
                    InvoiceNumber = dto.InvoiceNumber,
                    CreatedBy = dto.CreateUser,
                    CreatedDate = dto.CreateDateTime,
                    ModifiedBy = dto.ModifyUser,
                    ModifiedDate = dto.ModifyDateTime,
                    FuelVendors = await GetFuelVendorSelectListAsync()
                };

                return Json(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving boat fuel price", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new boat fuel price (AJAX POST)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "BoatFuelPrices.Create")]
        public async Task<IActionResult> Create([FromBody] BoatFuelPriceEditViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = new BoatFuelPriceDto
                {
                    EffectiveDate = model.EffectiveDate,
                    Price = model.Price,
                    FuelVendorBusinessUnitID = model.FuelVendorBusinessUnitID,
                    InvoiceNumber = model.InvoiceNumber
                };

                var created = await _boatFuelPriceService.CreateAsync(dto);

                return Json(new { success = true, message = "Boat fuel price created successfully", data = created });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update boat fuel price (AJAX POST)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "BoatFuelPrices.Edit")]
        public async Task<IActionResult> Edit([FromBody] BoatFuelPriceEditViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var dto = new BoatFuelPriceDto
                {
                    BoatFuelPriceID = model.BoatFuelPriceID,
                    EffectiveDate = model.EffectiveDate,
                    Price = model.Price,
                    FuelVendorBusinessUnitID = model.FuelVendorBusinessUnitID,
                    InvoiceNumber = model.InvoiceNumber
                };

                var updated = await _boatFuelPriceService.UpdateAsync(dto);

                return Json(new { success = true, message = "Boat fuel price updated successfully", data = updated });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete boat fuel price (AJAX POST)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "BoatFuelPrices.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _boatFuelPriceService.DeleteAsync(id);

                if (!deleted)
                    return NotFound(new { success = false, message = "Boat fuel price not found" });

                return Json(new { success = true, message = "Boat fuel price deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Get fuel vendor dropdown list
        /// Loads vendors where IsFuelSupplier = true
        /// </summary>
        private async Task<IEnumerable<SelectListItem>> GetFuelVendorSelectListAsync()
        {
            // TODO: Replace with actual vendor service call
            // var vendors = await _vendorService.GetFuelVendorsAsync();
            // return vendors.Select(v => new SelectListItem
            // {
            //     Value = v.BusinessUnitID.ToString(),
            //     Text = v.Name
            // });

            // Placeholder - replace with actual implementation
            return new List<SelectListItem>();
        }

        #endregion
    }
}
