using DataIngestion.Api.DTOs;
using DataIngestion.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataIngestion.Api.Pages;

public class IndexModel : PageModel
{
    private readonly IClientQueryService _queryService;
    private readonly IIngestionService _ingestionService;

    [BindProperty]
    public string ZipUrl { get; set; } = "http://localhost:5141/test-data-v1.zip";

    public List<ClientSummaryDto> Clients { get; set; } = new();
    public IngestionResult? LastIngestionResult { get; set; }

    public IndexModel(IClientQueryService queryService, IIngestionService ingestionService)
    {
        _queryService = queryService;
        _ingestionService = ingestionService;
    }

    public async Task OnGetAsync()
    {
        var paged = await _queryService.GetClientsAsync(1, 10000);
        Clients = paged.Items;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(ZipUrl))
            ModelState.AddModelError(nameof(ZipUrl), "URL is required");

        if (!ModelState.IsValid)
        {
            var paged = await _queryService.GetClientsAsync(1, 10000);
            Clients = paged.Items;
            return Page();
        }

        LastIngestionResult = await _ingestionService.IngestAsync(ZipUrl);
        var result = await _queryService.GetClientsAsync(1, 10000);
        Clients = result.Items;
        return Page();
    }
}
