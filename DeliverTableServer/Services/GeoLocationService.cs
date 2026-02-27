using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Gouv;

namespace DeliverTableServer.Services
{
    public sealed class GeoLocationService : IGeoLocationService
    {
        private readonly HttpClient _httpClient;

        public GeoLocationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Nécessaire pour l'Api Adresse
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DeliverTableServer-App");
        }

        public async Task<(double lon, double lat)?> GetCoordinatesAsync(string address, string city, string zipcode)
        {
            string query = Uri.EscapeDataString($"{address} {zipcode} {city}");
            string url = GouvApiRoutes.Geolocation + query;

            var response = await _httpClient.GetFromJsonAsync<GouvFeatureCollection>(url);

            var bestResult = response?.Features.FirstOrDefault();

            if (
                bestResult != null
                && bestResult.Properties.Score > 0.5
                && (
                    bestResult.Properties.Postcode == zipcode
                    || bestResult.Properties.Citycode == zipcode
                    )
                )
            {
                // On s'assure que le code postal passé en paramètre est correct
                return (bestResult.Geometry.Coordinates[0], bestResult.Geometry.Coordinates[1]);
            }

            return null;
        }
    }
}