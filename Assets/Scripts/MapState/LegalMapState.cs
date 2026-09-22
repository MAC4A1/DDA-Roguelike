using UnityEngine;

public class LegalMapState : BaseMapState
{
    public override void EnterState(MapFSM map)
    {
        base.EnterState(map);
        Debug.Log("Entered Legal Map State.");
        PopulateGrid();
        FSM.MoveToState(FSM.s_Render);
    }

    public void PopulateGrid()
    {
        FSM.grid.PopulateGrid();
        FSM.grid.CreateDoorways();
        FSM.grid.RenderGrid();
        //FSM.grid.CountTiles();
    }
}
