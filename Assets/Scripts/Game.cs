using System;
using Agents;
using Programming;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

public class Game : SerializedMonoBehaviour
{
	private static Game _instance;
	
	[SerializeField] private IComputer _initiallyControlledAgent;
	[SerializeField] private bool _mouseMovementEnabled;

	public static IComputer ControlledAgent { get; set; }

	private void Awake()
	{
		_instance = this;
		ControlledAgent = _initiallyControlledAgent;
	}

	private void Update()
	{
		if (_mouseMovementEnabled && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			// CREDIT: https://answers.unity.com/questions/1386777/get-origin-of-plane-in-world-coordinates.html
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit))
			{
				(ControlledAgent as Robot).MoveToPositionWithCallback(hit.point, null);
			}
		}
	}
}