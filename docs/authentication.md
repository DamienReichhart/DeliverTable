# Authentification

Résumé du fonctionnement de l'authentification dans DeliverTable, côté serveur (ASP.NET Core API)
et côté client (Blazor WebAssembly).

## Vue d'ensemble

- **Mécanisme** : JWT Bearer (token signé HMAC-SHA256), sans refresh token.
- **Identité serveur** : ASP.NET Identity (`UserManager<User>`) pour le hachage des mots de passe et
  la gestion des rôles.
- **Stockage client** : le token est conservé dans le `localStorage` du navigateur sous la clé
  `authToken`.
- **Durée de vie** : 60 minutes par défaut (`JwtConfig.ExpireMinutes`).

## Côté serveur

### Configuration JWT

`Extensions/JwtExtensions.cs` (`AddJwtAuthentication`) enregistre le schéma JWT Bearer avec une
validation complète :

- `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`
- clé de signature symétrique (HMAC-SHA256)

Les paramètres proviennent de `Configuration/JwtConfig.cs`, peuplé depuis les variables
d'environnement via `Configuration/AppEnvironment.cs` (`Key`, `Issuer`, `Audience`, `ExpireMinutes`).

### Émission du token

`Services/TokenService.cs` (`CreateToken`) produit le JWT :

- claims : `sub` = identifiant utilisateur, `ClaimTypes.Role` = rôle principal
- signé HMAC-SHA256, avec issuer / audience / expiration

### Flux de connexion

`Controllers/AuthController.cs` → `Services/AuthService.cs` (`LoginAsync`) :

1. récupération de l'utilisateur par email ;
2. vérification du mot de passe via ASP.NET Identity (derrière `UserRepository`) ;
3. refus des comptes `Suspended` / `Banned` (`401`) ;
4. `BuildConnectionResponse` renvoie `{ Token, User }`.

Le même mécanisme s'applique à `Register` et `RegisterRestaurant`.

### Pipeline HTTP

Ordre dans `Program.cs` :

```none
UseAuthentication → RefreshClaimsMiddleware → UseAuthorization → MapControllers
```

### RefreshClaimsMiddleware

`Middleware/RefreshClaimsMiddleware.cs` s'exécute à **chaque requête authentifiée** et :

- recharge l'utilisateur depuis la base de données ;
- rejette immédiatement les comptes suspendus / bannis (`401` / `403`), même si le token est encore
  valide ;
- **remplace le claim de rôle du JWT par le rôle actuel en base**.

Conséquence : un changement de rôle ou de statut prend effet sans reconnexion.

### Protection des endpoints

Attributs `[Authorize]` / `[Authorize(Roles = nameof(UserRole.X))]` sur les contrôleurs.
L'identifiant utilisateur est extrait des claims (`this.TryGetUserId(...)`).

## Côté client (Blazor WASM)

### AuthService

`Services/Auth/AuthService.cs` appelle les endpoints `/auth/login` et `/auth/register`. En cas de
succès :

- écrit le token dans le `localStorage` ;
- appelle `NotifyUserAuthentication`.

`Logout` supprime le token et notifie la déconnexion.

### ApiAuthStateProvider

`Services/Auth/ApiAuthStateProvider.cs` hérite de `AuthenticationStateProvider` et constitue le cœur
de l'authentification client :

- `GetAuthenticationStateAsync` : lit le token, pose l'en-tête `Authorization` sur le `HttpClient`
  partagé, appelle **`/auth/me`** pour valider le token et récupérer l'utilisateur, puis construit un
  `ClaimsPrincipal` (`NameIdentifier`, `Name`, `Role`). En cas d'échec → identité anonyme.
- `NotifyUserAuthentication` / `NotifyUserLogout` : propagent le changement d'état pour que
  `<AuthorizeView>` et le routage `[Authorize]` réagissent.

### Transport du token

L'en-tête bearer est porté par `HttpClient.DefaultRequestHeaders.Authorization`, positionné par le
state provider. Le `HttpClient` est enregistré une seule fois
(`Extensions/ApiClientServiceCollectionExtensions.cs`), donc l'en-tête s'applique à tous les services
API.

## Points d'attention

1. **`Services/Auth/JwtInterceptor.cs` est du code mort.** Ce `DelegatingHandler` est censé attacher
   le bearer à chaque requête, mais le `HttpClient` est construit sans
   `AddHttpMessageHandler<JwtInterceptor>()` : il n'est jamais branché. Le token transite uniquement
   par `DefaultRequestHeaders`. À supprimer, ou à câbler proprement (plus robuste qu'un état partagé
   mutable).

2. **Pas de refresh token** : un seul JWT de 60 minutes. À l'expiration, `/auth/me` échoue et
   l'utilisateur redevient anonyme (reconnexion nécessaire).

3. **Token en `localStorage`** : exposé au XSS. Compromis classique pour ce type d'architecture, à
   garder en tête.
