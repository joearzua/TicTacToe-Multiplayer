using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles Photon Fusion connection
/// Starts game session and manages NetworkRunner
/// </summary>
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Fusion")] [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkObject gameManagerPrefab;

    private NetworkRunner runner;

    public async void StartGameAfterLogin()
    {
        await StartGame();
    }

    /// <summary>
    /// Start Fusion game session
    /// </summary>
    private async Task StartGame()
    {
        // Create runner if needed
        if (runner == null)
        {
            runner = Instantiate(runnerPrefab);
            runner.name = "NetworkRunner";
            runner.AddCallbacks(this);
        }

        Debug.Log("Starting Fusion session...");

        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        NetworkSceneInfo info = new NetworkSceneInfo();
        info.AddSceneRef(sceneRef, LoadSceneMode.Single);

        // Start game session
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared, // Shared mode = one client is host (server)
            SessionName = "TicTacToe", // Room name
            Scene = info,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log("✓ Connected to Photon Fusion");
            Debug.Log($"🔍 runner.IsServer = {runner.IsServer}");
            Debug.Log($"🔍 runner.IsClient = {runner.IsClient}");
            Debug.Log($"🔍 runner.IsSharedModeMasterClient = {runner.IsSharedModeMasterClient}");
            Debug.Log($"🔍 gameManagerPrefab assigned = {gameManagerPrefab != null}");

            // Spawn GameManager (only host in Shared mode)
            // if (runner.IsSharedModeMasterClient)
            // {
            //     Debug.Log("✅ I am the HOST - Spawning GameManager...");
            //     var spawnedGM = runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
            //     Debug.Log($"✅ GameManager spawned: {spawnedGM}");
            // }
            // else
            // {
            //     Debug.Log("⚠️ I am a CLIENT (not host) - waiting for host to spawn GameManager");
            // }
        }
    }

    #region INetworkRunnerCallbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"✓ Player joined: {player}");
        Debug.Log($"   runner.LocalPlayer: {runner.LocalPlayer}");
        Debug.Log($"   Is this me? {player == runner.LocalPlayer}");

        // If this is the local player, send their backend info
        if (player == runner.LocalPlayer)
        {
            Debug.Log("   This IS the local player - registering with backend info");

            // GameManager might not be spawned yet, so retry
            StartCoroutine(RegisterPlayerWhenReady(runner, player));
        }
        else
        {
            Debug.Log("   This is NOT the local player - another client joined");
        }
    }

    private System.Collections.IEnumerator RegisterPlayerWhenReady(NetworkRunner runner, PlayerRef player)
    {
        // Wait for GameManager to spawn (max 5 seconds)
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            var gameManager = FindObjectOfType<GameManager>();

            if (gameManager != null && gameManager.Object != null && gameManager.Object.IsValid)
            {
                Debug.Log("   ✅ GameManager found, registering player");

                if (APIManager.Instance != null && APIManager.Instance.IsLoggedIn)
                {
                    Debug.Log(
                        $"   Calling RPC_RegisterPlayerWithInfo: {APIManager.Instance.CurrentUsername}, ELO: {APIManager.Instance.CurrentEloRating}");

                    gameManager.RPC_RegisterPlayerWithInfo(
                        APIManager.Instance.CurrentUsername,
                        APIManager.Instance.CurrentEloRating,
                        player
                    );
                }
                else
                {
                    Debug.LogError("   ❌ APIManager not logged in!");
                }

                yield break; // Success, stop retrying
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogError("   ❌ GameManager not found after 5 seconds timeout!");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"❌ Player left: {player}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("✓ Connected to server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"❌ Disconnected: {reason}");
    }

    // Required empty implementations
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    #endregion
}