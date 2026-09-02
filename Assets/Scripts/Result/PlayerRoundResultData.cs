using System;
using Unity.Netcode;

[Serializable]
public struct PlayerRoundResultData : INetworkSerializable
{
    public ulong ClientId;
    public int RoundIndex;
    public int RoundScore;
    public int TotalScore;

    public PlayerRoundResultData(ulong clientId, int roundIndex, int roundScore, int totalScore)
    {
        ClientId = clientId;
        RoundIndex = roundIndex;
        RoundScore = roundScore;
        TotalScore = totalScore;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref RoundIndex);
        serializer.SerializeValue(ref RoundScore);
        serializer.SerializeValue(ref TotalScore);
    }
}
