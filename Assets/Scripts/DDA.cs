using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class DDA : MonoBehaviour
{
    private string openAIApiKey = "REDACTED";
    private const string chatEndpoint = "https://api.openai.com/v1/chat/completions";

    private string systemPrompt = "In order to implement a Dynamic Difficulty Adjustment for a Roguelike game, I will be giving you a set of values that represent player performance and player stats during a roguelike game. Using these variables, I want you to recommend a difficulty level that will make the game slightly more challenging to the player given their performance. This difficulty level should impact the different aspects which control the difficulty of the game, in terms of enemy stats, treasure rewards, and spawn probabilites of special tiles (treasures, hazards, monsters, etc). The enemy stats are the enemy hitpoints (minimum of 1, maximum of 20), attack damage (minimum of 1, maximum of 5) and spawning probability (float value between 0 and 1). Below is the inputs that will be provided to gauge the player’s performance:\n" +
            "Total time: the amount of time currently invested into the match;\n" +
            "Player stats: the current stats of the player. These are player health, player attack, current gold and current level;\n" +
            "Current difficulty level: In terms of the special tile stats and spawn rates.\n\n" +
            "With these in mind, when prompted with player performance and stats, please return a new difficulty level you think would make the game slightly more challenging, keeping a balance between the difficulty and the player skill. Respond with the following syntax and nothing else:\n" +
            "Monster HP: <number>\n" +
            "Monster Damage: <number>\n" +
            "Monster Spawn Rate: <number>\n" +
            "Loot Gold Payout: <number>\n" +
            "Loot Healing Chance: <number>\n" +
            "Treasure Gold Chance: <number>\n" +
            "Treasure Heal Chance: <number>\n" +
            "Treasure Weapon Upgrade Chance: <number>\n" +
            "Hazard Damage: <number>\n" +
            "Hazard Spawn Rate: <number>\n";

    public IEnumerator PostRequest(string prompt)
    {
        string jsonData = "{\"prompt\": \"" + prompt + "\", \"max_tokens\": 5}";

        var request = new UnityWebRequest(chatEndpoint, "POST");
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            Debug.Log(request.downloadHandler.text);
        }
    }

    private DDASettings ParseReply(string reply)
    {
        DDASettings settings = DDASettings.CreateDefault();

        foreach (string rawLine in reply.Split('\n'))
        {
            string line = rawLine.Trim();

            string[] parts = line.Split(new[] { ':' }, 2);

            if (parts.Length != 2)
                continue;

            string name = parts[0].Trim();
            string value = parts[1].Trim();

            switch (name)
            {
                case "Monster HP":
                    int.TryParse(value, out settings.monsterHP);
                    break;

                case "Monster Damage":
                    int.TryParse(value, out settings.monsterAttack);
                    break;

                case "Monster Spawn Rate":
                    float.TryParse(value, out settings.monsterSpawnRate);
                    break;

                case "Loot Gold Payout":
                    int.TryParse(value, out settings.lootGold);
                    break;

                case "Loot Healing Chance":
                    float.TryParse(value, out settings.lootHealChance);
                    break;

                case "Treasure Gold Chance":
                    float.TryParse(value, out settings.treasureGoldChance);
                    break;

                case "Treasure Heal Chance":
                    float.TryParse(value, out settings.treasureHealChance);
                    break;

                case "Treasure Weapon Upgrade Chance":
                    float.TryParse(
                        value,
                        out settings.treasureUpgradeChance
                    );
                    break;

                case "Hazard Damage":
                    int.TryParse(value, out settings.hazardDamage);
                    break;

                case "Hazard Spawn Rate":
                    float.TryParse(value, out settings.hazardSpawnRate);
                    break;
            }
        }

        settings.monsterHP = Mathf.Clamp(settings.monsterHP, 1, 20);

        settings.monsterAttack = Mathf.Clamp(settings.monsterAttack, 1, 5);

        settings.monsterSpawnRate = Mathf.Clamp01(settings.monsterSpawnRate);

        settings.lootGold = Mathf.Clamp(settings.lootGold, 10, 100);

        settings.lootHealChance = Mathf.Clamp01(settings.lootHealChance);

        settings.treasureGoldChance = Mathf.Clamp01(settings.treasureGoldChance);

        settings.treasureHealChance = Mathf.Clamp01(settings.treasureHealChance);

        settings.treasureUpgradeChance = Mathf.Clamp01(settings.treasureUpgradeChance);

        settings.hazardDamage = Mathf.Clamp(settings.hazardDamage, 1, 5);

        settings.hazardSpawnRate = Mathf.Clamp01(settings.hazardSpawnRate);

        float total =
        settings.treasureGoldChance +
        settings.treasureHealChance +
        settings.treasureUpgradeChance;

        if (total <= 0f)
        {
            settings.treasureGoldChance = 0.6f;
            settings.treasureHealChance = 0.3f;
            settings.treasureUpgradeChance = 0.1f;
            return settings;
        }

        settings.treasureGoldChance /= total;
        settings.treasureHealChance /= total;
        settings.treasureUpgradeChance /= total;

        return settings;
    }

    private int Clamp(int value, int min, int max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    public Task<DDASettings> RequestDifficulty(float totalTime,
        int health,
        int maxHealth,
        int playerAttack,
        int gold,
        int floor,
        DDASettings current)

    {
        Debug.Log("Request initiated.");

        float healthPercentage = maxHealth > 0 ? health / (float)maxHealth * 100f : 0f;

        string userPrompt =
        $"Total time: {totalTime:F1} seconds\n" +
        $"Current health: {health}/{maxHealth} ({healthPercentage:F1}%)\n" +
        $"Player attack: {playerAttack}\n" +
        $"Gold obtained: {gold}\n" +
        $"Current floor: {floor}\n" +
        $"Current monster HP: {current.monsterHP}\n" +
        $"Current monster damage: {current.monsterAttack}\n" +
        $"Current monster spawn rate: {current.monsterSpawnRate:F2}\n" +
        $"Current hazard damage: {current.hazardDamage}\n" +
        $"Current hazard spawn rate: {current.hazardSpawnRate:F2}";

        return SendRequestAsync(systemPrompt, userPrompt);
    }

    public async Task<DDASettings> SendRequestAsync(string systemPrompt, string userPrompt)
    {
        //function to send prompt to LLM

        //get details of LLM
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt }
            },
            max_tokens = 400,
            temperature = 0.3
        };

        string jsonData = JsonConvert.SerializeObject(payload);

        using (var client = new HttpClient())
        {
            //establish connection using API key
            client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAIApiKey);

            using (var content = new StringContent(jsonData, Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage response = await client.PostAsync(chatEndpoint, content);
                string responseText = await response.Content.ReadAsStringAsync();

                Debug.Log(responseText);

                if (!response.IsSuccessStatusCode)
                {
                    //if an error occurs, print it out and return default values
                    Console.WriteLine("Error calling OpenAI: " + responseText);
                    return DDASettings.CreateDefault();
                    //return ParseReply(5, 1, 0.25f, 50, 0.2f, 0.6f, 0.3f, 0.1f, 2, 0.15f);
                }

                JObject responseJson = JObject.Parse(responseText);
                string reply = responseJson["choices"][0]["message"]["content"].ToString();

                return ParseReply(reply);
            }
        }

    }
}