using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace TiltDrive.Platform
{
    public enum TiltDrivePlatformEventSeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    public class TiltDrivePlatformClient : MonoBehaviour
    {
        public static TiltDrivePlatformClient Instance { get; private set; }

        [Header("API local")]
        [SerializeField] private string apiBaseUrl = "http://127.0.0.1:4000";
        [SerializeField] private bool autoLoginOnStart = false;
        [SerializeField] private bool autoCreateOnPlay = true;

        [Header("Acceso desde Login")]
        [SerializeField] private bool requireInstructorOrAdminSession = true;
        [SerializeField] private bool redirectToLoginWhenMissingSession = true;
        [SerializeField] private string loginSceneName = "Login";

        [Header("Credenciales simulador")]
        [SerializeField] private string simulatorEmail = "sim01@tilddrive.local";
        [SerializeField] private string simulatorPassword = "simulator123";
        [SerializeField] private string simulatorId = "sim-001";

        [Header("Sesion de clase")]
        [SerializeField] private string bookingCode = "TD-48291";
        [SerializeField] [Range(0, 100)] private int defaultCloseScore = 85;
        [SerializeField] private string defaultInstructorSummary = "Clase cerrada desde el simulador TiltDrive.";

        [Header("Panel de prueba")]
        [SerializeField] private bool showSessionPanel = true;
        [SerializeField] private Rect sessionPanelRect = new Rect(980f, 20f, 280f, 185f);

        private string authToken = string.Empty;
        private string activeSessionId = string.Empty;
        private string activeBookingId = string.Empty;
        private string activeStudentName = string.Empty;
        private string activeLessonTitle = string.Empty;
        private string statusMessage = "Sin conexion";
        private bool isBusy;

        public bool HasAuthToken => !string.IsNullOrEmpty(authToken);
        public bool HasActiveSession => !string.IsNullOrEmpty(activeSessionId);
        public string ActiveSessionId => activeSessionId;
        public string ActiveBookingId => activeBookingId;
        public string ActiveStudentName => activeStudentName;
        public string ActiveLessonTitle => activeLessonTitle;
        public string StatusMessage => statusMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureClient()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!Application.isPlaying)
            {
                return;
            }

            if (SceneManager.GetActiveScene().name == "Login")
            {
                return;
            }

            if (FindFirstObjectByType<TiltDrivePlatformClient>() != null)
            {
                return;
            }

            GameObject client = new GameObject("TiltDrive Platform Client");
            TiltDrivePlatformClient component = client.AddComponent<TiltDrivePlatformClient>();
            if (!component.autoCreateOnPlay)
            {
                Destroy(client);
            }
#endif
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyAuthSession();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            ApplyAuthSession();

            if (!ValidateInstructorOrAdminSession())
            {
                return;
            }

            if (autoLoginOnStart)
            {
                Login();
            }
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name == loginSceneName)
            {
                return;
            }

            if (!showSessionPanel)
            {
                return;
            }

            sessionPanelRect = GUI.Window(GetInstanceID(), sessionPanelRect, DrawSessionWindow, "TildDrive API");
        }

        public void Login()
        {
            if (isBusy)
            {
                return;
            }

            if (requireInstructorOrAdminSession)
            {
                statusMessage = "El login debe hacerse desde la escena Login.";
                RedirectToLoginScene();
                return;
            }

            StartCoroutine(LoginRoutine(null));
        }

        public void StartClassSession()
        {
            if (isBusy)
            {
                return;
            }

            StartCoroutine(StartClassSessionRoutine());
        }

        public void CloseClassSession(bool approved)
        {
            CloseClassSession(approved, defaultCloseScore, defaultInstructorSummary);
        }

        public void CloseClassSession(bool approved, int score, string instructorSummary)
        {
            if (isBusy || !HasActiveSession)
            {
                return;
            }

            StartCoroutine(CloseClassSessionRoutine(approved, score, instructorSummary));
        }

        public void RecordBadPractice(string message)
        {
            RecordSessionEvent("bad_practice", TiltDrivePlatformEventSeverity.Warning, message);
        }

        public void RecordError(string message)
        {
            RecordSessionEvent("error", TiltDrivePlatformEventSeverity.Critical, message);
        }

        public void RecordSystemEvent(string message)
        {
            RecordSessionEvent("system", TiltDrivePlatformEventSeverity.Info, message);
        }

        public void RecordInstructorNote(string message)
        {
            RecordSessionEvent("instructor_note", TiltDrivePlatformEventSeverity.Info, message);
        }

        public void RecordSessionEvent(string type, TiltDrivePlatformEventSeverity severity, string message)
        {
            if (!HasActiveSession || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            StartCoroutine(SendSessionEventRoutine(type, severity, message));
        }

        public void SetBookingCode(string value)
        {
            bookingCode = value ?? string.Empty;
        }

        public void SetSimulatorId(string value)
        {
            simulatorId = value ?? string.Empty;
        }

        private IEnumerator LoginRoutine(Action<bool> onCompleted)
        {
            isBusy = true;
            statusMessage = "Autenticando simulador...";

            LoginRequest request = new LoginRequest
            {
                email = simulatorEmail,
                password = simulatorPassword
            };

            using (UnityWebRequest webRequest = CreateJsonRequest("/auth/login", "POST", JsonUtility.ToJson(request), false))
            {
                yield return webRequest.SendWebRequest();

                bool ok = IsSuccessful(webRequest);
                if (ok)
                {
                    LoginResponse response = JsonUtility.FromJson<LoginResponse>(webRequest.downloadHandler.text);
                    authToken = response != null ? response.token : string.Empty;
                    ok = !string.IsNullOrEmpty(authToken);
                    statusMessage = ok ? "Simulador autenticado" : "Respuesta de login sin token";
                }
                else
                {
                    statusMessage = $"Login fallido: {ResolveError(webRequest)}";
                }

                isBusy = false;
                onCompleted?.Invoke(ok);
            }
        }

        private IEnumerator StartClassSessionRoutine()
        {
            ApplyAuthSession();
            if (!ValidateInstructorOrAdminSession())
            {
                yield break;
            }

            if (!HasAuthToken)
            {
                if (requireInstructorOrAdminSession)
                {
                    statusMessage = "Debe iniciar sesion como docente/admin.";
                    RedirectToLoginScene();
                    yield break;
                }

                bool loginOk = false;
                yield return LoginRoutine(ok => loginOk = ok);
                if (!loginOk)
                {
                    yield break;
                }
            }

            if (string.IsNullOrWhiteSpace(bookingCode))
            {
                statusMessage = "Ingresa el codigo de reserva";
                yield break;
            }

            isBusy = true;
            statusMessage = "Validando reserva...";

            StartSessionRequest request = new StartSessionRequest
            {
                bookingCode = bookingCode.Trim(),
                simulatorId = simulatorId.Trim()
            };

            using (UnityWebRequest webRequest = CreateJsonRequest(
                "/simulator/session/start",
                "POST",
                JsonUtility.ToJson(request),
                true))
            {
                yield return webRequest.SendWebRequest();

                if (IsSuccessful(webRequest))
                {
                    StartSessionResponse response = JsonUtility.FromJson<StartSessionResponse>(webRequest.downloadHandler.text);
                    activeSessionId = response != null && response.session != null ? response.session.id : string.Empty;
                    activeBookingId = response != null && response.booking != null ? response.booking.id : string.Empty;
                    activeStudentName = response != null && response.student != null ? response.student.fullName : string.Empty;
                    activeLessonTitle = response != null && response.lesson != null ? response.lesson.title : string.Empty;
                    statusMessage = HasActiveSession
                        ? $"Sesion iniciada: {activeStudentName}"
                        : "Reserva valida, pero sin id de sesion";

                    if (HasActiveSession)
                    {
                        RecordSystemEvent("Sesion iniciada desde Unity.");
                    }
                }
                else
                {
                    ClearActiveSession();
                    statusMessage = $"No se pudo iniciar: {ResolveError(webRequest)}";
                }

                isBusy = false;
            }
        }

        private IEnumerator SendSessionEventRoutine(string type, TiltDrivePlatformEventSeverity severity, string message)
        {
            SessionEventRequest request = new SessionEventRequest
            {
                message = message,
                severity = ToApiSeverity(severity),
                time = DateTime.UtcNow.ToString("o"),
                type = type
            };

            using (UnityWebRequest webRequest = CreateJsonRequest(
                $"/simulator/session/{activeSessionId}/events",
                "POST",
                JsonUtility.ToJson(request),
                true))
            {
                yield return webRequest.SendWebRequest();

                if (!IsSuccessful(webRequest))
                {
                    Debug.LogWarning($"[TiltDrive][Platform] No se pudo enviar evento: {ResolveError(webRequest)} | Message={message}");
                }
            }
        }

        private IEnumerator CloseClassSessionRoutine(bool approved, int score, string instructorSummary)
        {
            isBusy = true;
            statusMessage = "Cerrando clase...";

            CloseSessionRequest request = new CloseSessionRequest
            {
                approved = approved,
                instructorSummary = instructorSummary,
                score = Mathf.Clamp(score, 0, 100)
            };

            using (UnityWebRequest webRequest = CreateJsonRequest(
                $"/simulator/session/{activeSessionId}/close",
                "POST",
                JsonUtility.ToJson(request),
                true))
            {
                yield return webRequest.SendWebRequest();

                if (IsSuccessful(webRequest))
                {
                    statusMessage = approved ? "Clase cerrada como aprobada" : "Clase cerrada como no aprobada";
                    ClearActiveSession();
                }
                else
                {
                    statusMessage = $"No se pudo cerrar: {ResolveError(webRequest)}";
                }

                isBusy = false;
            }
        }

        private void DrawSessionWindow(int windowId)
        {
            GUILayout.Label(statusMessage);
            GUILayout.Space(4f);

            if (TiltDrivePlatformAuthSession.IsAuthenticated)
            {
                GUILayout.Label($"Usuario: {TiltDrivePlatformAuthSession.FullName}");
                GUILayout.Label($"Rol: {TiltDrivePlatformAuthSession.Role}");
                GUILayout.Space(4f);
            }

            GUILayout.Label("Codigo de reserva");
            bookingCode = GUILayout.TextField(bookingCode ?? string.Empty);

            GUILayout.Label("Simulador");
            simulatorId = GUILayout.TextField(simulatorId ?? string.Empty);

            GUILayout.BeginHorizontal();
            GUI.enabled = !isBusy;
            if (GUILayout.Button(HasAuthToken ? "Cambiar usuario" : "Login"))
            {
                RedirectToLoginScene();
            }

            GUI.enabled = !isBusy && !HasActiveSession;
            if (GUILayout.Button("Iniciar"))
            {
                StartClassSession();
            }
            GUILayout.EndHorizontal();

            GUI.enabled = !isBusy && HasActiveSession;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cerrar OK"))
            {
                CloseClassSession(true);
            }

            if (GUILayout.Button("Cerrar NO"))
            {
                CloseClassSession(false);
            }
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            if (HasActiveSession)
            {
                GUILayout.Label($"Estudiante: {activeStudentName}");
                GUILayout.Label($"Leccion: {activeLessonTitle}");
            }

            GUI.DragWindow();
        }

        private UnityWebRequest CreateJsonRequest(string path, string method, string jsonBody, bool includeAuth)
        {
            string url = $"{apiBaseUrl.TrimEnd('/')}{path}";
            UnityWebRequest request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };

            if (!string.IsNullOrEmpty(jsonBody))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.SetRequestHeader("Content-Type", "application/json");
            if (includeAuth && !string.IsNullOrEmpty(authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

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

        private static string ToApiSeverity(TiltDrivePlatformEventSeverity severity)
        {
            switch (severity)
            {
                case TiltDrivePlatformEventSeverity.Critical:
                    return "critical";
                case TiltDrivePlatformEventSeverity.Warning:
                    return "warning";
                default:
                    return "info";
            }
        }

        private void ClearActiveSession()
        {
            activeSessionId = string.Empty;
            activeBookingId = string.Empty;
            activeStudentName = string.Empty;
            activeLessonTitle = string.Empty;
        }

        private void ApplyAuthSession()
        {
            if (!TiltDrivePlatformAuthSession.IsAuthenticated)
            {
                authToken = string.Empty;
                return;
            }

            if (!TiltDrivePlatformAuthSession.CanAccessSimulator)
            {
                authToken = string.Empty;
                statusMessage = $"Rol sin acceso al simulador: {TiltDrivePlatformAuthSession.Role}";
                return;
            }

            authToken = TiltDrivePlatformAuthSession.Token;
            statusMessage = $"Sesion Unity: {TiltDrivePlatformAuthSession.FullName}";
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == loginSceneName)
            {
                authToken = string.Empty;
                ClearActiveSession();
                statusMessage = "Esperando login.";
                return;
            }

            ApplyAuthSession();
            ValidateInstructorOrAdminSession();
        }

        private bool ValidateInstructorOrAdminSession()
        {
            if (!requireInstructorOrAdminSession)
            {
                return true;
            }

            if (TiltDrivePlatformAuthSession.IsAuthenticated && TiltDrivePlatformAuthSession.CanAccessSimulator && HasAuthToken)
            {
                return true;
            }

            statusMessage = "Debe iniciar sesion como docente/admin antes de abrir el simulador.";
            RedirectToLoginScene();
            return false;
        }

        private void RedirectToLoginScene()
        {
            if (!redirectToLoginWhenMissingSession || string.IsNullOrWhiteSpace(loginSceneName))
            {
                return;
            }

            if (SceneManager.GetActiveScene().name == loginSceneName)
            {
                return;
            }

            SceneManager.LoadScene(loginSceneName);
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
            public string id;
            public string email;
            public string fullName;
            public string linkedProfileId;
            public string role;
        }

        [Serializable]
        private class StartSessionRequest
        {
            public string bookingCode;
            public string simulatorId;
        }

        [Serializable]
        private class StartSessionResponse
        {
            public PlatformBooking booking;
            public PlatformInstructor instructor;
            public PlatformLesson lesson;
            public PlatformSession session;
            public PlatformStudent student;
            public string error;
        }

        [Serializable]
        private class SessionEventRequest
        {
            public string message;
            public string severity;
            public string time;
            public string type;
        }

        [Serializable]
        private class CloseSessionRequest
        {
            public bool approved;
            public string instructorSummary;
            public int score;
        }

        [Serializable]
        private class PlatformBooking
        {
            public string code;
            public string id;
        }

        [Serializable]
        private class PlatformInstructor
        {
            public string fullName;
            public string id;
        }

        [Serializable]
        private class PlatformLesson
        {
            public string id;
            public string title;
        }

        [Serializable]
        private class PlatformSession
        {
            public string id;
            public string status;
        }

        [Serializable]
        private class PlatformStudent
        {
            public string fullName;
            public string id;
        }

        [Serializable]
        private class ErrorResponse
        {
            public string error;
        }
    }
}
