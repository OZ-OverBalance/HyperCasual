using System;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public GameSettings Settings;

    public string PlayerName;
    public string SelectedSkinId;
    public int Coins;

    public PlayerStats Stats;

    public GameData()
    {
        PlayerName = "Anonymous123";
        SelectedSkinId = "default_skin";
        Coins = 0;
        Settings = new GameSettings();
        Stats = new PlayerStats();
    }
}

[System.Serializable]
public class GameSettings
{
    public float MasterVolume = 1.0f;
    public float BgmVolume = 1.0f;
    public float SfxVolume = 1.0f;
    public bool IsFullScreen = true;
}

[System.Serializable]
public class PlayerStats
{
    public int TotalGamesPlayed = 0;
    public int TotalWins = 0;
}
