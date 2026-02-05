using Fusion;
using UnityEngine;

/// <summary>
/// Server-authoritative Tic-Tac-Toe game manager
/// Host validates all moves, clients just send requests
/// </summary>
public class GameManager : NetworkBehaviour
{
    [Networked] public NetworkString<_16> Player1Name { get; set; }
    [Networked] public NetworkString<_16> Player2Name { get; set; }
    [Networked] public int Player1Elo { get; set; }
    [Networked] public int Player2Elo { get; set; }
    [Header("Backend Integration")] private int localPlayerId = -1;

    [Networked, Capacity(9)] public NetworkArray<int> Board => default; // 0=empty, 1=player1, 2=player2

    [Networked] public int CurrentPlayer { get; set; } = 1;
    [Networked] public int Winner { get; set; } = 0;
    [Networked] public bool GameOver { get; set; } = false;

    [Networked] public PlayerRef Player1 { get; set; }
    [Networked] public PlayerRef Player2 { get; set; }

    public override void Spawned()
    {
        // Initialize game when this NetworkObject spawns
        if (Object.HasStateAuthority) // Only HOST runs this
        {
            InitializeGame();
        }
    }

    /// <summary>
    /// Initialize empty board (server only)
    /// </summary>
    private void InitializeGame()
    {
        for (int i = 0; i < 9; i++)
        {
            Board.Set(i, 0); // Empty cells
        }

        CurrentPlayer = 1;
        Winner = 0;
        GameOver = false;

        Debug.Log("🎮 Game initialized by host (server authority)");
    }

    /// <summary>
    /// Client requests to make a move
    /// RPC = Remote Procedure Call (client → host)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestMove(int position, PlayerRef player)
    {
        Debug.Log($"📥 Move request: Position {position} from {player}");

        // SERVER VALIDATES MOVE (anti-cheat)
        if (!ValidateMove(position, player))
        {
            Debug.LogWarning($"❌ Invalid move rejected: Position {position}");
            RPC_MoveRejected(player, "Invalid move");
            return;
        }

        // Server executes move
        ExecuteMove(position, player);
    }

    /// <summary>
    /// SERVER VALIDATES MOVE (prevents cheating)
    /// This is the key anti-cheat pattern
    /// </summary>
    private bool ValidateMove(int position, PlayerRef player)
    {
        // Check 1: Game not over
        if (GameOver)
        {
            Debug.Log("Game is over");
            return false;
        }

        // Check 2: Valid position (0-8)
        if (position < 0 || position > 8)
        {
            Debug.Log("Invalid position");
            return false;
        }

        // Check 3: Cell is empty
        if (Board[position] != 0)
        {
            Debug.Log("Cell occupied");
            return false;
        }

        // Check 4: Correct player's turn
        int playerNumber = GetPlayerNumber(player);
        if (playerNumber != CurrentPlayer)
        {
            Debug.Log($"Not player {playerNumber}'s turn (current: {CurrentPlayer})");
            return false;
        }

        // Check 5: Player is registered
        if (playerNumber == 0)
        {
            Debug.Log("Player not registered");
            return false;
        }

        return true; // All checks passed
    }

    /// <summary>
    /// SERVER EXECUTES VALIDATED MOVE
    /// Only host runs this - clients receive state updates
    /// </summary>
    private void ExecuteMove(int position, PlayerRef player)
    {
        int playerNumber = GetPlayerNumber(player);

        // Place mark on board
        Board.Set(position, playerNumber);

        Debug.Log($"✓ Move executed: Player {playerNumber} → Position {position}");

        // Check win condition
        if (CheckWin(playerNumber))
        {
            Winner = playerNumber;
            GameOver = true;
            Debug.Log($"🎉 PLAYER {playerNumber} WINS!");
            RPC_GameOver(Winner);
            return;
        }

        // Check draw
        if (IsBoardFull())
        {
            Winner = 0; // Draw
            GameOver = true;
            Debug.Log($"🤝 DRAW!");
            RPC_GameOver(0);
            return;
        }

        // Switch turn
        CurrentPlayer = (CurrentPlayer == 1) ? 2 : 1;
        Debug.Log($"🔄 Turn switched to Player {CurrentPlayer}");
    }

