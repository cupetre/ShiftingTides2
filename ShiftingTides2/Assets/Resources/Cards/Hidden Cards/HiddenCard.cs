using UnityEngine;
using System;

[System.Serializable]
public class HiddenCard 
{
    [System.Serializable]
    public class Effect
    {
        public class SelfEffect
        {
            public int money = 0;
            public int influence = 0;
            public int people = 0;
        }

        public class OthersEffect
        {
            public int money = 0;
            public int influence = 0;
            public int people = 0;
        }
        public SelfEffect selfEffect = new SelfEffect();
        public OthersEffect othersEffect = new OthersEffect();
    }

    public class Votes
        {
            public string type;
            public int count;
        }

    public enum TargetType { Self, Opponents }

    // Required fields
    public int id;
    public string type;
    public string title;
    public string description;

    // Resource requirements
    public Effect effect = new Effect();
    public Votes votes = new Votes();

}