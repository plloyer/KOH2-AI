namespace Logic;

public class CommerceQuest : Quest
{
	public new static Quest Create(Def def, Object owner, Object source = null)
	{
		return new CommerceQuest(def, owner, source);
	}

	public CommerceQuest(Def def, Object owner, Object source)
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
}
