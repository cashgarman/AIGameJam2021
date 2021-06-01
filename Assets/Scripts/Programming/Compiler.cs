using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Programming
{
	public class Token
	{
		public enum TokenType
		{
			Conditional,
			Equality,
			Noun,
			Subject,
			Description,
			Conjunction,
			Verb,
			Adjective,
			Filter,
			Interaction,
			InteractionDeliminator,
			Newline,
			Delay,
		}

		public string Word { get; }
		public TokenType Type { get; }
		public string Deliminator { get; }
		public bool Indented { get; set; }

		public Token(string word, TokenType tokenType, string deliminator, bool indented)
		{
			Word = word;
			Type = tokenType;
			Deliminator = deliminator;
			Indented = indented;
		}

		public override string ToString()
		{
			return $"{Word} ({Type})";
		}
	}

	public class NewlineToken : Token
	{
		public NewlineToken() : base("", TokenType.Newline, "", false)
		{
		}
	}

	[Serializable]
	public class TokenDefinition
	{
		public string word;
		public Token.TokenType type;
		public string deliminator;
		public string[] synonyms;
	}

	public class Compiler : SerializedMonoBehaviour
	{
		private static Compiler _instance;
		
		[SerializeField] private TMP_InputField _commandInput;
		[SerializeField] private Transform _tokenList;
		[SerializeField] private TokenUI _tokenPrefab;
		[SerializeField] private Dictionary<Token.TokenType, Color> _typeColours = new Dictionary<Token.TokenType, Color>();
		[SerializeField] private List<TokenDefinition> _tokens = new List<TokenDefinition>();

		private void Awake()
		{
			_instance = this;
		
			_commandInput.onValueChanged.AddListener(OnCommandChanged);
			
			_tokenList.ClearChildren();

			// Load all the valid tokens
			Util.PopulateArrayFromJSON(_tokens, Resources.Load<TextAsset>("tokens").text);
		}

		public static Program Compile(string code)
		{
			var program = new Program();
			
			// Handle empty programs
			if (string.IsNullOrEmpty(code))
				return new Program();
			
			// Tokenize the whole program
			var tokens = _instance.Tokenize(code);
			// Debug.Log(string.Join("\n", tokens.Select(t => $"{t.Word} ({t.Type})")));
			
			// Create the main section of the program
			var mainSection = new CodeSection();
			var currentSection = mainSection;
			program.AddSection(mainSection);
			
			// Iterate through all the tokens
			var tokenEnumerator = tokens.GetEnumerator();
			while (tokenEnumerator.MoveNext())
			{
				// If the token is the start of an if statement
				if (tokenEnumerator.Current.Type == Token.TokenType.Conditional)
				{
					// If we're already inside an if statement
					if (currentSection.Type == CodeSection.BlockType.Conditional)
						// Error, not supported
						throw new Exception("Nested if statements are not supported");
					
					// Create a new section for the if statement
					currentSection = new CodeSection(CodeSection.BlockType.Conditional);
					
					// Add the line of code to the current if statement
					currentSection.AddLine(GetLineOfTokens(tokenEnumerator));
					
					// Move to the next line
					continue;
				}
					
				// If we're inside an if statement
				if (currentSection.Type == CodeSection.BlockType.Conditional)
				{
					// If the first token of the line is indented
					if (tokenEnumerator.Current.Indented)
					{
						// Add the line of code to the current if statement
						currentSection.AddLine(GetLineOfTokens(tokenEnumerator));
					}
					// If the first token of the line is not indented
					else
					{
						// Add the if statement section to the program sections
						program.AddSection(currentSection);

						// We're not inside the if statement any more
						currentSection = mainSection;
						
						// Add the line of code to the current if statement
						currentSection.AddLine(GetLineOfTokens(tokenEnumerator));
					}
				}
				// If we're not inside an if statement
				else
				{
					// Add the line of code to the main section of the program
					currentSection.AddLine(GetLineOfTokens(tokenEnumerator));
				}
			}
			
			// If the current section is still an if statement
			if (currentSection.Type == CodeSection.BlockType.Conditional)
			{
				// Add the if statement section to the program sections
				program.AddSection(currentSection);
			}
			
			// For each section of the code
			foreach (var section in program.Sections)
			{
				// For each line in the section
				foreach (var line in section.Lines)
				{
					// Compile the line of code
					var rootNode = _instance.CreateNode(line.Tokens);
					if (!(rootNode is BranchNode))
						throw new Exception($"Invalid line of code '{line}': Root node is not executable");
					
					line.RootNode = rootNode as BranchNode;
				}
			}

			return program;
			
			// Break the program first into lines of code, and compile each line
			/*var lines = code.Split('\n');
			Debug.Log($"Code has {lines.Length} lines of code");

			var program = new Program();
			foreach (var line in lines)
			{
				var tokens = _instance.Tokenize(line);
				var rootNode = _instance.CreateNode(tokens);
				
				if (!(rootNode is BranchNode))
					throw new Exception($"Compiled a program and ended up with just a root leaf node");
				
				program.AddLine(line, rootNode as BranchNode);
			}

			return program;*/

			return null;
		}

		private static CodeLine GetLineOfTokens(IEnumerator<Token> tokenEnumerator)
		{
			var line = new CodeLine();
			while (tokenEnumerator.Current.Type != Token.TokenType.Newline)
			{
				line.AddToken(tokenEnumerator.Current);
				tokenEnumerator.MoveNext();
			}

			return line;
		}

		private IEnumerable<Token> Tokenize(string str)
		{
			var tokens = new List<Token>();
			
			// If the string is just a single word
			if (!str.Contains(' '))
			{
				// Try to find and add a token for the word
				tokens.Add(CreateTokenFromWord(str));
			}
			// If the string has multiple lines
			else if (str.Contains('\n'))
			{
				// Split the string into lines
				var lines = str.Split('\n');
				foreach (var line in lines)
				{
					tokens.AddRange(Tokenize(line));
					tokens.Add(new NewlineToken());
				}
			}
			// If the string is multiple words
			else
			{
				// Split the string into words and create tokens from them
				tokens.AddRange(str.Split(' ').Select(CreateTokenFromWord));
			}

			// Return the tokens
			return tokens;
		}

		private Token CreateTokenFromWord(string word)
		{
			if (string.IsNullOrEmpty(word))
				throw new Exception("Got an empty word while trying to convert to a token");
				
			word = word.ToLower();

			// Check if the token is indented
			var indented = word.Contains('\t');
			word = word.Replace("\t", "");
			
			// Check against tokens
			foreach (var token in _tokens)
			{
				// If the word matches any of the token's synonyms
				if (token.word.ToLower() == word || token.synonyms.Select(w => w.ToLower()).Contains(word))
					return new Token(token.word, token.type, token.deliminator, indented);
			}
			
			throw new Exception($"Couldn't find a matching token for word '{word}'");
		}

		private Node CreateNode(IEnumerable<Token> tokens)
		{
			// HACK: MEGAHACK!
			// HACK: Comparison nodes start with a noun, refactor this to be more flexible to grammar
			// TODO: Use regexes or something here instead?
			if (tokens.Count() > 1 && !tokens.Any(t => t.Type == Token.TokenType.Conditional || t.Type == Token.TokenType.Delay) && tokens.Any(t => t.Type == Token.TokenType.Equality))
			{
				// [Subject] (Equality) [Filter]
				var subjectTokens = tokens.TakeWhile(token => token.Word != "is" && token.Word != "not").ToArray();
				var equalityToken = tokens.Skip(subjectTokens.Length).Take(1).ToArray();
				var filterToken = tokens.Skip(subjectTokens.Length).Skip(1).ToArray();
				return new ComparisonNode(CreateNode(subjectTokens), CreateNode(equalityToken) as EqualityNode, CreateNode(filterToken) as FilterNode);
			}
			
			// Look at the first token
			var first = tokens.First();
			var rest = tokens.Count() > 1 ? tokens.Skip(1) : null;
			
			switch(first.Type)
			{
				case Token.TokenType.Noun:
					return new SubjectNode(first);
					break;
				case Token.TokenType.Subject:
					break;
				case Token.TokenType.Description:
					break;
				case Token.TokenType.Conjunction:
					break;
				case Token.TokenType.Verb:
					// (Verb) [Subject]
					return new ActionNode(new VerbNode(first), CreateNode(rest));
				case Token.TokenType.Adjective:
					// (Adjective) [Subject]
					return new QualifierNode(new AdjectiveNode(first), CreateNode(rest));
				case Token.TokenType.Filter:
					// HACK: Another shitty hack due to over-strict grammar matching
					if (rest == null)
						return new FilterNode(first);
					else
						// (Filter) [Subject]
						return new PredicateNode(new FilterNode(first), CreateNode(rest));
				case Token.TokenType.Interaction:
					// (Interaction) [SubjectA] (Deliminator) [SubjectB]
					var subjectATokens = rest.TakeWhile(token => token.Type != Token.TokenType.InteractionDeliminator && token.Word != first.Deliminator).ToArray();
					var subjectBTokens = rest.SkipWhile(token => token.Type != Token.TokenType.InteractionDeliminator && token.Word != first.Deliminator).Skip(1).ToArray();
					return new InteractionNode(new VerbNode(first), CreateNode(subjectATokens), CreateNode(subjectBTokens));
				case Token.TokenType.Conditional:
					// (Conditional) [Comparison]
					return new ConditionalNode(new IfNode(first), CreateNode(rest) as ComparisonNode);
				case Token.TokenType.Delay:
					// (Delay) [Comparison]
					return new DelayNode(new WaitUntilNode(first), CreateNode(rest) as ComparisonNode);
				case Token.TokenType.Equality:
					return new EqualityNode(first);
				default:
					throw new ArgumentOutOfRangeException();
			}

			throw new Exception($"Couldn't created a node with tokens: {string.Join(", ", tokens.Select(t => t.Word))}");
		}

		private void OnCommandChanged(string command)
		{
			Debug.Log($"Command: {command}");

			// AddTokensToUI(ParseTokens(command));
		}

		private void AddTokensToUI(IEnumerable<Token> tokens)
		{
			_tokenList.ClearChildren();
			foreach (var token in tokens)
			{
				AddTokenToUI(token);
			}
		}

		private void AddTokenToUI(Token token)
		{
			Instantiate(_tokenPrefab, _tokenList).Init(token);
			RefreshLayoutGroupsImmediateAndRecursive(_tokenList.gameObject);
		}

		// CREDIT: https://forum.unity.com/threads/force-immediate-layout-update.372630/

		public static void RefreshLayoutGroupsImmediateAndRecursive(GameObject root)
		{
			foreach (var layoutGroup in root.GetComponentsInChildren<LayoutGroup>())
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
			}
		}

		public static Color GetTokenTypeColor(Token.TokenType type)
		{
			_instance._typeColours.TryGetValue(type, out var colour);
			return colour;
		}
	}

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

		public bool Run(IComputer computer)
		{
			Debug.Log($"Running code '{this}' on {computer.name}");
			Status = LineStatus.Running;
			return RootNode.Run(computer);
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

			public void Run(IComputer computer)
			{
				Debug.Log($"Running code '{Code}' on {computer.name}");
				Status = LineStatus.Running;
				RootNode.Run(computer);
			}
		}

		private int _currentLine = 0;
		private bool _shouldLoop = true;
		private bool _halted;
		private CodeSection _currentSection;
		private int _interruptedLine;

		public List<CodeLine> CurrentLines => _currentSection.Lines;
		public List<CodeSection> Sections { get; } = new List<CodeSection>();
		public bool Interrupted { get; set; }

		public Program()
		{
		}

		// public void AddLine(string code, BranchNode rootNode)
		// {
		// 	Debug.Log($"Added a line of code '{code}' to the program with root node: {rootNode.Word}");
		// 	CurrentLines.Add(new LineOfCode(code, rootNode));
		// }

		public void StartMain()
		{
			_currentSection = Sections.First(s => s.Type == CodeSection.BlockType.Main);
		}

		public void RunCurrentLine(IComputer computer)
		{
			if (!CurrentLines.Any())
				return;
			
			if (_halted)
				return;
			
			// Run the current line of code
			CurrentLines[_currentLine].Run(computer);
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
				Debug.Log($"Program section finished");
				
				// If the current section is an if statement
				if (_currentSection.Type == CodeSection.BlockType.Conditional)
				{
					// Return to the last line of code in the main code section
					StartMain();
					_currentLine = _interruptedLine;
					Interrupted = false;
				}
				// TODO: This is where we could add loops
				// If the current section is the main section
				else
				{
					// Restart the current section of code
					RestartSection();
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
			
			// Interrupt the program and jump to the section of code
			Interrupted = true;
			_currentSection = section;
			
			// Skip the first line of code (because right now, it would have been the if statement that caused the interrupt)
			_currentLine = 1;
		}

		public void AddSection(CodeSection section)
		{
			Sections.Add(section);
		}
	}

	public interface IComputer
	{
		Transform transform { get; }
		string name { get; set; }
		void ExecuteCommand(string command, params object[] arguments);
		bool HasInteractedWithRecently(BaseAgent agent);
	}

	internal class AdjectiveNode : LeafNode
	{
		public AdjectiveNode(Token token) : base(token)
		{
		}
	}

	internal class SubjectNode : LeafNode
	{
		public SubjectNode(Token token) : base(token)
		{
		}
	}

	internal class VerbNode : LeafNode
	{
		public VerbNode(Token token) : base(token)
		{
		}
	}
	
	internal class  FilterNode : LeafNode
	{
		public FilterNode(Token token) : base(token)
		{
		}
	}
	
	internal class IfNode : LeafNode
	{
		public IfNode(Token token) : base(token)
		{
		}
	}

	internal class EqualityNode : LeafNode
	{
		public EqualityNode(Token token) : base(token)
		{
		}
	}
	
	internal class WaitUntilNode : LeafNode
	{
		public WaitUntilNode(Token token) : base(token)
		{
		}
	}
	
	internal class ActionNode : BranchNode
	{
		private readonly VerbNode _verb;
		private readonly Node _subject;

		public ActionNode(VerbNode verb, Node subject)
		{
			Word = verb.Word;
			_verb = verb;
			_subject = subject;
		}

		public override bool Run(IComputer computer)
		{
			// Get the actual subject
			BaseAgent subject = null;
			switch(_subject)
			{
				case SubjectNode subjectNode:
					subject = World.FindSubject(subjectNode, computer);
					break;
				
				case QualifierNode qualifierNode:
					qualifierNode.Run(computer);
					subject = qualifierNode.Subject;
					break;
			};
			
			// Tell the computer to run the verb on the subject
			computer.ExecuteCommand(_verb.Word, subject);

			// HACK: This isn't used for non-if statements
			return false;
		}
	}

	internal class QualifierNode : BranchNode
	{
		private readonly AdjectiveNode _adjective;
		private readonly Node _subject;
		private BaseAgent _foundSubject;

		public QualifierNode(AdjectiveNode adjective, Node subject)
		{
			Word = adjective.Word;
			_adjective = adjective;
			_subject = subject;
		}

		public BaseAgent Subject => _foundSubject;

		public override bool Run(IComputer computer)
		{
			// Get the actual subject
			switch(_subject)
			{
				case SubjectNode subjectNode:
					_foundSubject = _adjective.Word switch
					{
						// TODO: Refactor this to use lists of BaseAgents everywhere, so we can support multiple-adjective filtering
						"random" => World.GetRandomAgent(subjectNode.Word, computer),
						"nearest" => World.GetClosestAgent(subjectNode.Word, computer),
						// TODO: Add fullest and emptiest adjectives
						_ => throw new Exception($"Unhandled adjective in qualifying node {_adjective.Word}")
					};
					break;
				case QualifierNode qualifierNode:
					qualifierNode.Run(computer);
					_foundSubject = qualifierNode.Subject;
					break;
				case PredicateNode predicateNode:
					predicateNode.Run(computer);
					_foundSubject = _adjective.Word switch
					{
						// TODO: Refactor this to use lists of BaseAgents everywhere, so we can support multiple-adjective filtering
						"random" => Util.RandomElement(predicateNode.Subjects),
						"nearest" => predicateNode.Subjects.OrderBy(s => Vector3.Distance(s.transform.position, computer.transform.position)).FirstOrDefault(),
						_ => throw new Exception($"Unhandled adjective in qualifying node {_adjective.Word}")
					};
					break;
			};
			
			// HACK: This isn't used for non-if statements
			return false;
		}
	}
	
	internal class PredicateNode : BranchNode
	{
		private readonly FilterNode _filter;
		private readonly Node _subject;
		
		private IEnumerable<BaseAgent> _foundSubjects;

		public PredicateNode(FilterNode filter, Node subject)
		{
			Word = filter.Word;
			_filter = filter;
			_subject = subject;
		}

		public IEnumerable<BaseAgent> Subjects => _foundSubjects;

		public override bool Run(IComputer computer)
		{
			// HACK: If the filter is self-referential 
			if (_filter.Word == "self")
			{
				// Return the calling computer as the subject
				_foundSubjects = new[] {computer as BaseAgent};

				// HACK: This isn't used for non-if statements
				return false;
			}
			
			// Get the subject
			switch(_subject)
			{
				case SubjectNode subjectNode:
					_foundSubjects = World.GetClosestAgents(subjectNode.Word, computer, _filter.Word);
					break;
				case PredicateNode predicateNode:
					predicateNode.Run(computer);
					_foundSubjects = World.FilterAgents(predicateNode.Subjects, computer, _filter.Word);
					break;
			};
			
			// HACK: This isn't used for non-if statements
			return false;
		}
	}
	
	internal class InteractionNode : BranchNode
	{
		private readonly VerbNode _verb;
		private readonly Node _subjectA;
		private readonly Node _subjectB;
		
		private BaseAgent _foundSubjectA;
		private BaseAgent _foundSubjectB;

		public InteractionNode(VerbNode verb, Node subjectA, Node subjectB)
		{
			Word = verb.Word;
			_verb = verb;
			_subjectA = subjectA;
			_subjectB = subjectB;
		}

		public override bool Run(IComputer computer)
		{
			// TODO: These FindSubjects need to be made to take into account Qualifiers and Predicates
			
			// Get subject A
			_foundSubjectA = World.FindSubject(_subjectA, computer);
			
			// Get subject B
			_foundSubjectB = World.FindSubject(_subjectB, computer);

			// Debug.Log($"Found subject A: {_foundSubjectA?.Name}");
			// Debug.Log($"Found subject B: {_foundSubjectB?.Name}");
			
			// Tell the computer to run the verb on the subject
			computer.ExecuteCommand(_verb.Word, _foundSubjectA, _foundSubjectB);
			
			// HACK: This isn't used for non-if statements
			return false;
		}
	}
	
	internal class ConditionalNode : BranchNode
	{
		private readonly IfNode _if;
		private readonly ComparisonNode _comparison;

		public ConditionalNode(IfNode @if, ComparisonNode comparison)
		{
			if (comparison == null)
				throw new Exception($"A conditional node was given an invalid comparison");
			
			Word = @if.Word;
			_if = @if;
			_comparison = comparison;
		}

		public override bool Run(IComputer computer)
		{
			// Return the result of the comparison
			return _comparison.Run(computer);
		}
	}
	
	internal class ComparisonNode : BranchNode
	{
		private readonly Node _subject;
		private readonly EqualityNode _equality;
		private readonly FilterNode _filter;

		public ComparisonNode(Node subject, EqualityNode equality, FilterNode filter)
		{
			if (equality == null)
				throw new Exception($"A comparison was given an invalid equality");
			
			if(filter == null)
				throw new Exception($"A comparison was given an invalid filter to compare against");
				
			Word = subject.Word;
			_subject = subject;
			_equality = equality;
			_filter = filter;
		}

		public override bool Run(IComputer computer)
		{
			// Get the actual subject
			BaseAgent subject = null;
			switch(_subject)
			{
				// TODO: Refactor all these to create some kind of chain of subjects/qualifiers/predicates that can handle any amount in any order
				case SubjectNode subjectNode:
					subject = World.FindSubject(subjectNode, computer);
					break;
				
				case QualifierNode qualifierNode:
					qualifierNode.Run(computer);
					subject = qualifierNode.Subject;
					break;
				
				case PredicateNode predicateNode:
					predicateNode.Run(computer);
					// HACK: Default to just using the nearest matching subject
					subject = World.GetClosestAgent(predicateNode.Subjects, computer, true);
					break;
			};
			
			// Make sure an subject was found
			if (subject == null)
				throw new Exception("Couldn't find a subject to compare against");
			
			// Check if the matching subject matches the filter
			var result = subject.FilterBy(_filter.Word, computer);

			// Return the result (taking into account the quality operator)
			return _equality.Word switch
			{
				"is" => result,
				"not" => !result,
				_ => throw new Exception("Invalid quality operator found in comparison")
			};
		}
	}
	
	internal class DelayNode : BranchNode
	{
		private readonly WaitUntilNode _waitUntil;
		private readonly ComparisonNode _comparison;
		private bool _waiting;

		public bool Waiting => _waiting;
		
		public DelayNode(WaitUntilNode waitUntil, ComparisonNode comparison)
		{
			if (comparison == null)
				throw new Exception($"A delay node was given an invalid comparison");
			
			Word = waitUntil.Word;
			_waitUntil = waitUntil;
			_comparison = comparison;
		}

		public override bool Run(IComputer computer)
		{
			_waiting = true;
			(computer as BaseAgent).StartCoroutine(Wait(computer));
			computer.ExecuteCommand("wait", this);
			
			return !_waiting;
		}

		private IEnumerator Wait(IComputer computer)
		{
			while (_waiting)
			{
				_waiting = !_comparison.Run(computer);
				yield return new WaitForEndOfFrame();
			}
		}
	}

	[Serializable]
	public abstract class Node
	{
		public string Word;

		public override string ToString()
		{
			return Word;
		}
	}

	public abstract class BranchNode : Node
	{
		public abstract bool Run(IComputer computer);
	}

	internal abstract class LeafNode : Node
	{
		protected LeafNode(Token token)
		{
			Word = token.Word;
		}
	}
}