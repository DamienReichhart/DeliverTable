namespace DeliverTableSharedLibrary.Constants;

/// <summary>
///     API route paths. Single source of truth for client and server; keep in sync with controller routes.
/// </summary>
public static class GouvApiRoutes
{
    // Route pour accéder a la longitude et latitude query + "adresse+du+lieu+ville"
    // -> accéder au code postal pour vérifier que ce soit le bon renseigné par l'user
    // features.properties.postcode || features.properties.citycode 
    // response => response.features.geometry.coordinates[0](longitude) et [1](latitude) 
    // vérifier le code postal renseigné
    public const string Geolocation = "https://api-adresse.data.gouv.fr/search/?q=";
}