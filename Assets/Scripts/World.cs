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

	public static BaseAgent GetRandomAgent(string type)
	{
		return Util.RandomElement(FindObjectsOfType(GetTypeFromName(type))) as BaseAgent;
	}

	private static Type GetTypeFromName(string type)
	{
		return type switch
		{
			"charger" => typeof(Charger),
			"battery" => typeof(Battery),
			_ => throw new Exception($"Tried to get type from unknown name {type}")
		};
	}

	public static BaseAgent GetClosestAgent(string type, IComputer computer, string filter = null)
	{
		return FindObjectsOfType(GetTypeFromName(type)).Select(a => a as BaseAgent)
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
			.Where(a => a.FilterBy(filter))
			.OrderBy(a => Vector3.Distance(a.transform.position, computer.transform.position));
	}
}
