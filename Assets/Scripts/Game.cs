using System;
using Agents;
using Programming;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

public class Game : SerializedMonoBehaviour
{
	private static Game _instance;
	
	[SerializeField] private Robot _initiallyControlledAgent;
	[SerializeField] private bool _mouseMovementEnabled;

	public static Robot ControlledRobot { get; set; }

	private void Awake()
	{
		_instance = this;
		ControlledRobot = _initiallyControlledAgent;
	}

	private void Update()
	{
		if (_mouseMovementEnabled && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			// CREDIT: https://answers.unity.com/questions/1386777/get-origin-of-plane-in-world-coordinates.html
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit))
			{
				// Pause the robot's program and move to the targeted location
				ControlledRobot.Program?.Pause();
				ControlledRobot.MoveToPositionWithCallback(hit.point, null);
			}
		}
		
		// TODO: Make this a button or automatic when changing controlled robot
		if (Input.GetKeyDown(KeyCode.F5))
		{
			ResumeControlledRobotProgram();
		}
	}

	public void ResumeControlledRobotProgram()
	{
		// Resume the controlled robot's program
		ControlledRobot.Program.Resume(ControlledRobot);
	}
}