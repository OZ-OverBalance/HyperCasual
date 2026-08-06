using System;
using UnityEngine;
using UnityEngine.Audio;

public class GameManager : SingletonBase<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
    }

    //public void RestartGame()
    //{
    //    Time.timeScale = 1f;
    //}

    //public void PauseGame()
    //{
    //    Time.timeScale = 0f;
    //}
}