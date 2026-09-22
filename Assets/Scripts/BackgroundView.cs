using Unity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class BackgroundView : MonoBehaviour
{
    public Grid model;

    public GameObject tilePrefab;

    public Sprite floor;
    public Sprite wall;

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
            case TileType.Wall:
                scale = 6.8f;
                break;
            case TileType.Empty:
                scale = 6.6f;
                break;
            default:
                scale = 6.8f;
                break;
        }
        return scale;
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

    public void ResetMapView()
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
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

                renderer.sortingOrder = 0;

                tileRenderers[x, y] = renderer;
            }
        }
    }

    private void RenderTileAt(TileType tile, int x, int y, float scale = 3.0f)
    {
        SpriteRenderer renderer = tileRenderers[x, y];
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
        renderer.sprite = GetTileToRender(tile, x, y);

        //on second thoughts, wouldn't it be easier to handle the rendering on Grid.cs? Since we have direct access to the positions of each cell there, we can very easily find where the tiles need to be spawned instead of having to do messy stuff here
        //besides, the FSM is already calling the render method in Grid.cs, might as well shift the code there
        //well, having the rendering code in an external class will make everything cleaner, so this will remain here if we decide to go that way.
        //disregard all of what I talked about, I'm just going to use the helper methods in MapView.cs in Grid.cs
    }

    public Sprite GetTileToRender(TileType tile, int x = 0, int y = 0)
    {
        Sprite tileSprite;
        if (tile != TileType.Wall) tile = TileType.Empty;
        
        switch (tile)
        {
            case TileType.Wall:
                tileSprite = wall;
                return tileSprite;
                
            case TileType.Empty:
                tileSprite = floor;
                return tileSprite;
        }
        

        return wall;
    }

}
