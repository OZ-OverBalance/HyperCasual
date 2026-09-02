using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoundScoreBlock : MonoBehaviour
{
    [SerializeField] private Image Image_Background;
    [SerializeField] private TMP_Text Text_Score;
    [SerializeField] private LayoutElement LayoutElement_Block;
    [SerializeField] private float _baseWidth = 40f;

    public void Refresh(int score, Color color)
    {
        if (score <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        if (Image_Background != null)
        {
            Image_Background.color = color;
        }

        if (Text_Score != null)
        {
            Text_Score.text = $"+{score}";
        }

        if (LayoutElement_Block != null)
        {
            float blockWidth = _baseWidth * score;

            LayoutElement_Block.minWidth = blockWidth;
            LayoutElement_Block.preferredWidth = blockWidth;
            LayoutElement_Block.flexibleWidth = 0f;
        }
    }
}