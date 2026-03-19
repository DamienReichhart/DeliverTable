using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DeliverTableSharedLibrary.Dtos.Dish;
using DeliverTableSharedLibrary.Constants;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Mappers;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableServer.Middleware.ActionFilters;
using Microsoft.AspNetCore.Authorization;

namespace DeliverTableServer.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Dish.Base)]
    public class DishController(
        IDishRepository dishRepository
    ) : ControllerBase
    {
        private readonly IDishRepository _dishRepository = dishRepository;

        [HttpGet]
        public async Task<ActionResult<List<DishDto>>> GetAllDishes([FromQuery] DishQuery query)
        {
            List<Dish> dishes = await _dishRepository.GetAllDishes(query);
            return Ok(dishes.Select(d => d.ToDto()));
        }

        [HttpGet(ApiRoutes.Dish.ByIdRoute)]
        public async Task<IActionResult> GetDishById(int id)
        {
            Dish dish = await _dishRepository.GetDishById(id) ?? throw new KeyNotFoundException("Plat introuvable");
            return Ok(dish.ToDto());
        }

        [HttpGet(ApiRoutes.Dish.DishesByRestaurantIdRoute)]
        public async Task<ActionResult<List<DishDto>>> GetDishesByRestaurantId([FromQuery] DishQuery query, int id)
        {
            List<Dish> dishes = await _dishRepository.GetDishesByRestaurantId(query, id);
            return Ok(dishes.Select(d => d.ToDto()));
        }

        [HttpPost(ApiRoutes.Dish.DishesByRestaurantIdRoute)]
        [Authorize(Roles = "RestaurantOwner")]
        [EnsureOwner]
        public async Task<ActionResult<DishDto>> CreateDish([FromForm] CreateDishDto createDishDto, [FromRoute] int id, IFormFile? image)
        {
            Dish createdDish = await _dishRepository.CreateDish(createDishDto, id, image);
            return Ok(createdDish.ToDto());
        }

        [HttpPut(ApiRoutes.Dish.ByIdRoute)]
        [Authorize(Roles = "RestaurantOwner")]
        [RestaurantOwner]
        public async Task<ActionResult<DishDto>> UpdateDish([FromRoute] int id, [FromForm] CreateDishDto createDishDto, IFormFile? image)
        {
            Dish updatedDish = await _dishRepository.UpdateDish(id, createDishDto, image);
            return Ok(updatedDish.ToDto());
        }

        [HttpDelete(ApiRoutes.Dish.ByIdRoute)]
        [Authorize(Roles = "RestaurantOwner")]
        [RestaurantOwner]
        public async Task<ActionResult> DeleteDish([FromRoute] int id)
        {
            await _dishRepository.DeleteDish(id);
            return NoContent();
        }
    }
}
