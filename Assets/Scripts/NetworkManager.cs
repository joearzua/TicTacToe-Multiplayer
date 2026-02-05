using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages Photon Fusion networking and matchmaking.
/// Implements lobby-based matchmaking to automatically pair players.
/// Handles host migration and player registration.
/// </summary>
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkObject gameManagerPrefab;
    [SerializeField] private int maxPlayersPerRoom = 2;

    private NetworkRunner runner;
    private bool hasRegisteredPlayer;
    private LoginUI loginUI;
    private bool isSearchingForMatch;
    private List<SessionInfo> availableSessions = new List<SessionInfo>();

    /// <summary>
    /// Start matchmaking process - searches for available matches or creates new room
    /// </summary>
    public async void StartMatchmaking()
    {
        Debug.Log("Starting matchmaking...");
        hasRegisteredPlayer = false;
        isSearchingForMatch = true;

        loginUI = FindObjectOfType<LoginUI>();
        if (loginUI != null) loginUI.ShowSearchingStatus("Finding a match...");

        runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner";
        runner.AddCallbacks(this);

        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        NetworkSceneInfo info = new();
        info.AddSceneRef(sceneRef);

        try
        {
            var sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            var result = await runner.JoinSessionLobby(SessionLobby.Shared);

            if (!result.Ok)
            {
                Debug.LogError($"Failed to join lobby: {result.ShutdownReason}");
                if (loginUI != null) loginUI.ShowSearchingStatus("Connection failed. Please restart.");
                return;
            }

            Debug.Log("Joined lobby");
            await System.Threading.Tasks.Task.Delay(1000);
            await TryJoinOrCreateSession(sceneManager, info);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error: {e.Message}");
            if (loginUI != null) loginUI.ShowSearchingStatus("Connection failed. Please restart.");
        }
    }

    /// <summary>
    /// Attempt to join or create a session based on available rooms
    /// </summary>
    private async System.Threading.Tasks.Task TryJoinOrCreateSession(NetworkSceneManagerDefault sceneManager,
        NetworkSceneInfo info)
    {
        SessionInfo targetSession = null;

        foreach (var session in availableSessions)
        {
            if (session.PlayerCount < session.MaxPlayers && session.IsOpen)
            {
                targetSession = session;
                break;
            }
        }

        StartGameResult result;

        if (targetSession != null)
        {
            Debug.Log($"Joining match: {targetSession.Name}");
            if (loginUI != null) loginUI.ShowSearchingStatus("Match found! Joining...");

            result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = targetSession.Name,
                Scene = info,
                SceneManager = sceneManager,
                PlayerCount = maxPlayersPerRoom
            });
        }
        else
        {
            string sessionName = $"Match_{Guid.NewGuid().ToString().Substring(0, 8)}";
            Debug.Log($"Creating new match: {sessionName}");
            if (loginUI != null) loginUI.ShowSearchingStatus("Creating new match...");

            result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                Scene = info,
                SceneManager = sceneManager,
                PlayerCount = maxPlayersPerRoom
            });
        }

        isSearchingForMatch = false;

        if (result.Ok)
        {
            Debug.Log("Connected to match!");
            if (loginUI != null) loginUI.ShowGame();
        }
        else
        {
            Debug.LogError($"Failed to connect: {result.ShutdownReason}");
            if (loginUI != null) loginUI.ShowSearchingStatus("Connection failed. Please restart.");
        }
    }

    /// <summary>
    /// Called when session list is updated from Photon lobby
    /// </summary>
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        availableSessions = sessionList;
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log($"Connected to server");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        TrySpawnGameManager();
    }

    /// <summary>
    /// Handle host migration when original host disconnects
    /// </summary>
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("Host migration - I am now master client!");
        StartCoroutine(DelayedHostMigrationCheck());
    }

    private IEnumerator DelayedHostMigrationCheck()
    {
        yield return null;
        yield return null;
        TrySpawnGameManager();
    }

    /// <summary>
    /// Spawn GameManager prefab when acting as host
    /// </summary>
    private void TrySpawnGameManager()
    {
        if (!runner.IsSharedModeMasterClient) return;

        var existing = FindObjectOfType<GameManager>();
        if (existing != null && existing.Object != null && existing.Object.IsValid)
        {
            return;
        }

        if (gameManagerPrefab != null)
        {
            runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("GameManager spawned");
        }
        else
        {
            Debug.LogError("gameManagerPrefab is NULL!");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer && !hasRegisteredPlayer)
        {
            StartCoroutine(RegisterPlayer(runner, player));
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsSharedModeMasterClient)
        {
            StartCoroutine(CheckGameManagerAfterPlayerLeft());
        }
    }

    private IEnumerator CheckGameManagerAfterPlayerLeft()
    {
        yield return new WaitForSeconds(0.5f);

        var gm = FindObjectOfType<GameManager>();
        if (gm == null || gm.Object == null || !gm.Object.IsValid)
        {
            TrySpawnGameManager();
            yield return new WaitForSeconds(0.5f);
            hasRegisteredPlayer = false;
            StartCoroutine(RegisterPlayer(runner, runner.LocalPlayer));
        }
    }

    /// <summary>
    /// Register player with backend info once GameManager is ready
    /// </summary>
    private IEnumerator RegisterPlayer(NetworkRunner runner, PlayerRef player)
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 100; i++)
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm != null && gm.Object != null && gm.Object.IsValid)
            {
                Debug.Log($"Registering {APIManager.Instance.CurrentUsername}");
                gm.RPC_RegisterPlayerWithInfo(
                    APIManager.Instance.CurrentUsername,
                    APIManager.Instance.CurrentEloRating,
                    APIManager.Instance.CurrentPlayerId,
                    player
                );
                hasRegisteredPlayer = true;
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogError("GameManager never appeared!");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
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

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
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
}