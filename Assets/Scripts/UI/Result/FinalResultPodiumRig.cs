using UnityEngine;

public sealed class FinalResultPodiumRig : MonoBehaviour
{
    [SerializeField] private Camera Camera_Preview;
    [SerializeField] private Transform[] Transform_RankPoints;
    [SerializeField] private GameObject Prefab_CharacterPreview;

    private static readonly int Rank1AnimationHash = Animator.StringToHash("Base Layer.Rank1_Reaction");
    private static readonly int Rank2AnimationHash = Animator.StringToHash("Base Layer.Rank2_Reaction");
    private static readonly int Rank3AnimationHash = Animator.StringToHash("Base Layer.Rank3_Reaction");

    public Camera PreviewCamera => Camera_Preview;

    public void SpawnCharacter(int rankIndex, int colorIndex)
    {
        if (Prefab_CharacterPreview == null) return;

        if (Transform_RankPoints == null || rankIndex < 0 || rankIndex >= Transform_RankPoints.Length) return;

        Transform rankPoint = Transform_RankPoints[rankIndex];

        if (rankPoint == null) return;

        GameObject characterInstance = Instantiate(Prefab_CharacterPreview, rankPoint);

        characterInstance.transform.localPosition = Vector3.zero;
        characterInstance.transform.localRotation = Quaternion.identity;
        characterInstance.transform.localScale = Vector3.one;

        PlayerColor playerColor = characterInstance.GetComponentInChildren<PlayerColor>();

        if (playerColor != null) playerColor.ApplyMaterial(colorIndex);

        PlayRankAnimation(characterInstance, rankIndex);
    }

    private void PlayRankAnimation(GameObject characterInstance, int rankIndex)
    {
        Animator animator = characterInstance.GetComponentInChildren<Animator>();

        if (animator == null) return;

        int animationHash = rankIndex switch
        {
            0 => Rank1AnimationHash,
            1 => Rank2AnimationHash,
            2 => Rank3AnimationHash,
            _ => Rank3AnimationHash
        };

        animator.Play(animationHash, 0, 0f);
    }
}