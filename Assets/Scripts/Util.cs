using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

public class Util
{
	private static Util instance;
	private Random random;
	private static readonly int[] digits = {0,1,2,3,4,5,6,7,8,9};

	private static Util Instance => instance ??= new Util();

	public Util()
	{
		random = new Random();
	}

	public static IEnumerator Delay(float seconds, System.Action callback)
	{
		yield return new WaitForSeconds(seconds);
		callback();
	}

	public static int Lerp(int min, int max, float amount)
	{
		return (int)Math.Ceiling((max - min) * amount) + min;
	}

	public static float Lerp(float min, float max, float amount)
	{
		return (max - min) * amount + min;
	}

	public static float Random()
	{
		return (float)Instance.random.NextDouble();
	}

	public static byte RandomByte()
	{
		return Convert.ToByte(Instance.random.Next() % 255);
	}

	public static int RandomRange(int min, int max)
	{
		return Instance.random.Next(min, max + 1);
	}

	// CREDIT: http://stackoverflow.com/questions/6651554/random-number-in-long-range-is-this-the-way
	public static long RandomRange(long min, long max)
	{
		var buf = new byte[8];
		Instance.random.NextBytes(buf);
		var longRand = BitConverter.ToInt64(buf, 0);

		return (Math.Abs(longRand % (max - min)) + min);
	}

	public static float RandomRange(float min, float max)
	{
		return (max - min) * (float)Instance.random.NextDouble() + min;
	}

	public static T RandomElement<T>(T[] array)
	{
		if (array == null)
			return default;

		if (!array.Any())
			return default;

		return array[Instance.random.Next(0, array.Length)];
	}

	public static T RandomElement<T>(IEnumerable<T> list)
	{
		return RandomElement(list.ToArray());
	}
		
	public static T RandomElement<T>(Array array)
	{
		if (array == null)
			return default(T);	

		if (array.Length == 0)
			return default(T);

		return (T)array.GetValue(Instance.random.Next(0, array.Length));
	}

	public static int RandomIndex(IList list)
	{
		return Instance.random.Next(0, list.Count);
	}
		
	// Credit: https://stackoverflow.com/questions/3132126/how-do-i-select-a-random-value-from-an-enumeration
	public static T RandomEnum<T>()
	{
		var values = Enum.GetValues(typeof(T));
		return (T)values.GetValue(Instance.random.Next(values.Length));
	}
	
	// Credit: https://stackoverflow.com/questions/9545619/a-fast-hash-function-for-string-in-c-sharp
	public static ulong GetStringHash(string read)
	{
		var hashedValue = 3074457345618258791ul;
		foreach (var t in read)
		{
			hashedValue += t;
			hashedValue *= 3074457345618258799ul;
		}
		return hashedValue;
	}

	public static bool FlipCoin()
	{
		return Instance.random.Next(0, 2) == 1;
	}

	public static bool Chance(float chance)
	{
		return Random() <= chance;
	}

	public static T Choice<T>(params T[] list)
	{
		return RandomElement(list);
	}

	public static void SetSeed(int seed)
	{
		Instance.random = new Random(seed);
	}

	public static void SetRandomSeed()
	{
		Instance.random = new Random();
	}

	public static string GetTimeDeltaDescription(double delta)
	{
		return String.Format("{0} day{1} ago", delta, delta > 1 ? "s" : "");
	}

	public static void PopulateObjectFromJSON(object obj, string json)
	{
		try
		{
			JsonSerializer.CreateDefault(new JsonSerializerSettings())
				.Populate(JObject.Parse(json).CreateReader(), obj);
		}
		catch (JsonReaderException e)
		{
			throw new Exception("Error while parsing JSON: " + e.Message);
		}
		catch (JsonException e)
		{
			throw new Exception(e.Message);
		}
	}
	
