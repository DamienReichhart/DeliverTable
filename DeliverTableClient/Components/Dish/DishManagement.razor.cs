using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dish;

namespace DeliverTableClient.Components.Dish
{
    public partial class DishManagement
    {
        [Parameter]
        public int RestaurantId { get; set; }

        private List<DishDto>? dishes;
        private bool loading = true;
        private string? error;

        private bool isFormVisible = false;
        private bool isEditing = false;
        private bool isSubmitting = false;
        private string? formError;
        private int editingDishId = 0;

        private CreateDishDto currentDish = new();
        private IBrowserFile? selectedImage;

        protected override async Task OnInitializedAsync()
        {
            await LoadDishes();
        }

        private async Task LoadDishes()
        {
            loading = true;
            error = null;
            try
            {
                var query = new DishQuery { PageSize = 100 };
                var (result, errorResult) = await DishService.GetDishesByRestaurantId(RestaurantId, query);

                if (errorResult != null)
                {
                    error = errorResult.Error;
                }
                else
                {
                    dishes = result?.Items;
                }
            }
            catch (Exception ex)
            {
                error = "Erreur lors du chargement des plats: " + ex.Message;
            }
            finally
            {
                loading = false;
            }
        }

        private void ShowAddForm()
        {
            isEditing = false;
            isFormVisible = true;
            currentDish = new CreateDishDto();
            selectedImage = null;
            formError = null;
        }

        private void EditDish(DishDto dish)
        {
            isEditing = true;
            isFormVisible = true;
            editingDishId = dish.Id;

            currentDish = new CreateDishDto
            {
                Name = dish.Name,
                Description = dish.Description,
                BasePrice = dish.BasePrice,
                IsVegetarian = dish.IsVegetarian,
                IsVegan = dish.IsVegan,
                IsGlutenFree = dish.IsGlutenFree,
                IsAllergenHazard = dish.IsAllergenHazard,
                IsDishOfTheDay = dish.IsDishOfTheDay
            };

            selectedImage = null;
            formError = null;
        }

        private void CancelForm()
        {
            isFormVisible = false;
        }

        private void HandleImageSelected(InputFileChangeEventArgs e)
        {
            selectedImage = e.File;
        }

        private async Task HandleSubmit()
        {
            isSubmitting = true;
            formError = null;

            try
            {
                DishDto? resultDish;
                ErrorResponse? errorResponse;

                if (isEditing)
                {
                    (resultDish, errorResponse) = await DishService.UpdateDish(editingDishId, currentDish, selectedImage);
                }
                else
                {
                    (resultDish, errorResponse) = await DishService.CreateDish(RestaurantId, currentDish, selectedImage);
                }

                if (errorResponse != null)
                {
                    formError = errorResponse.Error;
                }
                else
                {
                    isFormVisible = false;
                    await LoadDishes();
                }
            }
            catch (Exception ex)
            {
                formError = "Erreur lors de l'enregistrement: " + ex.Message;
            }
            finally
            {
                isSubmitting = false;
            }
        }

        private async Task DeleteDish(int id)
        {
            bool confirm = true;

            if (confirm)
            {
                error = null;
                var errorResponse = await DishService.DeleteDish(id);

                if (errorResponse != null)
                {
                    error = errorResponse.Error;
                }
                else
                {
                    isFormVisible = false;
                    editingDishId = 0;
                    isEditing = false;
                    await LoadDishes();
                }
            }
        }
    }
}