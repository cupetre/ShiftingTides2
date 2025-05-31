using UnityEngine;
using System;
using Unity.Netcode;

[System.Serializable]
public class HiddenCard : INetworkSerializable
{
    [System.Serializable]
    public class Effect : INetworkSerializable
    {
        public int selfMoney = -1;
        public int selfPeople = -1;
        public int selfInfluence = -1;
        public int othersMoney = -1;
        public int othersPeople = -1;
        public int othersInfluence = -1;

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
    public enum TargetType { Self, Opponents }

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
    // Required fields
    public int id;
    public string type;
    public string title;
    public string description;

    // Resource requirements
    public Effect effect = new Effect();
    public int counts = 0;

}