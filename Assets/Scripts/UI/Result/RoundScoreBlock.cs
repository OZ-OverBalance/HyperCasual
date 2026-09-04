using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoundScoreBlock : MonoBehaviour
{
    [SerializeField] private Image Image_Background;
    [SerializeField] private TMP_Text Text_Score;
    [SerializeField] private LayoutElement LayoutElement_Block;
    [SerializeField] private float _baseWidth = 40f;

    public void Refresh(int score, Color blockColor, bool shouldShowScore)
    {
        gameObject.SetActive(score > 0);

        if (score <= 0)
        {
            return;
        }

        Image_Background.color = blockColor;
        Text_Score.text = shouldShowScore ? $"+{score}" : string.Empty;

        float blockWidth = score * _baseWidth;
        LayoutElement_Block.preferredWidth = blockWidth;
    }
}