using System.Collections.Generic;

namespace Logic;

public class AssassinateRebellionLeader : SpyPlot
{
	public AssassinateRebellionLeader(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AssassinateRebellionLeader(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Character character))
		{
			return false;
		}
		if (!character.IsAlive())
		{
			return false;
		}
		if (character.GetArmy()?.rebel?.rebellion == null)
		{
			return false;
		}
		if (character.GetArmy().rebel.rebellion.leader.character != character)
		{
			return false;
		}
		if (!own_kingdom.rebellions.Contains(character.GetArmy().rebel.rebellion))
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return null;
		}
		for (int i = 0; i < kingdom.rebellions.Count; i++)
		{
			AddTarget(ref targets, kingdom.rebellions[i].leader.character);
		}
		return targets;
	}

	public override void Run()
	{
		if (base.target is Character character)
		{
			base.own_character.NotifyListeners("murder", character);
			character.Die(new DeadStatus("assassination", character));
		}
		base.Run();
	}
}
