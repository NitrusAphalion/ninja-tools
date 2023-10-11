namespace NinjaTools.Interfaces
{
	public interface IDelayedLoading
	{
		bool Loaded { get; }
		void Load();
	}
}