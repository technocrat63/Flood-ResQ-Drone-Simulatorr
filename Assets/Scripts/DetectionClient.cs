using UnityEngine;
using System;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Collections.Concurrent;

public class DetectionClient : MonoBehaviour
{
    [Header("Drone")]
    public int droneId = 1;

    [Header("Camera")]
    public Camera droneCamera;

    [Header("Detection")]
    public float detectionInterval = 1.0f;
    public GameObject detectionMarkerPrefab;

    [Header("Victim Tracking")]
    public VictimTrackingManager victimTrackingManager;

    [Header("Network")]
    public string serverAddress = "127.0.0.1";
    public int serverPort = 65432;

    [Header("Mission Control")]
    [Tooltip("Detection starts only after the voice mission command.")]
    public bool detectionEnabled = false;

    private TcpClient client;
    private NetworkStream stream;

    private float timer = 0f;
    private bool requestRunning = false;
    private bool shuttingDown = false;

    private const int texSize = 640;

    // Detection results are received by a worker thread
    // and processed safely on Unity's main thread.
    private readonly ConcurrentQueue<string> responseQueue =
        new ConcurrentQueue<string>();

    public bool IsDetectionActive
    {
        get { return detectionEnabled; }
    }

    private void Start()
    {
        ConnectToServer();
    }

    // =========================================================
    // CONNECT
    // =========================================================

    private void ConnectToServer()
    {
        try
        {
            client = new TcpClient();

            client.Connect(
                serverAddress,
                serverPort
            );

            stream = client.GetStream();

            Debug.Log(
                $"Drone {droneId}: Connected to AI detection server. " +
                "Waiting for mission command."
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Drone {droneId}: Could not connect to Python server: " +
                e.Message
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // Process responses that arrived from worker threads.
        // This happens on Unity's main thread.
        // -----------------------------------------------------

        while (responseQueue.TryDequeue(out string response))
        {
            ProcessDetections(response);
        }

        // -----------------------------------------------------
        // Detection disabled
        // -----------------------------------------------------

        if (!detectionEnabled)
        {
            return;
        }

        // -----------------------------------------------------
        // Connection unavailable
        // -----------------------------------------------------

        if (
            client == null ||
            stream == null ||
            !client.Connected
        )
        {
            return;
        }

        // -----------------------------------------------------
        // Don't start another request while one is running.
        // -----------------------------------------------------

        if (requestRunning)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= detectionInterval)
        {
            timer = 0f;

            CaptureAndSendAsync();
        }
    }

    // =========================================================
    // MISSION CONTROL
    // =========================================================

    public void StartDetection()
    {
        detectionEnabled = true;
        timer = detectionInterval;

        Debug.Log(
            $"Drone {droneId}: Detection ACTIVATED."
        );
    }

    public void StopDetection()
    {
        detectionEnabled = false;
        timer = 0f;

        Debug.Log(
            $"Drone {droneId}: Detection STOPPED."
        );
    }

    // =========================================================
    // CAPTURE + SEND
    // =========================================================

    private void CaptureAndSendAsync()
    {
        if (droneCamera == null)
        {
            Debug.LogError(
                $"Drone {droneId}: Drone camera is not assigned."
            );

            return;
        }

        if (requestRunning)
        {
            return;
        }

        requestRunning = true;

        try
        {
            // -------------------------------------------------
            // IMPORTANT:
            // Camera rendering MUST happen on Unity main thread.
            // -------------------------------------------------

            RenderTexture rt =
                new RenderTexture(
                    texSize,
                    texSize,
                    24
                );

            droneCamera.targetTexture = rt;
            RenderTexture.active = rt;

            droneCamera.Render();

            Texture2D tex =
                new Texture2D(
                    texSize,
                    texSize,
                    TextureFormat.RGB24,
                    false
                );

            tex.ReadPixels(
                new Rect(
                    0,
                    0,
                    texSize,
                    texSize
                ),
                0,
                0
            );

            tex.Apply();

            droneCamera.targetTexture = null;
            RenderTexture.active = null;

            Destroy(rt);

            byte[] imageBytes =
                tex.EncodeToJPG(75);

            Destroy(tex);

            // -------------------------------------------------
            // Network work happens on background thread.
            // -------------------------------------------------

            ThreadPool.QueueUserWorkItem(
                delegate
                {
                    SendImageToServer(imageBytes);
                }
            );
        }
        catch (Exception e)
        {
            requestRunning = false;

            Debug.LogError(
                $"Drone {droneId}: Capture error: " +
                e.Message
            );
        }
    }

    // =========================================================
    // NETWORK THREAD
    // =========================================================

    private void SendImageToServer(
        byte[] imageBytes)
    {
        try
        {
            if (
                stream == null ||
                client == null ||
                !client.Connected ||
                shuttingDown
            )
            {
                requestRunning = false;
                return;
            }

            // -------------------------------------------------
            // Send image size
            // -------------------------------------------------

            byte[] sizeBytes =
                BitConverter.GetBytes(
                    imageBytes.Length
                );

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sizeBytes);
            }

            stream.Write(
                sizeBytes,
                0,
                4
            );

            // -------------------------------------------------
            // Send image
            // -------------------------------------------------

            stream.Write(
                imageBytes,
                0,
                imageBytes.Length
            );

            // -------------------------------------------------
            // Receive response size
            // -------------------------------------------------

            byte[] responseSizeBytes =
                new byte[4];

            ReadExact(
                responseSizeBytes,
                4
            );

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(responseSizeBytes);
            }

