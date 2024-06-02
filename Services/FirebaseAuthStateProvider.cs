using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Services
{
    public class FirebaseAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _http;
        private readonly NavigationManager _navigation;
        private readonly FirebaseAuthService _authService;

        public FirebaseAuthStateProvider(ILocalStorageService localStorage,
                                         HttpClient http,
                                         NavigationManager navigation,
                                         FirebaseAuthService authService)
        {
            _localStorage = localStorage;
            _http = http;
            _navigation = navigation;
            _authService = authService;
        }

        // comprueba el estado actual de autenticación y devuelve un objeto AuthenticationState con la información del usuario
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            var state = new AuthenticationState(user);

            // intenta recibir el token de la memoria local
            string? token = await _localStorage.GetItemAsStringAsync("token");

            if (!string.IsNullOrEmpty(token))
            {
                // si el token no está vacío, llama a la función que extrae los claims
                var claims = ParseClaimsFromJwt(token);

ç               // crea un nuevo ClaimsIdentity, y de este un objeto AuthenticationState
                identity = new ClaimsIdentity(claims, "jwt");
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("\"", ""));

                user = new ClaimsPrincipal(identity);
                state = new AuthenticationState(user);
            }

            if (!string.IsNullOrEmpty(token))
            {
                var currentClaims = ParseClaimsFromJwt(token);
                // si el token está caducado
                if (IsTokenExpired(currentClaims))
                {
                    await _localStorage.RemoveItemAsync("token");
                    await _localStorage.RemoveItemAsync("username");
                    await _localStorage.RemoveItemAsync("user_id");
                    _navigation.NavigateTo("/login", true);
                }
            }

            NotifyAuthenticationStateChanged(Task.FromResult(state));
            return state;
        }

        // comprueba el claim de caducidad del token para asegurarse de que no está caducado
        private bool IsTokenExpired(IEnumerable<Claim> claims)
        {
            var expirationClaim = claims.FirstOrDefault(c => c.Type == "exp");
            
            if (expirationClaim != null && long.TryParse(expirationClaim.Value, out long expirationTime))
            {
                var expirationDateTime = DateTimeOffset.FromUnixTimeSeconds(expirationTime).DateTime;
                return expirationDateTime <= DateTime.UtcNow;
            }

            Console.Error.WriteLine("token exp claim not found");
            return false;
        }

        // deserializa los claims codificados en el token
        public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = new List<Claim>();
            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    if (kvp.Key == "role" && kvp.Value is JsonElement roleElement)
                    {
                        if (roleElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var role in roleElement.EnumerateArray())
                            {
                                var _role = role.GetString();
                                if (_role != null) claims.Add(new Claim(ClaimTypes.Role, _role));
                            }
                        }
                        else
                        {
                            var _role = roleElement.GetString();
                            if (_role != null) claims.Add(new Claim(ClaimTypes.Role, _role));
                        }
                    }
                    else
                    {
                        var value = kvp.Value.ToString();
                        if (value != null) claims.Add(new Claim(kvp.Key, value));
                    }
                }
            }

            return claims;
        }

        // deserializa la estructura de base64 a string
        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}