using DataIngestion.Api.DTOs;

namespace DataIngestion.Api.Services;

public interface IClientQueryService
{
    Task<PagedResult<ClientSummaryDto>> GetClientsAsync(int page, int pageSize);
    Task<List<AccountSummaryDto>?> GetAccountsAsync(string clientId);
    Task<List<HoldingSummaryDto>?> GetHoldingsAsync(string clientId, string accountId);
}