            int responseSize =
                BitConverter.ToInt32(
                    responseSizeBytes,
                    0
                );

            if (
                responseSize <= 0 ||
                responseSize > 10_000_000
            )
            {
                throw new IOException(
                    "Invalid AI response size: " +
                    responseSize
                );
            }

            // -------------------------------------------------
            // Receive response
            // -------------------------------------------------

            byte[] responseBytes =
                new byte[responseSize];

            ReadExact(
                responseBytes,
                responseSize
            );

            string response =
                Encoding.UTF8.GetString(
                    responseBytes
                );

            // -------------------------------------------------
            // Send result back to Unity main thread.
            // -------------------------------------------------

            responseQueue.Enqueue(response);
        }
        catch (Exception e)
        {
            if (!shuttingDown)
            {
                Debug.LogError(
                    $"Drone {droneId}: Detection network error: " +
                    e.Message
                );
            }
        }
        finally
        {
            requestRunning = false;
        }
    }

    // =========================================================
    // READ EXACT
    // =========================================================

    private void ReadExact(
        byte[] buffer,
        int size)
    {
        int totalRead = 0;

        while (totalRead < size)
        {
            int bytesRead =
                stream.Read(
                    buffer,
                    totalRead,
                    size - totalRead
                );

            if (bytesRead <= 0)
            {
                throw new IOException(
                    "Connection closed while receiving data."
                );
            }

            totalRead += bytesRead;
        }
    }

    // =========================================================
    // PROCESS YOLO DETECTIONS
    // =========================================================

    private void ProcessDetections(
        string response)
    {
        if (
            string.IsNullOrWhiteSpace(response) ||
            response.Trim().ToLowerInvariant() == "none"
        )
        {
            return;
        }

        string[] entries =
            response.Split(';');

        foreach (string entry in entries)
        {
            string[] parts =
                entry.Split(',');

            if (parts.Length != 3)
            {
                continue;
            }

            if (
                !float.TryParse(
                    parts[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float cx
                )
            )
            {
                continue;
            }

            if (
                !float.TryParse(
                    parts[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float cy
                )
            )
            {
                continue;
            }

            if (
                !float.TryParse(
                    parts[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float conf
                )
            )
            {
                continue;
            }

            // -------------------------------------------------
            // Validate coordinates
            // -------------------------------------------------

            if (
                cx < 0f || cx > 1f ||
                cy < 0f || cy > 1f
            )
            {
                continue;
            }

            if (droneCamera == null)
            {
                continue;
            }

            // -------------------------------------------------
            // Convert YOLO coordinates to Unity viewport.
            // -------------------------------------------------

            Ray ray =
                droneCamera.ViewportPointToRay(
                    new Vector3(
                        cx,
                        1f - cy,
                        0f
                    )
                );

            if (
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    100f
                )
            )
            {
                Debug.Log(
                    $"Drone {droneId}: Detected person at " +
                    $"{hit.point}, confidence {conf:F2}"
                );

                VictimTrackingManager manager =
                    victimTrackingManager;

                if (manager == null)
                {
                    manager =
                        FindFirstObjectByType<
                            VictimTrackingManager
                        >();
                }

                if (manager != null)
                {
                    manager.ProcessDetection(
                        hit.point,
                        conf
                    );
                }
            }
        }
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        shuttingDown = true;
        detectionEnabled = false;

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch
        {
        }
    }

    private void OnApplicationQuit()
    {
        shuttingDown = true;
        detectionEnabled = false;

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch
        {
        }
    }
}