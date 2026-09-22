using UnityEngine;

public class GeneratingMapState : BaseMapState
{
    public override void EnterState(MapFSM map)
    {
        base.EnterState(map);
        Debug.Log("Entered Generating Map State.");
        GenerateMap();
        FSM.MoveToState(FSM.s_Generated);
    }

    //might want to look into adding a method to reset the grid if it's invalid
    public void GenerateMap()
    {
        //put the code that generated the grid in Grid.cs
        FSM.grid.ResetGrid();
        FSM.grid.GenerateProbabilities();
        FSM.grid.SetupInitialGrid();
        FSM.grid.SetupGrid();
        FSM.grid.RenderGrid();
    }
}
