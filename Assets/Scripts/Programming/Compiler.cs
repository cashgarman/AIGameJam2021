using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agents;
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
			Idle,
			Notification,
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
			return $"${(Indented ? "\t" : "")}{Word} ({Type})";
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
		// [SerializeField] private Dictionary<Token.TokenType, Color> _typeColours = new Dictionary<Token.TokenType, Color>();
		[SerializeField] private List<TokenDefinition> _tokens = new List<TokenDefinition>();

		private void Awake()
		{
			_instance = this;
		
			_commandInput.onValueChanged.AddListener(OnCommandChanged);
			

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
					if (currentSection.Type == CodeSection.BlockType.Conditional && tokenEnumerator.Current.Indented)
						// Error, not supported
						throw new Exception("Nested if statements are not supported");
					
					// Check if we've finished a previous condition section (NOTE: This only happens when you have back to back conditional statements)
					if (tokenEnumerator.Current.Type == Token.TokenType.Conditional)
					{
						// Add the previous if statement section to the program sections
						program.AddSection(currentSection);
					}
					
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
						
						// Add the line of code to the main section
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
					var rootNode = _instance.CreateNode(line.Tokens.ToArray());
					if (!(rootNode is BranchNode branchNode))
						throw new Exception($"Invalid line of code '{line}': Root node is not executable");
					
					line.RootNode = branchNode;
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
		}

		private static CodeLine GetLineOfTokens(IEnumerator<Token> tokenEnumerator)
		{
			var line = new CodeLine();
			while (tokenEnumerator.Current != null && tokenEnumerator.Current.Type != Token.TokenType.Newline)
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

		private Node CreateNode(Token[] tokens)
		{
			// HACK: MEGAHACK!
			// HACK: Comparison nodes start with a noun, refactor this to be more flexible to grammar
			// TODO: Use regexes or something here instead?
			if (tokens.Count() > 1 && !tokens.Any(t => t.Type == Token.TokenType.Conditional || t.Type == Token.TokenType.Delay) && tokens.Any(t => t.Type == Token.TokenType.Equality))
			{
				// [Subject] (Equality) [Filter]
				var subjectTokens = tokens.TakeWhile(token => token.Word != "is" && token.Word != "not").ToArray();
				var equalityToken = tokens.Skip(subjectTokens.Length).Take(1).FirstOrDefault();
				var filterToken = tokens.Skip(subjectTokens.Length).Skip(1).FirstOrDefault();
				return new ComparisonNode(CreateNode(subjectTokens), new LeafNode(equalityToken), new LeafNode(filterToken));
			}
			
			// Look at the first token
			var first = tokens.First();
			var rest = tokens.Length > 1 ? tokens.Skip(1) : null;
			
			switch(first.Type)
			{
				case Token.TokenType.Noun:
					return new SubjectNode(first);
				case Token.TokenType.Subject:
					break;
				case Token.TokenType.Description:
					break;
				case Token.TokenType.Conjunction:
					break;
				case Token.TokenType.Verb:
					// (Verb) [Subject]
					return new ActionNode(new LeafNode(first), CreateNode(rest.ToArray()));
				case Token.TokenType.Adjective:
					// (Adjective) [Subject]
					return new AdjectiveNode(new LeafNode(first), CreateNode(rest.ToArray()));
				case Token.TokenType.Filter:
					// HACK: Another shitty hack due to over-strict grammar matching
					if (rest == null)
						return new LeafNode(first);
					else
						// (Filter) [Subject]
						return new FilterNode(new LeafNode(first), CreateNode(rest.ToArray()));
				case Token.TokenType.Interaction:
					// (Interaction) [SubjectA] (Deliminator) [SubjectB]
					var subjectATokens = rest.TakeWhile(token => token.Type != Token.TokenType.InteractionDeliminator && token.Word != first.Deliminator).ToArray();
					var subjectBTokens = rest.SkipWhile(token => token.Type != Token.TokenType.InteractionDeliminator && token.Word != first.Deliminator).Skip(1).ToArray();
					return new InteractionNode(new LeafNode(first), CreateNode(subjectATokens), CreateNode(subjectBTokens));
				case Token.TokenType.Conditional:
					return first.Word switch
					{
						// If statement: (Conditional) [Comparison]
						"if" => new IfNode(CreateNode(rest.ToArray()) as ComparisonNode),
						
						// Heard statement: (Conditional) [Notification]
						"heard" => new HeardNode(new LeafNode(rest.First()))
					};
				case Token.TokenType.Delay:
					// (Delay) [Comparison]
					return new DelayNode(CreateNode(rest.ToArray()) as ComparisonNode);
				case Token.TokenType.Equality:
					return new LeafNode(first);
				case Token.TokenType.Idle:
					return new IdleNode();
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

		// CREDIT: https://forum.unity.com/threads/force-immediate-layout-update.372630/

		public static void RefreshLayoutGroupsImmediateAndRecursive(GameObject root)
		{
			foreach (var layoutGroup in root.GetComponentsInChildren<LayoutGroup>())
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
			}
		}

		// public static Color GetTokenTypeColor(Token.TokenType type)
		// {
		// 	_instance._typeColours.TryGetValue(type, out var colour);
		// 	return colour;
		// }
	}

	internal class SubjectNode : LeafNode
	{
		public SubjectNode(Token token) : base(token)
		{
		}
	}

	/*internal class AdjectiveNode : LeafNode
	{
		public AdjectiveNode(Token token) : base(token)
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
	}*/
	
	internal class ActionNode : BranchNode
	{
		private readonly LeafNode _verb;
		private readonly Node _subject;

		public ActionNode(LeafNode verb, Node subject)
		{
			Word = verb.Word;
			_verb = verb;
			_subject = subject;
		}

		public override bool Run(Program program)
		{
			// Get the actual subject
			BaseAgent subject = null;
			switch(_subject)
			{
				case SubjectNode subjectNode:
					// HACK: This should probably use kind of variable or other context
					if (subjectNode.Word == "notifier")
					{
						// Get the notifier from the running program
						subject = program.Notifier;
					}
					else if (subjectNode.Word == "target")
					{
						// Get the target from the running program
						subject = program.Target;
					}
					else if (subjectNode.Word == "home")
					{
						// Get the target from the running program
						subject = (program.Computer as BaseAgent).Home;
					}
					else
						subject = World.FindSubject(subjectNode, program);
					break;
				
				case FilterNode filterNode:
					filterNode.Run(program);
					subject = filterNode.Subjects.FirstOrDefault();
					break;
				
				case AdjectiveNode adjectiveNode:
					adjectiveNode.Run(program);
					subject = adjectiveNode.Subjects.FirstOrDefault();
					break;
			};
			
			// Tell the computer to run the verb on the subject
			program.Computer.ExecuteCommand(_verb.Word, subject);

			// HACK: This isn't used for non-if statements
			return false;
		}
	}

	internal class AdjectiveNode : BranchNode
	{
		private readonly LeafNode _adjective;
		private readonly Node _subject;
		private IEnumerable<BaseAgent> _foundSubjects;

		public AdjectiveNode(LeafNode adjective, Node subject)
		{
			Word = adjective.Word;
			_adjective = adjective;
			_subject = subject;
		}

		public IEnumerable<BaseAgent> Subjects => _foundSubjects;

		public override bool Run(Program program)
		{
			// Get the actual subject
			switch(_subject)
			{
				case SubjectNode subjectNode:
					_foundSubjects = _adjective.Word switch
					{
						// TODO: Refactor this to use lists of BaseAgents everywhere, so we can support multiple-adjective filtering
						"random" => World.GetRandomAgents(subjectNode.Word, program.Computer),
						"nearest" => World.GetClosestAgents(subjectNode.Word, program.Computer),
						// TODO: Add fullest and emptiest adjectives
						_ => throw new Exception($"Unhandled adjective in adjective node {_adjective.Word}")
					};
					break;
				case AdjectiveNode qualifierNode:
					qualifierNode.Run(program);
					_foundSubjects = qualifierNode.Subjects;
					break;
				case FilterNode predicateNode:
					predicateNode.Run(program);
					_foundSubjects = _adjective.Word switch
					{
						// TODO: Refactor this to use lists of BaseAgents everywhere, so we can support multiple-adjective filtering
						"random" => predicateNode.Subjects.Shuffle(),
						"nearest" => predicateNode.Subjects.OrderBy(s => Vector3.Distance(s.transform.position, program.Computer.transform.position)),
						_ => throw new Exception($"Unhandled adjective in qualifying node {_adjective.Word}")
					};
					break;
			};
			
			// HACK: This isn't used for non-if statements
			return false;
		}
	}
	
	internal class FilterNode : BranchNode
	{
		private readonly LeafNode _filter;
		private readonly Node _subject;
		
		private IEnumerable<BaseAgent> _foundSubjects;
		public IEnumerable<BaseAgent> Subjects => _foundSubjects;

		public FilterNode(LeafNode filter, Node subject)
		{
			Word = filter.Word;
			_filter = filter;
			_subject = subject;
		}

		public override bool Run(Program program)
		{
			// HACK: If the filter is self-referential 
			if (_filter.Word == "self")
			{
				// Return the calling computer as the subject
				_foundSubjects = new[] {program.Computer as BaseAgent};

				// HACK: This isn't used for non-if statements
				return false;
			}
			
			// Get the subject
			switch(_subject)
			{
				case SubjectNode subjectNode:
					_foundSubjects = World.GetClosestAgents(subjectNode.Word, program.Computer, _filter.Word);
					break;
				case FilterNode predicateNode:
					predicateNode.Run(program);
					_foundSubjects = World.FilterAgents(predicateNode.Subjects, program.Computer, _filter.Word);
					break;
				case AdjectiveNode qualifierNode:
					qualifierNode.Run(program);
					_foundSubjects = World.FilterAgents(qualifierNode.Subjects, program.Computer, _filter.Word);
					break;
			};
			
			// HACK: This isn't used for non-if statements
			return false;
		}
	}
	
	internal class InteractionNode : BranchNode
	{
		private readonly LeafNode _verb;
		private readonly Node _subjectA;
		private readonly Node _subjectB;
		
		private BaseAgent _foundSubjectA;
		private BaseAgent _foundSubjectB;

		public InteractionNode(LeafNode verb, Node subjectA, Node subjectB)
		{
			Word = verb.Word;
			_verb = verb;
			_subjectA = subjectA;
			_subjectB = subjectB;
		}

		public override bool Run(Program program)
		{
			// TODO: These FindSubjects need to be made to take into account Qualifiers and Predicates
			
			// Get subject A
			_foundSubjectA = World.FindSubject(_subjectA, program);
			
			// Get subject B
			_foundSubjectB = World.FindSubject(_subjectB, program);

			// Debug.Log($"Found subject A: {_foundSubjectA?.Name}");
			// Debug.Log($"Found subject B: {_foundSubjectB?.Name}");
			
			// Tell the computer to run the verb on the subject
			program.Computer.ExecuteCommand(_verb.Word, _foundSubjectA, _foundSubjectB);
			
			// HACK: This isn't used for non-if statements
			return false;
		}
	}

	internal abstract class ConditionNode : BranchNode
	{
	}

	internal class IfNode : ConditionNode
	{
		private readonly ComparisonNode _comparison;

		public IfNode(ComparisonNode comparison)
		{
			if (comparison == null)
				throw new Exception($"A conditional node was given an invalid comparison");
			
			_comparison = comparison;
		}

		public override bool Run(Program program)
		{
			// Get the result of the if statement
			var result = _comparison.Run(program);

			// If the if statement passed
			if (result)
			{
				// Store the comparison's subject as the current program target agent
				program.Target = _comparison.Subject;
			}
			
			// Return the result of the comparison
			return result;
		}
	}

	internal class ComparisonNode : BranchNode
	{
		private readonly Node _subject;
		private readonly LeafNode _equality;
		private readonly LeafNode _filter;

		public BaseAgent Subject { get; private set; }

		public ComparisonNode(Node subject, LeafNode equality, LeafNode filter)
		{
			Word = subject.Word;
			_subject = subject;
			_equality = equality ?? throw new Exception($"A comparison was given an invalid equality");
			_filter = filter ?? throw new Exception($"A comparison was given an invalid filter to compare against");
		}

		public override bool Run(Program program)
		{
			// Get the actual subject
			Subject = GetSubject(program);
			
			// If no subject could be found
			if (Subject == null)
			{
				// Debug.LogWarning("Couldn't find a subject to compare against");
				// The comparison failed
				return false;
			}
			
			// Check if the matching subject matches the filter
			var result = Subject.FilterBy(_filter.Word, program.Computer);

			// Return the result (taking into account the quality operator)
			return _equality.Word switch
			{
				"is" => result,
				"not" => !result,
				_ => throw new Exception("Invalid quality operator found in comparison")
			};
		}

		private BaseAgent GetSubject(Program program)
		{
			BaseAgent subject = null;
			switch(_subject)
			{
				// TODO: Refactor all these to create some kind of chain of subjects/qualifiers/predicates that can handle any amount in any order
				case SubjectNode subjectNode:
					subject = World.FindSubject(subjectNode, program);
					break;
				
				case AdjectiveNode qualifierNode:
					qualifierNode.Run(program);
					subject = qualifierNode.Subjects.FirstOrDefault();
					break;
				
				case FilterNode predicateNode:
					predicateNode.Run(program);
					// HACK: Default to just using the nearest matching subject
					subject = World.GetClosestAgent(predicateNode.Subjects, program.Computer, true);
					break;
			};

			return subject;
		}
	}

	internal class HeardNode : ConditionNode
	{
		private readonly LeafNode _notification;

		public string Notification => _notification.Word;

		public HeardNode(LeafNode notification)
		{
			_notification = notification ?? throw new Exception($"A heard notification node was given an invalid notification");
		}

		public override bool Run(Program program)
		{
			return true;
		}
	}

	internal class DelayNode : BranchNode
	{
		private readonly ComparisonNode _comparison;
		private bool _waiting;

		public bool Waiting => _waiting;
		
		public DelayNode(ComparisonNode comparison)
		{
			if (comparison == null)
				throw new Exception($"A delay node was given an invalid comparison");
			
			_comparison = comparison;
		}

		public override bool Run(Program program)
		{
			_waiting = true;
			(program.Computer as BaseAgent).StartCoroutine(Wait(program));
			program.Computer.ExecuteCommand("wait", this);
			
			return !_waiting;
		}

		private IEnumerator Wait(Program computer)
		{
			while (_waiting)
			{
				_waiting = !_comparison.Run(computer);
				yield return new WaitForEndOfFrame();
			}
		}
	}

	internal class IdleNode : BranchNode
	{
		private bool _waiting;
		public bool Waiting => _waiting;
		
		public IdleNode()
		{
			Word = "idle";
		}

		public override bool Run(Program program)
		{
			program.Computer.ExecuteCommand("idle", this);
			
			// HACK: This isn't used for non-if statements
			return false;
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
		public abstract bool Run(Program program);
	}

	internal class LeafNode : Node
	{
		public LeafNode(Token token)
		{
			Word = token.Word;
		}
	}
}