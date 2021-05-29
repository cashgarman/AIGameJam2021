using System;
using UnityEngine;

public abstract class BaseAgent : MonoBehaviour
{
	public abstract string Name { get; }
}

public abstract class Agent<AgentType> : BaseAgent where AgentType : BaseAgent
{
	protected AgentState<AgentType> _currentState;

	public void GotoState(AgentType agent, Type newStateType, params object[] args)
	{
		if(!newStateType.IsSubclassOf(typeof(BaseState)))
			throw new ArgumentException($"Invalid state type {newStateType.Name}");
		
		GotoState(agent, (AgentState<AgentType>) Activator.CreateInstance(newStateType), args);
	}
	
	public void GotoState<StateType>(AgentType agent, params object[] args) where StateType : AgentState<AgentType>
	{
		GotoState(agent, (StateType) Activator.CreateInstance(typeof(StateType)), args);
	}

	private void GotoState(AgentType agent, AgentState<AgentType> newState, params object[] args)
	{
		_currentState?.OnLeft();

		_currentState = newState;
		//_currentState.Agent = (AgentType)this; // TODO: This should be possible somehow surely
		_currentState.Agent = agent;
		_currentState.OnEntered(args);
	}

	protected virtual void Update()
	{
		_currentState.OnTick();
	}

	protected bool IsInState<T>() where T : BaseState
	{
		return _currentState.GetType().IsAssignableFrom(typeof(T)) || _currentState.GetType().IsSubclassOf(typeof(T));
	}
}