using System;
using DG.Tweening;
using Programming;
using TMPro;
using UnityEngine;

public class ProgrammingUI : MonoBehaviour
{
    private static ProgrammingUI _instance;

    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private TMP_InputField _codeInput;

    private CanvasGroup _container;
    private Robot _robot;
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _codeStatusText;

    private void Awake()
    {
        _instance = this;
        _container = GetComponentInChildren<CanvasGroup>();
    }

    private void Start()
    {
        _errorText.text = "";
        _container.alpha = 0;
        _container.gameObject.SetActive(false);
    }
    
    private void Update()
    {
        if (_robot == null)
            return;
        
        _codeStatusText.text = _robot.IsRunningProgram ? "Code: <color=green>running</color>" :"Code: <color=red>stopped</color>";
    }

    public static void Show(Robot robot, bool force = false)
    {
        _instance.ShowInternal(robot, force);
    }

    private void ShowInternal(Robot robot, bool force)
    {
        if (_robot != null && !force)
            return;

        if (_robot != null && _robot.IsEditingCode)
        {
            _robot.StartRobot();
            _robot.Program?.Resume(_robot);
            _robot.IsEditingCode = false;
        }
        
        _robot = robot;
        
        _container.gameObject.SetActive(true);
        _container.DOFade(1, _fadeDuration);

        PopulateCode(robot);
    }
    
    private static void Hide()
    {
        // TODO: Ask to save any changes?
        _instance.HideInternal();
    }

    public void OnCloseClicked()
    {
        _robot = null;
        Hide();
    }

    private void HideInternal()
    {
        if (_robot != null)
            return;
        
        _container.DOFade(0, _fadeDuration).OnComplete(() =>
        {
            _container.gameObject.SetActive(false);
        });
    }

    private void PopulateCode(Robot robot)
    {
        _titleText.text = $"{robot.Name} code";
        _codeInput.text = robot.Program != null ? robot.Program.Code : "";
    }

    public void OnStopClicked()
    {
        Debug.Log($"Stopping the program running on {_robot.Name}");
        _robot.Program?.Pause();
        _robot.StopRobot();
        _codeInput.interactable = true;
        _robot.IsEditingCode = true;
    }
    
    public void OnStartClicked()
    {
        Debug.Log($"Compiling the program on {_robot.Name}");
        _codeInput.interactable = false;

        Program newProgram;
        try
        {
            // Try compiling the program
            newProgram = Compiler.Compile(_codeInput.text);
        }
        catch (Exception e)
        {
            OnError(e.Message);
            _codeInput.interactable = true;
            return;
        }
        
        Debug.Log($"Starting the program on {_robot.Name}");
        
        // Load the new program into the robot
        _robot.Program = newProgram;

        // Try to start the program on the robot
        try
        {
            _robot.StartRobot();
            newProgram.StartMain(_robot);
            newProgram.RunCurrentLine(_robot);
        }
        catch (Exception e)
        {
            OnError(e.Message);
            _codeInput.interactable = true;
        }
        
        _robot.IsEditingCode = false;
    }

    private void OnError(string message)
    {
        _errorText.text = message;
        _errorText.gameObject.SetActive(true);
    }

    public void DismissError()
    {
        _errorText.gameObject.SetActive(false);
    }
}
