using Firebase.Database;
using Firebase.Database.Query;
using Blazored.LocalStorage;
//using Firebase.Database.Streaming;
//using System.Reactive.Linq;

namespace Services
{
    public class FirebaseRealtimeDatabaseService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly string firebaseUrl = "https://r--task-default-rtdb.europe-west1.firebasedatabase.app/";

        public FirebaseRealtimeDatabaseService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // recibe el id token del usuario guardado en la memoria local
        private async Task<string> GetIdTokenAsync()
        {
            var token = await _localStorage.GetItemAsStringAsync("token");
            if (token != null) return token.Replace("\"", "");
            else return "";
        }

        // genera un cliente de Firebase con el token local
        private async Task<FirebaseClient> GetFirebaseClientAsync()
        {
            var idToken = await GetIdTokenAsync();
            return new FirebaseClient(
                firebaseUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(idToken)
                });
        }

        /// <summary>
        /// Añade un nuevo valor en el nodo objetivo, creando un nuevo Id.
        /// El valor puede ser un objeto anónimo
        /// </summary>
        /// <typeparam name="T">Tipo de valor esperado</typeparam>
        /// <param name="path">Ruta al nodo</param>
        /// <param name="data">Valor a establecer</param>
        /// <returns>El nuevo Id creado</returns>
        public async Task<string> AddDataAsync<T>(string path, T data)
        {
            var client = await GetFirebaseClientAsync();
            var result = await client
                .Child(path)
                .PostAsync(data);
            return result.Key;
        }

        /// <summary>
        /// Borra el nodo objetivo
        /// </summary>
        /// <param name="path">Ruta al nodo</param>
        public async Task RemoveDataAsync(string path)
        {
            var client = await GetFirebaseClientAsync();
            await client
                .Child(path)
                .DeleteAsync();
        }

        /// <summary>
        /// Establece el valor en el nodo objetivo, sin crear un nuevo Id
        /// </summary>
        /// <typeparam name="T">Tipo de valor esperado</typeparam>
        /// <param name="path">Ruta al nodo</param>
        /// <param name="data">Valor a establecer</param>
        public async Task SetDataAsync<T>(string path, T data)
        {
            var client = await GetFirebaseClientAsync();
            await client
                .Child(path)
                .PutAsync(data);
        }

        /// <summary>
        /// Consulta el valor guardado en el nodo indicado
        /// </summary>
        /// <typeparam name="T">Tipo de valor esperado</typeparam>
        /// <param name="path">Ruta del nodo a consultar</param>
        /// <returns>Valor guardado</returns>
        public async Task<T> GetDataAsync<T>(string path)
        {
            var client = await GetFirebaseClientAsync();
            var data = await client
                .Child(path)
                .OnceSingleAsync<T>();
            return data;
        }

        /// <summary>
        /// Consulta una lista de valores en el nodo indicado
        /// </summary>
        /// <typeparam name="T">Tipo de valor esperado</typeparam>
        /// <param name="path">Ruta del nodo a consultar</param>
        /// <returns>Valor guardado en el nodo</returns>
        public async Task<Dictionary<string, T>> GetListAsync<T>(string path)
        {
            var client = await GetFirebaseClientAsync();
            var data = await client
                .Child(path)
                .OnceAsync<T>();

            return data.ToDictionary(item => item.Key, item => item.Object);
        }

        /// <summary>
        /// Devuelve el email correpondiente para el nombre de usuario
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <returns>El email del usuario</returns>
        /// <exception cref="Exception">"username_not_found" si no se encuentra email para el nombre de usuario</exception>
        public async Task<string> GetEmail (string username)
        {
            try
            {
                // genero un nuevo cliente sin token de autorización
                var firebaseClient = new FirebaseClient("https://r--task-default-rtdb.europe-west1.firebasedatabase.app/");

                // compruebo el email asociado al nombre de usuario
                var email = await firebaseClient
                                .Child($"public-user-data/{username}/Email")
                                .OnceSingleAsync<string>();

                // devuelvo el email
                return email;
            }
            // en caso de que no se encuentre el email
            catch (Exception)
            {
                throw new Exception("email_not_found");
            }
        }

        /// <summary>
        /// Devuelve el user_id correpondiente para el nombre de usuario
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <returns>El user_id del usuario</returns>
        /// <exception cref="Exception">"uid_not_found" si no se encuentra user_id</exception>
        public async Task<string> GetUserId (string username)
        {
            try
            {
                var firebaseClient = await GetFirebaseClientAsync();

                // compruebo el user_id asociado al nombre de usuario
                var uid = await firebaseClient
                                .Child($"public-user-data/{username}/Uid")
                                .OnceSingleAsync<string>();

                // devuelvo el email
                return uid;
            }
            // en caso de que no se encuentre el email
            catch (Exception)
            {
                throw new Exception("uid_not_found");
            }
        }

        /// <summary>
        /// Devuelve el nombre de usuario correpondiente para el user_id
        /// </summary>
        /// <param name="uid">user_id del usuario objetivo</param>
        /// <returns>El email del usuario</returns>
        /// <exception cref="Exception">"username_not_found" si no se encuentra nombre de usuario para el user_id</exception>
        public async Task<string> GetUsername (string uid)
        {
            try
            {
                var firebaseClient = await GetFirebaseClientAsync();

                // compruebo el nombre de usuario asociado al uid
                var username = await firebaseClient
                                .Child($"user-data/{uid}/username")
                                .OnceSingleAsync<string>();

                // devuelvo el email
                return username;
            }
            // en caso de que no se encuentre el email
            catch (Exception)
            {
                throw new Exception("username_not_found");
            }
        }
    }
}
