using DeliverTableSharedLibrary.Dtos.Auth;
using Microsoft.JSInterop;

namespace DeliverTableClient.Pages.Profile;

public partial class Profile
{
    private UpdateProfileRequest _profileModel = new();
    private ChangePasswordRequest _passwordModel = new();

    private bool _isLoading = true;
    private string? _loadError;
    private string _initials = "";

    private bool _isEditingProfile;
    private bool _isSavingProfile;
    private string? _profileError;
    private string? _profileSuccess;
    private UpdateProfileRequest? _originalProfile;

    private bool _isEditingPassword;
    private bool _isSavingPassword;
    private string? _passwordError;
    private string? _passwordSuccess;

    protected override async Task OnInitializedAsync()
    {
        await LoadProfile();
    }

    private async Task LoadProfile()
    {
        _isLoading = true;
        _loadError = null;
        StateHasChanged();

        var (user, error) = await UserService.GetProfileAsync();

        if (user != null)
        {
            _profileModel = new UpdateProfileRequest
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                BillingAddressLine1 = user.BillingAddressLine1,
                BillingAddressLine2 = user.BillingAddressLine2,
                BillingPostalCode = user.BillingPostalCode,
                BillingCity = user.BillingCity,
                BillingCountry = user.BillingCountry
            };

            if (string.IsNullOrWhiteSpace(_profileModel.BillingCountry))
                _profileModel.BillingCountry = "France";

            _initials = BuildInitials(user.FirstName, user.LastName);
        }
        else
        {
            _loadError = error?.Error ?? "Impossible de charger le profil";
        }

        _isLoading = false;
    }

    private void StartEditProfile()
    {
        _profileSuccess = null;
        _profileError = null;
        _originalProfile = new UpdateProfileRequest
        {
            FirstName = _profileModel.FirstName,
            LastName = _profileModel.LastName,
            Email = _profileModel.Email,
            BillingAddressLine1 = _profileModel.BillingAddressLine1,
            BillingAddressLine2 = _profileModel.BillingAddressLine2,
            BillingPostalCode = _profileModel.BillingPostalCode,
            BillingCity = _profileModel.BillingCity,
            BillingCountry = _profileModel.BillingCountry
        };
        _isEditingProfile = true;
    }

    private void CancelEditProfile()
    {
        if (_originalProfile != null)
        {
            _profileModel.FirstName = _originalProfile.FirstName;
            _profileModel.LastName = _originalProfile.LastName;
            _profileModel.Email = _originalProfile.Email;
            _profileModel.BillingAddressLine1 = _originalProfile.BillingAddressLine1;
            _profileModel.BillingAddressLine2 = _originalProfile.BillingAddressLine2;
            _profileModel.BillingPostalCode = _originalProfile.BillingPostalCode;
            _profileModel.BillingCity = _originalProfile.BillingCity;
            _profileModel.BillingCountry = _originalProfile.BillingCountry;
        }
        _profileError = null;
        _isEditingProfile = false;
    }

    private async Task HandleUpdateProfile()
    {
        _isSavingProfile = true;
        _profileError = null;
        _profileSuccess = null;

        try
        {
            var (connection, error) = await UserService.UpdateProfileAsync(_profileModel);

            if (connection?.User != null && !string.IsNullOrEmpty(connection.Token))
            {
                await Js.InvokeVoidAsync("localStorage.setItem", "authToken", connection.Token);
                AuthStateProvider.NotifyUserAuthentication(
                    connection.Token,
                    connection.User.Role,
                    connection.User.Id.ToString(),
                    connection.User.FirstName
                );

                _initials = BuildInitials(connection.User.FirstName, connection.User.LastName);
                _isEditingProfile = false;
                _profileSuccess = "Profil mis à jour avec succès";
                _originalProfile = null;
            }
            else
            {
                _profileError = error?.Error ?? "Une erreur est survenue";
            }
        }
        catch
        {
            _profileError = "Une erreur est survenue lors de la mise à jour";
        }
        finally
        {
            _isSavingProfile = false;
        }
    }

    private void StartEditPassword()
    {
        _passwordSuccess = null;
        _passwordError = null;
        _passwordModel = new ChangePasswordRequest();
        _isEditingPassword = true;
    }

    private void CancelEditPassword()
    {
        _passwordError = null;
        _passwordModel = new ChangePasswordRequest();
        _isEditingPassword = false;
    }

    private async Task HandleChangePassword()
    {
        _isSavingPassword = true;
        _passwordError = null;
        _passwordSuccess = null;

        try
        {
            var (success, error) = await UserService.ChangePasswordAsync(_passwordModel);

            if (success)
            {
                _passwordModel = new ChangePasswordRequest();
                _isEditingPassword = false;
                _passwordSuccess = "Mot de passe modifié avec succès";
            }
            else
            {
                _passwordError = error?.Error ?? "Une erreur est survenue";
            }
        }
        catch
        {
            _passwordError = "Une erreur est survenue lors de la modification";
        }
        finally
        {
            _isSavingPassword = false;
        }
    }

    private static string BuildInitials(string firstName, string lastName)
    {
        var first = string.IsNullOrEmpty(firstName) ? "" : firstName[..1].ToUpperInvariant();
        var last = string.IsNullOrEmpty(lastName) ? "" : lastName[..1].ToUpperInvariant();
        return $"{first}{last}";
    }
}