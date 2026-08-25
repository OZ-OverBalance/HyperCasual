using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class MapCreateSequence : MonoBehaviour
{
    [Header("연출 설정")]
    [SerializeField] private float gatherDuration = 0.5f;     // 중앙으로 모이는 시간
    [SerializeField] private float shuffleDuration = 0.4f;    // 셔플/흔들림 시간
    [SerializeField] private float deployDuration = 0.45f;    // 각 맵이 제자리로 날아가는 시간
    [SerializeField] private float deployInterval = 0.15f;    // 맵 전개 간격
    [SerializeField] private float finishDelay = 0.5f;        // 전개 완료 후 가림막 해제 간격 시간

    public async UniTask PlayMapAssembleAnimationAsync(List<BaseMap> mapList, Vector3 centerFocusPos)
    {
        if (mapList == null || mapList.Count == 0) return;

        List<Vector3> targetPositions = new List<Vector3>();
        foreach (var map in mapList)
        {
            targetPositions.Add(map.transform.position);
            map.SetCover(true);
        }

        Sequence assembleSeq = DOTween.Sequence();

        Sequence gatherSeq = DOTween.Sequence();
        foreach (var map in mapList)
        {
            gatherSeq.Join(map.transform.DOMove(centerFocusPos, gatherDuration).SetEase(Ease.OutQuart));
            gatherSeq.Join(map.transform.DOScale(Vector3.one * 0.75f, gatherDuration).SetEase(Ease.OutQuart));
        }
        assembleSeq.Append(gatherSeq);

        Sequence shuffleSeq = DOTween.Sequence();
        foreach (var map in mapList)
        {
            shuffleSeq.Join(map.transform.DOShakePosition(shuffleDuration, strength: new Vector3(0.8f, 0.4f, 0f), vibrato: 12, randomness: 90, snapping: false, fadeOut: true));
        }
        assembleSeq.Append(shuffleSeq);

        Sequence deploySeq = DOTween.Sequence();
        for (int i = 0; i < mapList.Count; i++)
        {
            var map = mapList[i];
            var targetPos = targetPositions[i];

            float startTime = i * deployInterval;

            deploySeq.Insert(startTime, map.transform.DOMove(targetPos, deployDuration).SetEase(Ease.OutBack, 1.2f));
            deploySeq.Insert(startTime, map.transform.DOScale(Vector3.one, deployDuration).SetEase(Ease.OutBack));
        }
        assembleSeq.Append(deploySeq);

        assembleSeq.AppendInterval(finishDelay);

        await assembleSeq.AsyncWaitForCompletion();

        // 6. 가림막 제거
        foreach (var map in mapList)
        {
            map.SetCover(false);
        }
    }
}
