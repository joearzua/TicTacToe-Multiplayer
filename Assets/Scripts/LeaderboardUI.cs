using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays top 10 players leaderboard
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform leaderboardContent; // Parent for entries
    [SerializeField] private GameObject entryPrefab; // Prefab for one leaderboard entry
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshLeaderboard);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(() => leaderboardPanel.SetActive(false));
        
        leaderboardPanel.SetActive(false);
    }

    public void ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        RefreshLeaderboard();
    }

    private void RefreshLeaderboard()
    {
        StartCoroutine(APIManager.Instance.GetLeaderboard(entries =>
        {
            // Clear existing entries
            foreach (Transform child in leaderboardContent)
            {
                Destroy(child.gameObject);
            }

            // Create new entries
            foreach (var entry in entries)
            {
                GameObject entryObj = Instantiate(entryPrefab, leaderboardContent);
                
                // Assuming entry prefab has: RankText, UsernameText, EloText, GamesText
                var texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 4)
                {
                    texts[0].text = $"#{entry.rank}";
                    texts[1].text = entry.username;
                    texts[2].text = $"{entry.eloRating} ELO";
                    texts[3].text = $"{entry.gamesPlayed} games";
                }
            }
        }));
    }
}