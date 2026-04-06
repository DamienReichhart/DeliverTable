using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Constants;
using DeliverTableClient.Services.Interfaces;

namespace DeliverTableClient.Pages.Home
{
    public partial class Home(
        IHealthApiClient healthApiClient
    )
    {

        private HealthResponse? _health = null;
        private bool _isLoading = false;
        private string _error = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            _isLoading = true;
            try
            {
                HealthResponse? response = await healthApiClient.GetHealthAsync();
                _health = response;
            }
            catch (Exception e)
            {
                _error = e.Message;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private string DetermineColor()
        {
            Enum.TryParse<HealthStatus>(_health?.Status ?? HealthStatus.Unhealthy.ToString(), true, out HealthStatus status);
            return status switch
            {
                HealthStatus.Healthy => "alert-success text-success",
                HealthStatus.Degraded => "alert-warning  text-warning",
                HealthStatus.Unhealthy => "alert-danger  text-danger",
                _ => "alert-secondary"
            };
        }
    }
}