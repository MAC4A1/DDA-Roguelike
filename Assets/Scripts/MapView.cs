using Unity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class MapView : MonoBehaviour
{
    public Grid model;

    public MapView (Grid grid)
    {
        model = grid;
    }

    public GameObject tilePrefab;
    public GameObject minimapExit;

    public Sprite player;
    public Sprite wall;
    public Sprite entrance;
    public Sprite exit;
    public Sprite[] monsters;
    public Sprite[] hazards;
    public Sprite[] treasures;
    public Sprite[] loot;
    public Sprite[] vendors;
    public Sprite[] doorways;

    private Sprite tileToRender;

    private SpriteRenderer[,] tileRenderers;

    public void Initialize(Grid grid)
    {
        model = grid;
        ResetMapView();
    }

    private float GetScale(TileType tileToScale)
    {
        float scale = 1f;
        switch (tileToScale)
        {
            case TileType.Monster:
                scale = 5.0f;
                break;
            case TileType.Loot:
                scale = 5.0f;
                break;
            case TileType.Entrance:
                scale = 6.5f;
                break;
            case TileType.Exit:
                scale = 7.0f;
                break;
            case TileType.Doorway:
                //scale = 6.25f;
                scale = 5.5f;
                break;
            case TileType.Vendor:
                scale = 5.0f;
                break;
            case TileType.Hazard:
                scale = 6.25f;
                break;
            case TileType.Treasure:
                scale = 5.0f;
                break;
            default:
                scale = 3.0f;
                break;
        }
        return scale;
    }

    private float GetRotation(TileType tileToRotate)
    {
        float rotation = 0.0f;
        switch (tileToRotate)
        {
            case TileType.Doorway:
                //rotation = 8.0f;
                break;
            default:
                rotation = 3.0f;
                break;
        }
        return rotation;
    }

    //render the entire map
    public void RenderMapView()
    {
        float scale = 1.0f;
        for (int i = 0; i < model.gridWidth; i++)
        {
            for (int j = 0; j < model.gridHeight; j++)
            {
                
                TileType tileType = model.GetTileTypeAt(i, j);
                scale = GetScale(tileType);
                RenderTileAt(tileType, i, j, scale);
            }
        }
    }

    public void SetupMapView(float scale)
    {
        tileRenderers = new SpriteRenderer[model.gridWidth, model.gridHeight];

        for (int x = 0; x < model.gridWidth; x++)
        {
            for (int y = 0; y < model.gridHeight; y++)
            {
                GameObject tileObject = Instantiate(tilePrefab, transform);
                tileObject.transform.position = new Vector3(x * scale, -y * scale, 0);
                tileObject.transform.localScale = new Vector3(scale, scale, 1);

                SpriteRenderer renderer = tileObject.GetComponent<SpriteRenderer>();

                renderer.sortingOrder = 10;

                tileRenderers[x, y] = renderer;
            }
        }
    }

    //render the part of the map that is only visible to the player
    public void RenderPlayerView(int x, int y, int radiusX, int radiusY, float scale)
    {
        for (int i = x - radiusX; i < x + radiusX; i++)
        {
            for (int j = y - radiusY; j < y + radiusY; j++)
            {
                TileType tileType = model.GetTileTypeAt(i, j);

                RenderTileAt(tileType, i - x, j - y);
            }
        }
    }

    private Sprite Combined2Sprites(Sprite floor, Sprite character)
    {
        float width = floor.bounds.size.x;
        float height = floor.bounds.size.y;
        //Extract background pixels from floor
        Color[] bg = floor.texture.GetPixels(
    (int)floor.rect.x,
    (int)floor.rect.y,
    (int)width,
        (int)height);

        //extract foreground pixels from character
        Color[] fg = character.texture.GetPixels(
            (int)character.rect.x,
        (int)character.rect.y,
        (int)width,
            (int)height);

        //combine background and foreground according to foreground transparency
        for (int i = 0; i < bg.Length; i++)
        {
            //if fg alpha is 0, then use b[g]
            float a = fg[i].a;
            if (fg[i].a != 0) bg[i] = fg[i];
        }

        //rebuild texture from background area of pixels
        Texture2D result = new Texture2D((int)width, (int)height);
        result.SetPixels(bg);
        result.Apply();

        //rebuild sprite from texture
        Sprite sprite = Sprite.Create(
            result,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f));
        
        return sprite;
    }

    public void ResetMapView()
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    private Sprite GetVariant(Sprite[] sprites, int x, int y, Sprite fallback)
    {
        if (sprites == null || sprites.Length == 0)
            return fallback;

        unchecked
        {
            int hash = (x * 73856093) ^ (y * 19349663);
            int index = (hash & int.MaxValue) % sprites.Length;

            return sprites[index];
        }
    }

    private void RenderTileAt(TileType tile, int x, int y, float scale = 3.0f)
    { 
        SpriteRenderer renderer = tileRenderers[x, y];
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
        renderer.sprite = GetTileToRender(tile, x, y);

        if (tile == TileType.Exit)
        {
            GameObject tileObject = Instantiate(minimapExit, transform);
            tileObject.transform.position = new Vector3(x, -y, 0);
        }

        //on second thoughts, wouldn't it be easier to handle the rendering on Grid.cs? Since we have direct access to the positions of each cell there, we can very easily find where the tiles need to be spawned instead of having to do messy stuff here
        //besides, the FSM is already calling the render method in Grid.cs, might as well shift the code there
        //well, having the rendering code in an external class will make everything cleaner, so this will remain here if we decide to go that way.
        //disregard all of what I talked about, I'm just going to use the helper methods in MapView.cs in Grid.cs
    }

    public Sprite GetTileToRender(TileType tile, int x = 0, int y = 0)
    {
        Sprite tileSprite;
        switch (tile)
        {
            case TileType.Entrance:
                tileSprite = entrance;
                return tileSprite;
                
            case TileType.Exit:
                tileSprite = exit;
                return tileSprite;
                
            case TileType.Monster:
                return GetVariant(monsters, x, y, wall);

            case TileType.Hazard:
                return GetVariant(hazards, x, y, wall);

            case TileType.Vendor:
                tileSprite = vendors[Random.Range(0, vendors.Length)];
                return tileSprite;
                
            case TileType.Treasure:
                tileSprite = treasures[Random.Range(0, treasures.Length)];
                return tileSprite;

            case TileType.Loot:
                tileSprite = loot[Random.Range(0, loot.Length)];
                return tileSprite;

            case TileType.Doorway:
                tileSprite = doorways[Random.Range(0, doorways.Length)];
                return tileSprite;

        }

        return null;

        //return wall;
    }

    public void SetTileToRender(TileType tile)
    {
        switch (tile)
        {
            case TileType.Entrance:
                tileToRender = entrance;
                break;
            case TileType.Exit:
                tileToRender = exit;
                break;
            case TileType.Monster:
                tileToRender = monsters[Random.Range(0, monsters.Length)];
                break;
            case TileType.Hazard:
                tileToRender = hazards[Random.Range(0, hazards.Length)];
                break;
            case TileType.Vendor:
                tileToRender = vendors[Random.Range(0, vendors.Length)];
                break;
            case TileType.Treasure:
                tileToRender = vendors[Random.Range(0, vendors.Length)];
                break;
            case TileType.Loot:
                tileToRender = vendors[Random.Range(0, vendors.Length)];
                break;
            case TileType.Doorway:
                tileToRender = vendors[Random.Range(0, vendors.Length)];
                break;
        }
    }
}
