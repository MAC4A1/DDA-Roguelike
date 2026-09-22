using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConwayCube : MonoBehaviour
{
    public Material wallMaterial;          // Black
    public Material emptyMaterial;         // White
    public Material monsterMaterial;       // Red
    public Material lootMaterial;          // Green
    public Material entranceMaterial;      // Pink
    public Material exitMaterial;          // Blue
    public Material doorwayMaterial;       // Lavender
    public Material vendorMaterial;        // Yellow
    public Material hazardMaterial;        // Orange
    public Material treasureMaterial;      // Purple

    private MeshRenderer meshRenderer;
    private Grid gridModel;
    private Vector2Int gridPosition;

    public TileType CurrentTileType { get; private set; }

    public void Initialize(Grid grid, Vector2Int position)
    {
        gridModel = grid;
        gridPosition = position;
    }

    public void SetTileType(TileType tileType)
    {
        SetupMeshRenderer();

        CurrentTileType = tileType;

        switch (tileType)
        {
            case TileType.Wall:
                meshRenderer.sharedMaterial = wallMaterial;
                RemoveMonsterController();
                break;

            case TileType.Empty:
                meshRenderer.sharedMaterial = emptyMaterial;
                RemoveMonsterController();
                break;

            case TileType.Monster:
                meshRenderer.sharedMaterial = monsterMaterial;
                SetupMonsterController();
                break;

            case TileType.Loot:
                meshRenderer.sharedMaterial = lootMaterial;
                RemoveMonsterController();
                break;

            case TileType.Entrance:
                meshRenderer.sharedMaterial = entranceMaterial;
                RemoveMonsterController();
                break;

            case TileType.Exit:
                meshRenderer.sharedMaterial = exitMaterial;
                RemoveMonsterController();
                break;

            case TileType.Doorway:
                meshRenderer.sharedMaterial = doorwayMaterial;
                RemoveMonsterController();
                break;

            case TileType.Vendor:
                meshRenderer.sharedMaterial = vendorMaterial;
                RemoveMonsterController();
                break;

            case TileType.Hazard:
                meshRenderer.sharedMaterial = hazardMaterial;
                RemoveMonsterController();
                break;

            case TileType.Treasure:
                meshRenderer.sharedMaterial = treasureMaterial;
                RemoveMonsterController();
                break;
        }
    }

    private void SetupMonsterController()
    {
        MonsterController monster = GetComponent<MonsterController>();

        if (monster == null)
        {
            monster = gameObject.AddComponent<MonsterController>();
            monster.Initialize(gridModel, gridPosition, gridModel.CurrentDifficulty);
        }
    }

    private void RemoveMonsterController()
    {
        MonsterController monster = GetComponent<MonsterController>();

        if (monster != null)
        {
            Destroy(monster);
        }
    }

    private void SetupMeshRenderer()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Awake()
    {
        SetupMeshRenderer();
        SetTileType(TileType.Wall);
    }
}