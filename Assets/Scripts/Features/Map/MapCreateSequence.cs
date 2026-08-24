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
            gatherSeq.Join(map.transform.DOMove(centerFocusPos, gatherDuration).SetEase(Ease.InCubic));
            gatherSeq.Join(map.transform.DOScale(Vector3.one * 0.8f, gatherDuration).SetEase(Ease.InCubic));
        }
        assembleSeq.Append(gatherSeq);

        assembleSeq.AppendCallback(() =>
        {
            // 사운드를 넣는다면
        });
        foreach (var map in mapList)
        {
            assembleSeq.Join(map.transform.DOShakePosition(shuffleDuration, strength: new Vector3(0.5f, 0.2f, 0f), vibrato: 10));
        }

        for (int i = 0; i < mapList.Count; i++)
        {
            var map = mapList[i];
            var targetPos = targetPositions[i];

            Sequence deploySeq = DOTween.Sequence();
            deploySeq.Append(map.transform.DOMove(targetPos, deployDuration).SetEase(Ease.OutBack, overshoot: 1.1f));
            deploySeq.Join(map.transform.DOScale(Vector3.one, deployDuration).SetEase(Ease.OutBack));

            assembleSeq.Insert(gatherDuration + shuffleDuration + (i * deployInterval), deploySeq);
        }

        await assembleSeq.AsyncWaitForCompletion();

        foreach (var map in mapList)
        {
            map.SetCover(false);
        }
    }
}
