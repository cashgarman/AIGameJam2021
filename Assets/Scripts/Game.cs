using System;
using System.Linq;
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
	private Robot[] _allPlayerRobots;
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
		
		_allPlayerRobots = FindObjectsOfType<Robot>().Where(r => r.Team == Robot.RobotTeam.Player).ToArray();
		_currentRobotIndex = _allPlayerRobots.IndexOf(r => r == ControlledRobot);
	}

	private void Update()
	{
		// Update the list of player robots in case they acquired a new one
		_allPlayerRobots = FindObjectsOfType<Robot>().Where(r => r.Team == Robot.RobotTeam.Player).ToArray();

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
			if (_currentRobotIndex == _allPlayerRobots.Length)
				_currentRobotIndex = 0;

			PossessRobot(_allPlayerRobots[_currentRobotIndex]);
		}

		if (Input.GetKeyDown(KeyCode.F1))
		{
			ToggleCurrentRobotProgramRunning();
		}

		UpdateUI();
	}

	public static void PossessRobot(Robot robot)
	{
		ControlledRobot = robot;
		ProgrammingUI.Show(robot, true);
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
	
	private void OnGUI()
	{
		return;
		
		var allRobots = FindObjectsOfType<Robot>();

		var numPolice = allRobots.Where(r => r.Name.ToLower().Contains("police"));
		var numPedestrians = allRobots.Where(r => r.Name.ToLower().Contains("pedestrian"));
		var numOurs = allRobots.Where(r => r.Name.ToLower().Contains("crime"));

		var style = new GUIStyle(GUI.skin.label)
		{
			fontSize = 24
		};
        
		GUILayout.Label($"Robots: {allRobots.Length}", style);
		GUILayout.Label($"Police: {numPolice.Count()}", style);
		GUILayout.Label($"Pedestrians: {numPedestrians.Count()}", style);
		GUILayout.Label($"Ours: {numOurs.Count()}", style);
	}

	public static void OnAgentSelected(BaseAgent agent)
	{
		if(agent is Robot robot)
			ProgrammingUI.Show(robot);
	}
}