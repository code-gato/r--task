﻿using Blazored.LocalStorage;
using Firebase.Auth.Providers;
using Firebase.Auth;

namespace Services
{
    public class FirebaseAuthService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly FirebaseRealtimeDatabaseService _firebaseDB;
        public FirebaseAuthService(ILocalStorageService localStorage,
                                   FirebaseRealtimeDatabaseService firebaseDB)
        {   
            _localStorage = localStorage;
            _firebaseDB = firebaseDB;
        }

        // configuración del cliente de FirebaseAuth
        private static FirebaseAuthConfig config = new FirebaseAuthConfig
        {
            ApiKey = "AIzaSyDgP9u_idoVmLoUhl9dftaRc1yLEf_B0nY",
            AuthDomain = "r--task.firebaseapp.com",
            Providers = new FirebaseAuthProvider[] {
                new EmailProvider()
            }
        };

        // cliente FirebaseAuth
        private static FirebaseAuthClient client = new FirebaseAuthClient(config);

        // Clase de estructura de datos para registrar nuevos usuarios al nodo "public-user-data"
        private class UserData {
            public string Email { get; set; } = "";
            public string Uid { get; set; } = "";
        }

        public async Task LogInWithEmail(string email, string password)
        {

            try
            {
                // si el email no contiene una "@" es que no es un email, sino un nombre de usuario
                // recupero de la base de datos el email correspondiente a dicho nombre de usuario
                if (!email.Contains("@")) email = await _firebaseDB.GetEmail(email);
                
                // intento de inicio de sesión con las credenciales introducidas
                var user = await client.SignInWithEmailAndPasswordAsync(email, password);
                var token = user.User.Credential.IdToken;
                var userId = user.User.Uid;

                // guardo el token en la memoria del navegador
                await _localStorage.SetItemAsync("token", token);
                await _localStorage.SetItemAsync("user_id", userId);

                var username = await _firebaseDB.GetUsername(userId);
                await _localStorage.SetItemAsync("username", username);

            }
            // errores específicos de firebase
            catch (FirebaseAuthException)
            {
                throw new Exception("auth_failed");
            }
            // errores generales
            catch (Exception)
            {
                throw new Exception("general_error");
            }
        }

        // registra un usuario y guarda sus datos en la base de datos
        public async Task RegisterWithEmail (string username, string email, string password)
        {
            try
            {
                // compruebo si el email ya está en uso
                var signInMethods = await client.FetchSignInMethodsForEmailAsync(email);
                bool emailInUse = signInMethods.SignInProviders != null && signInMethods.SignInProviders.Any();
                
                // si lo está, devuelvo un error
                if (emailInUse) throw new Exception("email_in_use");

                // compruebo si el nombre de usuario está en uso
                var usernameTaken = await _firebaseDB.GetEmail(username);

                // si lo está, devuelvo un error
                if (!String.IsNullOrEmpty(usernameTaken)) throw new Exception("username_taken");

                // si está todo bien, creo un nuevo usuario
                var user = await client.CreateUserWithEmailAndPasswordAsync(email, password);

                var token = user.User.Credential.IdToken;
                var userId = user.User.Uid;

                // guardo el token en la memoria del navegador
                await _localStorage.SetItemAsync("token", token);
                await _localStorage.SetItemAsync("user_id", userId);
                await _localStorage.SetItemAsync("username", username);

                var data = new UserData{
                    Email = email,
                    Uid = userId
                };

                // guardo la información pública en el nodo correspondiente
                await _firebaseDB.SetDataAsync($"public-user-data/{username}", data);
                // también guardo el nombre de usuario en su nodo privado
                await _firebaseDB.SetDataAsync($"user-data/{userId}/username", username);
            }
            catch (FirebaseAuthException e)
            {
                // errores específicos de firebase
                throw new Exception(e.Message);
            }
            catch (Exception e)
            {
                // errores generales
                throw new Exception($"{e.Message}");
            }
        }

        // borra las referencias al usuario actual de la memoria del navegador, cerrando sesión
        public async Task SignOut()
        {
            // borro el token de la memoria del navegador
            await _localStorage.RemoveItemAsync("token");
            await _localStorage.RemoveItemAsync("username");
            await _localStorage.RemoveItemAsync("user_id");
        }

    }
}