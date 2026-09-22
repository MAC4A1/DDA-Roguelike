using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [HideInInspector] public int monsterHealth;
    [HideInInspector] public int monsterAttack;

    private Grid gridModel;
    private Vector2Int gridPosition;
    private bool initialized;

    public void Initialize(Grid grid, Vector2Int position, DDASettings dda)
    {
        gridModel = grid;
        gridPosition = position;

        if (initialized) return;

        monsterHealth = dda.monsterHP;

        monsterAttack = dda.monsterAttack;

        initialized = true;
    }

    public bool OnAttack(PlayerController player)
    {
        monsterHealth -= player.attack;

        if (monsterHealth <= 0)
        {
            Die();
            return true;
        }

        player.TakeDamage(monsterAttack);
        return false;
    }

    private void Die()
    {
        Debug.Log($"Monster at {gridPosition} was defeated.");

        gridModel.SetTileTypeAt(
            gridPosition.x,
            gridPosition.y,
            TileType.Loot
        );
    }
}
