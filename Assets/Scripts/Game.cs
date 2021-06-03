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
	}

	private void Update()
	{
		if (!ControlledRobot.IsKnockedOut && _mouseMovementEnabled && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
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

		UpdateUI();
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