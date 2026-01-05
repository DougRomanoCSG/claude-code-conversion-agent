using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BargeOps.Shared.Dto;
using BargeOps.Shared.Models;

namespace BargeOpsAdmin.Services;

/// <summary>
/// Vendor service implementation for UI layer
/// HTTP client to call Vendor API endpoints
/// Location: src/BargeOps.UI/Services/
/// </summary>
public class VendorService : IVendorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VendorService> _logger;

    public VendorService(IHttpClientFactory httpClientFactory, ILogger<VendorService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BargeOpsApi");
        _logger = logger;
    }

    #region Vendor Operations

    public async Task<PagedResult<VendorDto>?> SearchVendorsAsync(VendorSearchRequest request, int page = 1, int pageSize = 25)
    {
        try
        {
            var queryString = BuildQueryString(request);
            queryString += $"&page={page}&pageSize={pageSize}";

            var response = await _httpClient.GetAsync($"api/vendor{queryString}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PagedResult<VendorDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vendors");
            return null;
        }
    }

    public async Task<DataTableResponse<VendorDto>?> GetVendorDataTableAsync(DataTableRequest request, VendorSearchRequest searchCriteria)
    {
        try
        {
            var queryString = BuildQueryString(searchCriteria);

            var response = await _httpClient.PostAsJsonAsync(
                $"api/vendor/datatable{queryString}",
                request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<DataTableResponse<VendorDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor DataTable");
            return null;
        }
    }

    public async Task<VendorDto?> GetVendorByIdAsync(int vendorID)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/vendor/{vendorID}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<VendorDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor {VendorID}", vendorID);
            return null;
        }
    }

    public async Task<VendorDto?> CreateVendorAsync(VendorDto vendor)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/vendor", vendor);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<VendorDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor");
            return null;
        }
    }

    public async Task<bool> UpdateVendorAsync(VendorDto vendor)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/vendor/{vendor.VendorID}", vendor);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor {VendorID}", vendor.VendorID);
            return false;
        }
    }

    public async Task<bool> SetVendorActiveAsync(int vendorID, bool isActive)
    {
        try
        {
            var request = new { IsActive = isActive };
            var response = await _httpClient.PatchAsJsonAsync($"api/vendor/{vendorID}/active", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting vendor {VendorID} active status", vendorID);
            return false;
        }
    }

    #endregion

    #region VendorContact Operations

    public async Task<IEnumerable<VendorContactDto>?> GetContactsAsync(int vendorID)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/vendor/{vendorID}/contacts");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<VendorContactDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts for vendor {VendorID}", vendorID);
            return null;
        }
    }

    public async Task<VendorContactDto?> GetContactByIdAsync(int vendorID, int vendorContactID)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/vendor/{vendorID}/contacts/{vendorContactID}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<VendorContactDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact {ContactID}", vendorContactID);
            return null;
        }
    }

    public async Task<VendorContactDto?> CreateContactAsync(VendorContactDto contact)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/vendor/{contact.VendorID}/contacts",
                contact);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<VendorContactDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact for vendor {VendorID}", contact.VendorID);
            return null;
        }
    }

    public async Task<bool> UpdateContactAsync(VendorContactDto contact)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/vendor/{contact.VendorID}/contacts/{contact.VendorContactID}",
                contact);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactID}", contact.VendorContactID);
            return false;
        }
    }

    public async Task<bool> DeleteContactAsync(int vendorID, int vendorContactID)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/vendor/{vendorID}/contacts/{vendorContactID}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactID}", vendorContactID);
            return false;
        }
    }

    #endregion

    #region VendorBusinessUnit Operations

    public async Task<IEnumerable<VendorBusinessUnitDto>?> GetBusinessUnitsAsync(int vendorID)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/vendor/{vendorID}/business-units");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<VendorBusinessUnitDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business units for vendor {VendorID}", vendorID);
            return null;
        }
    }

    public async Task<VendorBusinessUnitDto?> GetBusinessUnitByIdAsync(int vendorID, int vendorBusinessUnitID)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/vendor/{vendorID}/business-units/{vendorBusinessUnitID}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<VendorBusinessUnitDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business unit {BusinessUnitID}", vendorBusinessUnitID);
            return null;
        }
    }

    public async Task<VendorBusinessUnitDto?> CreateBusinessUnitAsync(VendorBusinessUnitDto businessUnit)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/vendor/{businessUnit.VendorID}/business-units",
                businessUnit);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<VendorBusinessUnitDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business unit for vendor {VendorID}", businessUnit.VendorID);
            return null;
        }
    }

    public async Task<bool> UpdateBusinessUnitAsync(VendorBusinessUnitDto businessUnit)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/vendor/{businessUnit.VendorID}/business-units/{businessUnit.VendorBusinessUnitID}",
                businessUnit);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business unit {BusinessUnitID}", businessUnit.VendorBusinessUnitID);
            return false;
        }
    }

    public async Task<bool> DeleteBusinessUnitAsync(int vendorID, int vendorBusinessUnitID)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/vendor/{vendorID}/business-units/{vendorBusinessUnitID}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business unit {BusinessUnitID}", vendorBusinessUnitID);
            return false;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Build query string from search request
    /// </summary>
    private string BuildQueryString(VendorSearchRequest request)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Name))
            queryParams.Add($"name={Uri.EscapeDataString(request.Name)}");

        if (!string.IsNullOrWhiteSpace(request.AccountingCode))
            queryParams.Add($"accountingCode={Uri.EscapeDataString(request.AccountingCode)}");

        if (request.ActiveOnly)
            queryParams.Add("activeOnly=true");

        if (request.FuelSuppliersOnly)
            queryParams.Add("fuelSuppliersOnly=true");

        if (request.InternalVendorOnly)
            queryParams.Add("internalVendorOnly=true");

        if (request.IsBargeExEnabledOnly)
            queryParams.Add("isBargeExEnabledOnly=true");

        if (request.EnablePortalOnly)
            queryParams.Add("enablePortalOnly=true");

        if (request.LiquidBrokerOnly)
            queryParams.Add("liquidBrokerOnly=true");

        if (request.TankermanOnly)
            queryParams.Add("tankermanOnly=true");

        return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
    }

    #endregion
}
