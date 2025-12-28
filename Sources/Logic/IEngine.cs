namespace Logic;

public interface IEngine : IListener
{
	void Log(Object obj, string msg, Game.LogType type);

	string GetTextFile(string name);

	byte[] GetBinaryFile(string name);
}
