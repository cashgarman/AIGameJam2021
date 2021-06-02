namespace Agents
{
	public interface IHackable
	{
		string Name { get; }
		float HackingDifficulty { get; }
		void OnHacked();
		void OnHackingStarted(Robot hacker);
		void OnHackingInterrupted();
	}
}