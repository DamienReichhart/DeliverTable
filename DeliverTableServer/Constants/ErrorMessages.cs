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
    public const string BillingAddressIncomplete =
        "Veuillez compléter votre adresse de facturation dans votre profil avant de commander.";

    public static string InvalidOrderStatus(string validValues) =>
        $"Statut de commande invalide. Valeurs possibles : {validValues}";

    public static string InvalidOrderType(string validValues) =>
        $"Type de commande invalide. Valeurs possibles : {validValues}";

    public const string ScheduledAtMustBeFuture = "La date planifiée doit être dans le futur";

    public const string PercentageDiscountTooHigh = "Un pourcentage de réduction ne peut pas dépasser 100%";

    // Restaurant Account
    public const string InsufficientBalance = "Solde insuffisant pour effectuer ce retrait";
    public const string InvalidWithdrawalAmount = "Le montant du retrait doit être supérieur à 0";

    // Promotions
    public const string PromotionNotFound = "Promotion introuvable";
    public const string InvalidPromotionDates = "La date de fin doit être postérieure à la date de début";
    public const string PromotionDishNotFromRestaurant = "Un ou plusieurs plats n'appartiennent pas à ce restaurant";

    // Discount Codes
    public const string DiscountCodeNotFound = "Code promo introuvable";
    public const string DiscountCodeInvalid = "Code promo invalide ou expiré";
    public const string DiscountCodeMaxRedemptions = "Ce code promo a atteint le nombre maximum d'utilisations";
    public const string DiscountCodePerUserLimit = "Vous avez déjà utilisé ce code promo le nombre de fois autorisé";
    public const string DiscountCodeMinOrderNotMet = "Le montant minimum de commande n'est pas atteint pour ce code promo";
    public const string DiscountCodeAlreadyExists = "Un code promo avec ce code existe déjà pour ce restaurant";
    public const string InvalidDiscountCodeDates = "La date de fin doit être postérieure à la date de début du code promo";

    // Loyalty
    public const string LoyaltyProgramNotFound = "Programme de fidélité introuvable";
    public const string LoyaltyProgramAlreadyExists = "Ce restaurant possède déjà un programme de fidélité";
    public const string LoyaltyAccountNotFound = "Compte fidélité introuvable";
    public const string InsufficientLoyaltyPoints = "Nombre de points de fidélité insuffisant";
    public const string LoyaltyProgramNotActive = "Le programme de fidélité n'est pas actif";

    // Action Filters
    public const string MissingOrInvalidRestaurantId = "ID de restaurant manquant ou invalide";
    public const string MissingOrInvalidDishId = "ID de plat manquant ou invalide";

    // Events
    public const string EventNotFound = "Événement introuvable";
    public const string InvalidEventDates = "La date de fin doit être postérieure à la date de début de l'événement";
    public const string EventMenuItemNotFound = "Élément du menu événementiel introuvable";
    public const string EventBookingPolicyNotFound = "Politique de réservation introuvable";

    // Notifications
    public const string NotificationNotFound = "Notification introuvable";

    // Ratings
    public const string RatingNotFound = "Avis introuvable";
    public const string OrderNotDelivered = "Vous ne pouvez noter qu'une commande livrée";
    public const string RatingAlreadyExists = "Vous avez déjà noté cette commande";
    public const string RatingOutOfRange = "La note doit être comprise entre 1 et 5";

    // Order Config
    public const string OrderRuleNotFound = "Règle de commande introuvable";
    public const string BlockedSlotNotFound = "Créneau bloqué introuvable";
    public const string InvalidBlockedSlotDates = "La date de fin doit être postérieure à la date de début du créneau";

    // Moderation
    public const string ModerationActionNotFound = "Action de modération introuvable";

    // Transactions
    public const string TransactionNotFound = "Transaction introuvable";

    // Restaurant Table
    public const string RestaurantTableNotFound = "Table de restaurant introuvable";

    // Invoices
    public const string InvoiceNotFound = "Facture introuvable";
    public const string InvoiceNotGeneratedYet = "La facture est en cours de génération, réessayez dans quelques instants";
    public const string InvoiceAccessDenied = "Vous n'êtes pas autorisé à consulter cette facture";

    // SIRET / Legal
    public const string SiretInvalid = "Le numéro SIRET est invalide";
    public const string LegalFieldsRequired = "Les informations légales (SIRET, raison sociale, adresse, forme juridique) sont obligatoires";

    // Disputes
    public const string DisputeNotFound = "Litige introuvable";
    public const string DisputeAccessDenied = "Vous n'êtes pas autorisé à consulter ce litige";
    public const string RefundBlockedByOpenDispute = "Impossible de rembourser : un litige est ouvert sur cette commande";
    public const string DisputePaymentNotFound = "Aucun paiement correspondant à ce litige n'a été trouvé";

    // Stripe / Payments
    public const string OrderAccessDenied = "Vous n'êtes pas autorisé à modifier cette commande";
    public const string PaymentIntentCreationFailed = "Impossible de créer l'intention de paiement";
    public const string PaymentCaptureFailed = "Le prélèvement du paiement a échoué";
    public const string PaymentCancelFailed = "L'annulation du paiement a échoué";
    public const string PaymentRefundFailed = "Le remboursement a échoué";
    public const string PaymentAlreadyRefunded = "Ce paiement a déjà été intégralement remboursé";
    public const string PaymentRefundExceedsAmount = "Le montant demandé dépasse le solde remboursable";
    public const string PaymentNotFound = "Paiement introuvable";
    public const string OrderPaymentRequired = "Cette commande est en attente de paiement";
    public const string OrderPaymentAlreadyProcessed = "Le paiement de cette commande est déjà traité";
    public const string StripeCustomerCreationFailed = "Impossible de créer le client Stripe";
    public const string WebhookSignatureInvalid = "Signature Stripe invalide";
}
