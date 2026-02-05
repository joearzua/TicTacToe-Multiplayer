using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoardUI : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private GameManager gameManager;

    [SerializeField] private Button[] cellButtons;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playerInfoText;
    [SerializeField] private TextMeshProUGUI opponentInfoText;
    [SerializeField] private Button resetButton;

    [Header("Cell Display")] [SerializeField]
    private string player1Symbol = "X";

    [SerializeField] private string player2Symbol = "O";

    private NetworkRunner runner;
    private bool opponentDisconnected = false;
    private bool opponentEverConnected = false;

    private void Start()
    {
        for (int i = 0; i < cellButtons.Length; i++)
        {
            int index = i;
            cellButtons[i].onClick.AddListener(() => OnCellClicked(index));
        }

        resetButton.onClick.AddListener(OnResetClicked);
        runner = FindObjectOfType<NetworkRunner>();
    }

    private void Update()
    {
        if (runner == null)
        {
            runner = FindObjectOfType<NetworkRunner>();
        }

        // Find GameManager if not cached
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null) return; // Not spawned yet
        }

        if (gameManager.Object == null || !gameManager.Object.IsValid) return;

        int myPlayerNumber = GetMyPlayerNumber(gameManager);

        // Track if opponent ever connected
        if (myPlayerNumber == 1 && gameManager.Player2 != PlayerRef.None)
        {
            opponentEverConnected = true;
        }
        else if (myPlayerNumber == 2 && gameManager.Player1 != PlayerRef.None)
        {
            opponentEverConnected = true;
        }

        // Check for disconnect
        if (!gameManager.GameOver && opponentEverConnected && !opponentDisconnected)
        {
            bool opponentStillConnected = false;

            if (myPlayerNumber == 1)
            {
                opponentStillConnected = runner.ActivePlayers.Contains(gameManager.Player2);
            }
            else if (myPlayerNumber == 2)
            {
                opponentStillConnected = runner.ActivePlayers.Contains(gameManager.Player1);
            }

            if (!opponentStillConnected)
            {
                Debug.Log("Opponent disconnected!");
                opponentDisconnected = true;
            }
        }

        UpdateBoard(gameManager);
        UpdateStatus(gameManager);
        UpdatePlayerInfo(gameManager);
        UpdateButtonStates(gameManager);
    }

    private void OnCellClicked(int position)
    {
        if (runner == null || gameManager == null) return;
        if (gameManager.GameOver) return;
        if (opponentDisconnected) return;
        if (!IsMyTurn(gameManager)) return;

        gameManager.RPC_RequestMove(position, runner.LocalPlayer);
    }

    private void UpdateBoard(GameManager gm)
    {
        // Get current theme colors
        Color player1Color = Color.blue;
        Color player2Color = Color.red;

        if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
        {
            player1Color = ThemeManager.Instance.CurrentTheme.player1Color;
            player2Color = ThemeManager.Instance.CurrentTheme.player2Color;
        }

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
                cellText.color = player1Color; // ← Uses theme color
            }
            else if (cellValue == 2)
            {
                cellText.text = player2Symbol;
                cellText.color = player2Color; // ← Uses theme color
            }
        }
    }

    private void UpdateStatus(GameManager gm)
    {
        int myPlayerNumber = GetMyPlayerNumber(gm);

        // Check if player is not registered yet
        if (myPlayerNumber == 0)
        {
            statusText.text = "CONNECTING...";
            statusText.color = Color.yellow;
            return;
        }

        // Check if opponent disconnected
        if (opponentDisconnected)
        {
            statusText.text = "OPPONENT DISCONNECTED";
            statusText.color = Color.red;
            return;
        }

        // Check if waiting for opponent
        bool waitingForOpponent = false;
        if (myPlayerNumber == 1 && gm.Player2 == PlayerRef.None)
        {
            waitingForOpponent = true;
        }
        else if (myPlayerNumber == 2 && gm.Player1 == PlayerRef.None)
        {
            waitingForOpponent = true;
        }

        if (waitingForOpponent)
        {
            statusText.text = "WAITING FOR OPPONENT...";
            statusText.color = Color.yellow;
            return;
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
                statusText.text = iWon ? "YOU WIN!" : $"PLAYER {gm.Winner} WINS!";
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

    private void UpdatePlayerInfo(GameManager gm)
    {
        int myPlayerNumber = GetMyPlayerNumber(gm);

        if (myPlayerNumber == 1)
        {
            playerInfoText.text = $"{gm.Player1Name}\nELO: {gm.Player1Elo}";

            if (gm.Player2 != PlayerRef.None)
            {
                opponentInfoText.text = $"{gm.Player2Name}\nELO: {gm.Player2Elo}";
            }
            else
            {
                opponentInfoText.text = "Waiting for opponent...";
            }
        }
        else if (myPlayerNumber == 2)
        {
            playerInfoText.text = $"{gm.Player2Name}\nELO: {gm.Player2Elo}";
            opponentInfoText.text = $"{gm.Player1Name}\nELO: {gm.Player1Elo}";
        }
        else
        {
            playerInfoText.text = "Connecting...";
            opponentInfoText.text = "";
        }
    }

    private void UpdateButtonStates(GameManager gm)
    {
        int myPlayerNumber = GetMyPlayerNumber(gm);
        bool waitingForOpponent = (myPlayerNumber == 1 && gm.Player2 == PlayerRef.None) ||
                                  (myPlayerNumber == 2 && gm.Player1 == PlayerRef.None);

        bool canPlay = !gm.GameOver &&
                       !opponentDisconnected &&
                       !waitingForOpponent &&
                       IsMyTurn(gm);

        // Enable/disable all cell buttons
        foreach (var button in cellButtons)
        {
            button.interactable = canPlay;
        }
    }

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