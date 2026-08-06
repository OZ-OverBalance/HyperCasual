using System;
using UnityEngine;

[System.Serializable]
public class PlayerModel 
{
    public event Action<string> OnSomethingChanged; // 예시로 써둔거

    private int _uniqueId; 
    private int _score;
    private int _moveSpeed; // 혹시 느려지는 효과 있을까봐 모델에 넣음

    public int UniqueId => _uniqueId;
    public int Score
    {
        get { return _score; }
        set
        {
            if (_score != value)
            {
                _score = value;
                OnSomethingChanged?.Invoke(nameof(Score)); // 예시용
            }
        }
    }

    public int MoveSpeed
    {
        get { return _moveSpeed; }
        set
        {
            if (_moveSpeed != value)
            {
                _moveSpeed = value;
                OnSomethingChanged?.Invoke(nameof(MoveSpeed)); // 예시용
            }
        }
    }

    public PlayerModel()
    {
        _uniqueId = -1;
        _score = 0;
        _moveSpeed = 10;
    }
    public PlayerModel(int uniqueId, int score, int moveSpeed)
    {
        _uniqueId = uniqueId;
        _score = score;
        _moveSpeed = moveSpeed;
    }

}
