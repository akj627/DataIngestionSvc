using DataIngestion.Api.DTOs;

namespace DataIngestion.Api.Services;

public interface IIngestionService
{
    Task<IngestionResult> IngestAsync(string zipUrl);
    Task<IngestionResult> IngestAsync(byte[] zipBytes, string sourceLabel);
}
