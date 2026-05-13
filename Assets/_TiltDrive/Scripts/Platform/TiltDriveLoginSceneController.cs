using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace TiltDrive.Platform
{
    public class TiltDriveLoginSceneController : MonoBehaviour
    {
        [Serializable]
        private struct LoginPreset
        {
            public string label;
            public string email;
            public string password;
            public string role;
        }

        [Header("API local")]
        [SerializeField] private string apiBaseUrl = "http://127.0.0.1:4000";

        [Header("Navegacion")]
        [SerializeField] private string simulatorSceneName = "TestDrive";
        [SerializeField] private bool loadSimulatorOnLogin = true;

        [Header("Credenciales")]
        [SerializeField] private string email = string.Empty;
        [SerializeField] private string password = string.Empty;

        [Header("Debug")]
        [SerializeField] private bool showDebugUserList;
        [SerializeField] private KeyCode debugUserListKey = KeyCode.R;
        [SerializeField] private LoginPreset[] debugPresets =
        {
            new LoginPreset { label = "Admin - Administrador TildDrive", email = "admin@tilddrive.local", password = "admin123", role = "admin" },
            new LoginPreset { label = "Docente - Carlos Andrade", email = "instructor@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Laura Mendez", email = "laura.mendez@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Miguel Torres", email = "miguel.torres@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Paola Ramirez", email = "paola.ramirez@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Andres Salazar", email = "andres.salazar@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Natalia Rojas", email = "natalia.rojas@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Sergio Molina", email = "sergio.molina@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Camila Duarte", email = "camila.duarte@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Jorge Castillo", email = "jorge.castillo@tilddrive.local", password = "instructor123", role = "instructor" },
            new LoginPreset { label = "Docente - Valentina Arias", email = "valentina.arias@tilddrive.local", password = "instructor123", role = "instructor" }
        };

        [Header("Layout")]
        [SerializeField] private Rect loginRect = new Rect(390f, 210f, 500f, 320f);
        [SerializeField] private Rect debugListRect = new Rect(910f, 120f, 330f, 560f);

        private string statusMessage = string.Empty;
        private bool isBusy;
        private Vector2 debugScroll;

        private void Awake()
        {
            TiltDrivePlatformAuthSession.Clear();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(debugUserListKey))
            {
                showDebugUserList = !showDebugUserList;
            }
        }

        private void OnGUI()
        {
            loginRect = GUI.Window(GetInstanceID(), loginRect, DrawLoginWindow, "TildDrive Login");

            if (showDebugUserList)
            {
                debugListRect = GUI.Window(GetInstanceID() + 1, debugListRect, DrawDebugUserWindow, "Usuarios Debug");
            }
        }

        private void DrawLoginWindow(int windowId)
        {
            GUILayout.Label("Correo");
            GUI.enabled = !isBusy;
            email = GUILayout.TextField(email ?? string.Empty);

            GUILayout.Label("Contrasena");
            password = GUILayout.PasswordField(password ?? string.Empty, '*');

            GUILayout.Space(8f);
            if (GUILayout.Button(isBusy ? "Validando..." : "Ingresar", GUILayout.Height(34f)))
            {
                Login();
            }

            GUI.enabled = true;
            GUILayout.Space(8f);
            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Label(statusMessage);
            }

            GUI.DragWindow();
        }

        private void DrawDebugUserWindow(int windowId)
        {
            GUILayout.Label("Seleccione para llenar credenciales");
            debugScroll = GUILayout.BeginScrollView(debugScroll);
            for (int i = 0; i < debugPresets.Length; i++)
            {
                LoginPreset preset = debugPresets[i];
                if (GUILayout.Button($"{preset.label}\n{preset.email}", GUILayout.Height(48f)))
                {
                    email = preset.email;
                    password = preset.password;
                    statusMessage = $"Credenciales cargadas: {preset.label}";
                    showDebugUserList = false;
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void Login()
        {
            if (isBusy)
            {
                return;
            }

            StartCoroutine(LoginRoutine());
        }

        private IEnumerator LoginRoutine()
        {
            isBusy = true;
            statusMessage = "Conectando con API local...";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                statusMessage = "Ingrese correo y contrasena.";
                isBusy = false;
                yield break;
            }

            LoginRequest request = new LoginRequest
            {
                email = email,
                password = password
            };

            using (UnityWebRequest webRequest = CreateJsonRequest("/auth/login", JsonUtility.ToJson(request)))
            {
                yield return webRequest.SendWebRequest();

                if (!IsSuccessful(webRequest))
                {
                    statusMessage = $"Login fallido: {ResolveError(webRequest)}";
                    isBusy = false;
                    yield break;
                }

                LoginResponse response = JsonUtility.FromJson<LoginResponse>(webRequest.downloadHandler.text);
                if (response == null || string.IsNullOrEmpty(response.token) || response.user == null)
                {
                    statusMessage = "La API no devolvio una sesion valida.";
                    isBusy = false;
                    yield break;
                }

                if (response.user.role != "instructor" && response.user.role != "admin")
                {
                    TiltDrivePlatformAuthSession.Clear();
                    statusMessage = $"Acceso denegado para rol: {response.user.role}";
                    isBusy = false;
                    yield break;
                }

                TiltDrivePlatformAuthSession.Set(
                    response.token,
                    response.user.id,
                    response.user.email,
                    response.user.fullName,
                    response.user.linkedProfileId,
                    response.user.role);

                statusMessage = $"Bienvenido {response.user.fullName}";
                isBusy = false;

                if (loadSimulatorOnLogin)
                {
                    EnsurePlatformClientExists();
                    SceneManager.LoadScene(simulatorSceneName);
                }
            }
        }

        private static void EnsurePlatformClientExists()
        {
            if (FindFirstObjectByType<TiltDrivePlatformClient>() != null)
            {
                return;
            }

            GameObject client = new GameObject("TiltDrive Platform Client");
            client.AddComponent<TiltDrivePlatformClient>();
        }

        private UnityWebRequest CreateJsonRequest(string path, string jsonBody)
        {
            UnityWebRequest request = new UnityWebRequest($"{apiBaseUrl.TrimEnd('/')}{path}", "POST")
            {
                downloadHandler = new DownloadHandlerBuffer(),
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody))
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private static bool IsSuccessful(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.Success &&
                request.responseCode >= 200 &&
                request.responseCode < 300;
        }

        private static string ResolveError(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                try
                {
                    ErrorResponse error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                    if (error != null && !string.IsNullOrEmpty(error.error))
                    {
                        return error.error;
                    }
                }
                catch (ArgumentException)
                {
                    return request.downloadHandler.text;
                }
            }

            return string.IsNullOrEmpty(request.error) ? $"HTTP {request.responseCode}" : request.error;
        }

        [Serializable]
        private class LoginRequest
        {
            public string email;
            public string password;
        }

        [Serializable]
        private class LoginResponse
        {
            public string token;
            public PublicUser user;
            public string error;
        }

        [Serializable]
        private class PublicUser
        {
            public string email;
            public string fullName;
            public string id;
            public string linkedProfileId;
            public string role;
        }

        [Serializable]
        private class ErrorResponse
        {
            public string error;
        }
    }
}
