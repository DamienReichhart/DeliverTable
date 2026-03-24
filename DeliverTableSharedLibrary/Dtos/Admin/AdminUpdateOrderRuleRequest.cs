using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminUpdateOrderRuleRequest
{
    [Range(0, double.MaxValue, ErrorMessage = "Le montant minimum doit être positif")]
    public decimal? MinConfirmAmount { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Le délai minimum doit être positif")]
    public int? MinLeadTimeHours { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Le nombre de jours d'avance doit être au moins 1")]
    public int? MaxAdvanceDays { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La durée du créneau doit être au moins 1 minute")]
    public int? SlotDurationMinutes { get; set; }

    [MaxLength(2000)]
    public string AvailabilityRanges { get; set; } = "";

    public bool AllowPreorder { get; set; }

    public bool AllowDelivery { get; set; }
}