	public static void PopulateObjectFromJSONPath(object obj, string path)
	{
		try
		{
			JsonSerializer.CreateDefault(new JsonSerializerSettings())
				.Populate(JObject.Parse(File.ReadAllText(path)).CreateReader(), obj);
		}
		catch (JsonReaderException e)
		{
			throw new Exception("Error while parsing JSON file " + path + ": " + e.Message);
		}
		catch (JsonException e)
		{
			throw new Exception(e.Message);
		}
	}

	public static void PopulateArrayFromJSON(object obj, string json)
	{
		try
		{
			JsonSerializer.CreateDefault(new JsonSerializerSettings()).Populate(JArray.Parse(json).CreateReader(), obj);
		}
		catch (JsonReaderException e)
		{
			throw new Exception("Error while parsing JSON " + json + ": " + e.Message);
		}
	}

	public static void PopulateArrayFromJSONPath(object obj, string path)
	{
		PopulateArrayFromJSON(obj, File.ReadAllText(path));
	}

	public static string Capitalize(string str)
	{
		return Char.ToUpper(str[0]) + str.Substring(1);
	}

	public static string CapitalizeWords(string sentance)
	{
		var spaceWords = sentance.Split(' ');
		var result = String.Join(" ", spaceWords.Select(w => (w != "of" && w != "and") ? Capitalize(w) : w).ToArray());
		var dashWords = result.Split('-');
		result = String.Join("-", dashWords.Select(w => (w != "of" && w != "and") ? Capitalize(w) : w).ToArray());

		return result;
	}
		
	// Credit: https://stackoverflow.com/questions/3824059/linq-list-to-sentence-format-insert-commas-and
	public static string ToCommaList<T>(List<T> source, System.Func<T, string> stringSelector)
	{
		var count = source.Count;

		System.Func<int, string> prefixSelector = x => 
			x == 0 ? "" :
			x == count - 1 ? " and " :
			", ";

		var sb = new StringBuilder();

		for(var i = 0; i < count; i++)
		{
			sb.Append(prefixSelector(i));
			sb.Append(stringSelector(source[i]));
		}

		var result = sb.ToString();
		return result;
	}

	public static int RandomDigit()
	{
		return RandomElement(digits);
	}

	// Credit: http://stackoverflow.com/questions/10390356/c-sharp-serializing-deserializing-with-memory-stream

	public static MemoryStream SerializeToStream(object o)
	{
		var stream = new MemoryStream();
		var dcs = new DataContractSerializer(o.GetType());
//			var xdw = XmlDictionaryWriter.CreateTextWriter(stream);
		dcs.WriteObject(stream, o);
//			xdw.Flush();

		return stream;
	}

//		public static object DeserializeFromStream(MemoryStream stream)

//		{

//			IFormatter formatter = new BinaryFormatter();

//			stream.Seek(0, SeekOrigin.Begin);

//			object o = formatter.Deserialize(stream);

//			return o;

//		}

	public static string FormatLargeNumber(long number)
	{
		string result = "";
		if (number < 1000000)
			result = String.Format("{0:N}", number);
		else if(number < 1000000000)
			result = String.Format("{0:N}M", number / 1000000f);
		else if (number < 1000000000000)
			result = String.Format("{0:N}B", number / 1000000000f);
		else if (number < 1000000000000000)
			result = String.Format("{0:N}T", number / 1000000000000f);

		result = result.Replace(".00", "");

		return result;
	}

//		public static string GetSecureHash(string str)

//		{

//			// Generate the hash, with an automatic 32 byte salt

//			var rfc2898DeriveBytes = new Rfc2898DeriveBytes(str, 32) {IterationCount = 10000};

//			var hash = rfc2898DeriveBytes.GetBytes(20);

//			var salt = rfc2898DeriveBytes.Salt;

//			

//			// Return the salt and the hash

//			return Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(hash);

//		}


	// Credit: https://cmatskas.com/-net-password-hashing-using-pbkdf2/

