using UnityEngine;

public class CheckpointFlag : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Player")) return;

        PlayerController player = collider.GetComponent<PlayerController>();
        if (player == null)
        {
            player = collider.GetComponentInParent<PlayerController>();
        }

        if (player != null)
        {
            player.SetCheckpoint(transform.position);
            Debug.Log($"[Checkpoint] {player.name} 플레이어 체크포인트 등록: {transform.position}");
        }
    }

    public void ResetFlag()
    {
        // 라운드 재시작 시 깃발 상태 초기화가 필요하다면 여기에 작성
    }
}
