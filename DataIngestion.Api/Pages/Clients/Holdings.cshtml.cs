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

    public HoldingsModel(IClientQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IActionResult> OnGetAsync(string clientId, string accountId)
    {
        ClientId = clientId;
        AccountId = accountId;
        var holdings = await _queryService.GetHoldingsAsync(clientId, accountId);
        if (holdings == null) return NotFound();
        Holdings = holdings;
        return Page();
    }
}
