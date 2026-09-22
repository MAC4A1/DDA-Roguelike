using System;

[System.Serializable]
public class PlayerPerformanceData
{
    public int floor;
    public int currentHealth;
    public int maxHealth;
    public int attack;
    public int gold;
    public float floorCompletionTime;
    public float totalTime;
    public int monstersKilled;

    public PlayerPerformanceData(int floor, int currentHealth, int maxHealth, int attack, int gold, float floorCompletionTime, float totalTime, int monstersKilled)
    {
        this.floor = floor;
        this.currentHealth = currentHealth;
        this.maxHealth = maxHealth;
        this.attack = attack;
        this.gold = gold;
        this.floorCompletionTime = floorCompletionTime;
        this.totalTime = totalTime;
        this.monstersKilled = monstersKilled;
    }
}

public class DDAData
{
    public int floor;
    public DDASettings settings;

    public DDAData (int floor, DDASettings settings)
    {
        this.floor = floor;
        this.settings = settings;
    }
}