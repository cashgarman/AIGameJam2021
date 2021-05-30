using System;
using System.Linq;
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
			_ => throw new Exception($"Tried to get type from unknown name {type}")
		};
	}

	public static BaseAgent GetClosestAgent(string type, IComputer computer)
	{
		return FindObjectsOfType(GetTypeFromName(type)).Select(a => a as BaseAgent)
			.OrderBy(a => Vector3.Distance(a.transform.position, computer.transform.position)).FirstOrDefault();
	}
}
