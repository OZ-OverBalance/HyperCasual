using System;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public GameSettings settings;

    public string playerName;
    public string selectedSkinId;
    public int coins;

    public PlayerStats stats;

    public GameData()
    {
        playerName = "Anonymous123";
        selectedSkinId = "default_skin";
        coins = 0;
        settings = new GameSettings();
        stats = new PlayerStats();
    }
}

[System.Serializable]
public class GameSettings
{
    public float masterVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;
    public bool isFullScreen = true;
}

[System.Serializable]
public class PlayerStats
{
    public int totalGamesPlayed = 0;
    public int totalWins = 0;
}
