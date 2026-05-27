using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliverTableServer.Services.Interfaces
{
    public interface IGeoLocationService
    {
        Task<(double lat, double lon)?> GetCoordinatesAsync(string address, string city, string zipcode);
    }
}