	public class PasswordHash
	{
		public const int SaltByteSize = 32;
		public const int HashByteSize = 20; // to match the size of the PBKDF2-HMAC-SHA-1 hash 
		public const int Pbkdf2Iterations = 1000;
		public const int IterationIndex = 0;
		public const int SaltIndex = 1;
		public const int Pbkdf2Index = 2;

		public static string GenerateHash(string password)
		{
			var cryptoProvider = new RNGCryptoServiceProvider();
			byte[] salt = new byte[SaltByteSize];
			cryptoProvider.GetBytes(salt);

			var hash = GetPbkdf2Bytes(password, salt, Pbkdf2Iterations, HashByteSize);
			return Pbkdf2Iterations + ":" +
			       Convert.ToBase64String(salt) + ":" +
			       Convert.ToBase64String(hash);
		}

		private static byte[] GetPbkdf2Bytes(string password, byte[] salt, int iterations, int outputBytes)
		{
			var pbkdf2 = new Rfc2898DeriveBytes(password, salt);
			pbkdf2.IterationCount = iterations;
			return pbkdf2.GetBytes(outputBytes);
		}
	}

	public static string RandomizeCapitalization(string message)
	{
		// ll oo pp qq
		// ^
		var originalMessage = message.ToCharArray();
		var result = message.ToCharArray();
		var lastChar = ' ';
		for (var i = 0; i < result.Length; ++i)
		{
			if (char.ToLower(originalMessage[i]) == char.ToLower(lastChar))
			{
				result[i] = char.IsUpper(lastChar) ? char.ToLower(lastChar) : char.ToUpper(lastChar);
			}
			else
			{
				result[i] = FlipCoin() ? char.ToUpper(result[i]) : char.ToLower(result[i]);
			}
				
			lastChar = result[i];
		}

		return new string(result);
	}

	// public static void Idiot(string message, EmployeeType type = EmployeeType.designer)
	// {
	// 	throw new IdiotException($"{message}. You fucking shitty {type}");
	// }

	public static void LogDictionary(Dictionary<string,object> dict)
	{
		foreach (var element in dict)
		{
			Debug.Log($"{element.Key}: {element.Value}");
		}
	}
	
	// CREDIT: https://answers.unity.com/questions/486545/getting-all-assets-of-the-specified-type.html
	#if UNITY_EDITOR
		public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
		{
			List<T> assets = new List<T>();
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
			for( int i = 0; i < guids.Length; i++ )
			{
				string assetPath = AssetDatabase.GUIDToAssetPath( guids[i] );
				T asset = AssetDatabase.LoadAssetAtPath<T>( assetPath );
				if( asset != null )
				{
					assets.Add(asset);
				}
			}
			return assets;
		}
	#endif
	
	public static void DestroyMultiple(IEnumerable<MonoBehaviour> objects)
	{
		foreach (var obj in objects)
		{
			GameObject.Destroy(obj.gameObject);
		}
	}

	public static void Each<T>(IEnumerable<T> elements, Action<T> callback)
	{
		foreach (var element in elements)
			callback(element);
	}
	
	// CREDIT: https://stackoverflow.com/questions/5461295/using-isassignablefrom-with-open-generic-types
	public static bool IsAssignableToGenericType(Type givenType, Type genericType)
	{
		var interfaceTypes = givenType.GetInterfaces();

		foreach (var it in interfaceTypes)
		{
			if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
				return true;
		}

		if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
			return true;

		Type baseType = givenType.BaseType;
		if (baseType == null) return false;

		return IsAssignableToGenericType(baseType, genericType);
	}
}

public enum EmployeeType
{
	programmer,
	designer,
	artist,
}

// Credit: http://stackoverflow.com/questions/2683442/where-can-i-find-the-clamp-function-in-net
public static class MathExtensionMethods
{
	public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
	{
		if (val.CompareTo(min) < 0) return min;
		return val.CompareTo(max) > 0 ? max : val;
	}
}

