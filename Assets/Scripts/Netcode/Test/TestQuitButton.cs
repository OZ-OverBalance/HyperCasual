using Unity.Netcode;
using UnityEngine;

public class TestQuitButton : MonoBehaviour
{
    [SerializeField] private UIButton Button_Quit;

    private void OnEnable()
    {
        Button_Quit.BindOnClickButtonEvent(QuitGame);
    }

    private void QuitGame()
    {

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void NoneCode()
    {

    }
}
