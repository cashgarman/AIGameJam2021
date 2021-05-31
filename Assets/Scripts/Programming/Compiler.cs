using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Programming
{
	public class Token
	{
		public enum TokenType
		{
			ConditionalTest,
			ConditionalThen,
			Comparison,
			Noun,
			Subject,
			Description,
			Conjunction,
			Verb,
			Adjective,
			Filter,
			Interaction,
			InteractionDeliminator
		}

		public string Word { get; }
		public TokenType Type { get; }
		public string Deliminator { get; }

		public Token(string word, TokenType tokenType, string deliminator = null)
		{
			Word = word;
			Type = tokenType;
			Deliminator = deliminator;
		}

		public override string ToString()
		{
			return $"{Word} ({Type})";
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

			// AddTokensToUI(testTokens);

			Util.PopulateArrayFromJSON(_tokens, Resources.Load<TextAsset>("tokens").text);

			TestSamples();
		}

		private void TestSamples()
		{
			// var command = "goto closest charger";
			//
			// var tokens = Tokenize(command);
			//
			// var rootNode = CreateNode(tokens);
			//
			// var program = new Program(rootNode);
			//
			// AddTokensToUI(tokens);
		}

		private List<Token> Tokenize(string str)
		{
			var tokens = new List<Token>();
			
			// If the string is just a single word
			if (!str.Contains(' '))
			{
				// Try to find and add a token for the word
				tokens.Add(CreateTokenFromWord(str));
			}
			// If the string is multiple words
			else
			{
				// Split the string into words
				var parts = str.Split(' ');
				foreach (var part in parts)
				{
					// Try to find and add a token for the word
					tokens.Add(CreateTokenFromWord(part));
				}
			}

			// Return the tokens
			return tokens;
		}

		private Token CreateTokenFromWord(string word)
		{
			if (string.IsNullOrEmpty(word))
				throw new Exception("Got an empty word while trying to convert to a token");
				
			word = word.ToLower();
			
			// Check against tokens
			foreach (var token in _tokens)
			{
				// If the word matches any of the token's synonyms
				if (token.word.ToLower() == word || token.synonyms.Select(w => w.ToLower()).Contains(word))
					return new Token(token.word, token.type, token.deliminator);
			}
			
			throw new Exception($"Couldn't find a matching token for word '{word}'");
		}

		private Node CreateNode(IEnumerable<Token> tokens)
		{
			// Look at the first token
			var first = tokens.First();
			var rest = tokens.Skip(1);
			
			switch(first.Type)
			{
				case Token.TokenType.ConditionalTest:
					break;
				case Token.TokenType.ConditionalThen:
					break;
				case Token.TokenType.Comparison:
					break;
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
					// (Filter) [Subject]
					return new PredicateNode(new FilterNode(first), CreateNode(rest));
				case Token.TokenType.Interaction:
					// (Interaction) [SubjectA] (Deliminator) [SubjectB]
					var subjectA = rest.TakeWhile(token => token.Type != Token.TokenType.InteractionDeliminator && token.Word != first.Deliminator).ToArray();
					var subjectB = rest.SkipWhile(token => token.Type != Token.TokenType.InteractionDeliminator && token.Word != first.Deliminator).Skip(1).ToArray();
					Debug.Log($"Subject A tokens: {string.Join(", ", subjectA.Select(t => t.Word))}");
					Debug.Log($"Subject B tokens: {string.Join(", ", subjectB.Select(t => t.Word))}");
					return new InteractionNode(new VerbNode(first), CreateNode(subjectA), CreateNode(subjectB));
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

		public static Program Compile(string code)
		{
			// Handle empty programs
			if (string.IsNullOrEmpty(code))
				return new Program();
			
			// Break the program first into lines of code, and compile each line
			var lines = code.Split('\n');
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

			return program;
		}
	}

	[Serializable]
	public class Program
	{
		public class LineOfCode
		{
			public string Code { get; }
			private BranchNode RootNode { get; }

			public LineOfCode(string code, BranchNode rootNode)
			{
				Code = code;
				RootNode = rootNode;
			}

			public void Run(IComputer computer)
			{
				Debug.Log($"Running code '{Code}' on {computer.name}");
				RootNode.Run(computer);
			}
		}

		private readonly List<LineOfCode> _lineEntryPoints = new List<LineOfCode>();
		private int _currentLine = 0;
		private bool _shouldLoop = true;
		private bool _halted;

		public Program()
		{
		}

		public void AddLine(string code, BranchNode rootNode)
		{
			Debug.Log($"Added a line of code '{code}' to the program with root node: {rootNode.Word}");
			_lineEntryPoints.Add(new LineOfCode(code, rootNode));
		}

		public void RunCurrentLine(IComputer computer)
		{
			if (!_lineEntryPoints.Any())
				return;
			
			if (_halted)
				return;
			
			// Run the current line of code
			_lineEntryPoints[_currentLine].Run(computer);
		}

		public void GotoNextLine()
		{
			if (_halted)
				return;
			
			// Move to the next line of code
			_currentLine++;
			
			// Loop the program once it's reached the end
			if (_currentLine == _lineEntryPoints.Count)
			{
				Debug.Log($"Program finished");
				_currentLine = 0;
				if (!_shouldLoop)
				{
					_halted = true;
				}
			}
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

		public override void Run(IComputer computer)
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

		public override void Run(IComputer computer)
		{
			// Get the actual subject
			switch(_subject)
			{
				case SubjectNode subjectNode:
					_foundSubject = _adjective.Word switch
					{
						// TODO: Refactor this to use lists of BaseAgents everywhere, so we can support multiple-adjective filtering
						"nearest" => World.GetClosestAgent(subjectNode.Word, computer),
						_ => throw new Exception($"Unhandled adjective in qualifying node {_adjective.Word}")
					};
					break;
				case QualifierNode qualifierNode:
					qualifierNode.Run(computer);
					_foundSubject = qualifierNode.Subject;
					break;
				case PredicateNode predicateNode:
					predicateNode.Run(computer);
					_foundSubject = predicateNode.Subjects.FirstOrDefault();
					break;
			};
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

		public override void Run(IComputer computer)
		{
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

		public override void Run(IComputer computer)
		{
			// Get subject A
			_foundSubjectA = World.FindSubject(_subjectA, computer);
			
			// Get subject B
			_foundSubjectB = World.FindSubject(_subjectB, computer);

			// Debug.Log($"Found subject A: {_foundSubjectA?.Name}");
			// Debug.Log($"Found subject B: {_foundSubjectB?.Name}");
			
			// Tell the computer to run the verb on the subject
			computer.ExecuteCommand(_verb.Word, _foundSubjectA, _foundSubjectB);
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
		public abstract void Run(IComputer computer);
	}

	internal abstract class LeafNode : Node
	{
		protected LeafNode(Token token)
		{
			Word = token.Word;
		}
	}
}