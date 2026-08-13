using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct NetCodeNetworkPlayerData : INetworkSerializable, IEquatable<NetCodeNetworkPlayerData>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public bool IsReady;                 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(NetCodeNetworkPlayerData other)
    {
        return ClientId == other.ClientId &&
            PlayerName == other.PlayerName &&
               IsReady == other.IsReady;
    }

    public override bool Equals(object obj)
    {
        return obj is NetCodeNetworkPlayerData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, PlayerName, IsReady);
    }
}
