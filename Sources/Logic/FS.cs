namespace Logic;

public static class FS
{
	public const char DirectorySeparator = '/';

	public const string DirectorySeparatorStr = "/";

	public static string EnforceDirectorySeparator(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			return path.Replace('\\', '/');
		}
		return path;
	}
}
