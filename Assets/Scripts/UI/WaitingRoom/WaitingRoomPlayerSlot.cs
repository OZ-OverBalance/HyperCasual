using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WaitingRoomPlayerSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text Text_Nickname;
    [SerializeField] private Image Image_Character;
    [SerializeField] private TMP_Text Text_ReadyState;
    [SerializeField] private GameObject Object_HostBadge;

    private ulong _clientId;

    public ulong ClientId => _clientId;

    public void InitializeSlot(ulong clientId, string nickname, Sprite characterSprite, bool isReady, bool isHost)
    {
        _clientId = clientId;

        SetNickname(nickname);
        SetCharacterSprite(characterSprite);
        SetReadyState(isReady);
        SetHostState(isHost);
    }

    public void SetNickname(string nickname)
    {
        Text_Nickname.text = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname;
    }

    public void SetCharacterSprite(Sprite characterSprite)
    {
        Image_Character.sprite = characterSprite;
        Image_Character.enabled = characterSprite != null;
    }

    public void SetReadyState(bool isReady)
    {
        Text_ReadyState.text = isReady ? "READY!" : "WAITING";
        Text_ReadyState.color = isReady ? new Color(0.45f, 1f, 0.45f): Color.white;
    }

    public void SetHostState(bool isHost)
    {
        Object_HostBadge.SetActive(isHost);
        Text_ReadyState.gameObject.SetActive(!isHost);
    }

    public void ClearSlot()
    {
        _clientId = 0;

        Text_Nickname.text = string.Empty;
        Text_ReadyState.text = string.Empty;
        Object_HostBadge.SetActive(false);

        Image_Character.sprite = null;
        Image_Character.enabled = false;
    }
}