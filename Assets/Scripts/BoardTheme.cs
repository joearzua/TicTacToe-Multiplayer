using UnityEngine;

[CreateAssetMenu(fileName = "BoardTheme", menuName = "TicTacToe/Board Theme")]
public class BoardTheme : ScriptableObject
{
    [Header("Theme Info")]
    public string themeName;
    public string themeDescription;
    
    [Header("Board Colors")]
    public Color backgroundColor;
    public Color cellDefaultColor;
    public Color cellHoverColor;
    
    [Header("Symbol Colors")]
    public Color player1Color; // X color
    public Color player2Color; // O color
    
    [Header("UI Colors")]
    public Color statusTextColor;
    public Color buttonColor;
    public Color buttonTextColor;
}