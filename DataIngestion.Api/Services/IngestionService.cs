using System.IO.Compression;
using System.Text.Json;
using DataIngestion.Api.Data;
using DataIngestion.Api.DTOs;
using DataIngestion.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DataIngestion.Api.Services;

public class IngestionService : IIngestionService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(HttpClient httpClient, AppDbContext dbContext, ILogger<IngestionService> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestAsync(string zipUrl)
    {
        var result = new IngestionResult();

        var zipBytes = await DownloadZipAsync(zipUrl);
        var clientDtos = ParseZip(zipBytes);

        foreach (var dto in clientDtos)
        {
            await UpsertClientAsync(dto, result);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Ingestion complete: {Clients} clients, {Accounts} accounts, {Holdings} holdings",
            result.ClientsProcessed, result.AccountsProcessed, result.HoldingsProcessed);

        return result;
    }

    private async Task<byte[]> DownloadZipAsync(string url)
    {
        _logger.LogInformation("Downloading ZIP from {Url}", url);
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    private List<ClientDto> ParseZip(byte[] zipBytes)
    {
        var clients = new List<ClientDto>();

        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (!entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using var entryStream = entry.Open();
                var dto = JsonSerializer.Deserialize<ClientDto>(entryStream);
                if (dto != null)
                    clients.Add(dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse entry {Entry}, skipping", entry.Name);
            }
        }

        return clients;
    }

    private async Task UpsertClientAsync(ClientDto dto, IngestionResult result)
    {
        var existing = await _dbContext.Clients
            .Include(c => c.Accounts)
                .ThenInclude(a => a.Holdings)
            .FirstOrDefaultAsync(c => c.ClientId == dto.ClientId);

        if (existing != null)
        {
            // Remove old accounts and holdings — will be replaced with incoming data
            _dbContext.Accounts.RemoveRange(existing.Accounts);
            existing.Accounts.Clear();
            MapClientFields(dto, existing);
        }
        else
        {
            existing = new Client { ClientId = dto.ClientId };
            MapClientFields(dto, existing);
            _dbContext.Clients.Add(existing);
        }

        foreach (var accountDto in dto.Accounts)
        {
            var account = MapAccount(accountDto);
            existing.Accounts.Add(account);
            result.AccountsProcessed++;
            result.HoldingsProcessed += account.Holdings.Count;
        }

        result.ClientsProcessed++;
    }

    private static void MapClientFields(ClientDto dto, Client client)
    {
        client.FirstName = dto.FirstName;
        client.LastName = dto.LastName;
        client.Email = dto.Email;
        client.AdvisorId = dto.AdvisorId;
        client.LastUpdated = dto.LastUpdated;
    }

    private static Account MapAccount(AccountDto dto)
    {
        var account = new Account
        {
            AccountId = dto.AccountId,
            AccountType = dto.AccountType,
            Custodian = dto.Custodian,
            OpenedDate = DateOnly.Parse(dto.OpenedDate),
            Status = dto.Status,
            CashBalance = dto.CashBalance,
            TotalValue = dto.TotalValue,
            Holdings = dto.Holdings.Select(h => new Holding
            {
                Ticker = h.Ticker,
                Cusip = h.Cusip,
                Description = h.Description,
                Quantity = h.Quantity,
                MarketValue = h.MarketValue,
                CostBasis = h.CostBasis,
                Price = h.Price,
                AssetClass = h.AssetClass
            }).ToList()
        };

        return account;
    }
}
