using UnityEngine;

public class MapGeneratedState : BaseMapState
{
    public override void EnterState(MapFSM map)
    {
        base.EnterState(map);
        Debug.Log("Entered Map Generated State.");
        EvolveMap();
        FSM.MoveToState(FSM.s_Evolved);
    }

    public void EvolveMap()
    {
        //put the code that generated the grid in Grid.cs
        while (FSM.grid.Modified)
        {
            FSM.grid.StepForward();
        }
    }
}