    /// <summary>
    /// Check if player won
    /// </summary>
    private bool CheckWin(int player)
    {
        // Win patterns (rows, columns, diagonals)
        int[,] patterns = new int[,]
        {
            { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, // Rows
            { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 }, // Columns
            { 0, 4, 8 }, { 2, 4, 6 } // Diagonals
        };

        for (int i = 0; i < 8; i++)
        {
            if (Board[patterns[i, 0]] == player &&
                Board[patterns[i, 1]] == player &&
                Board[patterns[i, 2]] == player)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if board is full (draw)
    /// </summary>
    private bool IsBoardFull()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Board[i] == 0) return false;
        }

        return true;
    }

    /// <summary>
    /// Reset game (server only)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestReset()
    {
        if (Object.HasStateAuthority)
        {
            InitializeGame();
            Debug.Log("🔄 Game reset by host");
        }
    }

    /// <summary>
    /// Get player number (1 or 2) from PlayerRef
    /// </summary>
    private int GetPlayerNumber(PlayerRef player)
    {
        if (player == Player1) return 1;
        if (player == Player2) return 2;
        return 0;
    }

    /// <summary>
    /// Register player with their backend info (name, ELO)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RegisterPlayerWithInfo(string username, int elo, PlayerRef player)
    {
        Debug.Log($"🎯 RPC_RegisterPlayerWithInfo called: {username}, ELO: {elo}, Player: {player}");
        Debug.Log($"   HasStateAuthority: {Object.HasStateAuthority}");
        Debug.Log($"   Current Player1: {Player1}, Player2: {Player2}");

        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("   ⚠️ Not state authority - ignoring");
            return;
        }

        if (Player1 == PlayerRef.None)
        {
            Player1 = player;
            Player1Name = username;
            Player1Elo = elo;
            Debug.Log($"✅ Player 1 registered: {username} (ELO: {elo})");
        }
        else if (Player2 == PlayerRef.None)
        {
            Player2 = player;
            Player2Name = username;
            Player2Elo = elo;
            Debug.Log($"✅ Player 2 registered: {username} (ELO: {elo})");
        }
        else
        {
            Debug.LogWarning($"⚠️ Both player slots full - cannot register {username}");
        }
    }

    /// <summary>
    /// Notify client their move was rejected
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_MoveRejected(PlayerRef player, string reason)
    {
        if (Runner.LocalPlayer == player)
        {
            Debug.LogWarning($"❌ Your move was rejected: {reason}");
        }
    }

    /// <summary>
    /// Notify all clients game is over
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GameOver(int winner)
    {
        string message = winner == 0 ? "DRAW!" : $"PLAYER {winner} WINS!";
        Debug.Log($"🎮 Game Over: {message}");

        // Save match to backend (if logged in)
        if (APIManager.Instance != null && APIManager.Instance.IsLoggedIn && Object.HasStateAuthority)
        {
            SaveMatchToBackend(winner);
        }
    }

    /// <summary>
    /// Save match result to backend API
    /// </summary>
    private void SaveMatchToBackend(int winner)
    {
        // Get player IDs
        int player1BackendId = localPlayerId;
        int player2BackendId = GetOpponentPlayerId();

        // For single-player demo, we can skip if opponent ID not set
        if (player1BackendId <= 0 || player2BackendId <= 0)
        {
            Debug.Log("⚠️ Skipping backend save - player IDs not set (single player mode)");
            return;
        }

        // Determine winner ID (null for draw)
        int? winnerBackendId = null;
        if (winner == 1)
        {
            winnerBackendId = player1BackendId;
        }
        else if (winner == 2)
        {
            winnerBackendId = player2BackendId;
        }

        // Save to backend
        StartCoroutine(APIManager.Instance.SaveMatch(
            player1BackendId,
            player2BackendId,
            winnerBackendId,
            success =>
            {
                if (success)
                {
                    Debug.Log("✅ Match saved to backend");
                }
            }
        ));
    }

    /// <summary>
    /// Set the local player's backend ID (called after login)
    /// </summary>
    public void SetLocalPlayerId(int playerId)
    {
        localPlayerId = playerId;
        Debug.Log($"Local player backend ID set to: {playerId}");
    }

    /// <summary>
    /// Get the opponent's backend player ID
    /// Returns -1 if not set or if playing locally without backend
    /// </summary>
    private int GetOpponentPlayerId()
    {
        // In a real multiplayer game, you'd get this from the other player
        // For demo purposes, we'll use a dummy ID or let it be -1
        // You can extend this later to properly sync player IDs across network
        return -1; // Placeholder - replace with actual opponent ID in full implementation
    }
}