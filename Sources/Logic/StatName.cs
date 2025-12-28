namespace Logic;

public struct StatName
{
	public string name;

	public int idx;

	public override string ToString()
	{
		return $"{name}: {idx}";
	}
}
