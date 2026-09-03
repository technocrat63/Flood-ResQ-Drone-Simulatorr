using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceCommandController : MonoBehaviour
{
    [Header("Main Systems")]
    public AIRescueController aiRescueController;

    [Header("Drone Fleet")]
    public DroneMovement[] drones;

    [Header("Detection Clients")]
    public DetectionClient[] detectionClients;

    [Header("Voice Settings")]
    public bool startVoiceListening = true;

    [Tooltip("Use Low while testing voice recognition. Change to Medium later if required.")]
    public ConfidenceLevel recognitionConfidence = ConfidenceLevel.Low;

    private KeywordRecognizer keywordRecognizer;

    private readonly Dictionary<string, Action> commands =
        new Dictionary<string, Action>();

    private bool voiceListening = false;
    private bool missionActive = false;

    public bool VoiceListening
    {
        get { return voiceListening; }
    }

    public bool MissionActive
    {
        get { return missionActive; }
    }


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        Debug.Log("========================================");
        Debug.Log("VOICE SYSTEM: VoiceCommandController AWAKE");
        Debug.Log("========================================");
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Debug.Log("VOICE SYSTEM: STARTING...");

        // IMPORTANT DIAGNOSTICS
        CheckSpeechSystem();
        CheckMicrophones();

        FindSystems();

        missionActive = false;

        SetupCommands();

        // Detection should be OFF initially.
        StopAllDetectionClients();

        if (aiRescueController != null)
        {
            aiRescueController.DisableAutomaticRescue();
        }

        Debug.Log(
            "VOICE SYSTEM: Drones found = " +
            (drones != null ? drones.Length : 0)
        );

        Debug.Log(
            "VOICE SYSTEM: Detection clients found = " +
            (detectionClients != null ? detectionClients.Length : 0)
        );

        Debug.Log("VOICE SYSTEM: Mission = STANDBY");

        if (startVoiceListening)
        {
            ActivateVoiceRecognition();
        }
        else
        {
            Debug.LogWarning(
                "VOICE SYSTEM: startVoiceListening is FALSE."
            );
        }
    }


    // =========================================================
    // SPEECH SYSTEM DIAGNOSTICS
    // =========================================================

    private void CheckSpeechSystem()
    {
        Debug.Log("========================================");
        Debug.Log("VOICE SYSTEM: SPEECH DIAGNOSTICS");

        try
        {
            Debug.Log(
                "PhraseRecognitionSystem Status = " +
                PhraseRecognitionSystem.Status
            );

            Debug.Log(
                "PhraseRecognitionSystem Supported = " +
                PhraseRecognitionSystem.isSupported
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "VOICE SYSTEM: Could not read speech system status."
            );

            Debug.LogError(e.ToString());
        }

        Debug.Log("========================================");
    }


    // =========================================================
    // MICROPHONE DIAGNOSTICS
    // =========================================================

    private void CheckMicrophones()
    {
        Debug.Log("========================================");
        Debug.Log("VOICE SYSTEM: MICROPHONE DIAGNOSTICS");

        try
        {
            string[] microphones = Microphone.devices;

            if (microphones == null || microphones.Length == 0)
            {
                Debug.LogWarning(
                    "VOICE SYSTEM: NO MICROPHONES DETECTED BY UNITY."
                );
            }
            else
            {
                Debug.Log(
                    "VOICE SYSTEM: Microphones detected = " +
                    microphones.Length
                );

                foreach (string microphone in microphones)
                {
                    Debug.Log(
                        "VOICE SYSTEM: MIC = " +
                        microphone
                    );
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "VOICE SYSTEM: Microphone check failed."
            );

            Debug.LogError(e.ToString());
        }

        Debug.Log("========================================");
    }


    // =========================================================
    // FIND SYSTEMS
    // =========================================================

    private void FindSystems()
    {
        if (aiRescueController == null)
        {
            aiRescueController =
                FindFirstObjectByType<AIRescueController>();
        }

        if (drones == null || drones.Length == 0)
        {
            drones =
                FindObjectsByType<DroneMovement>(
                    FindObjectsSortMode.None
                );
        }

        if (detectionClients == null ||
            detectionClients.Length == 0)
        {
            detectionClients =
                FindObjectsByType<DetectionClient>(
                    FindObjectsSortMode.None
                );
        }
    }


    // =========================================================
    // COMMAND SETUP
    // =========================================================

    private void SetupCommands()
    {
        commands.Clear();

        // =====================================================
        // SIMPLE TEST COMMANDS
        // Use these FIRST to test recognition.
        // =====================================================

        commands.Add(
            "start",
            StartAutonomousRescue
        );

        commands.Add(
            "stop",
            StopMission
        );

        commands.Add(
            "deploy",
            StartMission
        );

        commands.Add(
            "patrol",
            ReturnAllToPatrol
        );


        // =====================================================
        // FULL SIH COMMANDS
        // =====================================================

        commands.Add(
            "start autonomous scan",
            StartAutonomousRescue
        );

        commands.Add(
            "start autonomous rescue",
            StartAutonomousRescue
        );

        commands.Add(
            "start rescue scan",
            StartMission
        );

        commands.Add(
            "deploy drones",
            StartMission
        );

        commands.Add(
            "stop autonomous rescue",
            StopMission
        );

        commands.Add(
            "stop autonomous scan",
            StopMission
        );

        commands.Add(
            "stop drones",
            StopMission
        );

        commands.Add(
            "recall drones",
            StopMission
        );

        commands.Add(
            "return to patrol",
            ReturnAllToPatrol
        );


        // =====================================================
        // DEBUG OUTPUT
        // =====================================================

        Debug.Log("========================================");
        Debug.Log(
            "VOICE SYSTEM: Commands loaded = " +
            commands.Count
        );

        foreach (string command in commands.Keys)
        {
            Debug.Log(
                "VOICE COMMAND: \"" +
                command +
                "\""
            );
        }

        Debug.Log("========================================");
    }


    // =========================================================
    // ACTIVATE VOICE
    // =========================================================

    public void ActivateVoiceRecognition()
    {
        Debug.Log("========================================");
        Debug.Log("VOICE SYSTEM: Attempting to activate...");
        Debug.Log("========================================");

        if (voiceListening)
        {
            Debug.Log(
                "VOICE SYSTEM: Already listening."
            );

            return;
        }


        // -----------------------------------------------------
        // Check Windows speech recognition support
        // -----------------------------------------------------

        try
        {
            if (!PhraseRecognitionSystem.isSupported)
            {
                Debug.LogError(
                    "VOICE SYSTEM: Windows speech recognition " +
                    "is NOT supported on this system."
                );

                return;
            }

            Debug.Log(
                "VOICE SYSTEM: Windows speech recognition is supported."
            );

            Debug.Log(
                "VOICE SYSTEM: Current speech status = " +
                PhraseRecognitionSystem.Status
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "VOICE SYSTEM: Speech support check failed."
            );

            Debug.LogError(e.ToString());

            return;
        }


        // -----------------------------------------------------
        // Create recognizer
        // -----------------------------------------------------

        try
        {
            CreateKeywordRecognizer();

            if (keywordRecognizer == null)
            {
                Debug.LogError(
                    "VOICE SYSTEM: KeywordRecognizer could not be created."
                );

                return;
            }


            // -------------------------------------------------
            // Start recognizer
            // -------------------------------------------------

            keywordRecognizer.Start();

            voiceListening = true;

            Debug.Log("========================================");
            Debug.Log("VOICE SYSTEM: LISTENING ACTIVE");
            Debug.Log(
                "VOICE SYSTEM: Confidence = " +
                recognitionConfidence
            );

            Debug.Log("VOICE SYSTEM: TEST COMMANDS:");
            Debug.Log("\"START\"");
            Debug.Log("\"STOP\"");
            Debug.Log("\"DEPLOY\"");
            Debug.Log("\"PATROL\"");

            Debug.Log("========================================");
        }
        catch (Exception e)
        {
            voiceListening = false;

            Debug.LogError(
                "VOICE SYSTEM: FAILED TO START."
            );

            Debug.LogError(
                "VOICE SYSTEM: Exception = " +
                e.Message
            );

            Debug.LogError(e.ToString());
        }
    }


    // =========================================================
    // DEACTIVATE
    // =========================================================

    public void DeactivateVoiceRecognition()
    {
        try
        {
            if (keywordRecognizer != null)
            {
                if (keywordRecognizer.IsRunning)
                {
                    keywordRecognizer.Stop();
                }

                keywordRecognizer.Dispose();

                keywordRecognizer = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "VOICE SYSTEM: Error stopping recognizer: " +
                e.Message
            );
        }

        voiceListening = false;

        Debug.Log(
            "VOICE SYSTEM: LISTENING STOPPED."
        );
    }


    // =========================================================
    // CREATE RECOGNIZER
    // =========================================================

    private void CreateKeywordRecognizer()
    {
        // -----------------------------------------------------
        // Destroy previous recognizer
        // -----------------------------------------------------

        if (keywordRecognizer != null)
        {
            try
            {
                if (keywordRecognizer.IsRunning)
                {
                    keywordRecognizer.Stop();
                }

                keywordRecognizer.Dispose();
            }
            catch
            {
                // Ignore cleanup errors.
            }

            keywordRecognizer = null;
        }


        // -----------------------------------------------------
        // Check commands
        // -----------------------------------------------------

        if (commands.Count == 0)
        {
            Debug.LogError(
                "VOICE SYSTEM: No commands registered."
            );

            return;
        }


        string[] phrases =
            new List<string>(
                commands.Keys
            ).ToArray();


        Debug.Log(
            "VOICE SYSTEM: Creating recognizer with " +
            phrases.Length +
            " phrases."
        );


        // -----------------------------------------------------
        // Create KeywordRecognizer
        // -----------------------------------------------------

        keywordRecognizer =
            new KeywordRecognizer(
                phrases,
                recognitionConfidence
            );


        // -----------------------------------------------------
        // Event
        // -----------------------------------------------------

        keywordRecognizer.OnPhraseRecognized +=
            OnPhraseRecognized;


        Debug.Log(
            "VOICE SYSTEM: KeywordRecognizer created successfully."
        );
    }


    // =========================================================
    // PHRASE RECEIVED
    // =========================================================

    private void OnPhraseRecognized(
        PhraseRecognizedEventArgs args)
    {
        string command =
            args.text.ToLower().Trim();


        Debug.Log("========================================");
        Debug.Log("VOICE SYSTEM: PHRASE EVENT FIRED");
        Debug.Log(
            "VOICE COMMAND RECEIVED: " +
            command
        );
        Debug.Log(
            "VOICE CONFIDENCE LEVEL: " +
            args.confidence
        );
        Debug.Log("========================================");


        // -----------------------------------------------------
        // Execute command
        // -----------------------------------------------------

        if (commands.TryGetValue(
            command,
            out Action action))
        {
            Debug.Log(
                "VOICE SYSTEM: COMMAND MATCHED."
            );

            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "VOICE SYSTEM: Command execution failed."
                );

                Debug.LogError(e.ToString());
            }
        }
        else
        {
            Debug.LogWarning(
                "VOICE SYSTEM: Command not found: " +
                command
            );
        }


        // -----------------------------------------------------
        // Keep listening
        // -----------------------------------------------------

        if (keywordRecognizer != null &&
            !keywordRecognizer.IsRunning)
        {
            Debug.Log(
                "VOICE SYSTEM: Recognizer stopped after phrase."
            );

            try
            {
                keywordRecognizer.Start();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "VOICE SYSTEM: Could not restart recognizer."
                );

                Debug.LogError(e.ToString());

                voiceListening = false;

                return;
            }
        }

        voiceListening = true;
    }


    // =========================================================
    // START NORMAL MISSION
    // =========================================================

    private void StartMission()
    {
        Debug.Log(
            "VOICE SYSTEM: START MISSION"
        );


        if (missionActive)
        {
            Debug.Log(
                "VOICE SYSTEM: Mission already active."
            );

            return;
        }


        missionActive = true;

        DeployAllDrones();

        StartAllDetectionClients();

        Debug.Log(
            "VOICE SYSTEM: RESCUE MISSION STARTED."
        );
    }


    // =========================================================
    // START AUTONOMOUS RESCUE
    // =========================================================

    private void StartAutonomousRescue()
    {
        Debug.Log(
            "VOICE SYSTEM: START AUTONOMOUS SCAN COMMAND."
        );


        missionActive = true;


        DeployAllDrones();

        StartAllDetectionClients();


        if (aiRescueController != null)
        {
            aiRescueController.EnableAutomaticRescue();

            Debug.Log(
                "VOICE SYSTEM: AUTONOMOUS RESCUE ENABLED."
            );
        }
        else
        {
            Debug.LogError(
                "VOICE SYSTEM: AIRescueController NOT FOUND."
            );
        }


        Debug.Log(
            "VOICE SYSTEM: SCANNING STARTED."
        );
    }


    // =========================================================
    // STOP
    // =========================================================

    private void StopMission()
    {
        Debug.Log(
            "VOICE SYSTEM: STOP COMMAND RECEIVED."
        );


        missionActive = false;


        if (aiRescueController != null)
        {
            aiRescueController.DisableAutomaticRescue();
        }


        StopAllDetectionClients();

        StopAllDrones();


        Debug.Log(
            "VOICE SYSTEM: MISSION STOPPED."
        );

        Debug.Log(
            "VOICE SYSTEM: VOICE LISTENING REMAINS ACTIVE."
        );
    }


    // =========================================================
    // RETURN PATROL
    // =========================================================

    private void ReturnAllToPatrol()
    {
        Debug.Log(
            "VOICE SYSTEM: RETURN TO PATROL."
        );


        missionActive = false;


        if (aiRescueController != null)
        {
            aiRescueController.DisableAutomaticRescue();
        }


        StopAllDetectionClients();


        if (drones != null)
        {
            foreach (DroneMovement drone in drones)
            {
                if (drone != null)
                {
                    drone.ReturnToPatrol();
                }
            }
        }


        Debug.Log(
            "VOICE SYSTEM: ALL DRONES RETURNED TO PATROL."
        );
    }


    // =========================================================
    // DEPLOY DRONES
    // =========================================================

    private void DeployAllDrones()
    {
        if (drones == null ||
            drones.Length == 0)
        {
            Debug.LogError(
                "VOICE SYSTEM: No drones found."
            );

            return;
        }


        foreach (DroneMovement drone in drones)
        {
            if (drone != null)
            {
                drone.Deploy();
            }
        }


        Debug.Log(
            "VOICE SYSTEM: ALL DRONES DEPLOYED."
        );
    }


    // =========================================================
    // STOP DRONES
    // =========================================================

    private void StopAllDrones()
    {
        if (drones == null)
        {
            return;
        }


        foreach (DroneMovement drone in drones)
        {
            if (drone != null)
            {
                drone.StopDrone();
            }
        }
    }


    // =========================================================
    // START DETECTION
    // =========================================================

    private void StartAllDetectionClients()
    {
        if (detectionClients == null ||
            detectionClients.Length == 0)
        {
            Debug.LogError(
                "VOICE SYSTEM: No DetectionClients found."
            );

            return;
        }


        foreach (
            DetectionClient detectionClient
            in detectionClients)
        {
            if (detectionClient != null)
            {
                detectionClient.StartDetection();
            }
        }


        Debug.Log(
            "VOICE SYSTEM: AI DETECTION ACTIVATED."
        );
    }


    // =========================================================
    // STOP DETECTION
    // =========================================================

    private void StopAllDetectionClients()
    {
        if (detectionClients == null)
        {
            return;
        }


        foreach (
            DetectionClient detectionClient
            in detectionClients)
        {
            if (detectionClient != null)
            {
                detectionClient.StopDetection();
            }
        }
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        try
        {
            if (keywordRecognizer != null)
            {
                if (keywordRecognizer.IsRunning)
                {
                    keywordRecognizer.Stop();
                }

                keywordRecognizer.Dispose();

                keywordRecognizer = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "VOICE SYSTEM: Cleanup error = " +
                e.Message
            );
        }

        voiceListening = false;
    }
}