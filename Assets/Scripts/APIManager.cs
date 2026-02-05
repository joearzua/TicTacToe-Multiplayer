using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles all HTTP requests to the backend API
/// Singleton pattern for easy access from any script
/// </summary>
public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }

    [Header("API Configuration")] [SerializeField]
    private string apiBaseUrl = "http://localhost:5000/api";

    // Current logged-in player info
    public int CurrentPlayerId { get; private set; }
    public string CurrentUsername { get; private set; }
    public int CurrentEloRating { get; private set; }
    public bool IsLoggedIn => CurrentPlayerId > 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Authentication

    /// <summary>
    /// Register new player account
    /// </summary>
    public IEnumerator Register(string username, string password, Action<bool, string> callback)
    {
        var requestData = new RegisterRequest
        {
            username = username,
            password = password
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest($"{apiBaseUrl}/auth/register", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                SetCurrentPlayer(response);
                callback?.Invoke(true, "Registration successful!");
            }
            else
            {
                string error = request.downloadHandler.text;
                callback?.Invoke(false, $"Registration failed: {error}");
            }
        }
    }

    /// <summary>
    /// Login with existing account
    /// </summary>
    public IEnumerator Login(string username, string password, Action<bool, string> callback)
    {
        var requestData = new LoginRequest
        {
            username = username,
            password = password
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest($"{apiBaseUrl}/auth/login", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                SetCurrentPlayer(response);
                callback?.Invoke(true, "Login successful!");
            }
            else
            {
                string error = request.downloadHandler.text;
                callback?.Invoke(false, $"Login failed: {error}");
            }
        }
    }

    private void SetCurrentPlayer(LoginResponse response)
    {
        CurrentPlayerId = response.playerId;
        CurrentUsername = response.username;
        CurrentEloRating = response.eloRating;
        Debug.Log($"Logged in as {CurrentUsername} (ELO: {CurrentEloRating})");
    }

    #endregion

    #region Matches

    /// <summary>
    /// Save match result to backend
    /// </summary>
    public IEnumerator SaveMatch(int player1Id, int player2Id, int? winnerId, Action<bool> callback)
    {
        var requestData = new MatchRequest
        {
            player1Id = player1Id,
            player2Id = player2Id,
            winnerId = winnerId
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest($"{apiBaseUrl}/matches", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Match saved to backend");
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError($"Failed to save match: {request.downloadHandler.text}");
                callback?.Invoke(false);
            }
        }
    }

    #endregion

    #region Leaderboard

    /// <summary>
    /// Get top 10 players
    /// </summary>
    public IEnumerator GetLeaderboard(Action<LeaderboardEntry[]> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{apiBaseUrl}/leaderboard"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Unity's JsonUtility doesn't handle arrays directly, so wrap it
                string json = "{\"entries\":" + request.downloadHandler.text + "}";
                var wrapper = JsonUtility.FromJson<LeaderboardWrapper>(json);
                callback?.Invoke(wrapper.entries);
            }
            else
            {
                Debug.LogError($"Failed to get leaderboard: {request.error}");
                callback?.Invoke(Array.Empty<LeaderboardEntry>());
            }
        }
    }

    #endregion

    #region Data Classes

    [Serializable]
    private class RegisterRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class LoginResponse
    {
        public int playerId;
        public string username;
        public int eloRating;
    }

    [Serializable]
    private class MatchRequest
    {
        public int player1Id;
        public int player2Id;
        public int? winnerId;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string username;
        public int eloRating;
        public int gamesPlayed;
    }

    [Serializable]
    private class LeaderboardWrapper
    {
        public LeaderboardEntry[] entries;
    }

    #endregion
}