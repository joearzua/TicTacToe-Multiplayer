using Fusion;
using UnityEngine;

/// <summary>
/// Server-authoritative game manager for Tic-Tac-Toe multiplayer.
/// Handles all game logic, move validation, win detection, and backend integration.
/// Runs on Photon Fusion's state authority to prevent cheating.
/// </summary>
public class GameManager : NetworkBehaviour
{
    [Networked] public int Player1BackendId { get; set; }
    [Networked] public int Player2BackendId { get; set; }
    [Networked] public NetworkString<_16> Player1Name { get; set; }
    [Networked] public NetworkString<_16> Player2Name { get; set; }
    [Networked] public int Player1Elo { get; set; }
    [Networked] public int Player2Elo { get; set; }
    [Networked, Capacity(9)] public NetworkArray<int> Board => default;

    [Networked] public int CurrentPlayer { get; set; } = 1;
    [Networked] public int Winner { get; set; } = 0;
    [Networked] public bool GameOver { get; set; } = false;

    [Networked] public PlayerRef Player1 { get; set; }
    [Networked] public PlayerRef Player2 { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
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
            Board.Set(i, 0);
        }

        CurrentPlayer = 1;
        Winner = 0;
        GameOver = false;

        Debug.Log("🎮 Game initialized by host");
    }

    /// <summary>
    /// RPC called by clients to request a move. Validates and executes on server.
    /// </summary>
    /// <param name="position">Board position (0-8)</param>
    /// <param name="player">PlayerRef making the move</param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestMove(int position, PlayerRef player)
    {
        if (!ValidateMove(position, player))
        {
            RPC_MoveRejected(player, "Invalid move");
            return;
        }

        ExecuteMove(position, player);
    }

    /// <summary>
    /// Validates move on server to prevent cheating
    /// </summary>
    /// <param name="position">Board position</param>
    /// <param name="player">PlayerRef attempting move</param>
    /// <returns>True if move is valid</returns>
    private bool ValidateMove(int position, PlayerRef player)
    {
        if (GameOver) return false;
        if (position < 0 || position > 8) return false;
        if (Board[position] != 0) return false;

        int playerNumber = GetPlayerNumber(player);
        if (playerNumber != CurrentPlayer) return false;
        if (playerNumber == 0) return false;

        return true;
    }

    /// <summary>
    /// Executes validated move on server and checks for win/draw
    /// </summary>
    /// <param name="position">Board position</param>
    /// <param name="player">PlayerRef making the move</param>
    private void ExecuteMove(int position, PlayerRef player)
    {
        int playerNumber = GetPlayerNumber(player);
        Board.Set(position, playerNumber);

        if (CheckWin(playerNumber))
        {
            Winner = playerNumber;
            GameOver = true;
            Debug.Log($"🎮 Game Over: PLAYER {playerNumber} WINS!");
            RPC_GameOver(Winner);
            return;
        }

        if (IsBoardFull())
        {
            Winner = 0;
            GameOver = true;
            Debug.Log($"🎮 Game Over: DRAW!");
            RPC_GameOver(0);
            return;
        }

        CurrentPlayer = (CurrentPlayer == 1) ? 2 : 1;
    }

    /// <summary>
    /// Check if current board state is a win for the given player
    /// </summary>
    /// <param name="player">1 or 2</param>
    /// <returns>True if player has won</returns>
    private bool CheckWin(int player)
    {
        int[,] patterns = new int[,]
        {
            { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 },
            { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 },
            { 0, 4, 8 }, { 2, 4, 6 }
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
    /// Check if board is full (draw condition)
    /// </summary>
    /// <returns>True if all cells occupied</returns>
    private bool IsBoardFull()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Board[i] == 0) return false;
        }

        return true;
    }

    /// <summary>
    /// Reset game to initial state (server only)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestReset()
    {
        if (Object.HasStateAuthority)
        {
            InitializeGame();
        }
    }

    /// <summary>
    /// Get player number from PlayerRef
    /// </summary>
    /// <param name="player">PlayerRef to check</param>
    /// <returns>1 or 2, or 0 if not registered</returns>
    private int GetPlayerNumber(PlayerRef player)
    {
        if (player == Player1) return 1;
        if (player == Player2) return 2;
        return 0;
    }

    /// <summary>
    /// Register player with backend info (username, ELO, backend ID)
    /// </summary>
    /// <param name="username">Player's username</param>
    /// <param name="elo">Player's ELO rating</param>
    /// <param name="backendId">Player's backend database ID</param>
    /// <param name="player">PlayerRef to register</param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RegisterPlayerWithInfo(string username, int elo, int backendId, PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (Player1 == PlayerRef.None)
        {
            Player1 = player;
            Player1Name = username;
            Player1Elo = elo;
            Player1BackendId = backendId;
            Debug.Log($"Player 1 registered: {username} (ELO: {elo})");
        }
        else if (Player2 == PlayerRef.None)
        {
            Player2 = player;
            Player2Name = username;
            Player2Elo = elo;
            Player2BackendId = backendId;
            Debug.Log($"Player 2 registered: {username} (ELO: {elo})");
        }
    }

    /// <summary>
    /// Notify client their move was rejected
    /// </summary>
    /// <param name="player">PlayerRef whose move was rejected</param>
    /// <param name="reason">Rejection reason</param>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_MoveRejected(PlayerRef player, string reason)
    {
        if (Runner.LocalPlayer == player)
        {
            Debug.LogWarning($"Move rejected: {reason}");
        }
    }

    /// <summary>
    /// Notify all clients game is over and save to backend
    /// </summary>
    /// <param name="winner">1 for Player 1, 2 for Player 2, 0 for draw</param>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GameOver(int winner)
    {
        if (APIManager.Instance != null && APIManager.Instance.IsLoggedIn && Object.HasStateAuthority)
        {
            SaveMatchToBackend(winner);
        }
    }

    /// <summary>
    /// Save match result to backend API and update ELO ratings
    /// </summary>
    /// <param name="winner">1 for Player 1, 2 for Player 2, 0 for draw</param>
    private void SaveMatchToBackend(int winner)
    {
        int player1BackendId = Player1BackendId;
        int player2BackendId = Player2BackendId;

        if (player1BackendId <= 0 || player2BackendId <= 0)
        {
            Debug.Log("Skipping backend save - players not logged in");
            return;
        }

        int? winnerBackendId = null;
        if (winner == 1) winnerBackendId = player1BackendId;
        else if (winner == 2) winnerBackendId = player2BackendId;

        Debug.Log($"💾 Saving match to backend...");

        StartCoroutine(APIManager.Instance.SaveMatch(
            player1BackendId,
            player2BackendId,
            winnerBackendId,
            success =>
            {
                if (success)
                {
                    Debug.Log("Match saved - ELO updated");
                }
                else
                {
                    Debug.LogError("Failed to save match");
                }
            }
        ));
    }
}