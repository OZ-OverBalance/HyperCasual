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
    [SerializeField] private TextMeshProUGUI Text_RoomCode;

    private void Start()
    {
        Button_Host.onClick.AddListener(OnClickCreateButton);
        Button_Client.onClick.AddListener(OnClickJoinButton);
    }

    public async void OnClickCreateButton()
    {
        string joinCode = await NetCodeNetworkManager.Inst.StartAsHostWithRelay(4);
        if (!string.IsNullOrEmpty(joinCode))
        {
            if (Text_RoomCode != null)
            {
                Text_RoomCode.text = "Room Code : " + joinCode;
            }
        }
    }

    public async void OnClickJoinButton()
    {
        string code = InputField_RoomCode.text.Trim();
        if (!string.IsNullOrEmpty(code))
        {
           bool isSuccess = await NetCodeNetworkManager.Inst.StartAsClientWithRelay(code);

            if(isSuccess)
            {
                Text_RoomCode.text = "JoinSuccess : " + code;
            }
            else
            {
                Text_RoomCode.text = "Join Fail";
            }
        }
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
