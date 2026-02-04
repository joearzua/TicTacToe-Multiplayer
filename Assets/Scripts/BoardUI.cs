using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Client-side UI - displays game state from server
/// All game logic is on server, this just displays it
/// </summary>
public class BoardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Button[] cellButtons; // 9 buttons (0-8)
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playerInfoText;
    [SerializeField] private Button resetButton;

    [Header("Cell Display")]
    [SerializeField] private string player1Symbol = "X";
    [SerializeField] private string player2Symbol = "O";

    private NetworkRunner runner;

    private void Start()
    {
        Debug.Log("🎬 BoardUI Start - setting up buttons");
    
        // Setup button callbacks
        for (int i = 0; i < cellButtons.Length; i++)
        {
            int index = i; // Capture for closure
            cellButtons[i].onClick.AddListener(() => OnCellClicked(index));
        }

        resetButton.onClick.AddListener(OnResetClicked);
    
        // Find runner
        runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("✅ Found NetworkRunner in Start");
        }
        else
        {
            Debug.LogError("❌ NetworkRunner NOT found in Start!");
        }
    }

    private void Update()
    {
        // Keep trying to find runner until we have it
        if (runner == null)
        {
            runner = FindObjectOfType<NetworkRunner>();
            if (runner != null)
            {
                Debug.Log("✅ Found NetworkRunner!");
            }
        }
    
        // Don't update board until we have both
        if (gameManager == null || runner == null) return;

        UpdateBoard(gameManager);
        UpdateStatus(gameManager);
    }

    private void OnCellClicked(int position)
    {
        if (runner == null || gameManager == null)
        {
            Debug.LogWarning("⚠️ Runner or GameManager not ready yet");
            return;
        }
    
        if (gameManager.GameOver) return;
        if (!IsMyTurn(gameManager)) return;

        Debug.Log($"📤 Sending move for position {position}");
        gameManager.RPC_RequestMove(position, runner.LocalPlayer);
    }

    /// <summary>
    /// Update board display from server state
    /// </summary>
    private void UpdateBoard(GameManager gm)
    {
        for (int i = 0; i < 9; i++)
        {
            int cellValue = gm.Board[i];
            TextMeshProUGUI cellText = cellButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            if (cellValue == 0)
            {
                cellText.text = "";
            }
            else if (cellValue == 1)
            {
                cellText.text = player1Symbol;
                cellText.color = Color.blue;
            }
            else if (cellValue == 2)
            {
                cellText.text = player2Symbol;
                cellText.color = Color.red;
            }
        }
    }

    /// <summary>
    /// Update status text
    /// </summary>
    private void UpdateStatus(GameManager gm)
    {
        // Show player info
        int myPlayerNumber = GetMyPlayerNumber(gm);
        if (myPlayerNumber > 0)
        {
            playerInfoText.text = $"You are Player {myPlayerNumber}";
        }
        else
        {
            playerInfoText.text = "Connecting...";
        }

        // Show game status
        if (gm.GameOver)
        {
            if (gm.Winner == 0)
            {
                statusText.text = "DRAW!";
                statusText.color = Color.yellow;
            }
            else
            {
                bool iWon = IsLocalPlayer(gm, gm.Winner);
                statusText.text = iWon ? "YOU WIN! 🎉" : $"PLAYER {gm.Winner} WINS!";
                statusText.color = iWon ? Color.green : Color.red;
            }
        }
        else
        {
            bool isMyTurn = IsMyTurn(gm);
            if (isMyTurn)
            {
                statusText.text = "YOUR TURN";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = $"PLAYER {gm.CurrentPlayer}'S TURN";
                statusText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Reset button clicked
    /// </summary>
    private void OnResetClicked()
    {
        if (gameManager != null)
        {
            gameManager.RPC_RequestReset();
        }
    }

    private bool IsMyTurn(GameManager gm)
    {
        if (runner == null || gm == null) return false;
    
        int myPlayerNumber = GetMyPlayerNumber(gm);
        return myPlayerNumber == gm.CurrentPlayer;
    }

    private int GetMyPlayerNumber(GameManager gm)
    {
        if (runner == null || gm == null) return 0;
    
        if (runner.LocalPlayer == gm.Player1) return 1;
        if (runner.LocalPlayer == gm.Player2) return 2;
        return 0;
    }

    private bool IsLocalPlayer(GameManager gm, int playerNumber)
    {
        return playerNumber == GetMyPlayerNumber(gm);
    }
}