using System;
using System.Collections.Generic;
using System.Linq;
using Programming;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace UI
{
    public class CommandLineUI : MonoBehaviour
    {
        [SerializeField] private GameObject _commandLineContainer;
        [SerializeField] private TMP_InputField _commandLineInput;
        [SerializeField] private CommandLineErrorUI _errorDisplay;
        [SerializeField] private GameObject _codeInput;

        private bool _visible;
        private List<string> _commandHistory = new List<string>();
        private int _commandHistoryIndex;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                // Prevent changing focus when editing code
                if (EventSystem.current.currentSelectedGameObject != _codeInput)
                {
                    if (!_visible)
                    {
                        _commandLineContainer.SetActive(true);
                        _commandLineInput.ActivateInputField();
                        _visible = true;
                    }
                    else
                    {
                        _commandLineInput.ActivateInputField();
                        RunCommand();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if(_commandHistory.Any())
                {
                    _commandLineInput.text = _commandHistory[_commandHistoryIndex];
                    _commandHistoryIndex = Math.Max(0, _commandHistoryIndex - 1);
                }
            }
        }

        public void RunCommand()
        {
            // Make sure there is a command entered
            if (string.IsNullOrEmpty(_commandLineInput.text))
                return;
            
            try
            {
                // Compile the command
                var program = Compiler.Compile(_commandLineInput.text);
                program.RunOnce = true;

                // Run the command on the currently controlled agent
                Game.ControlledRobot.RunProgram(program);

                // Clear the error
                _errorDisplay.Hide();
                
                // Add the command to the history
                _commandHistory.Add(_commandLineInput.text);
                _commandHistoryIndex = _commandHistory.Count - 1;
                
                // Clear the command
                _commandLineInput.text = "";
            }
            catch (Exception e)
            {
                // Display the error
                _errorDisplay.Show(e.Message);
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
                throw e;
            }
        }
    }
}
