using UnityEngine;

public class RenderMapState : BaseMapState
{
    public override void EnterState(MapFSM map)
    {
        base.EnterState(map);
        Debug.Log("Entered Render Map State.");
        RenderMap();
        FSM.MoveToState(FSM.s_Finished);
    }

    public void RenderMap()
    {
        //FSM.grid.PlaceEntranceAndExit();
        FSM.grid.RenderMap(); //this must be changed, rendering is no longer happening in grid.cs
        //FSM.mapView.RenderMapView();
    }
}
