using UnityEngine;

public class MapFinalisedState : BaseMapState
{
    public override void EnterState(MapFSM map)
    {
        base.EnterState(map);
        Debug.Log("Entered Map Finalised State.");
        if (HasLegalPath())
        {
            FSM.MoveToState(FSM.s_Legal);
        } 
        else
        {
            FSM.MoveToState(FSM.s_Generating);
        }
    }

    public bool HasLegalPath()
    {
        return FSM.grid.HasLegalPathBetween(FSM.grid.EntrancePosition, FSM.grid.ExitPosition);
    }
}
