using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace TiltDrive.ExternalCameraSystem
{
    public class ExternalCameraScreen : MonoBehaviour
    {
        [Header("Fuente")]
        [SerializeField] private string streamUrl = "http://192.168.1.50:8080/vuforia.mjpg";
        [SerializeField] private ExternalCameraStreamMode streamMode = ExternalCameraStreamMode.Auto;
        [SerializeField] private bool connectOnStart = true;
        [SerializeField] [Min(1f)] private float snapshotFramesPerSecond = 12f;
        [SerializeField] [Min(0.25f)] private float reconnectDelaySeconds = 2f;
        [SerializeField] [Min(1)] private int requestTimeoutSeconds = 8;
        [SerializeField] [Min(256)] private int maxMjpegBufferKilobytes = 2048;

        [Header("Pantalla 3D")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private bool createScreenIfMissing = true;
        [SerializeField] private Vector2 screenSizeMeters = new Vector2(3.2f, 1.8f);
        [SerializeField] private Vector3 screenLocalPosition = new Vector3(0f, 1.8f, -4.5f);
        [SerializeField] private Vector3 screenLocalEulerAngles = Vector3.zero;
        [SerializeField] private Color placeholderColor = new Color(0.015f, 0.018f, 0.022f, 1f);
        [SerializeField] private bool mirrorX = false;
        [SerializeField] private bool mirrorY = true;

        [Header("UI opcional")]
        [SerializeField] private RawImage targetRawImage;
        [SerializeField] private bool logConnectionChanges = true;

        private readonly object frameLock = new object();
        private byte[] latestFrameBytes;
        private bool hasPendingFrame;

        private Texture2D streamTexture;
        private Material runtimeMaterial;
        private Mesh generatedMesh;
        private Coroutine streamRoutine;
        private UnityWebRequest activeRequest;
        private string lastStatus = "Desconectado";
        private bool connected;

        public string LastStatus => lastStatus;
        public bool IsConnected => connected;
        public Texture CurrentTexture => streamTexture;

        private void Awake()
        {
            EnsureScreen();
            EnsureTexture();
            ApplyTextureToTargets();
        }

        private void OnEnable()
        {
            if (connectOnStart)
            {
                StartStream();
            }
        }

        private void OnDisable()
        {
            StopStream();
        }

        private void OnDestroy()
        {
            StopStream();

            if (Application.isPlaying)
            {
                if (runtimeMaterial != null) Destroy(runtimeMaterial);
                if (streamTexture != null) Destroy(streamTexture);
                if (generatedMesh != null) Destroy(generatedMesh);
            }
            else
            {
                if (runtimeMaterial != null) DestroyImmediate(runtimeMaterial);
                if (streamTexture != null) DestroyImmediate(streamTexture);
                if (generatedMesh != null) DestroyImmediate(generatedMesh);
            }
        }

        private void Update()
        {
            if (!TryTakeFrame(out byte[] frameBytes))
            {
                return;
            }

            EnsureTexture();
            if (streamTexture.LoadImage(frameBytes, false))
            {
                streamTexture.filterMode = FilterMode.Bilinear;
                streamTexture.wrapMode = TextureWrapMode.Clamp;
                ApplyTextureToTargets();
                SetConnected(true, $"Recibiendo {streamTexture.width}x{streamTexture.height}");
            }
            else
            {
                SetConnected(false, "Frame invalido");
            }
        }

        [ContextMenu("Start Stream")]
        public void StartStream()
        {
            if (streamRoutine != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(streamUrl))
            {
                SetConnected(false, "URL vacia");
                return;
            }

            streamRoutine = StartCoroutine(RunStream());
        }

        [ContextMenu("Stop Stream")]
        public void StopStream()
        {
            if (activeRequest != null)
            {
                activeRequest.Abort();
                activeRequest.Dispose();
                activeRequest = null;
            }

            if (streamRoutine != null)
            {
                StopCoroutine(streamRoutine);
                streamRoutine = null;
            }

            SetConnected(false, "Desconectado");
        }

        public void SetStreamUrl(string nextUrl)
        {
            streamUrl = nextUrl;

            if (isActiveAndEnabled && streamRoutine != null)
            {
                StopStream();
                StartStream();
            }
        }

        private IEnumerator RunStream()
        {
            ExternalCameraStreamMode resolvedMode = ResolveStreamMode();

            while (enabled)
            {
                if (resolvedMode == ExternalCameraStreamMode.SnapshotPolling)
                {
                    yield return PollSnapshots();
                }
                else
                {
                    yield return ReadMjpegStream();
                }

                if (!enabled)
                {
                    break;
                }

                yield return new WaitForSeconds(reconnectDelaySeconds);
            }

            streamRoutine = null;
        }

        private ExternalCameraStreamMode ResolveStreamMode()
        {
            if (streamMode != ExternalCameraStreamMode.Auto)
            {
                return streamMode;
            }

            string lowerUrl = streamUrl.ToLowerInvariant();
            if (lowerUrl.EndsWith(".jpg") ||
                lowerUrl.EndsWith(".jpeg") ||
                lowerUrl.EndsWith(".png") ||
                lowerUrl.Contains("snapshot"))
            {
                return ExternalCameraStreamMode.SnapshotPolling;
            }

            return ExternalCameraStreamMode.Mjpeg;
        }

        private IEnumerator PollSnapshots()
        {
            float frameInterval = 1f / Mathf.Max(1f, snapshotFramesPerSecond);

            while (enabled)
            {
                string url = BuildCacheBustedUrl(streamUrl);
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    activeRequest = request;
                    request.timeout = requestTimeoutSeconds;
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        QueueFrame(request.downloadHandler.data);
                    }
                    else
                    {
                        SetConnected(false, request.error);
                        activeRequest = null;
                        yield break;
                    }
                }

                activeRequest = null;
                yield return new WaitForSeconds(frameInterval);
            }
        }

        private IEnumerator ReadMjpegStream()
        {
            MjpegDownloadHandler handler = new MjpegDownloadHandler(
                QueueFrame,
                Mathf.Max(256, maxMjpegBufferKilobytes) * 1024);

            using (UnityWebRequest request = new UnityWebRequest(streamUrl, UnityWebRequest.kHttpVerbGET))
            {
                activeRequest = request;
                request.timeout = requestTimeoutSeconds;
                request.downloadHandler = handler;
                request.disposeDownloadHandlerOnDispose = true;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetConnected(false, request.error);
                }
            }

            activeRequest = null;
        }

        private static string BuildCacheBustedUrl(string url)
        {
            string separator = url.Contains("?") ? "&" : "?";
            return $"{url}{separator}t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        }

        private void QueueFrame(byte[] frameBytes)
        {
            if (frameBytes == null || frameBytes.Length == 0)
            {
                return;
            }

            lock (frameLock)
            {
                latestFrameBytes = frameBytes;
                hasPendingFrame = true;
            }
        }

        private bool TryTakeFrame(out byte[] frameBytes)
        {
            lock (frameLock)
            {
                if (!hasPendingFrame)
                {
                    frameBytes = null;
                    return false;
                }

                frameBytes = latestFrameBytes;
                latestFrameBytes = null;
                hasPendingFrame = false;
                return true;
            }
        }

        private void EnsureTexture()
        {
            if (streamTexture != null)
            {
                return;
            }

            streamTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "External Camera Stream",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            streamTexture.SetPixel(0, 0, placeholderColor);
            streamTexture.SetPixel(1, 0, placeholderColor);
            streamTexture.SetPixel(0, 1, placeholderColor);
            streamTexture.SetPixel(1, 1, placeholderColor);
            streamTexture.Apply();
        }

        private void EnsureScreen()
        {
            if (targetRenderer != null || !createScreenIfMissing)
            {
                return;
            }

            GameObject surface = new GameObject("External Camera Screen Surface");
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = screenLocalPosition;
            surface.transform.localRotation = Quaternion.Euler(screenLocalEulerAngles);

            MeshFilter meshFilter = surface.AddComponent<MeshFilter>();
            targetRenderer = surface.AddComponent<MeshRenderer>();
            generatedMesh = BuildScreenMesh();
            meshFilter.sharedMesh = generatedMesh;
            targetRenderer.sharedMaterial = CreateRuntimeMaterial();
        }

        private Mesh BuildScreenMesh()
        {
            float width = Mathf.Max(0.1f, screenSizeMeters.x);
            float height = Mathf.Max(0.1f, screenSizeMeters.y);
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            float u0 = mirrorX ? 1f : 0f;
            float u1 = mirrorX ? 0f : 1f;
            float v0 = mirrorY ? 1f : 0f;
            float v1 = mirrorY ? 0f : 1f;

            Mesh mesh = new Mesh { name = "External Camera Screen Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(-halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(u0, v0),
                new Vector2(u0, v1),
                new Vector2(u1, v1),
                new Vector2(u1, v0)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Material CreateRuntimeMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Standard");

            runtimeMaterial = new Material(shader)
            {
                name = "External Camera Screen Material"
            };

            runtimeMaterial.color = Color.white;
            return runtimeMaterial;
        }

        private void ApplyTextureToTargets()
        {
            if (streamTexture == null)
            {
                return;
            }

            if (targetRawImage != null)
            {
                targetRawImage.texture = streamTexture;
                targetRawImage.uvRect = new Rect(
                    mirrorX ? 1f : 0f,
                    mirrorY ? 1f : 0f,
                    mirrorX ? -1f : 1f,
                    mirrorY ? -1f : 1f);
            }

            if (targetRenderer == null)
            {
                return;
            }

            Material material = targetRenderer.material != null ? targetRenderer.material : CreateRuntimeMaterial();
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", streamTexture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", streamTexture);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        }

        private void SetConnected(bool value, string status)
        {
            if (connected == value && lastStatus == status)
            {
                return;
            }

            connected = value;
            lastStatus = string.IsNullOrEmpty(status) ? (value ? "Conectado" : "Desconectado") : status;

            if (logConnectionChanges)
            {
                Debug.Log($"[TiltDrive][ExternalCameraScreen] {lastStatus}");
            }
        }

        private sealed class MjpegDownloadHandler : DownloadHandlerScript
        {
            private const byte JpegMarkerPrefix = 0xFF;
            private const byte JpegStartMarker = 0xD8;
            private const byte JpegEndMarker = 0xD9;

            private readonly Action<byte[]> onFrame;
            private readonly int maxBufferBytes;
            private readonly List<byte> buffer = new List<byte>(1024 * 256);

            public MjpegDownloadHandler(Action<byte[]> onFrame, int maxBufferBytes)
                : base(new byte[64 * 1024])
            {
                this.onFrame = onFrame;
                this.maxBufferBytes = Mathf.Max(1024 * 256, maxBufferBytes);
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength <= 0)
                {
                    return true;
                }

                for (int i = 0; i < dataLength; i++)
                {
                    buffer.Add(data[i]);
                }

                ExtractFrames();
                TrimOversizedBuffer();
                return true;
            }

            private void ExtractFrames()
            {
                while (buffer.Count > 4)
                {
                    int startIndex = FindMarker(JpegStartMarker, 0);
                    if (startIndex < 0)
                    {
                        KeepLastByteOnly();
                        return;
                    }

                    if (startIndex > 0)
                    {
                        buffer.RemoveRange(0, startIndex);
                    }

                    int endIndex = FindMarker(JpegEndMarker, 2);
                    if (endIndex < 0)
                    {
                        return;
                    }

                    int frameLength = endIndex + 2;
                    byte[] frame = buffer.GetRange(0, frameLength).ToArray();
                    buffer.RemoveRange(0, frameLength);
                    onFrame?.Invoke(frame);
                }
            }

            private int FindMarker(byte marker, int startIndex)
            {
                for (int i = Mathf.Max(0, startIndex); i < buffer.Count - 1; i++)
                {
                    if (buffer[i] == JpegMarkerPrefix && buffer[i + 1] == marker)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private void TrimOversizedBuffer()
            {
                if (buffer.Count <= maxBufferBytes)
                {
                    return;
                }

                int keepStart = Mathf.Max(0, buffer.Count - 2);
                byte[] tail = buffer.GetRange(keepStart, buffer.Count - keepStart).ToArray();
                buffer.Clear();
                buffer.AddRange(tail);
            }

            private void KeepLastByteOnly()
            {
                if (buffer.Count <= 1)
                {
                    return;
                }

                byte lastByte = buffer[buffer.Count - 1];
                buffer.Clear();
                buffer.Add(lastByte);
            }
        }
    }
}
