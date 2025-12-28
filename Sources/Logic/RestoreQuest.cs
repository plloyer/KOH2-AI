namespace Logic;

public class RestoreQuest : Quest
{
	public new static Quest Create(Def def, Object owner, Object source = null)
	{
		return new RestoreQuest(def, owner, source);
	}

	public RestoreQuest(Def def, Object owner, Object source)
		: base(def, owner, source)
	{
	}

	public new static bool CheckActivateConditions(Def def, Object obj)
	{
		if (!Quest.CheckActivateConditions(def, obj))
		{
			return false;
		}
		return true;
	}

	public override bool CheckConditions()
	{
		if (!base.CheckConditions())
		{
			return false;
		}
		return true;
	}
}
