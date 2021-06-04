using System;
using System.Collections.Generic;
using System.Linq;
using Agents;
using UnityEngine;

namespace Programming
{
	public class CodeLine
	{
		public enum LineStatus
		{
			Running,
			Done,
			Pending
		}
		
		public List<Token> Tokens { get; } = new List<Token>();
		public BranchNode RootNode { get; set; }
		public LineStatus Status { get; set; }

		public void AddToken(Token token)
		{
			Tokens.Add(token);
		}

		public override string ToString()
		{
			return string.Join(" ", Tokens.Select(t => t.Word));
		}

		public bool Run(Program program)
		{
			//Debug.Log($"Running code '{this}' on {program.Computer.name}");
			Status = LineStatus.Running;
			return RootNode.Run(program);
		}
	}

	public class CodeSection
	{
		public enum BlockType
		{
			Main,
			Conditional,
		}

		public BlockType Type { get; }
		public List<CodeLine> Lines { get; } = new List<CodeLine>();
		public bool IsIfStatement => Lines[0].Tokens[0].Word == "if";
		public bool IsHeardStatement => Lines[0].Tokens[0].Word == "heard";
		public CodeLine FirstLine => Lines[0];

		public CodeSection(BlockType type = BlockType.Main)
		{
			Type = type;
		}

		public void AddLine(CodeLine line)
		{
			Lines.Add(line);
		}
	}

	[Serializable]
	public class Program
	{
		public class LineOfCode
		{
			public enum LineStatus
			{
				Running,
				Done,
				Pending
			}

			public string Code { get; }
			public LineStatus Status { get; set; }
			private BranchNode RootNode { get; }

			public LineOfCode(string code, BranchNode rootNode)
			{
				Code = code;
				RootNode = rootNode;
				Status = LineStatus.Pending;
			}

			public void Run(Program program)
			{
				Debug.Log($"Running code '{Code}' on {program.Computer.name}");
				Status = LineStatus.Running;
				RootNode.Run(program);
			}
		}

		private int _currentLine = 0;
		private bool _shouldLoop = true;
		private bool _halted;
		private CodeSection _currentSection;
		private int _interruptedLine;
		private CodeSection _interruptedSection;
		public CodeSection MainSection => Sections.FirstOrDefault(s => s.Type == CodeSection.BlockType.Main);

		public List<CodeLine> CurrentLines => _currentSection.Lines;
		public CodeLine CurrentLine => _currentSection?.Lines[_currentLine];
		public List<CodeSection> Sections { get; } = new List<CodeSection>();
		public bool Interrupted { get; set; }
		public bool RunOnce { get; set; }
		public IComputer Computer { get; private set; }
		public BaseAgent Notifier { get; set; }
		public BaseAgent Target { get; set; }
		public string Code { get; set; }

		public Program(string code)
		{
			Code = code;
		}

		// public void AddLine(string code, BranchNode rootNode)
		// {
		// 	Debug.Log($"Added a line of code '{code}' to the program with root node: {rootNode.Word}");
		// 	CurrentLines.Add(new LineOfCode(code, rootNode));
		// }

		public void StartMain(IComputer computer)
		{
			Computer = computer;
			_currentSection = Sections.FirstOrDefault(s => s.Type == CodeSection.BlockType.Main);
			if (_currentSection == null)
				throw new Exception("No main section found in program");
		}

		public void RunCurrentLine(IComputer computer)
		{
			if (!CurrentLines.Any())
				return;

			if (_halted)
				return;

			// Run the current line of code
			CurrentLines[_currentLine].Run(this);
		}

		public void GotoNextLine()
		{
			if (_halted)
				return;

			// Move to the next line of code
			_currentLine++;

			// If the current section of code has finished
			if (_currentLine == CurrentLines.Count)
			{
				Debug.Log($"{Computer.name}'s program section finished");

				// If the current section is an if statement
				if (_currentSection.Type == CodeSection.BlockType.Conditional)
				{
					// Return to the last line of code in the main code section
					StartMain(Computer);
					_currentLine = _interruptedLine;
					Interrupted = false;
				}
				// TODO: This is where we could add loops
				// If the current section is the main section
				else
				{
					// If we should loop the program
					if (!RunOnce)
						// Restart the current section of code
						RestartSection();
					else
						_halted = true;
				}
			}
		}

		private void RestartSection()
		{
			_currentLine = 0;
			foreach (var line in CurrentLines)
			{
				line.Status = CodeLine.LineStatus.Pending;
			}
		}

		public void InterruptAndJumpToSection(CodeSection section)
		{
			// Store the interrupted line of code so we can return to it
			_interruptedLine = _currentLine;
			// TODO: Should I store the interrupted section as well?

			// Interrupt the program and jump to the section of code
			Interrupted = true;
			_currentSection = section;

			// Skip the first line of code (because right now, it would have been the if statement that caused the interrupt)
			_currentLine = 1;
		}

		public void Pause()
		{
			if (_halted)
				return;
			
			Debug.Log($"Pausing program on line {CurrentLine}");
			
			// Store the interrupted line and section of code so we can return to it
			_interruptedSection = _currentSection;
			_interruptedLine = _currentLine;

			// Interrupt the program and jump to the section of code
			Interrupted = true;
		}

		public void Resume(IComputer computer)
		{
			if (_halted)
				return;
			
			// Return to the last line and section of code in the main code section
			_currentSection = _interruptedSection ?? MainSection;
			_currentLine = _interruptedLine;
			Interrupted = false;
			RunCurrentLine(computer);
			
			Debug.Log($"Resumed program on line {CurrentLine}");
		}

		public void AddSection(CodeSection section)
		{
			Sections.Add(section);
		}
	}
}