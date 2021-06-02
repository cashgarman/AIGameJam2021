using System;
using System.Collections.Generic;
using System.Linq;
using Agents;
using Programming;
using UnityEngine;

public class World : MonoBehaviour
{
	private void Update()
	{
		Time.timeScale = Input.GetKey(KeyCode.Tab) ? 20f : 1f;
	}

	private static Type GetTypeFromName(string type, IComputer computer)
	{
		return type switch
		{
			"charger" => typeof(Charger),
			"battery" => typeof(Battery),
			"robot" => typeof(Robot),
			"parkingmeter" => typeof(ParkingMeter),
			"waypoint" => typeof(Waypoint),
			_ => throw new Exception($"Tried to get type from unknown name {type}")
		};
	}

	public static BaseAgent GetRandomAgent(string type, IComputer computer)
	{
		return Util.RandomElement(FindObjectsOfType(GetTypeFromName(type, computer)).Where(a => a != computer)) as BaseAgent;
	}

	public static IEnumerable<BaseAgent> GetRandomAgents(string type, IComputer computer)
	{
		return FindObjectsOfType(GetTypeFromName(type, computer)).Where(a => a != computer).Select(a => a as BaseAgent).Shuffle();
	}

	public static BaseAgent GetClosestAgent(string type, IComputer computer, string filter = null, bool includeSelf = false)
	{
		return FindObjectsOfType(GetTypeFromName(type, computer)).Select(a => a as BaseAgent)
			.Where(a => a != computer || includeSelf)
			.OrderBy(a => Vector3.Distance(a.transform.position, computer.transform.position))
			.FirstOrDefault();
	}
	
	public static BaseAgent GetClosestAgent(IEnumerable<BaseAgent> agents, IComputer computer, bool includeSelf = false)
	{
		return agents
			.Where(a => a != computer || includeSelf)
			.OrderBy(a => Vector3.Distance(a.transform.position, computer.transform.position))
			.FirstOrDefault();
	}

	public static BaseAgent GetAgentSortedBy(string type, IComputer computer, string sortType)
	{
		return FindObjectsOfType(GetTypeFromName(type, computer)).Select(a => a as BaseAgent)
			.OrderBy(a => a.GetOrderBy(sortType))
			.FirstOrDefault();
	}

	public static IEnumerable<BaseAgent> GetClosestAgents(string type, IComputer computer, string filter = null, bool includeSelf = false)
	{
		return FindObjectsOfType(GetTypeFromName(type, computer)).Select(a => a as BaseAgent)
			.Where(a => a != computer || includeSelf)
			.Where(a => filter == null || a.FilterBy(filter, computer))
			.OrderBy(a => Vector3.Distance(a.transform.position, computer.transform.position));
	}

	public static IEnumerable<BaseAgent> FilterAgents(IEnumerable<BaseAgent> agents, IComputer computer, string filter)
	{
		return agents.Where(a => a.FilterBy(filter, computer));
	}

	public static BaseAgent FindSubject(Node subject, IComputer computer)
	{
		switch(subject)
		{
			case SubjectNode subjectNode:
				return GetClosestAgent(subjectNode.Word, computer);
			case PredicateNode predicateNode:
				predicateNode.Run(computer);
				return predicateNode.Subjects.FirstOrDefault();
		};

		return null;
	}

	public static void BroadcastShout<AgentType>(Agent<AgentType>.ShoutType shout, Agent<AgentType> shouter, float range) where AgentType : BaseAgent
	{
		// Foreach agent in the world that's in range
		foreach (var agent in FindObjectsOfType<BaseAgent>()
			.Where(a => Vector3.Distance(a.transform.position, shouter.transform.position) < range))
		{
			// Let the agent know about the shout
			agent.OnHeardShout(shout, shouter);
		}
	}
}
