using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class UIConfig
{
    [FormerlySerializedAs("UiType")]
    [SerializeField] private UIType _uiType;

    [FormerlySerializedAs("Address")]
    [SerializeField] private string _address;

    public UIType UIType => _uiType;
    public string Address => _address;
}