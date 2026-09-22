using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System.Runtime.InteropServices;

public class DataLogger : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DownloadCSV(
            string filename,
            string content
        );
#endif

    public string CreatePerformanceCSV(List<PlayerPerformanceData> performanceHistory)
    {
        StringBuilder csvContent = new StringBuilder(
            "Floor Reached,Current Player Health,Max Player Health," +
            "Player Attack,Player Gold,Time Spent This Floor," +
            "Total Time Spent,Monsters Killed\n"
        );

        foreach (PlayerPerformanceData value in performanceHistory)
        {
            csvContent.AppendLine(string.Join(",",
                value.floor,
                value.currentHealth,
                value.maxHealth,
                value.attack,
                value.gold,
                value.floorCompletionTime.ToString(
                    CultureInfo.InvariantCulture),
                value.totalTime.ToString(
                    CultureInfo.InvariantCulture),
                value.monstersKilled
            ));
        }

        return csvContent.ToString();
    }

    public void DownloadPerformanceCSV(
    List<PlayerPerformanceData> performanceHistory,
    string fileName)
    {
        string csv = CreatePerformanceCSV(performanceHistory);

#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadCSV(fileName, csv);
#else
        string path = Path.Combine(
            Application.persistentDataPath,
            fileName
        );

        File.WriteAllText(path, csv);
#endif
    }

    public string CreateDDACSV(List<DDAData> ddaHistory)
    {
        StringBuilder csvContent = new StringBuilder("Floor Reached,Monster Health,Monster Attack,Monster Spawn Rate,Loot Payout,Loot Heal Chance,Gold Reward from Treasure Chance,Heal from Treasure Chance,Health/Damage Upgrade from Treasure Chance,Hazard Damage,Hazard Spawn Rate\n");

        foreach (DDAData value in ddaHistory)
        {
            csvContent.AppendLine(string.Join(",",
                value.floor,
                value.settings.monsterHP,
                value.settings.monsterAttack,
                value.settings.monsterSpawnRate.ToString(CultureInfo.InvariantCulture),
                value.settings.lootGold,
                value.settings.lootHealChance.ToString(CultureInfo.InvariantCulture),
                value.settings.treasureGoldChance.ToString(CultureInfo.InvariantCulture),
                value.settings.treasureHealChance.ToString(CultureInfo.InvariantCulture),
                value.settings.treasureUpgradeChance.ToString(CultureInfo.InvariantCulture),
                value.settings.hazardDamage,
                value.settings.hazardSpawnRate.ToString(CultureInfo.InvariantCulture)
            ));
        }

        return csvContent.ToString();
    }

    public void DownloadDDACSV(List<DDAData> ddaHistory, string fileName)
    {
        string csv = CreateDDACSV(ddaHistory);

#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadCSV(fileName, csv);
#else
        string path = Path.Combine(
            Application.persistentDataPath,
            fileName
        );

        File.WriteAllText(path, csv);
#endif
    }

    public void ExportPerformanceToCSV(List<PlayerPerformanceData> performanceHistory, string filePath)
    {
        StringBuilder csvContent = new StringBuilder("Floor Reached,Current Player Health,Max Player Health,Player Attack,Player Gold,Time Spent This Floor,Total Time Spent,Monsters Killed\n");
        foreach (PlayerPerformanceData value in performanceHistory)
        {
            csvContent.AppendLine(string.Join(",", value.floor, value.currentHealth, value.maxHealth, value.attack, value.gold, value.floorCompletionTime, value.totalTime, value.monstersKilled));
        }
        File.WriteAllText(filePath, csvContent.ToString());
    }

    public void ExportDDAToCSV(List<DDAData> ddaData, string filePath)
    {
        StringBuilder csvContent = new StringBuilder("Floor Reached,Monster Health,Monster Attack,Monster Spawn Rate,Loot Payout,Loot Heal Chance,Gold Reward from Treasure Chance,Heal from Treasure Chance,Health/Damage Upgrade from Treasure Chance,Hazard Damage,Hazard Spawn Rate\n");
        foreach (DDAData value in ddaData)
        {
            //find a way to log floor number here too
            csvContent.AppendLine(string.Join(",", value.floor, value.settings.monsterHP, value.settings.monsterAttack, value.settings.monsterSpawnRate.ToString(CultureInfo.InvariantCulture), value.settings.lootGold, value.settings.lootHealChance.ToString(CultureInfo.InvariantCulture), value.settings.treasureGoldChance.ToString(CultureInfo.InvariantCulture), value.settings.treasureHealChance.ToString(CultureInfo.InvariantCulture), value.settings.treasureUpgradeChance.ToString(CultureInfo.InvariantCulture), value.settings.hazardDamage, value.settings.hazardSpawnRate.ToString(CultureInfo.InvariantCulture)));
        }
        File.WriteAllText(filePath, csvContent.ToString());
    }

}
