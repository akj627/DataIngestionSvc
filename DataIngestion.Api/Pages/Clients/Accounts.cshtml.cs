using DataIngestion.Api.DTOs;
using DataIngestion.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataIngestion.Api.Pages.Clients;

public class AccountsModel : PageModel
{
    private readonly IClientQueryService _queryService;

    public string ClientId { get; set; } = string.Empty;
    public List<AccountSummaryDto> Accounts { get; set; } = new();

    public AccountsModel(IClientQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IActionResult> OnGetAsync(string clientId)
    {
        ClientId = clientId;
        var accounts = await _queryService.GetAccountsAsync(clientId);
        if (accounts == null) return NotFound();
        Accounts = accounts;
        return Page();
    }
}
