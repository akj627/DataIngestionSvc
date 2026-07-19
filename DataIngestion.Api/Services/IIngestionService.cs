using DataIngestion.Api.DTOs;

namespace DataIngestion.Api.Services;

public interface IIngestionService
{
    Task<IngestionResult> IngestAsync(string zipUrl);
}
