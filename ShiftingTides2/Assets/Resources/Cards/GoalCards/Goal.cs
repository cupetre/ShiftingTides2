using UnityEngine;

[System.Serializable]
public class Goal
{
    [System.Serializable]
    public class Resources
    {
        public int money = 0;
        public int influence = 0;
        public int people = 0;
    }

    public enum TargetType { Self, Opponents }

    // Required fields
    public int id;
    public string type;
    public string title;
    public string description;
    
    // Resource requirements
    public Resources resources = new Resources();
    
    // Gameplay parameters
    public int rounds = -1; // -1 = not applicable
    public string target = "self"; // "self" or "opponents"
    
    [System.NonSerialized]
    public bool used = false;

    // Helper property
    public TargetType Target
    {
        get => target.ToLower() == "opponents" ? TargetType.Opponents : TargetType.Self;
    }
}