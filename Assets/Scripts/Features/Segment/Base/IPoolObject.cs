using UnityEngine;

public interface IPoolObject
{
    /// <summary>
    /// 오브젝트 풀에서 꺼내져 활성화될때 실행할 초기화 메서드
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// 오브젝트 풀로 돌아가서 비활성화 될때 실행할 상태 정리 메서드
    /// </summary>
    void OnDespawn();
}
