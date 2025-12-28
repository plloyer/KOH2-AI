namespace Logic;

public class AttackerGarrison : Garrison
{
	public AttackerGarrison(Settlement s)
		: base(s)
	{
	}

	public new int SlotCount()
	{
		return int.MaxValue;
	}
}
