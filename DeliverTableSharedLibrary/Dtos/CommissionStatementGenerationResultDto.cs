namespace DeliverTableSharedLibrary.Dtos;

public sealed class CommissionStatementGenerationResultDto
{
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public int RestaurantsProcessed { get; set; }
    public int StatementsCreated { get; set; }
    public int RestaurantsSkipped { get; set; }
    public List<GenerationFailureDto> Failures { get; set; } = [];
}

public sealed class GenerationFailureDto
{
    public int RestaurantId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
