using System.Collections.Generic;
using UnityEngine;

public class MapFSM : MonoBehaviour
{
    [Header("States")]
    public GeneratingMapState s_Generating = new GeneratingMapState();
    public MapGeneratedState s_Generated = new MapGeneratedState();
    public MapEvolvedState s_Evolved = new MapEvolvedState();
    public MapFinalisedState s_Finalised = new MapFinalisedState();
    public LegalMapState s_Legal = new LegalMapState();
    public RenderMapState s_Render = new RenderMapState();
    public MapFinishedState s_Finished = new MapFinishedState();

    private BaseMapState currentState;

    public BaseMapState CurrentState { get { return currentState; } }

    public Grid grid;

    void Start()
    {
        MoveToState(s_Generating);
    }

    public void MoveToState(BaseMapState state)
    {
        currentState = state;
        currentState.EnterState(this);
    }

    public void Restart()
    {
        MoveToState(s_Generating);
    }
}
