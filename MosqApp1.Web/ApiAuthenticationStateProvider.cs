namespace MosqApp1.Web
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Authorization;
    using Microsoft.JSInterop;
    using System.Net.Http.Headers;
    using System.Security.Claims;

    public class ApiAuthenticationStateProvider(TokenStorageService tokenStorageService, WorkoutRecordsApiClient workoutRecordsApi) : AuthenticationStateProvider
    {
        private static AuthenticationState Anonymous => new(new ClaimsPrincipal(new ClaimsIdentity()));
        private bool _isInitialized;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (!_isInitialized)
            {
                // During prerendering, return an anonymous state
                return Anonymous;
            }

            var token = await tokenStorageService.GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return Anonymous;
            }

            var response = await workoutRecordsApi.RequestUserInfo(token);
            if (!response.IsSuccessStatusCode)
            {
                return Anonymous;
            }

            var userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();

            var identity = new ClaimsIdentity(
            [
            new Claim(ClaimTypes.Name, userInfo.Username),
            new Claim("DisplayName", userInfo.DisplayName),
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId)
        ], "apiauth");

            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        public async Task MarkUserAsAuthenticated(string token)
        {
            await tokenStorageService.SetTokenAsync(token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task MarkUserAsLoggedOut()
        {
            await tokenStorageService.RemoveTokenAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }

        public void SetInitialized()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }

    public record UserInfo
    {
        public string UserId {  get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
    }

    public class TokenStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public TokenStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetTokenAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
        }

        public async Task<string> GetTokenAsync()
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            return token;// await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
        }

        public async Task RemoveTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        }
    }

}
