using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DeliverTableServer.Data;
using DeliverTableServer.Mappers;
using DeliverTableServer.Middleware.ActionFilters;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Restaurant)]
    public class RestaurantController(
        IGeoLocationService geoLocationService,
        IRestaurantRepository restaurantRepository
    ) : ControllerBase
    {
        private readonly IGeoLocationService _geoLocationService = geoLocationService;
        private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

        [HttpGet]
        // Filter by name / city / type
        public async Task<IActionResult> GetAll([FromQuery] RestaurantQuery query)
        {
            List<Restaurant> restaurants = await _restaurantRepository.GetAllRestaurant(query);
            return Ok(restaurants.Select(r => r.ToDto()));
        }

        // /id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            object? restaurant = await _restaurantRepository.GetRestaurantById(id);
            return restaurant == null ? NotFound(new { Error = "Etablissement introuvable" }) : Ok(restaurant);
        }

        // put
        [HttpPut("{id:int}")]
        [EnsureOwner]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateRestaurantDto restaurantDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new { Message = "Des erreurs de validation ont été détectées.", Errors = errors });
            }

            var coords = await _geoLocationService.GetCoordinatesAsync(
                restaurantDto.AdressLine1,
                restaurantDto.City,
                restaurantDto.ZipCode
            );

            if (coords == null)
            {
                return BadRequest(new { message = "Impossible de localiser l'adresse fournie." });
            }

            try
            {
                await _restaurantRepository.Update(
                    id,
                    restaurantDto,
                    coords.Value.lon,
                    coords.Value.lat
                    );

                object? restaurant  = await _restaurantRepository.GetRestaurantById(id);
                
                return Ok(restaurant);
            }
            catch (System.Exception exception)
            {
                return BadRequest(new { Error = exception.Message });
            }
        }

        // delete
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "RestaurantOwner")]
        [EnsureOwner]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var isModelDeleted = await _restaurantRepository.Delete(id);

            return isModelDeleted ? NoContent() : NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> CreateRestaurant([FromBody] CreateRestaurantDto creationDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ownerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var coords = await _geoLocationService.GetCoordinatesAsync(
                creationDto.AdressLine1,
                creationDto.City,
                creationDto.ZipCode
            );


            if (coords == null || !int.TryParse(ownerIdString, out int ownerId))
            {
                return BadRequest(new { message = "Impossible de localiser l'adresse fournie." });
            }

            var restaurant = await _restaurantRepository.CreateRestaurant(
                creationDto,
                ownerId,
                coords.Value.lon,
                coords.Value.lat
            );

            return CreatedAtAction("GetById", new { id = restaurant.Id }, restaurant.ToDto());
        }
    }
}