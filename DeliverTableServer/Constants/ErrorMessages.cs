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

    // Cart
    public const string CartNotFound = "Panier introuvable";
    public const string CartItemNotFound = "Article du panier introuvable";
    public const string CartEmpty = "Le panier est vide";
    public const string DishNotAvailable = "Ce plat n'est pas disponible";

    public static string FileTooLarge(int maxSizeMb) =>
        $"Le fichier dépasse la taille maximale autorisée ({maxSizeMb} Mo)";
    public const string DishNotFromRestaurant = "Ce plat n'appartient pas à ce restaurant";
    public const string RestaurantNotActive = "Cet établissement n'est pas actif";

    // Order
    public const string OrderNotFound = "Commande introuvable";
    public const string OrderCannotBeCancelled = "Cette commande ne peut pas être annulée";
    public const string DeliveryAddressRequired = "L'adresse de livraison est obligatoire pour une commande en livraison";
    public const string GuestCountRequired = "Le nombre de convives doit être compris entre 1 et 50";

    public static string InvalidOrderStatus(string validValues) =>
        $"Statut de commande invalide. Valeurs possibles : {validValues}";

    public static string InvalidOrderType(string validValues) =>
        $"Type de commande invalide. Valeurs possibles : {validValues}";

    // Restaurant Account
    public const string InsufficientBalance = "Solde insuffisant pour effectuer ce retrait";
    public const string InvalidWithdrawalAmount = "Le montant du retrait doit être supérieur à 0";
}
