using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace TiltDrive.ExternalCameraSystem
{
    public class VuforiaMjpegBroadcaster : MonoBehaviour
    {
        private enum CaptureSource
        {
            FinalScreen = 0,
            CameraRender = 1
        }

        [Header("Servidor")]
        [SerializeField] private bool startOnEnable = true;
        [SerializeField] [Min(1024)] private int port = 8080;
        [SerializeField] private string streamPath = "/vuforia.mjpg";
        [SerializeField] private string snapshotPath = "/snapshot.jpg";

        [Header("Captura")]
        [SerializeField] private CaptureSource captureSource = CaptureSource.FinalScreen;
        [SerializeField] private Camera captureCamera;
        [SerializeField] [Range(1, 30)] private int framesPerSecond = 12;
        [SerializeField] [Range(25, 95)] private int jpegQuality = 70;
        [SerializeField] [Min(128)] private int maxFrameWidth = 960;
        [SerializeField] [Min(64)] private int cameraCaptureWidth = 960;
        [SerializeField] [Min(64)] private int cameraCaptureHeight = 540;
        [SerializeField] private bool keepApplicationAwake = true;
        [SerializeField] private bool logServerStatus = true;

        private readonly object frameLock = new object();
        private byte[] latestFrameBytes;
        private int latestFrameVersion;

        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool serverRunning;
        private WaitForEndOfFrame waitForEndOfFrame;
        private Coroutine captureRoutine;

        public string StreamUrlHint => $"http://TELEFONO_IP:{port}{NormalizePath(streamPath)}";
        public bool IsRunning => serverRunning;

        private void OnEnable()
        {
            if (keepApplicationAwake)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Application.runInBackground = true;
            }

            waitForEndOfFrame = new WaitForEndOfFrame();

            if (startOnEnable)
            {
                StartBroadcast();
            }
        }

        private void OnDisable()
        {
            StopBroadcast();
        }

        private void OnApplicationQuit()
        {
            StopBroadcast();
        }

        [ContextMenu("Start Broadcast")]
        public void StartBroadcast()
        {
            if (serverRunning)
            {
                return;
            }

            streamPath = NormalizePath(streamPath);
            snapshotPath = NormalizePath(snapshotPath);
            serverRunning = true;

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                acceptThread = new Thread(AcceptClientsLoop)
                {
                    IsBackground = true,
                    Name = "TiltDrive Vuforia MJPEG Broadcaster"
                };
                acceptThread.Start();

                captureRoutine = StartCoroutine(CaptureFramesLoop());
                LogStatus($"Servidor listo: {StreamUrlHint}");
            }
            catch (Exception exception)
            {
                serverRunning = false;
                LogStatus($"No se pudo iniciar el servidor: {exception.Message}");
            }
        }

        [ContextMenu("Stop Broadcast")]
        public void StopBroadcast()
        {
            serverRunning = false;

            try
            {
                listener?.Stop();
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            listener = null;
            acceptThread = null;

            if (captureRoutine != null)
            {
                StopCoroutine(captureRoutine);
                captureRoutine = null;
            }
        }

        private System.Collections.IEnumerator CaptureFramesLoop()
        {
            float frameInterval = 1f / Mathf.Max(1, framesPerSecond);

            while (serverRunning && enabled)
            {
                yield return waitForEndOfFrame;

                Texture2D frame = captureSource == CaptureSource.CameraRender
                    ? CaptureCameraFrame()
                    : ScreenCapture.CaptureScreenshotAsTexture();

                if (frame != null)
                {
                    Texture2D output = ResizeIfNeeded(frame);
                    byte[] jpgBytes = output.EncodeToJPG(jpegQuality);
                    PublishFrame(jpgBytes);

                    Destroy(frame);
                    if (output != frame)
                    {
                        Destroy(output);
                    }
                }

                yield return new WaitForSeconds(frameInterval);
            }

            captureRoutine = null;
        }

        private Texture2D CaptureCameraFrame()
        {
            Camera sourceCamera = captureCamera != null ? captureCamera : Camera.main;
            if (sourceCamera == null)
            {
                return null;
            }

            int width = Mathf.Max(64, cameraCaptureWidth);
            int height = Mathf.Max(64, cameraCaptureHeight);
            RenderTexture previousTarget = sourceCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);

            try
            {
                sourceCamera.targetTexture = renderTexture;
                sourceCamera.Render();
                RenderTexture.active = renderTexture;

                Texture2D frame = new Texture2D(width, height, TextureFormat.RGB24, false);
                frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                frame.Apply(false);
                return frame;
            }
            finally
            {
                sourceCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private Texture2D ResizeIfNeeded(Texture2D source)
        {
            if (source == null || source.width <= maxFrameWidth)
            {
                return source;
            }

            int width = Mathf.Max(128, maxFrameWidth);
            int height = Mathf.Max(64, Mathf.RoundToInt(source.height * (width / (float)source.width)));
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;

                Texture2D resized = new Texture2D(width, height, TextureFormat.RGB24, false);
                resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                resized.Apply(false);
                return resized;
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private void PublishFrame(byte[] jpgBytes)
        {
            if (jpgBytes == null || jpgBytes.Length == 0)
            {
                return;
            }

            lock (frameLock)
            {
                latestFrameBytes = jpgBytes;
                latestFrameVersion++;
            }
        }

        private void AcceptClientsLoop()
        {
            while (serverRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => ServeClient(client))
                    {
                        IsBackground = true,
                        Name = "TiltDrive Vuforia MJPEG Client"
                    };
                    clientThread.Start();
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }

        private void ServeClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    client.NoDelay = true;
                    client.ReceiveTimeout = 5000;
                    NetworkStream stream = client.GetStream();
                    string path = ReadRequestPath(stream);

                    if (path == snapshotPath)
                    {
                        ServeSnapshot(stream);
                        return;
                    }

                    if (path == streamPath)
                    {
                        ServeMjpeg(stream);
                        return;
                    }

                    ServeIndex(stream);
                }
                catch (Exception)
                {
                }
            }
        }

        private static string ReadRequestPath(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int count = stream.Read(buffer, 0, buffer.Length);
            if (count <= 0)
            {
                return "/";
            }

            string request = Encoding.ASCII.GetString(buffer, 0, count);
            string[] parts = request.Split(' ');
            if (parts.Length < 2)
            {
                return "/";
            }

            string path = parts[1];
            int queryIndex = path.IndexOf('?');
            return queryIndex >= 0 ? path.Substring(0, queryIndex) : path;
        }

        private void ServeIndex(NetworkStream stream)
        {
            string body =
                "TiltDrive Vuforia MJPEG Broadcaster\n\n" +
                $"Stream: {streamPath}\n" +
                $"Snapshot: {snapshotPath}\n";

            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            WriteAscii(stream,
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/plain; charset=utf-8\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Cache-Control: no-cache\r\n" +
                "Connection: close\r\n\r\n");
            stream.Write(bodyBytes, 0, bodyBytes.Length);
        }

        private void ServeSnapshot(NetworkStream stream)
        {
            byte[] frame = GetLatestFrameCopy();
            if (frame == null)
            {
                WriteAscii(stream,
                    "HTTP/1.1 503 Service Unavailable\r\n" +
                    "Content-Type: text/plain\r\n" +
                    "Connection: close\r\n\r\n" +
                    "No frame available yet.");
                return;
            }

            WriteAscii(stream,
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: image/jpeg\r\n" +
                $"Content-Length: {frame.Length}\r\n" +
                "Cache-Control: no-cache\r\n" +
                "Connection: close\r\n\r\n");
            stream.Write(frame, 0, frame.Length);
        }

        private void ServeMjpeg(NetworkStream stream)
        {
            WriteAscii(stream,
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: multipart/x-mixed-replace; boundary=frame\r\n" +
                "Cache-Control: no-cache\r\n" +
                "Pragma: no-cache\r\n" +
                "Connection: close\r\n\r\n");

            int lastVersion = -1;
            int sleepMs = Math.Max(5, (int)Math.Round(1000f / Math.Max(1, framesPerSecond)));

            while (serverRunning)
            {
                byte[] frame = GetLatestFrameCopyIfNew(ref lastVersion);
                if (frame != null)
                {
                    WriteAscii(stream,
                        "--frame\r\n" +
                        "Content-Type: image/jpeg\r\n" +
                        $"Content-Length: {frame.Length}\r\n\r\n");
                    stream.Write(frame, 0, frame.Length);
                    WriteAscii(stream, "\r\n");
                    stream.Flush();
                }

                Thread.Sleep(sleepMs);
            }
        }

        private byte[] GetLatestFrameCopy()
        {
            lock (frameLock)
            {
                return latestFrameBytes;
            }
        }

        private byte[] GetLatestFrameCopyIfNew(ref int lastVersion)
        {
            lock (frameLock)
            {
                if (latestFrameBytes == null || latestFrameVersion == lastVersion)
                {
                    return null;
                }

                lastVersion = latestFrameVersion;
                return latestFrameBytes;
            }
        }

        private static void WriteAscii(NetworkStream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string NormalizePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "/";
            }

            return value.StartsWith("/") ? value : "/" + value;
        }

        private void LogStatus(string message)
        {
            if (logServerStatus)
            {
                Debug.Log($"[TiltDrive][VuforiaMjpegBroadcaster] {message}");
            }
        }
    }
}
