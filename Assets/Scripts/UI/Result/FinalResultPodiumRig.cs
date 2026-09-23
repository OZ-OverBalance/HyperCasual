using UnityEngine;

public sealed class FinalResultPodiumRig : MonoBehaviour
{
    [SerializeField] private Camera Camera_Preview;
    [SerializeField] private Transform[] Transform_RankPoints;
    [SerializeField] private GameObject Prefab_CharacterPreview;

    public Camera PreviewCamera => Camera_Preview;

    public void SpawnCharacter(int rankIndex, int colorIndex)
    {
        if (Prefab_CharacterPreview == null)
        {
            return;
        }

        if (Transform_RankPoints == null || rankIndex < 0 || rankIndex >= Transform_RankPoints.Length)
        {
            return;
        }

        Transform rankPoint = Transform_RankPoints[rankIndex];

        if (rankPoint == null)
        {
            return;
        }

        GameObject characterInstance = Instantiate(Prefab_CharacterPreview, rankPoint);

        characterInstance.transform.localPosition = Vector3.zero;
        characterInstance.transform.localRotation = Quaternion.identity;
        characterInstance.transform.localScale = Vector3.one;

        PlayerColor playerColor = characterInstance.GetComponentInChildren<PlayerColor>();

        if (playerColor != null)
        {
            playerColor.ApplyMaterial(colorIndex);
        }
    }
}