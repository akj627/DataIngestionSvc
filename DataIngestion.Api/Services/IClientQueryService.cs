using DataIngestion.Api.DTOs;

namespace DataIngestion.Api.Services;

public interface IClientQueryService
{
    Task<PagedResult<ClientSummaryDto>> GetClientsAsync(int page, int pageSize, DateTimeOffset? asOf = null);
    Task<List<AccountSummaryDto>?> GetAccountsAsync(string clientId, DateTimeOffset? asOf = null);
    Task<List<HoldingSummaryDto>?> GetHoldingsAsync(string clientId, string accountId, DateTimeOffset? asOf = null);
}
