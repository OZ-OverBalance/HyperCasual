using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoundScoreBlock : MonoBehaviour
{
    [SerializeField] private Image Image_Background;
    [SerializeField] private TMP_Text Text_Score;
    [SerializeField] private LayoutElement LayoutElement_Block;
    [SerializeField] private float _baseWidth = 60f;

    public void Refresh(int score, Color color)
    {
        if (Image_Background != null)
        {
            Image_Background.color = score > 0 ? color : new Color(0.35f, 0.35f, 0.35f, 0.7f);
        }

        if (Text_Score != null)
        {
            Text_Score.text = score > 0 ? $"+{score}" : "0";
        }

        if (LayoutElement_Block != null)
        {
            LayoutElement_Block.preferredWidth = _baseWidth * Mathf.Max(1, score);
        }
    }
}
