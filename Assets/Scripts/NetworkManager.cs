using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkObject gameManagerPrefab;
    [SerializeField] private int maxPlayersPerRoom = 2;

    private NetworkRunner runner;
    private bool hasRegisteredPlayer = false;
    private LoginUI loginUI;
    private bool isSearchingForMatch = false;
    private List<SessionInfo> availableSessions = new List<SessionInfo>();

    public async void StartMatchmaking()
    {
        Debug.Log("🔍 Starting matchmaking...");
        hasRegisteredPlayer = false;
        isSearchingForMatch = true;

        loginUI = FindObjectOfType<LoginUI>();
        if (loginUI != null) loginUI.ShowSearchingStatus("Finding a match...");

        // Create runner for session browsing
        runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner";
        runner.AddCallbacks(this);

        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        NetworkSceneInfo info = new();
        info.AddSceneRef(sceneRef);

        try
        {
            var sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            // Start in Shared mode and join lobby to see available sessions
            var result = await runner.JoinSessionLobby(SessionLobby.Shared);

            if (!result.Ok)
            {
                Debug.LogError($"❌ Failed to join lobby: {result.ShutdownReason}");
                if (loginUI != null) loginUI.ShowSearchingStatus("Connection failed. Please restart.");
                return;
            }

            Debug.Log("✅ Joined lobby, searching for available matches...");

            // Wait a moment for session list to populate
            await System.Threading.Tasks.Task.Delay(1000);

            // Try to find and join an available session
            await TryJoinOrCreateSession(sceneManager, info);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error: {e.Message}");
            if (loginUI != null) loginUI.ShowSearchingStatus("Connection failed. Please restart.");
        }
    }

    private async System.Threading.Tasks.Task TryJoinOrCreateSession(NetworkSceneManagerDefault sceneManager,
        NetworkSceneInfo info)
    {
        // Look for a session with space
        SessionInfo targetSession = null;

        foreach (var session in availableSessions)
        {
            Debug.Log($"📋 Found session: {session.Name} ({session.PlayerCount}/{session.MaxPlayers})");

            if (session.PlayerCount < session.MaxPlayers && session.IsOpen)
            {
                targetSession = session;
                break;
            }
        }

        StartGameResult result;

        if (targetSession != null)
        {
            // Join existing session
            Debug.Log($"🎯 Joining existing match: {targetSession.Name}");
            if (loginUI != null) loginUI.ShowSearchingStatus($"Match found! Joining...");

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
            // Create new session with unique name
            string sessionName = $"Match_{Guid.NewGuid().ToString().Substring(0, 8)}";
            Debug.Log($"🆕 No available matches, creating: {sessionName}");
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
            Debug.Log("✅ Connected to match!");
            if (loginUI != null) loginUI.ShowGame();
        }
        else
        {
            Debug.LogError($"❌ Failed to connect: {result.ShutdownReason}");
            if (loginUI != null) loginUI.ShowSearchingStatus("Connection failed. Please restart.");
        }
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"📋 Session list updated: {sessionList.Count} sessions found");
        availableSessions = sessionList;

        foreach (var session in sessionList)
        {
            Debug.Log($"   - {session.Name}: {session.PlayerCount}/{session.MaxPlayers} (Open: {session.IsOpen})");
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log($"✓ Connected! Master: {runner.IsSharedModeMasterClient}, LocalPlayer: {runner.LocalPlayer}");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"✓ Scene loaded! Master: {runner.IsSharedModeMasterClient}");
        TrySpawnGameManager();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("👑 Host migration - I am now master client!");
        StartCoroutine(DelayedHostMigrationCheck());
    }

    private IEnumerator DelayedHostMigrationCheck()
    {
        yield return null;
        yield return null;

        TrySpawnGameManager();
    }

    private void TrySpawnGameManager()
    {
        if (!runner.IsSharedModeMasterClient)
        {
            Debug.Log("   Not master client, skipping spawn");
            return;
        }

        var existing = FindObjectOfType<GameManager>();
        if (existing != null && existing.Object != null && existing.Object.IsValid)
        {
            Debug.Log("✅ GameManager already exists");
            return;
        }

        Debug.Log("✅ I AM HOST - SPAWNING GAMEMANAGER");

        if (gameManagerPrefab != null)
        {
            runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            Debug.LogError("❌ gameManagerPrefab is NULL!");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"✓ Player joined: {player} (Me: {runner.LocalPlayer})");

        if (player == runner.LocalPlayer && !hasRegisteredPlayer)
        {
            StartCoroutine(RegisterPlayer(runner, player));
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👋 Player left: {player}");

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
            Debug.Log("🔄 GameManager was destroyed with previous host, respawning...");
            TrySpawnGameManager();

            yield return new WaitForSeconds(0.5f);
            hasRegisteredPlayer = false;
            StartCoroutine(RegisterPlayer(runner, runner.LocalPlayer));
        }
    }

    private IEnumerator RegisterPlayer(NetworkRunner runner, PlayerRef player)
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 100; i++)
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm != null && gm.Object != null && gm.Object.IsValid)
            {
                Debug.Log($"✅ Registering {APIManager.Instance.CurrentUsername}");
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

        Debug.LogError("❌ GameManager never appeared!");
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