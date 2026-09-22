using UnityEngine;

public abstract class BaseMapState
{
    protected MapFSM FSM;

    public virtual void EnterState(MapFSM map)
    {
        FSM = map;
    }
}
