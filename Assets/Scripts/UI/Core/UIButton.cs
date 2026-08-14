using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIButton : MonoBehaviour
{
    [SerializeField] private Button Button_Target;

    private event Action _onClickButton;
    private bool _isInitialized;

    public bool IsInteractable => Button_Target != null && Button_Target.interactable;

    private void Awake()
    {
        InitializeButton();
    }

    private void OnDestroy()
    {
        ReleaseButton();
    }

    // 버튼 클릭 이벤트 연결 (동일한 이벤트가 중복 등록되지 않도록 처리)
    public void BindOnClickButtonEvent(Action onClickButton)
    {
        if (onClickButton == null)
        {
            return;
        }

        _onClickButton -= onClickButton;
        _onClickButton += onClickButton;
    }

    // 연결된 버튼 클릭 이벤트 해제
    public void UnbindOnClickButtonEvent(Action onClickButton)
    {
        if (onClickButton == null)
        {
            return;
        }

        _onClickButton -= onClickButton;
    }

    // 등록된 모든 버튼 클릭 이벤트 해제 (UI 제거될 때 사용)
    public void UnbindAllOnClickButtonEvents()
    {
        _onClickButton = null;
    }

    // 버튼 입력 가능 여부 변경
    public void SetInteractable(bool isInteractable)
    {
        if (Button_Target == null)
        {
            return;
        }

        Button_Target.interactable = isInteractable;
    }

    private void InitializeButton()
    {
        if (_isInitialized)
        {
            return;
        }

        if (Button_Target == null)
        {
            Debug.LogError($"UIButton - {name} Button 이 연결되지 않음");
            return;
        }

        Button_Target.onClick.AddListener(HandleClickButton);
        _isInitialized = true;
    }

    private void HandleClickButton()
    {
        if (!IsInteractable)
        {
            return;
        }

        _onClickButton?.Invoke();
    }

    private void ReleaseButton()
    {
        if (!_isInitialized)
        {
            return;
        }

        Button_Target.onClick.RemoveListener(HandleClickButton);
        UnbindAllOnClickButtonEvents();

        _isInitialized = false;
    }
}