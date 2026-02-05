using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles login/register UI
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("UI References")] [SerializeField]
    private GameObject loginPanel;

    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private TextMeshProUGUI searchingStatusText;

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);

        // Show login panel, hide game UI
        loginPanel.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
    }

    private void OnLoginClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter username and password";
            statusText.color = Color.red;
            return;
        }

        statusText.text = "Logging in...";
        statusText.color = Color.yellow;

        StartCoroutine(APIManager.Instance.Login(username, password, (success, message) =>
        {
            if (success)
            {
                statusText.text = message;
                statusText.color = Color.green;

                // Start matchmaking
                var networkManager = FindObjectOfType<NetworkManager>();
                if (networkManager != null)
                {
                    networkManager.StartMatchmaking();
                }
            }
            else
            {
                statusText.text = message;
                statusText.color = Color.red;
            }
        }));
    }

    private void OnRegisterClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter username and password";
            statusText.color = Color.red;
            return;
        }

        if (password.Length < 6)
        {
            statusText.text = "Password must be at least 6 characters";
            statusText.color = Color.red;
            return;
        }

        statusText.text = "Registering...";
        statusText.color = Color.yellow;

        StartCoroutine(APIManager.Instance.Register(username, password, (success, message) =>
        {
            if (success)
            {
                statusText.text = message;
                statusText.color = Color.green;

                // Start matchmaking
                var networkManager = FindObjectOfType<NetworkManager>();
                if (networkManager != null)
                {
                    networkManager.StartMatchmaking();
                }
            }
            else
            {
                statusText.text = message;
                statusText.color = Color.red;
            }
        }));
    }

    public void ShowSearchingStatus(string message)
    {
        if (searchingStatusText != null)
        {
            searchingStatusText.text = message;
            searchingStatusText.gameObject.SetActive(true);
        }

        statusText.text = message;
        statusText.color = Color.yellow;
    }

    public void ShowGame()
    {
        loginPanel.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);
        if (searchingStatusText != null) searchingStatusText.gameObject.SetActive(false);
    }
}