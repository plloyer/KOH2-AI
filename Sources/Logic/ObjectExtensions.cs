namespace Logic;

public static class ObjectExtensions
{
	public static Value DefaultGetVar(this Object obj, string key, IVars vars = null, bool as_value = true)
	{
		return Value.Unknown;
	}

	public static bool IsAuthority(this Object obj)
	{
		return false;
	}
}
