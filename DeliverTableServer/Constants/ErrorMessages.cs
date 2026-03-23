namespace DeliverTableServer.Constants;

/// <summary>
///     Centralized, user-facing error messages.
///     Avoids string duplication across services and ensures consistent wording.
/// </summary>
public static class ErrorMessages
{
    public const string InvalidCredentials = "Identifiants invalides";
    public const string InvalidFields = "Champs invalides";
    public const string AccountSuspendedOrBanned = "Compte suspendu ou banni";
    public const string EmailAlreadyUsed = "Cette adresse email est déjà utilisée";
    public const string InternalError = "Une erreur est survenue";
    public const string InvalidOrExpiredToken = "Token invalide ou expiré";
    public const string UserNotFound = "Utilisateur introuvable";
    public const string CurrentPasswordIncorrect = "Le mot de passe actuel est incorrect";
    public const string PasswordChangedSuccessfully = "Mot de passe modifié avec succès";
    public const string RestaurantNotFound = "Etablissement introuvable";
    public const string DishNotFound = "Plat introuvable";
    public const string AddressNotLocatable = "Impossible de localiser l'adresse fournie.";
    public const string ResourceNotFound = "Ressource non trouvée";
    public const string InternalServerError = "Une erreur interne est survenue";

    public static string InvalidRole(string validValues) =>
        $"Rôle invalide. Valeurs possibles : {validValues}";

    public static string InvalidStatus(string validValues) =>
        $"Statut invalide. Valeurs possibles : {validValues}";
}
