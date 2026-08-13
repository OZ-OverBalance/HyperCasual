using Unity.Netcode.Components;
using UnityEngine;

public class NetCodeClientNetTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
