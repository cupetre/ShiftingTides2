using UnityEngine;
using System;
using Unity.Netcode;

[System.Serializable]
public class Trade : INetworkSerializable
{
    // Required fields
    public int id;
    public string type;
    public string title;
    public string description;
    public Effect effect = new Effect();

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref title);
        serializer.SerializeValue(ref description);

        if (effect == null)
        {
            effect = new Effect();
        }

        effect.NetworkSerialize(serializer);
    }
}

[System.Serializable]
public class Effect : INetworkSerializable
    {
    public int selfMoney = 0;
    public int selfPeople = 0;
    public int selfInfluence = 0;
    public int othersMoney = 0;
    public int othersPeople = 0;
    public int othersInfluence = 0;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref selfMoney);
        serializer.SerializeValue(ref selfPeople);
        serializer.SerializeValue(ref selfInfluence);
        serializer.SerializeValue(ref othersMoney);
        serializer.SerializeValue(ref othersPeople);
        serializer.SerializeValue(ref othersInfluence);
    }
}