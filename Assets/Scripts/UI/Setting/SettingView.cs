using UnityEngine;
using UnityEngine.UI;

public class SettingView : UIBase
{
    [SerializeField] private UIButton Button_Cancel;
    [SerializeField] private Slider Slider_Bgm;
    [SerializeField] private Slider Slider_Sfx;

    public override UILayer Layer => UILayer.Popup;

    protected override void RefreshUI()
    {
        base.RefreshUI();

        if (SoundManager.Inst != null)
        {
            Slider_Bgm.SetValueWithoutNotify(SoundManager.Inst.BgmVolume);
            Slider_Sfx.SetValueWithoutNotify(SoundManager.Inst.SfxVolume);
        }
    }

    private void OnDisable()
    {
        Slider_Bgm.onValueChanged.RemoveListener(OnChangedBgmVolume);
        Slider_Sfx.onValueChanged.RemoveListener(OnChangedSfxVolume);
    }
    protected override void BindEvents()
    {
        base.BindEvents();

        Button_Cancel.BindOnClickButtonEvent(HandleClickCancelButton);

        Slider_Bgm.onValueChanged.AddListener(OnChangedBgmVolume);
        Slider_Sfx.onValueChanged.AddListener(OnChangedSfxVolume);
    }

    protected override void UnbindEvents()
    {
        base.UnbindEvents();

        Button_Cancel.UnbindOnClickButtonEvent(HandleClickCancelButton);

        Slider_Bgm.onValueChanged.RemoveListener(OnChangedBgmVolume);
        Slider_Sfx.onValueChanged.RemoveListener(OnChangedSfxVolume);
    }

    private void HandleClickCancelButton()
    {
        Close();
    }

    private void OnChangedBgmVolume(float value)
    {
        if (SoundManager.Inst != null)
        {
            SoundManager.Inst.SetBgmVolume(value);
        }
    }

    private void OnChangedSfxVolume(float value)
    {
        if (SoundManager.Inst != null)
        {
            SoundManager.Inst.SetSfxVolume(value);
        }
    }
}
