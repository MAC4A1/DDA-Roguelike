using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDistance = 1f;
    [SerializeField] public int maxHealth = 10;
    [SerializeField] private Grid gridModel;
    [SerializeField] private GameController gameController;

    [SerializeField] private GameObject pointer;
    [SerializeField] private GameObject pauseUi;

    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;

    [HideInInspector] public int goldPayout;
    [HideInInspector] public int gold;
    [HideInInspector] public float lootHealChance;

    [HideInInspector] public float goldChance, healChance;
    
    [HideInInspector] public int currentHealth;
    [HideInInspector] public int attack;

    [HideInInspector] public int kills;

    [HideInInspector] public int hazardDamage;
    [HideInInspector] public Vector2Int playerPosition;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip stepAudio;
    [SerializeField] private AudioClip floorClearAudio;
    [SerializeField] private AudioClip lootAudio;
    [SerializeField] private AudioClip playerHitAudio;
    [SerializeField] private AudioClip monsterHitAudio;
    [SerializeField] private AudioClip monsterDeathAudio;

    private bool gamePaused;
    [HideInInspector] public bool gameLoading;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("PlayerController activated");
        currentHealth = maxHealth;
        attack = 1;
        hazardDamage = 2;
        goldPayout = 50;

        lootHealChance = 0.2f;

        goldChance = 0.6f;
        healChance = 0.3f;

        goldText.text = "Score: " + gold;
        attackText.text = "Attack Power:" + attack;
        healthText.text = "Health: " + currentHealth + " / " + maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
            return;
        }
    }

    public void ApplyDDA(DDASettings settings)
    {
        hazardDamage = settings.hazardDamage;
        goldPayout = settings.lootGold;

        lootHealChance = settings.lootHealChance;
        goldChance = settings.treasureGoldChance;
        healChance = settings.treasureHealChance;
    }

    public void OnPause()
    {
        if (!gameLoading)
        {
            gamePaused = !gamePaused;

            pauseUi.SetActive(gamePaused);

            gameController.PauseGame();
        }
    }

    void OnMove(InputValue value)
    {
        if (!gamePaused)
        {
            goldText.text = "Score: " + gold;
            attackText.text = "Attack Power: " + attack;
            healthText.text = "Health: " + currentHealth + " / " + maxHealth;

            Vector2 input = value.Get<Vector2>();

            if (input == Vector2.zero) return;

            Vector3 movement = Vector3.zero;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                movement = input.x > 0 ? Vector3.right : Vector3.left;
            }
            else
            {
                movement = input.y > 0 ? Vector3.up : Vector3.down;
            }

            Vector3 targetWorldPosition =
                transform.position + movement * moveDistance;

            int targetGridX = Mathf.RoundToInt(targetWorldPosition.x);
            int targetGridY = Mathf.RoundToInt(-targetWorldPosition.y);

            //check if tile we're moving to is walkable
            if (!gridModel.IsTileWalkable(targetGridX, targetGridY))
            {
                return;
            }

            if (gridModel.IsMonster(targetGridX, targetGridY))
            {
                if (gridModel.GetMonsterAt(targetGridX, targetGridY, out MonsterController monster))
                {
                    bool monsterDefeated = monster.OnAttack(this);

                    audioSource.PlayOneShot(monsterHitAudio);

                    healthText.text = "Health: " + currentHealth + " / " + maxHealth;

                    gameController.RenderMap();

                    if (monsterDefeated)
                    {
                        kills++;
                        audioSource.PlayOneShot(monsterDeathAudio);
                        Debug.Log("The monster dropped loot.");
                    }
                }
                else
                {
                    Debug.LogError($"Tile ({targetGridX}, {targetGridY}) is marked as a monster, but no MonsterController exists on its grid object.");
                }

                return;
            }

            if (gridModel.IsHazard(targetGridX, targetGridY))
            {
                TakeDamage(hazardDamage);
                audioSource.PlayOneShot(playerHitAudio);
                gridModel.SetTileTypeAt(targetGridX, targetGridY, TileType.Empty);
                gameController.RenderMap();
                return;
            }

            if (gridModel.IsTileExit(targetGridX, targetGridY))
            {
                audioSource.PlayOneShot(floorClearAudio);
                gameController.NextLevel();
                gameLoading = true;
                return;
            }

            if (gridModel.IsLoot(targetGridX, targetGridY))
            {
                audioSource.PlayOneShot(lootAudio);
                float random = Random.value;
                gold += goldPayout;
                if (random < lootHealChance && currentHealth != maxHealth) //monster had a healing potion
                {
                    Debug.Log("You found a healing potion among the dropped loot");
                    currentHealth += 1;
                    healthText.text = "Health: " + currentHealth + " / " + maxHealth;
                }
                gridModel.SetTileTypeAt(targetGridX, targetGridY, TileType.Empty);
                gameController.RenderMap();
                goldText.text = "Score: " + gold;
            }

            if (gridModel.IsTreasure(targetGridX, targetGridY))
            {
                audioSource.PlayOneShot(lootAudio);
                float random = Random.value;
                if (random < goldChance) //player found gold
                {
                    Debug.Log("The chest had gold inside it.");
                    gold += goldPayout * 3;
                    goldText.text = "Score: " + gold;
                }
                else if (random >= goldChance && random < goldChance + healChance) //player found a healing potion
                {
                    Debug.Log("The chest had a healing potion inside it.");
                    if (currentHealth + 5 < maxHealth) currentHealth += 5;
                    else if (currentHealth == maxHealth) { gold += goldPayout * 3; goldText.text = "Score: " + gold; }
                    else currentHealth = maxHealth; healthText.text = "Health: " + currentHealth + " / " + maxHealth;
                }
                else //player found a weapon upgrade
                {
                    if (Random.value < 0.5)
                    {
                        Debug.Log("The chest had a stronger weapon inside it.");
                        attack += 1;
                        attackText.text = "Attack Power: " + attack;
                    }
                    else
                    {
                        Debug.Log("The chest had a stronger piece of armor inside it.");
                        maxHealth += 2; currentHealth += 2;
                        healthText.text = "Health: " + currentHealth + " / " + maxHealth;
                    }
                }
                gridModel.SetTileTypeAt(targetGridX, targetGridY, TileType.Empty);
                gameController.RenderMap();
            }

            transform.position += movement * moveDistance;
            audioSource.PlayOneShot(stepAudio);
            playerPosition = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(-transform.position.y)
            );

            if (movement.x < 0)
            {
                playerPosition.x--;
            }
            else if (movement.x > 0)
            {
                playerPosition.x++;
            }
            if (movement.y < 0)
            {
                playerPosition.y++;
            }
            else if (movement.y > 0)
            {
                playerPosition.y--;
            }

            Vector2 gridDir =
                (Vector2)gridModel.ExitPosition -
                (Vector2)playerPosition;

            Vector2 pointerDir = new Vector2(
                gridDir.x,
                -gridDir.y
            );
            Vector2 pointerAngle = pointerDir.normalized;

            Debug.DrawLine(new Vector3(playerPosition.x, playerPosition.y, 0), new Vector3(pointerAngle.x, pointerAngle.y, 0), Color.blue, 5f);

            Debug.DrawLine(
                new Vector3(playerPosition.x, playerPosition.y, 0),
                new Vector3(playerPosition.x + pointerDir.x, playerPosition.y + pointerDir.y, 0),
                Color.magenta, 5f);

            float angle =
            Vector2.SignedAngle(Vector2.up, pointerDir.normalized);

            pointer.transform.localRotation =
                Quaternion.Euler(0f, 0f, angle);
        }
    }

    void Die()
    {
        Debug.Log("Player has died.");
        currentHealth = 0;
        gameController.EndGame();
    }
}
