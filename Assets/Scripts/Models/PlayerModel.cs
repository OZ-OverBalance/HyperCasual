using System;
using UnityEngine;

[System.Serializable]
public class PlayerModel 
{
    public event Action<string> OnSomethingChanged; // 예시로 써둔거

    private int score;
    private int moveSpeed; // 혹시 느려지는 효과 있을까봐 모델에 넣음

    public int Score
    {
        get { return score; }
        set
        {
            if (score != value)
            {
                score = value;
                OnSomethingChanged?.Invoke(nameof(Score)); // 예시용
            }
        }
    }

    public int MoveSpeed
    {
        get { return moveSpeed; }
        set
        {
            if (moveSpeed != value)
            {
                moveSpeed = value;
                OnSomethingChanged?.Invoke(nameof(MoveSpeed); // 예시용
            }
        }
    }

    public PlayerModel()
    {
        score = 0;
        moveSpeed = 10;
    }
    public PlayerModel(int score, int moveSpeed)
    {
        this.score = score;
        this.moveSpeed = moveSpeed;
    }
}
