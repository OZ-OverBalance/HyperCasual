using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WaitingRoomPlayerSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text Text_Nickname;
    [SerializeField] private RawImage RawImage_Character;
    [SerializeField] private TMP_Text Text_ReadyState;
    [SerializeField] private GameObject Object_HostBadge;

    [SerializeField] private GameObject Prefab_SlotPreviewRig;
    
    private ulong _clientId;
    private RenderTexture _renderTexture;
    private GameObject _previewRigInstance;
    private PlayerColor _previewPlayerColor;

    public ulong ClientId => _clientId;

    public void InitializeSlot(ulong clientId, string nickname, int colorIndex, bool isReady, bool isHost)
    {
        _clientId = clientId;

        SetNickname(nickname);
        SetupPreviewRig(clientId);
        SetCharacterColor(colorIndex);
        SetReadyState(isReady);
        SetHostState(isHost);
    }

    public void SetNickname(string nickname)
    {
        Text_Nickname.text = string.IsNullOrWhiteSpace(nickname) ? "Player" : nickname;
    }

    private void SetupPreviewRig(ulong clientId)
    {
        if (_previewRigInstance != null) return;

        Vector3 spawnPos = new Vector3(500f + (clientId * 20f), 500f, 500f);
        _previewRigInstance = Instantiate(Prefab_SlotPreviewRig, spawnPos, Quaternion.identity);

        _renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        _renderTexture.Create();

        Camera rigCam = _previewRigInstance.GetComponentInChildren<Camera>();
        if (rigCam != null)
        {
            rigCam.targetTexture = _renderTexture;
        }

        RawImage_Character.texture = _renderTexture;
        _previewPlayerColor = _previewRigInstance.GetComponentInChildren<PlayerColor>();
    }

    public void SetCharacterColor(int colorIndex)
    {
        if (_previewPlayerColor != null)
        {
            _previewPlayerColor.ApplyMaterial(colorIndex);
        }
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
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        if (_previewRigInstance != null)
        {
            Destroy(_previewRigInstance);
        }
    }
}
