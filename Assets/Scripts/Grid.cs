//Grid.cs
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Grid : MonoBehaviour
{
    [Header("Cube prefab")]
    public GameObject conwayCube;

    [Header("Grid settings")]
    public int gridWidth;
    public int gridHeight;

    [Header("Simulation settings")]
    public bool autoProgress;
    [Range(0.01f, 5f)]
    public float autoProgressionTime = 1;
    private float deltaTime;
    public bool useRandomStart;
    [Range(0f, 1f)]
    public float initialWallChance = 0.45f;
    public float minimumEntranceExitDistance = 10f;

    private TileType[,] grid;
    private TileType[,] nextGrid;
    private GameObject[,] cubes;

    private bool modified = true;
    public bool Modified { get { return modified; } }

    public MapView mapView;
    public BackgroundView backgroundView;

    private float[] probabilities = new float[10000];
    private List<Vector2Int> doorways = new List<Vector2Int>();
    private int nextProbability = 0; //pointer to the next probability to use in determining monster/hazard/vendor

    private Vector2Int entrancePosition;
    private Vector2Int exitPosition;

    public Vector2Int EntrancePosition { get { return entrancePosition; } }
    public Vector2Int ExitPosition { get { return exitPosition; } }

    public float hazardChance = 0.15f;
    public float monsterChance = 0.1f;

    private DDASettings currentDifficulty = DDASettings.CreateDefault();

    public DDASettings CurrentDifficulty => currentDifficulty;

    [SerializeField]
    [Min(0)]
    private int minimumEntranceBorderDistance = 3;

    private int hazardCount;

    //	Check if a given point is within the bounds of the grid
    private bool IsPointInBounds(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    private bool IsPointInBounds(GridItem gridItem)
    {
        return IsPointInBounds(gridItem.x, gridItem.y);
    }

    private bool IsWalkableTile(TileType tileType)
    {
        return tileType != TileType.Wall;// || tileType != TileType.Monster || tileType != TileType.Hazard;
    }

    private bool IsPathWalkableTile(TileType tileType)
    {
        return tileType != TileType.Wall && tileType != TileType.Monster && tileType != TileType.Hazard;
    }

    private bool IsSpecialTile(TileType tileType)
    {
        return tileType == TileType.Monster ||
               tileType == TileType.Vendor ||
               tileType == TileType.Hazard ||
               tileType == TileType.Treasure ||
               tileType == TileType.Loot ||
               tileType == TileType.Entrance ||
               tileType == TileType.Exit;
    }

    public TileType GetTileTypeAt(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
        {
            return TileType.Wall;
        }

        return grid[x, y];
    }

    public void SetTileTypeAt(int x, int y, TileType newTileType)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            return;

        grid[x, y] = newTileType;
        nextGrid[x, y] = newTileType;

        RenderGrid();
    }

    public bool IsTileWalkable(int x, int y)
    {
        TileType tileType = GetTileTypeAt(x, y);

        return tileType != TileType.Wall;
    }

    public bool IsTileExit(int x, int y)
    {
        return GetTileTypeAt(x, y) == TileType.Exit;
    }

    public bool IsHazard(int x, int y)
    {
        return GetTileTypeAt(x, y) == TileType.Hazard;
    }

    public bool IsTreasure(int x, int y)
    {
        return GetTileTypeAt(x, y) == TileType.Treasure;
    }

    public bool IsMonster(int x, int y)
    {
        return GetTileTypeAt(x, y) == TileType.Monster;
    }

    public bool IsLoot(int x, int y)
    {
        return GetTileTypeAt(x, y) == TileType.Loot;
    }

    public bool GetMonsterAt(int x, int y, out MonsterController monster)
    {
        monster = null;

        if (x < 0 || x >= gridWidth ||
            y < 0 || y >= gridHeight)
        {
            return false;
        }

        GameObject cube = cubes[x, y];

        if (cube == null)
            return false;

        return cube.TryGetComponent(out monster);
    }

    //	Get number of neighbors that are alive
    private int GetWalkableNeighborsCount(int x, int y)
    {
        List<GridItem> neighbors = GetNeighborsAt(x, y);
        int count = 0;
        foreach (GridItem neighbor in neighbors)
        {
            TileType neighborType = grid[neighbor.x, neighbor.y];

            if (IsWalkableTile(neighborType) && !IsSpecialTile(neighborType))
            {
                count++;
            }
        }

        return count;
    }

    //	Get neighbors at a specific (x,y) point
    private List<GridItem> GetNeighborsAt(int x, int y)
    {
        List<GridItem> neighbors = new List<GridItem>();

        if (!IsPointInBounds(x, y))
        {
            return neighbors;
        }

        for (int xCheck = x - 1; xCheck <= x + 1; xCheck++)
        {
            for (int yCheck = y - 1; yCheck <= y + 1; yCheck++)
            {
                bool isCurrentCell = xCheck == x && yCheck == y;

                if (!isCurrentCell && IsPointInBounds(xCheck, yCheck))
                {
                    neighbors.Add(new GridItem(xCheck, yCheck));
                }
            }
        }

        return neighbors;
    }

    public void ApplyDDA(DDASettings settings)
    {
        currentDifficulty = settings;

        hazardChance = settings.hazardSpawnRate;
        monsterChance = settings.monsterSpawnRate;
        float combinedChance = monsterChance + hazardChance;

        if (combinedChance > 1f)
        {
            monsterChance /= combinedChance;
            hazardChance /= combinedChance;
        }
    }

    private TileType GetDominantNeighborTileType(int x, int y)
    {
        int emptyCount = 0;
        int monsterCount = 0;
        int lootCount = 0;
        int exitCount = 0;
        int entranceCount = 0;
        int doorwayCount = 0;
        int vendorCount = 0;
        int hazardCount = 0;
        int treasureCount = 0;

        List<GridItem> neighbors = GetNeighborsAt(x, y);

        foreach (GridItem neighbor in neighbors)
        {
            TileType neighborType = grid[neighbor.x, neighbor.y];

            switch (neighborType)
            {
                case TileType.Empty:
                    emptyCount++;
                    break;

                case TileType.Monster:
                    monsterCount++;
                    break;

                case TileType.Loot:
                    lootCount++;
                    break;

                case TileType.Exit:
                    exitCount++;
                    break;

                case TileType.Entrance:
                    entranceCount++;
                    break;

                case TileType.Doorway:
                    doorwayCount++;
                    break;

                case TileType.Vendor:
                    vendorCount++;
                    break;

                case TileType.Hazard:
                    hazardCount++;
                    break;

                case TileType.Treasure:
                    treasureCount++;
                    break;
            }
        }

        int highestCount = emptyCount;
        TileType dominantType = TileType.Empty;

        if (monsterCount > highestCount)
        {
            highestCount = monsterCount;
            dominantType = TileType.Monster;
        }

        if (lootCount > highestCount)
        {
            highestCount = lootCount;
            dominantType = TileType.Loot;
        }

        if (exitCount > highestCount)
        {
            highestCount = exitCount;
            dominantType = TileType.Exit;
        }

        if (entranceCount > highestCount)
        {
            highestCount = entranceCount;
            dominantType = TileType.Entrance;
        }

        if (doorwayCount > highestCount)
        {
            highestCount = doorwayCount;
            dominantType = TileType.Doorway;
        }

        if (vendorCount > highestCount)
        {
            highestCount = vendorCount;
            dominantType = TileType.Vendor;
        }

        if (hazardCount > highestCount)
        {
            highestCount = hazardCount;
            dominantType = TileType.Hazard;
        }

        if (treasureCount > highestCount)
        {
            highestCount = treasureCount;
            dominantType = TileType.Treasure;
        }

        return dominantType;
    }

    //	Instantiate the given prefab at the point, settings it as a child of the Grid
    private GameObject CreateCubeAt(GameObject prefab, int x, int y)
    {
        GameObject cube = Instantiate(prefab, transform);
        cube.transform.position = new Vector3(x, -y, 0);
        ConwayCube conwayCubeComponent = cube.GetComponent<ConwayCube>();
        conwayCubeComponent.Initialize(
            this,
            new Vector2Int(x, y)
        );
        conwayCubeComponent.SetTileType(TileType.Wall);
        return cube;
    }

    //	Setup the visual grid
    public void SetupGrid()
    {
        cubes = new GameObject[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                cubes[x, y] = CreateCubeAt(conwayCube, x, y);
            }
        }
    }

    public void ResetGrid()
    {
        modified = true;
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public void RenderGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject cube = cubes[x, y];
                ConwayCube cubeScript = cube.GetComponent<ConwayCube>();

                TileType tileType = grid[x, y];

                if (cubeScript.CurrentTileType != tileType)
                {
                    cubeScript.SetTileType(tileType);
                }
            }
        }
    }

    private TileType GetRandomWalkableTileType()
    {
        float randomValue = Random.value;

        return TileType.Empty;
    }

    public Vector3 GetEntrancePos()
    {
        if (grid == null)
        {
            Debug.LogError("Grid data has not been initialised yet. SetupInitialGrid/generation has not finished.");
            return Vector3.zero;
        }

        Vector3 entrancePos = Vector3.zero;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                //if tile is entrance get its position
                if (grid[x, y] == TileType.Entrance)
                {
                    entrancePos = new Vector3(x, -y, 0);
                    break;
                }
            }
        }
        //if entrance was not found inform user
        if (entrancePos == Vector3.zero) Debug.LogError("Entrance could not be found.");
        return entrancePos;
    }

    /* Phase 1: Map Cleanse */

    // Kill off a tile if it is considered useless
    // tiles with 0 or 1 neighbors are always killed (so no solitary tiles/dead ends)
    // tiles with 2 neighbors are killed if they are not structured as a corridor or a corner
    // tiles with 3 or more neighbors are never killed
    private bool HasEmptyCorners(int x, int y)
    {
        if ((IsPointInBounds(x - 1, y - 1) && grid[x - 1, y - 1] != TileType.Wall) || 
            (IsPointInBounds(x + 1, y - 1) && grid[x + 1, y - 1] != TileType.Wall) || 
            (IsPointInBounds(x - 1, y + 1) && grid[x - 1, y + 1] != TileType.Wall) ||
            (IsPointInBounds(x + 1, y + 1) && grid[x + 1, y + 1] != TileType.Wall))
        {
            return false;
        }

        return true;
    }

    private bool HasEmptySides(int x, int y)
    {
        if ((IsPointInBounds(x - 1, y) && grid[x - 1, y] != TileType.Wall) &&
            (IsPointInBounds(x + 1, y) && grid[x + 1, y] != TileType.Wall) &&
            (IsPointInBounds(x, y + 1) && grid[x, y + 1] != TileType.Wall) &&
            (IsPointInBounds(x, y - 1) && grid[x, y - 1] != TileType.Wall))
        {
            return true;
        }

        return false;
    }

    private bool IsDoorway(int x, int y)
    {
        if (((IsPointInBounds(x + 1, y) && grid[x + 1, y] == TileType.Wall) &&
            (IsPointInBounds(x - 1, y) && grid[x - 1, y] == TileType.Wall) &&
            (IsPointInBounds(x, y + 1) && grid[x, y + 1] == TileType.Empty) &&
            (IsPointInBounds(x, y - 1) && grid[x, y - 1] == TileType.Empty) &&
            (IsPointInBounds(x + 1, y + 1) && grid[x + 1, y + 1] != TileType.Doorway) &&
            (IsPointInBounds(x + 1, y - 1) && grid[x + 1, y - 1] != TileType.Doorway) &&
            (IsPointInBounds(x - 1, y + 1) && grid[x - 1, y + 1] != TileType.Doorway) &&
            (IsPointInBounds(x - 1, y - 1) && grid[x - 1, y - 1] != TileType.Doorway)) || 
            (IsPointInBounds(x, y - 1) && grid[x, y - 1] == TileType.Wall) &&
            (IsPointInBounds(x, y + 1) && grid[x, y + 1] == TileType.Wall) && 
            (IsPointInBounds(x + 1, y) && grid[x + 1, y] == TileType.Empty) &&
            (IsPointInBounds(x - 1, y) && grid[x - 1, y] == TileType.Empty) &&
            (IsPointInBounds(x + 1, y + 1) && grid[x + 1, y + 1] != TileType.Doorway) &&
            (IsPointInBounds(x + 1, y - 1) && grid[x + 1, y - 1] != TileType.Doorway) &&
            (IsPointInBounds(x - 1, y + 1) && grid[x - 1, y + 1] != TileType.Doorway) &&
            (IsPointInBounds(x - 1, y - 1) && grid[x - 1, y - 1] != TileType.Doorway))
        {
            return true;
        }

        return false;
    }

    private void ClearAreaAround(Vector2Int centre, int radius = 1)
    {
        for (int y = centre.y - radius; y <= centre.y + radius; y++)
        {
            for (int x = centre.x - radius; x <= centre.x + radius; x++)
            {
                if (grid[x, y] == TileType.Entrance || grid[x, y] == TileType.Exit) continue;
                if (!IsPointInBounds(x, y))
                    continue;

                // do not clear the center tile 
                if (x == centre.x && y == centre.y)
                    continue;

                grid[x, y] = TileType.Empty;
                nextGrid[x, y] = TileType.Empty;
            }
        }
    }

    private bool IsInCorner(int x, int y)
    {
        if ((
                (IsPointInBounds(x - 1, y - 1) && grid[x - 1, y - 1] == TileType.Wall) &&
                (IsPointInBounds(x - 0, y - 1) && grid[x - 0, y - 1] == TileType.Wall) &&
                (IsPointInBounds(x + 1, y - 1) && grid[x + 1, y - 1] == TileType.Wall) &&
                (IsPointInBounds(x - 1, y + 0) && grid[x - 1, y + 0] == TileType.Wall) &&
                (IsPointInBounds(x - 1, y + 1) && grid[x - 1, y + 1] == TileType.Wall) &&

                (IsPointInBounds(x + 1, y - 0) && grid[x + 1, y - 0] == TileType.Empty) &&
                (IsPointInBounds(x - 0, y + 1) && grid[x - 0, y + 1] == TileType.Empty) &&
                (IsPointInBounds(x + 1, y + 1) && grid[x + 1, y + 1] == TileType.Empty)

            ) || (
                (IsPointInBounds(x - 1, y - 1) && grid[x - 1, y - 1] == TileType.Wall) &&
                (IsPointInBounds(x - 0, y - 1) && grid[x - 0, y - 1] == TileType.Wall) &&
                (IsPointInBounds(x + 1, y + 1) && grid[x + 1, y + 1] == TileType.Wall) &&
                (IsPointInBounds(x + 1, y - 0) && grid[x + 1, y - 0] == TileType.Wall) &&
                (IsPointInBounds(x + 1, y - 1) && grid[x + 1, y - 1] == TileType.Wall) &&

                (IsPointInBounds(x - 1, y - 0) && grid[x - 1, y - 0] == TileType.Empty) &&
                (IsPointInBounds(x - 0, y + 1) && grid[x - 0, y + 1] == TileType.Empty) &&
                (IsPointInBounds(x - 1, y + 1) && grid[x - 1, y + 1] == TileType.Empty)

            ) || (

                (IsPointInBounds(x - 1, y - 1) && grid[x - 1, y - 1] == TileType.Wall) &&
                (IsPointInBounds(x - 1, y + 0) && grid[x - 1, y + 0] == TileType.Wall) &&
                (IsPointInBounds(x - 1, y + 1) && grid[x - 1, y + 1] == TileType.Wall) &&
                (IsPointInBounds(x + 0, y + 1) && grid[x + 0, y + 1] == TileType.Wall) &&
                (IsPointInBounds(x + 1, y + 1) && grid[x + 1, y + 1] == TileType.Wall) &&

                (IsPointInBounds(x - 0, y - 1) && grid[x - 0, y - 1] == TileType.Empty) &&
                (IsPointInBounds(x + 1, y - 1) && grid[x + 1, y - 1] == TileType.Empty) &&
                (IsPointInBounds(x + 1, y + 0) && grid[x + 1, y + 0] == TileType.Empty)

            ) || (

                (IsPointInBounds(x + 1, y - 1) && grid[x + 1, y - 1] == TileType.Wall) &&
                (IsPointInBounds(x + 1, y + 0) && grid[x + 1, y + 0] == TileType.Wall) &&
                (IsPointInBounds(x + 1, y + 1) && grid[x + 1, y + 1] == TileType.Wall) &&
                (IsPointInBounds(x + 0, y + 1) && grid[x + 0, y + 1] == TileType.Wall) &&
                (IsPointInBounds(x - 1, y + 1) && grid[x - 1, y + 1] == TileType.Wall) &&

                (IsPointInBounds(x - 1, y - 1) && grid[x - 1, y - 1] == TileType.Empty) &&
                (IsPointInBounds(x - 0, y - 1) && grid[x - 0, y - 1] == TileType.Empty) &&
                (IsPointInBounds(x - 1, y + 0) && grid[x - 1, y + 0] == TileType.Empty)
            ))
        {
            return true;
        }

        return false;
    }

    //flood fill method (no diagonal movement)
    //update this to use the actual grid and not the Vector2Int position references
    public bool HasLegalPathBetween(Vector2Int start, Vector2Int target)
    {
        if (!IsPointInBounds(start.x, start.y)) return false;
        if (!IsPointInBounds(target.x, target.y)) return false;

        if (!IsPathWalkableTile(grid[start.x, start.y]))
            return false;

        if (!IsPathWalkableTile(grid[target.x, target.y]))
            return false;

        bool[,] visited = new bool[gridWidth, gridHeight];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        
        //queue the first cell
        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        
        //stores the list of directions the program can take
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            //if the current cell is the cell we are looking for end the search
            if (current == target) return true;

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                //skip cell if it is not in a legal position
                if (!IsPointInBounds(next.x, next.y)) continue;

                //skip cell if it has already been visited
                if (visited[next.x, next.y]) continue;

                //skip cell if it is a wall/monster/hazard
                if (!IsPathWalkableTile(grid[next.x, next.y])) continue;

                visited[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }

        Debug.LogWarning("No legal path was identified");

        return false;
    }

    //this is always returning true, even when an entrance-exit pair have not been identified
    public bool HasLegalPath()
    {
        bool[,] visited = new bool[gridWidth, gridHeight];

        bool entranceExists = false;
        bool exitExists = false;

        int entranceCount = 0;
        int exitCount = 0;

        Vector2Int entrance = Vector2Int.zero;
        Vector2Int exit = Vector2Int.zero;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == TileType.Entrance)
                {
                    entrance = new Vector2Int(x, y);
                    entranceExists = true;
                    entranceCount++;
                }

                if (grid[x, y] == TileType.Exit)
                {
                    exit = new Vector2Int(x, y);
                    exitExists = true;
                    exitCount++;
                }
            }
        }

        if (!entranceExists || !exitExists)
        {
            Debug.LogWarning("Legal path check failed: entrance or exit is missing.");
            return false;
        }

        if (entranceCount != 1 || exitCount != 1)
        {
            Debug.LogWarning(
                "Legal path check failed: expected exactly one entrance and one exit. " +
                "Entrance count: " + entranceCount + ", Exit count: " + exitCount
            );

            return false;
        }

        return HasLegalPathBetween(entrance, exit);
    }

    private void ClearExistingEntranceAndExit()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == TileType.Entrance ||
                    grid[x, y] == TileType.Exit)
                {
                    grid[x, y] = TileType.Empty;
                    nextGrid[x, y] = TileType.Empty;
                }
            }
        }
    }

    //would it be better to have multiple exits spawn, or to only have one exit?
    //above is moreso something to talk about, though would be easier to generate a valid level earlier rather than multiple retries until a complete path manifests
    /*
    public void PlaceEntranceAndExit()
    {
        //if (entranceExitIdentified) return;
        doorways.Clear();

        //find all doorways candidates
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if ((grid[x, y] == TileType.Empty || grid[x, y] == TileType.Doorway) && IsDoorway(x, y))
                {
                    doorways.Add(new Vector2Int(x, y));
                }
            }
        }

        //if there are less than 2 doorways then we can never identify the entrance/exit pair
        if (doorways.Count < 2)
        {
            Debug.LogWarning("Not enough entrance/exit candidates found.");
            return;
        }

        Vector2Int entrance = doorways[Random.Range(0, doorways.Count)];

        //temporarily set the exit variable to the entrance variable
        Vector2Int exit = entrance;
        float furthestDistance = -1f;

        //find the farmost doorway from the entrance to be the exit
        foreach (Vector2Int candidate in doorways)
        {
            if (candidate == entrance) continue;

            float distance = Vector2Int.Distance(entrance, candidate);

            if (distance < minimumEntranceExitDistance)
            {
                Debug.LogWarning("Entrance and exit are closer than the desired minimum distance, choosing new candidate.");
                continue;
            }

            if (distance > furthestDistance && HasLegalPath())
            {
                furthestDistance = distance;
                exit = candidate;
            }
        }

        if (exit == entrance)
        {
            Debug.LogWarning("Could not find a separate exit position.");
            return;
            //entrance = doorways[Random.Range(0, doorways.Count)];
            
        }

        grid[entrance.x, entrance.y] = TileType.Entrance;
        grid[exit.x, exit.y] = TileType.Exit;

        nextGrid[entrance.x, entrance.y] = TileType.Entrance;
        nextGrid[exit.x, exit.y] = TileType.Exit;

        entrancePosition = entrance;
        exitPosition = exit;

        Debug.Log("Entrance placed at: " + entrance + " with tile type: " + grid[entrance.x, entrance.y]);
        Debug.Log("Exit placed at: " + exit + " with tile type: " + grid[exit.x, exit.y]);
        //Debug.Log("Legal path exists: " + HasLegalPath(entrance, exit));
    }
    */

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            T temporary = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temporary;
        }
    }

    private int GetDistanceFromBorder(Vector2Int position)
    {
        int horizontalDistance = Mathf.Min(
            position.x,
            gridWidth - 1 - position.x
        );

        int verticalDistance = Mathf.Min(
            position.y,
            gridHeight - 1 - position.y
        );

        return Mathf.Min(
            horizontalDistance,
            verticalDistance
        );
    }

    public bool PlaceEntranceAndExit()
    {
        ClearExistingEntranceAndExit();
        doorways.Clear();

        // Collect every valid doorway candidate.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                bool validTileType =
                    grid[x, y] == TileType.Empty ||
                    grid[x, y] == TileType.Doorway;

                if (validTileType && IsDoorway(x, y))
                {
                    doorways.Add(new Vector2Int(x, y));
                }
            }
        }

        if (doorways.Count < 2)
        {
            Debug.LogWarning(
                "Not enough entrance/exit candidates found."
            );

            return false;
        }

        // Prevent candidates with lower x/y coordinates
        // from always being tested first.
        ShuffleList(doorways);

        foreach (Vector2Int entrance in doorways)
        {
            if (GetDistanceFromBorder(entrance) <
                minimumEntranceBorderDistance)
            {
                continue;
            }
            Vector2Int bestExit = entrance;
            float furthestDistance = -1f;

            foreach (Vector2Int candidateExit in doorways)
            {
                if (candidateExit == entrance)
                    continue;

                float distance =
                    Vector2Int.Distance(
                        entrance,
                        candidateExit
                    );

                if (distance < minimumEntranceExitDistance)
                {
                    Debug.LogWarning("Distance is smaller than the minimum allowed distance, choosing next exit candidate.");
                    continue;
                }
                    

                TileType originalEntranceType =
                    grid[entrance.x, entrance.y];

                TileType originalExitType =
                    grid[candidateExit.x, candidateExit.y];

                // Temporarily place the pair.
                grid[entrance.x, entrance.y] =
                    TileType.Entrance;

                grid[candidateExit.x, candidateExit.y] =
                    TileType.Exit;

                bool legalPathExists = HasLegalPath();

                // Restore original tile types.
                grid[entrance.x, entrance.y] =
                    originalEntranceType;

                grid[candidateExit.x, candidateExit.y] =
                    originalExitType;

                if (legalPathExists &&
                    distance > furthestDistance)
                {
                    furthestDistance = distance;
                    bestExit = candidateExit;
                }
            }

            if (bestExit == entrance)
                continue;

            grid[entrance.x, entrance.y] =
                TileType.Entrance;

            grid[bestExit.x, bestExit.y] =
                TileType.Exit;

            nextGrid[entrance.x, entrance.y] =
                TileType.Entrance;

            nextGrid[bestExit.x, bestExit.y] =
                TileType.Exit;

            entrancePosition = entrance;
            exitPosition = bestExit;

            Debug.Log(
                $"Entrance placed at {entrance}. "
            );

            Debug.Log(
                $"Exit placed at {bestExit}. " +
                $"Distance: {furthestDistance:F1}"
            );

            return true;
        }

        Debug.LogWarning(
            "No legal entrance-exit pair could be found."
        );

        return false;
    }

    public void ClearPillars()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == TileType.Entrance || grid[x, y] == TileType.Exit) continue;
                int walkableNeighbors = GetWalkableNeighborsCount(x, y);
                
                if(HasEmptySides(x, y) && grid[x, y] == TileType.Wall)
                {
                    nextGrid[x, y] = TileType.Empty;
                }

                if (walkableNeighbors > 6 && (grid[x, y] == TileType.Wall))
                {
                    nextGrid[x, y] = TileType.Empty;
                }
            }
        }
    }

    public void CreateDoorways()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == TileType.Entrance || grid[x, y] == TileType.Exit) continue;
                int walkableNeighbors = GetWalkableNeighborsCount(x, y);

                float random = Random.value;

                if (walkableNeighbors > 2 && IsDoorway(x, y))
                {
                    if (random < 1 - hazardChance * 2)
                    {
                        grid[x, y] = TileType.Doorway;
                        nextGrid[x, y] = TileType.Doorway;
                    } 
                    else
                    {
                        grid[x, y] = TileType.Hazard;
                        nextGrid[x, y] = TileType.Hazard;
                        hazardCount++;
                    }
                    
                }
            }
        }

        Debug.Log("Hazards Spawned: " + hazardCount);
    }

    public void PopulateGrid()
    {
        nextProbability = 0;
        modified = false;

        int monsterCount = 0, treasureCount = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == TileType.Entrance || grid[x, y] == TileType.Exit) continue;
                TileType currentTile = grid[x, y];
                int walkableNeighbors = GetWalkableNeighborsCount(x, y);
                
                if (IsSpecialTile(currentTile))
                {
                    if (walkableNeighbors >= 2) nextGrid[x, y] = currentTile;
                    if (walkableNeighbors < 2) { nextGrid[x, y] = TileType.Wall; modified = true; }
                    continue;
                }

                if (IsWalkableTile(currentTile))
                {
                    if (walkableNeighbors > 2)
                    {
                        if(IsInCorner(x, y)) 
                        {
                            grid[x, y] = TileType.Treasure; //avoids infinite chest re-creation
                            nextGrid[x, y] = TileType.Treasure;
                            treasureCount++;
                        }                        

                        if (walkableNeighbors == 8) //monster, vendor and hazard share the same spawning logic
                            {
                            float random = probabilities[nextProbability];
                            nextProbability++;

                            if (random >= 0.0 && random < monsterChance)
                            {
                                nextGrid[x, y] = TileType.Monster;
                                monsterCount++;
                            }
                            else if (random >= monsterChance && random < monsterChance + hazardChance)
                            {
                                nextGrid[x, y] = TileType.Hazard;
                                hazardCount++;
                            }
                            else
                            {
                                grid[x, y] = TileType.Empty;
                                nextGrid[x, y] = TileType.Empty;
                            }
                            /*
                            else if (random >= 0.95 && random <= 1.0)
                            {
                                //grid[x, y] = TileType.Vendor;
                                nextGrid[x, y] = TileType.Vendor;
                                vendorCount++;
                            }
                            */
                        }
                    } 
                }
            }
        }

        // Copy nextGrid into grid.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = nextGrid[x, y];
            }
        }

        //entranceExitIdentified = true;

        Debug.Log("Monsters Spawned: " + monsterCount);
        Debug.Log("Treasures Spawned: " + treasureCount);
    }

    public void RenderMap()
    {
        backgroundView.Initialize(this);
        backgroundView.SetupMapView(1f);
        backgroundView.RenderMapView();
        mapView.Initialize(this);
        mapView.SetupMapView(1f);
        mapView.RenderMapView();
        
    }

    //	Perform neighbor calculations and render the grid
    public void StepForward()
    {
        modified = false;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                TileType currentTile = grid[x, y];
                int walkableNeighbors = GetWalkableNeighborsCount(x, y);

                if (IsWalkableTile(currentTile))
                {
                    if(walkableNeighbors < 2)
                    {
                        modified = true;
                        nextGrid[x, y] = TileType.Wall;
                    }
                    else if (walkableNeighbors > 2)
                    {
                        // Survives.
                        if (grid[x, y] == TileType.Wall) modified = true;
                        nextGrid[x, y] = TileType.Empty;
                    } 
                    else if (walkableNeighbors == 2)
                    {
                        if (HasEmptyCorners(x, y))
                        {
                            nextGrid[x, y] = currentTile;
                        }
                        else
                        {
                            modified = true;
                            nextGrid[x, y] = TileType.Wall;
                        }
                    }
                }
            }
        }

        // Copy nextGrid into grid.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = nextGrid[x, y];
            }
        }

        ClearPillars();
        RenderGrid();
    }

    public void CountTiles()
    {
        int[] tileTypeCounts = new int[10];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                tileTypeCounts[(int)grid[x, y]]++;
            }
        }

        foreach (int count in tileTypeCounts)
        {
            Debug.Log(count);
        }
        
    }

    public void GenerateProbabilities()
    {
        //generate 100 random numbers in the range 0 to 1
        //0 - 0.5: monster
        //0.5 - 0.9: hazard
        //0.9 - 1: vendor
        for (int i = 0; i < probabilities.Length; i++)
        {
            probabilities[i] = Random.value;
            //if (i < 100) Debug.Log(probabilities[i]);
        }

    }

    private void SetManualTiles(GridItem[] items, TileType tileType)
    {
        foreach (GridItem gridItem in items)
        {
            if (IsPointInBounds(gridItem))
            {
                grid[gridItem.x, gridItem.y] = tileType;
            }
            else
            {
                Debug.LogWarning("Index (" + gridItem.x + "," + gridItem.y + ") is out of bounds");
            }
        }
    }

    public void SetupInitialGrid()
    {
        grid = new TileType[gridWidth, gridHeight];
        nextGrid = new TileType[gridWidth, gridHeight];

        if (useRandomStart)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    bool shouldBeWall = Random.value < initialWallChance;

                    if (shouldBeWall)
                    {
                        grid[x, y] = TileType.Wall;
                    }
                    else
                    {
                        grid[x, y] = GetRandomWalkableTileType();
                    }
                }
            }
        }
        else
        {
            // Default everything to wall.
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = TileType.Empty;
                }
            }
        }
    }

    /*
    void Awake()
    {
        //	Render
        GenerateProbabilities();
        SetupInitialGrid();
        SetupGrid();
        RenderGrid();
    }

    void Update()
    {
        //	If not auto-progressing, advance when user hits Return
        if (!autoProgress && Input.GetKeyDown(KeyCode.Return) && modified)
        {
            StepForward();
        }

        //	If auto-probgressing, advance when time has elapsed
        if (autoProgress && modified)
        {
            deltaTime += Time.deltaTime;
            if (deltaTime >= autoProgressionTime)
            {
                StepForward();
                deltaTime -= autoProgressionTime;
            }
        }

        if (!modified)
        {
            Debug.Log("Path Exists: " + HasLegalPath(entrancePosition, exitPosition));
        }
    }
    */
}