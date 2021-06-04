using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agents;
using Programming;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class AgentInteraction
{
	public BaseAgent Agent { get; }
	public float NextInteractionCooldown { get; set; }

	public AgentInteraction(BaseAgent agent, float cooldown)
	{
		Agent = agent;
		NextInteractionCooldown = cooldown;
	}
}

public abstract class BaseAgent : SerializedMonoBehaviour
{
	public enum ShoutType
	{
		CrimeInProgress
	}
	
	[SerializeField] private float _shoutRange = 30f;
	[SerializeField] private float _criminalFlagCooldown = 30f;
	
	public abstract Transform PluginSlot { get; }
	public abstract string Name { get; }
	public abstract BaseAgent PluggedInto { get; set; }
	public bool IsCriminal { get; private set; }
	public abstract float AlreadyVisitedCooldownMultiplier { get; }

	// Animation
	private Animator _animator;
	
	// Health
	private float _health;
	[SerializeField] private float _maxHealth;
	[SerializeField] private float _healthOverride;
	[SerializeField] private Slider _healthDisplay;
	public bool IsKnockedOut { get; private set; }
	public bool Disabled { get; set; }

	public float Health
	{
		get => _health;
		set
		{
			_health = Mathf.Clamp(value, 0f, _maxHealth);
			if(_healthDisplay != null)
				_healthDisplay.value = _health / _maxHealth;
		}
	}
	
	[SerializeField] private HomeAgent _home;
	private Coroutine _criminalCoroutine;
	private float _timeOfLastClick;
	[SerializeField] private float _doubleClickTime = 0.5f;
	public HomeAgent Home => _home;

	protected virtual void Awake()
	{
		_animator = GetComponent<Animator>();
	}
	
	protected virtual void Start()
	{
		// Set the initial health
		Health = _healthOverride > 0 ? _healthOverride : _maxHealth;
	}
	
	public virtual float GetOrderBy(string sortType)
	{
		return 1f;
	}

	public virtual bool FilterBy(string filter, IComputer computer)
	{
		return filter switch
		{
			"next" => !computer.HasInteractedWithRecently(this),
			"alive" => !IsKnockedOut,
			"dead" => IsKnockedOut,
			_ => throw new Exception($"Invalid filter provided to {GetType().Name}: {filter}")
		};
	}

	public virtual void OnClicked()
	{
		if (_timeOfLastClick < _doubleClickTime)
			OnDoubleClick();

		_timeOfLastClick = Time.time;
	}

	protected virtual void OnDoubleClick()
	{
		Debug.Log($"Double clicked {name}");
	}

	public virtual void OnObjectPluggedIn(BaseAgent subject)
	{
		PluggedInto = subject;
	}

	public virtual void OnObjectUnplugged(BaseAgent subject)
	{
		PluggedInto = null;
	}
	
	public void UnplugFromSelf(BaseAgent subject, Action onComplete = null)
	{
		// The subject should be plugged into the agent already
		if (PluggedInto != subject)
			throw new Exception($"Tried to unplug {subject} that wasn't plugged into {Name}");
        
		// Turn on the subject's physics
		subject.SetPhysicsEnabled(true);
        
		// Unplug the subject from this agent
		Debug.Log($"{name} is unplugging {subject} from themselves");
		subject.PluggedInto = null;
		PluggedInto = null;
		subject.transform.SetParent(null);
		subject.OnObjectUnplugged(subject);
        
		// TODO: Wait for any animations and whatnot
		onComplete?.Invoke();
	}

	public virtual void OnHeardShout(ShoutType shout, BaseAgent shouter)
	{
		// Debug.Log($"{Name} heard shout {shout} from {shouter.Name}");
	}

	protected void Shout(ShoutType shout)
	{
		Debug.Log($"{Name} shouted {shout}");
		World.BroadcastShout(shout, this, _shoutRange);
	}
	
	public void FlagACriminal()
	{
		if(_criminalCoroutine != null)
			StopCoroutine(_criminalCoroutine);
		
		_criminalCoroutine = StartCoroutine(FlagACriminalInternal());
	}

	private IEnumerator FlagACriminalInternal()
	{
		// Agent is a criminal now		
		IsCriminal = true;

		// Wait for the criminal cooldown
		yield return new WaitForSeconds(_criminalFlagCooldown);

		// Agent is no longer a criminal
		IsCriminal = false;
	}

	public void PlayAnimationTrigger(string triggerName)
	{
		Debug.Log($"Triggering animation trigger {triggerName} on {Name}");
		_animator.SetTrigger(triggerName);
	}

	public void ApplyDamage(float amount)
	{
		Health -= amount;
		
		// Check if the agent is knocked out
		if (Health <= 0f)
		{
			IsKnockedOut = true;
			_animator.SetTrigger("Death");
		}
	}

	public void SetPhysicsEnabled(bool enabled)
	{
		var rigidbody = gameObject.GetComponent<Rigidbody>();
		rigidbody.useGravity = enabled;
		rigidbody.isKinematic = !enabled;
	}
}

public abstract class Agent<AgentType> : BaseAgent where AgentType : BaseAgent
{
	protected AgentState<AgentType> _currentState;
	private List<AgentInteraction> _recentlyInteractedAgents = new List<AgentInteraction>();
	[SerializeField] private float _agentInteractionCooldown = 5f;
	[SerializeField] protected float _minInteractionDistance = 3f;
	
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
			interactedAgent.NextInteractionCooldown -= Time.deltaTime;
			
			// If enough time has passed
			if (interactedAgent.NextInteractionCooldown <= 0f)
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
		// If the agent is already in the list of recently interacted agents
		var existingAgentInteraction = _recentlyInteractedAgents.FirstOrDefault(a => a.Agent == agent);
		if (existingAgentInteraction != null)
		{
			existingAgentInteraction.NextInteractionCooldown = _agentInteractionCooldown * agent.AlreadyVisitedCooldownMultiplier;
		}
		else
			// Add the agent to the list of agents recently interacted with
			_recentlyInteractedAgents.Add(new AgentInteraction(agent, _agentInteractionCooldown * agent.AlreadyVisitedCooldownMultiplier));
	}

	public bool InRange(BaseAgent target, float? rangeOverride = null)
	{
		return Vector3.Distance(transform.position, target.transform.position) <= (rangeOverride ?? _minInteractionDistance);
	}
}