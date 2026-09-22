using UnityEngine;

public class MapEvolvedState : BaseMapState
{
    public override void EnterState(MapFSM map)
    {
        base.EnterState(map);
        Debug.Log("Entered Map Evolved State.");
        PlaceEntranceAndExit(); 
        FSM.MoveToState(FSM.s_Finalised);
    }

    public void PlaceEntranceAndExit()
    {
        FSM.grid.PlaceEntranceAndExit();
        FSM.grid.RenderGrid();
    }
}
