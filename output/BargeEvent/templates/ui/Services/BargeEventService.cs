using BargeOps.Shared.Dto;
using System.Net.Http.Json;

namespace BargeOpsAdmin.Services;

/// <summary>
/// UI Service for BargeEvent operations.
/// HTTP client that calls the BargeEvent API endpoints.
/// Returns DTOs from BargeOps.Shared (same DTOs used by API).
/// </summary>
public class BargeEventService : IBargeEventService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BargeEventService> _logger;
    private const string BaseUrl = "api/bargeevent";

    public BargeEventService(
        IHttpClientFactory httpClientFactory,
        ILogger<BargeEventService> logger)
    {
        if (httpClientFactory == null)
            throw new ArgumentNullException(nameof(httpClientFactory));

        _httpClient = httpClientFactory.CreateClient("BargeOpsApi");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ===== READ OPERATIONS =====

    public async Task<BargeEventDto?> GetByIdAsync(int ticketEventId)
    {
        _logger.LogDebug("UI Service: Getting barge event {TicketEventId}", ticketEventId);

        try
        {
            return await _httpClient.GetFromJsonAsync<BargeEventDto>($"{BaseUrl}/{ticketEventId}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Barge event {TicketEventId} not found", ticketEventId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barge event {TicketEventId}", ticketEventId);
            throw;
        }
    }

    public async Task<IEnumerable<BargeEventDto>> GetByTicketIdAsync(int ticketId)
    {
        _logger.LogDebug("UI Service: Getting barge events for ticket {TicketId}", ticketId);

        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<BargeEventDto>>($"{BaseUrl}/ticket/{ticketId}");
            return result ?? Enumerable.Empty<BargeEventDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barge events for ticket {TicketId}", ticketId);
            throw;
        }
    }

    public async Task<IEnumerable<BargeDto>> GetBargesAsync(int ticketEventId)
    {
        _logger.LogDebug("UI Service: Getting barges for event {TicketEventId}", ticketEventId);

        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<BargeDto>>($"{BaseUrl}/{ticketEventId}/barges");
            return result ?? Enumerable.Empty<BargeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting barges for event {TicketEventId}", ticketEventId);
            throw;
        }
    }

    public async Task<IEnumerable<BillingAuditDto>> GetBillingAuditsAsync(int ticketEventId)
    {
        _logger.LogDebug("UI Service: Getting billing audits for event {TicketEventId}", ticketEventId);

        try
        {
            var result = await _httpClient.GetFromJsonAsync<IEnumerable<BillingAuditDto>>($"{BaseUrl}/{ticketEventId}/billing-audits");
            return result ?? Enumerable.Empty<BillingAuditDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing audits for event {TicketEventId}", ticketEventId);
            throw;
        }
    }

    // ===== SEARCH OPERATIONS =====

    public async Task<PagedResult<BargeEventSearchDto>> SearchAsync(BargeEventSearchRequest request)
    {
        _logger.LogDebug("UI Service: Searching barge events with {@Request}", request);

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/search", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PagedResult<BargeEventSearchDto>>();
            return result ?? new PagedResult<BargeEventSearchDto>();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogWarning("Bad request for barge event search: {Message}", ex.Message);
            throw new InvalidOperationException("Invalid search criteria. Please check your inputs.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching barge events");
            throw;
        }
    }

    public async Task<PagedResult<BargeEventBillingDto>> BillingSearchAsync(BargeEventBillingSearchRequest request)
    {
        _logger.LogDebug("UI Service: Searching billing events with {@Request}", request);

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/billing-search", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PagedResult<BargeEventBillingDto>>();
            return result ?? new PagedResult<BargeEventBillingDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching billing events");
            throw;
        }
    }

    // ===== WRITE OPERATIONS =====

    public async Task<BargeEventDto> CreateAsync(BargeEventDto bargeEvent)
    {
        _logger.LogInformation("UI Service: Creating barge event for Ticket {TicketId}", bargeEvent.TicketID);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, bargeEvent);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Bad request creating barge event: {Error}", errorMessage);
                throw new InvalidOperationException($"Cannot create barge event: {errorMessage}");
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BargeEventDto>();
            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize created barge event");
            }

            _logger.LogInformation("Created barge event: {TicketEventId}", result.TicketEventID);

            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating barge event");
            throw;
        }
    }

    public async Task UpdateAsync(BargeEventDto bargeEvent)
    {
        _logger.LogInformation("UI Service: Updating barge event {TicketEventId}", bargeEvent.TicketEventID);

        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{bargeEvent.TicketEventID}", bargeEvent);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Bad request updating barge event: {Error}", errorMessage);
                throw new InvalidOperationException($"Cannot update barge event: {errorMessage}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Barge event {bargeEvent.TicketEventID} not found");
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Updated barge event {TicketEventId}", bargeEvent.TicketEventID);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating barge event {TicketEventId}", bargeEvent.TicketEventID);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int ticketEventId)
    {
        _logger.LogInformation("UI Service: Voiding barge event {TicketEventId}", ticketEventId);

        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{ticketEventId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Barge event {TicketEventId} not found for deletion", ticketEventId);
                return false;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Bad request voiding barge event: {Error}", errorMessage);
                throw new InvalidOperationException($"Cannot void barge event: {errorMessage}");
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Voided barge event {TicketEventId}", ticketEventId);

            return true;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding barge event {TicketEventId}", ticketEventId);
            throw;
        }
    }

    // ===== REBILLING OPERATIONS =====

    public async Task<int> MarkForRebillAsync(IEnumerable<int> ticketEventIds)
    {
        var ids = ticketEventIds.ToList();
        _logger.LogInformation("UI Service: Marking {Count} events for rebill", ids.Count);

        if (!ids.Any())
        {
            return 0;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/mark-rebill", ids);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            var markedCount = result?.markedCount ?? 0;

            _logger.LogInformation("Marked {Count} events for rebill", markedCount);

            return markedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking events for rebill");
            throw;
        }
    }

    public async Task<int> UnmarkForRebillAsync(IEnumerable<int> ticketEventIds)
    {
        var ids = ticketEventIds.ToList();
        _logger.LogInformation("UI Service: Unmarking {Count} events from rebill", ids.Count);

        if (!ids.Any())
        {
            return 0;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/unmark-rebill", ids);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            var unmarkedCount = result?.unmarkedCount ?? 0;

            _logger.LogInformation("Unmarked {Count} events from rebill", unmarkedCount);

            return unmarkedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmarking events from rebill");
            throw;
        }
    }
}
