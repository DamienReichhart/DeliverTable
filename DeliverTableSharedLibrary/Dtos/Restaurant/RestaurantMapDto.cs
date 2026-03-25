namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public record RestaurantMapDto(
    int Id,
    string Name,
    string Type,
    double Latitude,
    double Longitude
);
