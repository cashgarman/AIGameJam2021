using System;
using System.Collections.Generic;
using System.Linq;
using Programming;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Token = Programming.Token;

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
			Adjective
		}

		public string Word { get; set; }
		public TokenType Type { get; set; }
		
		public Token(string word, TokenType tokenType)
		{
			Word = word;
			Type = tokenType;
		}
	}

	[Serializable]
	public class TokenDefinition
	{
		public string word;
		public Token.TokenType type;
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
		}

		private void Start()
		{
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
					return new Token(token.word, token.type);
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
			// TODO: Break the program first into lines of code, and compile each line
			// TODO: When running the program, wait until the action from the previous line of code finishes first before executing the next line
			
			var tokens = _instance.Tokenize(code);
			var rootNode = _instance.CreateNode(tokens);

			if (!(rootNode is BranchNode))
				throw new Exception($"Compiled a program and ended up with just a root leaf node");
			
			return new Program(rootNode as BranchNode);
		}
	}

	[Serializable]
	public class Program
	{
		private readonly BranchNode _rootNode;

		public Program(BranchNode rootNode)
		{
			_rootNode = rootNode;
		}

		public void Run(IComputer computer)
		{
			_rootNode.Run(computer);
		}
	}

	public interface IComputer
	{
		Transform transform { get; }
		void ExecuteCommand(string command, object argument);
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

	internal class ActionNode : BranchNode
	{
		private readonly VerbNode _verb;
		private readonly Node _subject;

		public ActionNode(VerbNode verb, Node subject)
		{
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
					subject = World.GetRandomAgent(subjectNode.word);

					if (subject == null)
						throw new Exception($"Couldn't find a subject of type {subjectNode.word} to execute verb '{_verb.word}'");
							
					break;
				case QualifierNode qualifierNode:
					qualifierNode.Run(computer);
					subject = qualifierNode.Subject;
					
					if(subject == null)
						throw new Exception($"Couldn't find a subject when look with qualifier {qualifierNode.word} to execute verb '{_verb.word}'");
							
					break;
			};
			
			// Tell the computer to run the verb on the subject
			computer.ExecuteCommand(_verb.word, subject);
		}
	}

	internal class QualifierNode : BranchNode
	{
		private readonly AdjectiveNode _adjective;
		private readonly Node _subject;
		private BaseAgent _foundSubject;

		public QualifierNode(AdjectiveNode adjective, Node subject)
		{
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
					_foundSubject = _adjective.word switch
					{
						// TODO: Refactor this to use lists of BaseAgents everywhere, so we can support multiple-adjective filtering
						"nearest" => World.GetClosestAgent(subjectNode.word, computer),
						"emptiest" => World.GetAgentSortedBy(subjectNode.word, computer, "emptiest"),
						"fullest" => World.GetAgentSortedBy(subjectNode.word, computer, "fullest"),
						_ => throw new Exception($"Unhandled adjective in qualifying node {_adjective.word}")
					};
					break;
				case QualifierNode qualifierNode:
					qualifierNode.Run(computer);
					_foundSubject = qualifierNode.Subject;
					break;
			};
		}
	}

	[Serializable]
	public abstract class Node
	{
		public string word;
	}

	public abstract class BranchNode : Node
	{
		public abstract void Run(IComputer computer);
	}

	internal abstract class LeafNode : Node
	{

		protected LeafNode(Token token)
		{
			word = token.Word;
		}
	}
}