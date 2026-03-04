using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableServer.Mappers
{
    public static class UserMappers
    {
        public static UserResponse ToDto(this User userModel, string? role = null)
        {
            return new UserResponse
            {
                Id = userModel.Id,
                FirstName = userModel.FirstName,
                LastName = userModel.LastName,
                Email = userModel.Email ?? "",
                Role = role ?? "Non défini"
            };
        }
    }
}