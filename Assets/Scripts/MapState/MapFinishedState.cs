using UnityEngine;

public class MapFinishedState : BaseMapState
{
    public override void EnterState(MapFSM map)
    {
        base.EnterState(map);
        Debug.Log("Entered Map Finished State.");
    }

    //here will be the placement of the tile sprites using the grid as reference
}
