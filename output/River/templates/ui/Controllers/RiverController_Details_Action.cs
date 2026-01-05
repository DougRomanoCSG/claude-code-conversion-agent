// RiverController_Details_Action.cs
// Add this action to the existing RiverController.cs

/// <summary>
/// View river details (GET) - Read-only view
/// </summary>
[HttpGet("Details/{id}")]
public async Task<IActionResult> Details(int id)
{
    try
    {
        var river = await _riverService.GetByIdAsync(id);

        if (river == null)
        {
            TempData["ErrorMessage"] = "River not found";
            return RedirectToAction(nameof(Index));
        }

        var model = new RiverDetailsViewModel
        {
            River = river
        };

        return View(model);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading river details for ID {RiverId}", id);
        return View("Error");
    }
}
