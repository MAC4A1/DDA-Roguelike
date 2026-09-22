using System;

[Serializable]
public class DDASettings
{
    public int monsterHP;
    public int monsterAttack;
    public float monsterSpawnRate;

    public int lootGold;
    public float lootHealChance;

    public float treasureGoldChance;
    public float treasureHealChance;
    public float treasureUpgradeChance;

    public int hazardDamage;
    public float hazardSpawnRate;

    public static DDASettings CreateDefault()
    {
        DDASettings standard = new DDASettings();
        standard.monsterHP = 3;
        standard.monsterAttack = 1;
        standard.monsterSpawnRate = 0.1f;
        standard.lootGold = 50;
        standard.lootHealChance = 0.2f;
        standard.treasureGoldChance = 0.6f;
        standard.treasureHealChance = 0.3f;
        standard.treasureUpgradeChance = 0.1f;
        standard.hazardDamage = 2;
        standard.hazardSpawnRate = 0.15f;
        return standard;
    }
}
