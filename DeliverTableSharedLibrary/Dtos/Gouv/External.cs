using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DeliverTableSharedLibrary.Dtos.Gouv
{
    public record GouvFeatureCollection(
        [property: JsonPropertyName("features")] List<GouvFeature> Features
    );

    public record GouvFeature(
        [property: JsonPropertyName("geometry")] GouvGeometry Geometry,
        [property: JsonPropertyName("properties")] GouvProperties Properties
    );

    public record GouvGeometry(
        [property: JsonPropertyName("coordinates")] List<double> Coordinates
    );

    public record GouvProperties(
        [property: JsonPropertyName("score")] double Score,
        [property: JsonPropertyName("postcode")] string Postcode,
        [property: JsonPropertyName("citycode")] string Citycode,
        [property: JsonPropertyName("label")] string Label
    );
}