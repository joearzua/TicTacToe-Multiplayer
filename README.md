# Server-Authoritative Multiplayer Tic-Tac-Toe

2D Tic-Tac-Toe game built with Unity and ASP.NET Core, demonstrating **server-authoritative architecture**, **backend integration**, and **real-time networking**.

---

## 🎮 Live Features

### Multiplayer & Networking
- Real-time multiplayer using **Photon Fusion 2**
- **Server-authoritative game logic** - clients send requests, server validates
- Turn-based synchronization with state replication
- Anti-cheat validation (turn order, cell availability, win conditions)
- Automatic matchmaking via lobby system

### Backend & Database (ASP.NET Core + MySQL)
- **REST API** for player accounts and match history
- **MySQL database** with Entity Framework Core ORM
- Player registration & authentication (BCrypt password hashing)
- **ELO rating system** - ratings update after each match
- Top 10 leaderboard with ranked players

### Unity Integration
- HTTP requests to backend API (UnityWebRequest)
- Login/Register UI with validation
- Automatic match saving after games complete
- Leaderboard display with real-time updates

### Addressables
- Board themes loaded from GitHub at runtime
- Shows modern Unity asset management and clean separation of game logic and UI
- Remote location: `TicTacToe-Multiplayer/AddressableContent/`

---

## 🛠 Tech Stack

**Frontend:**
- Unity 2022.3 LTS
- Photon Fusion 2.0.9
- TextMeshPro UI

**Backend:**
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- MySQL 8.0
- BCrypt.Net (password hashing)

---

## 📁 Project Structure
```
TicTacToe-Multiplayer/
├── Assets/
│   ├── Scripts/
│   │   ├── GameManager.cs         # Server-side game logic
│   │   ├── BoardUI.cs              # Client-side UI
│   │   ├── NetworkManager.cs       # Photon connection
│   │   ├── APIManager.cs           # HTTP requests to backend
│   │   ├── LoginUI.cs              # Authentication UI
│   │   ├── ThemeManager.cs         # Addressables theme loading
│   │   └── LeaderboardUI.cs        # Rankings display
│   ├── Themes/
│   │   ├── ClassicTheme.asset      # Blue/red classic colors
│   │   └── DarkNeonTheme.asset     # Cyberpunk-inspired theme
│   └── Scenes/
│       └── Game.unity
└── TicTacToeBackend/TicTacToeAPI/
│   ├── Controllers/
│    │   ├── AuthController.cs        # Register/Login endpoints
│    │   ├── MatchesController.cs     # Save matches, update ELO
│    │   └── LeaderboardController.cs # Get top players
│    ├── Models/
│    │   ├── Player.cs                # Player entity
│    │   └── Match.cs                 # Match entity
│    ├── Data/
│    │   └── TicTacToeDbContext.cs    # EF Core DbContext
│    └── Program.cs                   # API configuration
└── AddressableContent/               # Remotely loaded assets
    └── StandaloneWindows64/
        └── *.bundle                  # Theme bundles
```

---

## 🚀 Setup & Running

### Prerequisites
- Unity 2022.3 LTS or newer
- .NET 8.0 SDK
- MySQL Server 8.0
- Photon Fusion account (free)

### Backend Setup

1. **Configure MySQL:**
```sql
CREATE DATABASE tictactoe;
```

2. **Update connection string:**
Edit `TicTacToeBackend/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;port=3306;database=tictactoe;user=root;password=YOUR_PASSWORD"
}
```

3. **Run migrations:**
```bash
cd TicTacToeBackend/TicTacToeAPI
dotnet ef database update
```

4. **Start API:**
```bash
dotnet run
```
API runs on `http://localhost:5248` (or assigned port)

### Unity Setup

1. **Open project in Unity**

2. **Import Photon Fusion:**
   - Window → Asset Store
   - Search "Photon Fusion 2"
   - Download & Import

3. **Configure Photon:**
   - Create Photon account at photonengine.com
   - Create Fusion app
   - Add App ID to Unity

4. **Update API URL:**
   - Select APIManager in scene
   - Set API Base URL to your backend port

5. **Play:**
   - Run 2 instances (ParrelSync or builds)
   - Register/login with different accounts
   - Play multiplayer

---

## 🔒 Security & Best Practices

- Password hashing with BCrypt (never store plaintext)
- Server validates ALL game actions (clients cannot cheat)
- CORS configured for Unity client
- Input validation on all API endpoints
- Entity Framework prevents SQL injection

---
## Code Highlights

### Server Authority Pattern
All game logic runs on the host. Clients send requests, server validates:
```csharp
// Client sends move request
gameManager.RPC_RequestMove(position, runner.LocalPlayer);

// Server validates before executing
[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_RequestMove(int position, PlayerRef player)
{
    if (!ValidateMove(position, player)) 
    {
        RPC_MoveRejected(player, "Invalid move");
        return;
    }
    ExecuteMove(position, player); // Only server executes
}

private bool ValidateMove(int position, PlayerRef player)
{
    if (GameOver) return false;
    if (Board[position] != 0) return false;
    if (GetPlayerNumber(player) != CurrentPlayer) return false;
    return true; // All checks passed
}
```

### Automatic Matchmaking
Lobby-based system finds available matches:
```csharp
public async void StartMatchmaking()
{
    await runner.JoinSessionLobby(SessionLobby.Shared);
    
    // Find session with space
    foreach (var session in availableSessions)
    {
        if (session.PlayerCount < maxPlayers && session.IsOpen)
        {
            await JoinSession(session);
            return;
        }
    }
    
    CreateNewSession(); // No available matches
}
```

### Remote Asset Loading
Addressables load themes from GitHub at runtime:
```csharp
public async void LoadThemeAsync(string themeName)
{
    AsyncOperationHandle<BoardTheme> handle = 
        Addressables.LoadAssetAsync<BoardTheme>(themeName);
    await handle.Task;
    
    if (handle.Status == AsyncOperationStatus.Succeeded)
    {
        ApplyTheme(handle.Result);
    }
}
```
---

## 📊 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Create new player account |
| POST | `/api/auth/login` | Authenticate player |
| POST | `/api/matches` | Save match result, update ELO |
| GET | `/api/leaderboard` | Get top 10 players by ELO |

Swagger documentation available at: `http://localhost:PORT/swagger`

---

## 📝 Note on Photon Server SDK

This project uses **Photon Fusion** instead of Photon Self-Hosting Server SDK because:
- Server SDK requires Gaming Circle membership (not publicly available)
- Photon discontinued free SDK access for game development

Architectural patterns (server authority, RPC validation, client/server separation) translate directly to Photon Server SDK environments.

---

## 📄 License

This project is for portfolio and educational purposes.


