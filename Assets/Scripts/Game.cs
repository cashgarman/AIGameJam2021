using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Game : SerializedMonoBehaviour
{
	private static Game _instance;
	
	[SerializeField] private Robot _initiallyControlledAgent;
	[SerializeField] private bool _mouseMovementEnabled;
	[SerializeField] private int _startingCoins;
	[SerializeField] private TMP_Text _coinText;

	private static Robot _controlledRobot;
	private Robot[] _allRobots;
	private int _currentRobotIndex;

	public static Robot ControlledRobot
	{
		get => _controlledRobot;
		set
		{
			_controlledRobot = value;
			FindObjectOfType<CameraController>().Target = value;
		}
	}
	public static int Coins { get; set; }

	private void Awake()
	{
		_instance = this;
		ControlledRobot = _initiallyControlledAgent;
	}

	private void Start()
	{
		Coins = _startingCoins;
		UpdateUI();
		
		_allRobots = FindObjectsOfType<Robot>();
		_currentRobotIndex = _allRobots.IndexOf(r => r == ControlledRobot);
	}

	private void Update()
	{
		Time.timeScale = Input.GetKey(KeyCode.LeftShift) ? 20f : 1f;

		if (!ControlledRobot.IsKnockedOut && !ControlledRobot.IsRunningProgram && _mouseMovementEnabled && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
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

		if (Input.GetKeyDown(KeyCode.Tab))
		{
			++_currentRobotIndex;
			if (_currentRobotIndex == _allRobots.Length)
				_currentRobotIndex = 0;

			PossessRobot(_allRobots[_currentRobotIndex]);
		}

		if (Input.GetKeyDown(KeyCode.F1))
		{
			ToggleCurrentRobotProgramRunning();
		}

		UpdateUI();
	}

	private static void PossessRobot(Robot robot)
	{
		ControlledRobot = robot;
	}

	public void ToggleCurrentRobotProgramRunning()
	{
		if(!ControlledRobot.Program.Interrupted)
			ControlledRobot.Program.Pause();
		else
			ControlledRobot.Program.Resume(ControlledRobot);
	}

	private void UpdateUI()
	{
		_coinText.text = $"Coins: {Coins:n0}";
	}
	
	public void ResumeControlledRobotProgram()
	{
		// Resume the controlled robot's program
		ControlledRobot.Program.Resume(ControlledRobot);
	}
}