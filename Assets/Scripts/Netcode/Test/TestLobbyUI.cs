using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestLobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField InputField_Name;
    [SerializeField] private TMP_InputField InputField_RoomCode;
    [SerializeField] private Button Button_Host;
    [SerializeField] private Button Button_Client;

    private void Start()
    {
        Button_Host.onClick.AddListener(OnClickHostButton);
        Button_Client.onClick.AddListener(OnClickClientButton);
    }

    private void OnClickHostButton()
    {
        NetCodeNetworkManager.Inst.StartAsHost();
    }

    private void OnClickClientButton()
    {
        NetCodeNetworkManager.Inst.StartAsClient();
    }

    private void SyncPlayerName()
    {
        string playerName = string.Empty;
        if(InputField_Name != null && !string.IsNullOrEmpty(InputField_Name.text))
        {
            playerName = InputField_Name.text;
        }
        else
        {
            playerName = $"Player_{Random.Range(100, 999)}";
        }

        Debug.Log($"설정된 닉네임 : {playerName}");
    }
}
