namespace DataIngestion.Api.DTOs;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ClientSummaryDto
{
    public string ClientId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AdvisorId { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset KnowledgeDate { get; set; }
}

public class AccountSummaryDto
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Custodian { get; set; } = string.Empty;
    public string OpenedDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CashBalance { get; set; }
    public decimal TotalValue { get; set; }
}

public class HoldingSummaryDto
{
    public string Ticker { get; set; } = string.Empty;
    public string Cusip { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MarketValue { get; set; }
    public decimal CostBasis { get; set; }
    public decimal Price { get; set; }
    public string AssetClass { get; set; } = string.Empty;
}
