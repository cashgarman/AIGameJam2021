using UnityEngine;

public abstract class BaseState
{
	public abstract string Name { get; }

	public abstract void OnEntered(params object[] args);
	public abstract void OnLeft();
	public abstract void OnTick();
}

public abstract class AgentState<AgentType> : BaseState where AgentType : BaseAgent
{
	public AgentType Agent { get; set; }
	
	public override void OnEntered(params object[] args)
	{
		Debug.Log($"{Agent.Name} entered {Name} state");
	}

	public override void OnLeft()
	{
		Debug.Log($"{Agent.Name} left {Name} state");
	}

	public override void OnTick()
	{
		// Debug.Log($"{Name} ticked for {Agent.Name}");
	}
}