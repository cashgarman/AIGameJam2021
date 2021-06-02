using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agents;
using Programming;
using Sirenix.OdinInspector;
using UnityEngine;

public class AgentInteraction
{
	public BaseAgent Agent { get; }
	public float TimeInteractedWith { get; }

	public AgentInteraction(BaseAgent agent)
	{
		Agent = agent;
		TimeInteractedWith = Time.time;
	}
}

public abstract class BaseAgent : SerializedMonoBehaviour
{
	public abstract Transform PluginSlot { get; }
	public abstract string Name { get; }
	public abstract bool IsPluggedIn { get; set; }
	public abstract BaseAgent PluggedIn { get; set; }

	public abstract float GetOrderBy(string sortType);
	public abstract bool FilterBy(string filter, IComputer computer);

	public virtual void OnClicked()
	{
	}

	public virtual void OnObjectPluggedIn(BaseAgent subject)
	{
		PluggedIn = subject;
	}

	public virtual void OnObjectUnplugged(BaseAgent subject)
	{
		PluggedIn = null;
	}
}

public abstract class Agent<AgentType> : BaseAgent where AgentType : BaseAgent
{
	protected AgentState<AgentType> _currentState;
	private List<AgentInteraction> _recentlyInteractedAgents = new List<AgentInteraction>();
	[SerializeField] private float _agentInteractionCooldown = 5f;

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
		_currentState?.OnTick();
		
		// Update the recently interacted with agents list
		foreach (var interactedAgent in _recentlyInteractedAgents.ToArray())
		{
			// If enough time has passed
			if (Time.time - interactedAgent.TimeInteractedWith >= _agentInteractionCooldown)
				_recentlyInteractedAgents.Remove(interactedAgent);
		}
	}

	protected bool IsInState<T>() where T : BaseState
	{
		return _currentState.GetType().IsAssignableFrom(typeof(T)) || _currentState.GetType().IsSubclassOf(typeof(T));
	}
	
	public bool HasInteractedWithRecently(BaseAgent agent)
	{
		// If the agent is in the recently interacted with list
		return _recentlyInteractedAgents.Any(recentlyInteractedAgent => recentlyInteractedAgent.Agent == agent);
	}

	public void AddAgentInteraction(BaseAgent agent)
	{
		// Add the agent to the list of agents recently interacted with
		_recentlyInteractedAgents.Add(new AgentInteraction(agent));
	}
}