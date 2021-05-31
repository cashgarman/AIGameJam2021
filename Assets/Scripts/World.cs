using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Programming;
using UnityEngine;

public class World : MonoBehaviour
{
	private void Update()
	{
		Time.timeScale = Input.GetKey(KeyCode.Tab) ? 20f : 1f;
	}

	private static Type GetTypeFromName(string type)
	{
		return type switch
		{
			"charger" => typeof(Charger),
			"battery" => typeof(Battery),
			"robot" => typeof(Robot),
			_ => throw new Exception($"Tried to get type from unknown name {type}")
		};
	}

	public static BaseAgent GetRandomAgent(string type)
	{
		return Util.RandomElement(FindObjectsOfType(GetTypeFromName(type))) as BaseAgent;
	}

	public static BaseAgent GetClosestAgent(string type, IComputer computer, string filter = null)
	{
		return FindObjectsOfType(GetTypeFromName(type)).Select(a => a as BaseAgent)
			.Where(a => a != computer)	// NOTE: What if we want to get ourselves as the closest agent?
			.OrderBy(a => Vector3.Distance(a.transform.position, computer.transform.position))
			.FirstOrDefault();
	}

	public static BaseAgent GetAgentSortedBy(string type, IComputer computer, string sortType)
	{
		return FindObjectsOfType(GetTypeFromName(type)).Select(a => a as BaseAgent)
			.OrderBy(a => a.GetOrderBy(sortType))
			.FirstOrDefault();
	}

	public static IEnumerable<BaseAgent> GetClosestAgents(string type, IComputer computer, string filter)
	{
		return FindObjectsOfType(GetTypeFromName(type)).Select(a => a as BaseAgent)
			.Where(a => a != computer)	// NOTE: What if we want to get ourselves as the closest agent?
			.Where(a => a.FilterBy(filter, computer))
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
		};

		return null;
	}
}