public static class TMP_DropdownExtensionMethods
{
	public static string GetValue(this TMP_Dropdown dropdown)
	{
		return dropdown.options[dropdown.value].text;
	}
}

public static class StringExtensionMethods
{
	// Credit: http://stackoverflow.com/questions/8809354/replace-first-occurrence-of-pattern-in-a-string
	public static string ReplaceFirst(this string text, string search, string replace)
	{
		var pos = text.IndexOf(search);
		if (pos < 0)
		{
			return text;
		}
		return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
	}

	// Credit: http://stackoverflow.com/questions/9367119/replacing-a-char-at-a-given-index-in-string
	public static string ReplaceAt(this string input, int index, char newChar)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		var chars = input.ToCharArray();
		chars[index] = newChar;
		return new string(chars);
	}
}

public static class IEnumerableExtensionMethods
{
	// Credit: https://stackoverflow.com/questions/9300169/linq-indexof-a-particular-entry
	public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {

		var index = 0;
		foreach (var item in source) {
			if (predicate.Invoke(item)) {
				return index;
			}
			index++;
		}

		return -1;
	}
}

public static class QueueExtensionMethods
{
	public static void Add<T>(this Queue<T> queue, T element)
	{
		queue.Enqueue(element);
	}
}

class WritablePropertiesOnlyResolver : DefaultContractResolver
{
	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
	{
		var props = base.CreateProperties(type, memberSerialization);
		return props.Where(p => p.Writable).ToList();
	}
}

public static class EnumerableExtension
{
	public static T PickRandom<T>(this IEnumerable<T> source)
	{
		return source.PickRandom(1).Single();
	}

	public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
	{
		return source.Shuffle().Take(count);
	}

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
	{
		return source.OrderBy(x => Util.Random());
	}
}

public static class TransformExtension
{
	public static void ClearChildren(this Transform transform, bool immediate = false)
	{
		foreach (Transform child in transform)
		{
			if (immediate)
			{
				Object.DestroyImmediate(child.gameObject);
			}
			else
			{
				Object.Destroy(child.gameObject);
			}
		}
	}
		
	public static void SetChildrenActive(this Transform transform, bool active)
	{
		foreach (Transform child in transform)
		{
			child.gameObject.SetActive(active);
		}
	}

	public static IEnumerable<T> GetChildren<T>(this Transform transform)
	{
		return transform.GetComponentsInChildren<T>();
	}
}

public static class ListExtension
{
	public static void LogAll<T>(this List<T> list, string prefix)
	{
		foreach (var element in list)
		{
			Debug.Log($"{prefix}{element.ToString()}");
		}
	}
	
	// CREDIT: https://stackoverflow.com/questions/12231569/is-there-in-c-sharp-a-method-for-listt-like-resize-in-c-for-vectort
	public static void Resize<T>(this List<T> list, int sz) where T : new()
	{
		int cur = list.Count;
		if(sz < cur)
			list.RemoveRange(sz, cur - sz);
		else if(sz > cur)
		{
			if(sz > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
				list.Capacity = sz;
			for (var i = 0; i < sz - cur; ++i)
			{
				list.Add(new T());
			}
			// list.AddRange(Enumerable.Repeat(new T(), sz - cur));
		}
	}
	// public static void Resize<T>(this List<T> list, int sz) where T : new()
	// {
	// 	Resize(list, sz, new T());
	// }
	
	public static T PickRandom<T>(this List<T> source)
	{
		return source.PickRandom(1).Single();
	}

	public static List<T> PickRandom<T>(this List<T> source, int count)
	{
		return source.Shuffle().Take(count).ToList();
	}

	public static List<T> Shuffle<T>(this List<T> source)
	{
		return source.OrderBy(x => Util.Random()).ToList();
	}
}

public static class ArrayExtension
{
	public static void LogAll(this Array list, string prefix)
	{
		foreach (var element in list)
		{
			Debug.Log($"{prefix}{element}");
		}
	}
}