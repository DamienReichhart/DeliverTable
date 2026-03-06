using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dish;
using Microsoft.AspNetCore.Components.Forms;

namespace DeliverTableClient.Services.Interfaces
{
    public interface IDishService
    {
        Task<(List<DishDto>?, ErrorResponse?)> GetDishesByRestaurantId(int restaurantId, DishQuery query, CancellationToken cancellationToken = default);
        Task<(DishDto?, ErrorResponse?)> CreateDish(int restaurantId, CreateDishDto createDto, IBrowserFile? image, CancellationToken cancellationToken = default);
        Task<(DishDto?, ErrorResponse?)> UpdateDish(int dishId, CreateDishDto updateDto, IBrowserFile? image, CancellationToken cancellationToken = default);
        Task<ErrorResponse?> DeleteDish(int dishId, CancellationToken cancellationToken = default);
    }
}
