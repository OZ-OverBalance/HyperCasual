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
    [SerializeField] private Transform Transform_PlayerListContainer;
    [SerializeField] private GameObject GameObject_PlayerData;

    public static string LocalPlayerInputName;


    private void Start()
    {
        Button_Host.onClick.AddListener(OnClickCreateButton);
        Button_Client.onClick.AddListener(OnClickJoinButton);
    }

    public async void OnClickCreateButton()
    {
        LocalPlayerInputName = GetPlayerName();
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
        LocalPlayerInputName = GetPlayerName();
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
    private string GetPlayerName()
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
        return playerName;
    }
    private void OnListChanged(NetworkListEvent<NetCodeNetworkPlayerData> changeEvent)
    {
        RefreshUI();
    }
    public void OnClientConnected()
    {
        if(NetCodeRoomManager.Instance != null && NetCodeRoomManager.Instance.PlayerList != null)
        {
            NetCodeRoomManager.Instance.PlayerList.OnListChanged += OnListChanged;
            RefreshUI();
        }
    }

    public void OnClientDisconnected()
    {
        if (NetCodeRoomManager.Instance != null && NetCodeRoomManager.Instance.PlayerList != null)
        {
            NetCodeRoomManager.Instance.PlayerList.OnListChanged -= OnListChanged;
        }
    }

    public void RefreshUI()
    {
        if (Transform_PlayerListContainer == null || GameObject_PlayerData == null) return;

        foreach (Transform child in Transform_PlayerListContainer)
        {
            Destroy(child.gameObject);
        }

        if (NetCodeRoomManager.Instance == null || NetCodeRoomManager.Instance.PlayerList == null) return;

        foreach (var playerData in NetCodeRoomManager.Instance.PlayerList)
        {
            GameObject slotObj = Instantiate(GameObject_PlayerData, Transform_PlayerListContainer);

            var textComp = slotObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComp != null)
            {
                if(playerData.ClientId == 0)
                {
                    textComp.text = playerData.PlayerName.ToString() + " (HOST)";
                }
                else
                {
                    textComp.text = playerData.PlayerName.ToString();
                }
            }
        }
    }
}
