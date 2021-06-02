using Programming;
using UnityEngine;

namespace Agents
{
	public interface IComputer
	{
		Transform transform { get; }
		string name { get; set; }
		void ExecuteCommand(string command, params object[] arguments);
		bool HasInteractedWithRecently(BaseAgent agent);
		void RunProgram(Program program);
	}
}