using DataIngestion.Api.DTOs;
using DataIngestion.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataIngestion.Api.Pages.Clients;

public class HoldingsModel : PageModel
{
    private readonly IClientQueryService _queryService;

    public string ClientId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public List<HoldingSummaryDto> Holdings { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? AsOf { get; set; }

    public HoldingsModel(IClientQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IActionResult> OnGetAsync(string clientId, string accountId)
    {
        ClientId = clientId;
        AccountId = accountId;
        var holdings = await _queryService.GetHoldingsAsync(clientId, accountId, ParseAsOf());
        if (holdings == null) return NotFound();
        Holdings = holdings;
        return Page();
    }

    private DateTimeOffset? ParseAsOf()
    {
        if (string.IsNullOrEmpty(AsOf)) return null;
        return DateTime.TryParse(AsOf, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var dt)
            ? new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero)
            : null;
    }
}
