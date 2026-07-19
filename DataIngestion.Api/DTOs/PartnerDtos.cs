using System.Text.Json.Serialization;

namespace DataIngestion.Api.DTOs;

public class ClientDto
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("accounts")]
    public List<AccountDto> Accounts { get; set; } = new();

    [JsonPropertyName("advisor_id")]
    public string AdvisorId { get; set; } = string.Empty;

    [JsonPropertyName("last_updated")]
    public DateTimeOffset LastUpdated { get; set; }
}

public class AccountDto
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("account_type")]
    public string AccountType { get; set; } = string.Empty;

    [JsonPropertyName("custodian")]
    public string Custodian { get; set; } = string.Empty;

    [JsonPropertyName("opened_date")]
    public string OpenedDate { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("holdings")]
    public List<HoldingDto> Holdings { get; set; } = new();

    [JsonPropertyName("cash_balance")]
    public decimal CashBalance { get; set; }

    [JsonPropertyName("total_value")]
    public decimal TotalValue { get; set; }
}

public class HoldingDto
{
    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [JsonPropertyName("cusip")]
    public string Cusip { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("market_value")]
    public decimal MarketValue { get; set; }

    [JsonPropertyName("cost_basis")]
    public decimal CostBasis { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("asset_class")]
    public string AssetClass { get; set; } = string.Empty;
}

public class WebhookRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class IngestionResult
{
    public int ClientsProcessed { get; set; }
    public int AccountsProcessed { get; set; }
    public int HoldingsProcessed { get; set; }
}
