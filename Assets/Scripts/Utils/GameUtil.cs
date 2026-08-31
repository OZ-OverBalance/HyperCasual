using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class GameUtil 
{
    private static long _lastId;
    public static long GenerateUniqueId()
    {
        long newId = DateTime.UtcNow.Ticks;

        while (true)
        {
            long lastId = Volatile.Read(ref _lastId);

            // 만약 현재 시간이 이전 ID보다 작거나 같다면 (루프가 너무 빠른 경우 포함)
            // 이전 ID + 1로 강제 설정하여 중복 방지
            long idToAssign = (newId <= lastId) ? lastId + 1 : newId;

            // _lastId가 내가 읽은 시점과 같다면 idToAssign으로 교체 (성공 시 루프 탈출)
            if (Interlocked.CompareExchange(ref _lastId, idToAssign, lastId) == lastId)
            {
                return idToAssign;
            }
            // 그 사이 다른 스레드가 값을 바꿨다면 다시 시도
        }
    }

    public static void UtilShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

}
