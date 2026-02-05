using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    [Header("References")] [SerializeField]
    private Image backgroundImage;

    [SerializeField] private Button[] cellButtons;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button showLeaderboardButton;

    [Header("Current Theme")] [SerializeField]
    private BoardTheme currentTheme;

    private Dictionary<string, BoardTheme> loadedThemes = new Dictionary<string, BoardTheme>();

    public BoardTheme CurrentTheme => currentTheme;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Apply default theme
        if (currentTheme != null)
        {
            ApplyTheme(currentTheme);
        }
    }

    /// <summary>
    /// Load a theme from Addressables
    /// </summary>
    public async void LoadThemeAsync(string themeName)
    {
        Debug.Log($"Loading theme: {themeName}");

        if (loadedThemes.ContainsKey(themeName))
        {
            Debug.Log($"Theme already loaded, applying: {themeName}");
            ApplyTheme(loadedThemes[themeName]);
            return;
        }

        AsyncOperationHandle<BoardTheme> handle = Addressables.LoadAssetAsync<BoardTheme>(themeName);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"Theme loaded: {themeName}");
            loadedThemes[themeName] = handle.Result;
            ApplyTheme(handle.Result);
        }
        else
        {
            Debug.LogError($"Failed to load theme: {themeName}");
        }
    }

    /// <summary>
    /// Apply theme to UI
    /// </summary>
    public void ApplyTheme(BoardTheme theme)
    {
        if (theme == null)
        {
            Debug.LogWarning("No theme provided");
            return;
        }

        currentTheme = theme;
        Debug.Log($"Applying theme: {theme.themeName}");

        // Apply background
        if (backgroundImage != null)
        {
            backgroundImage.color = theme.backgroundColor;
        }

        // Apply cell buttons
        if (cellButtons != null)
        {
            foreach (var button in cellButtons)
            {
                if (button != null)
                {
                    var colors = button.colors;
                    colors.normalColor = theme.cellDefaultColor;
                    colors.highlightedColor = theme.cellHoverColor;
                    button.colors = colors;
                }
            }
        }

        // Apply status text
        if (statusText != null)
        {
            statusText.color = theme.statusTextColor;
        }

        // Apply reset button color
        if (resetButton != null)
        {
            var colors = resetButton.colors;
            colors.normalColor = theme.buttonColor;
            resetButton.colors = colors;
            resetButton.GetComponentInChildren<TextMeshProUGUI>().color = theme.buttonTextColor;
        }
        
        //Apply show leaderboard button
        if (showLeaderboardButton != null)
        {
            var colors = showLeaderboardButton.colors;
            colors.normalColor = theme.buttonColor;
            showLeaderboardButton.colors = colors;
            showLeaderboardButton.GetComponentInChildren<TextMeshProUGUI>().color = theme.buttonTextColor;
        }
    }

    /// <summary>
    /// Get list of available themes
    /// </summary>
    public List<string> GetAvailableThemes()
    {
        return new List<string> { "ClassicTheme", "DarkNeonTheme" };
    }
}