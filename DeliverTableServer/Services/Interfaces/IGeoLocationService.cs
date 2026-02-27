using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliverTableServer.Services.Interfaces
{
    public interface IGeoLocationService
    {
        Task<(double lon, double lat)?> GetCoordinatesAsync(string address, string city, string zipcode);
    }
}