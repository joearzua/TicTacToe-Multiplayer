using UnityEngine;
using UnityEngine.UI;

public class ThemeSwitcher : MonoBehaviour
{
    [SerializeField] 
    private Button themeSwitchButton;
    private bool isDarkTheme = false;

    private void Start()
    {
        if (themeSwitchButton != null)
        {
            themeSwitchButton.onClick.AddListener(SwitchTheme);
        }
    }

    private void SwitchTheme()
    {
        if (ThemeManager.Instance == null) return;

        if (isDarkTheme)
        {
            ThemeManager.Instance.LoadThemeAsync("ClassicTheme");
        }
        else
        {
            ThemeManager.Instance.LoadThemeAsync("DarkNeonTheme");
        }

        isDarkTheme = !isDarkTheme;
    }
}