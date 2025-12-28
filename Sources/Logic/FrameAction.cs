using System.Collections.Generic;

namespace Logic;

public class FrameAction : SpyPlot
{
	public FrameAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new FrameAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.mission_kingdom == null)
		{
			return "not_on_mission";
		}
		if (base.own_character.mission_kingdom.foreigners.Count <= 1)
		{
			return "no_other_foreighners";
		}
		return base.Validate(quick_out);
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		if (args != null && args.Count > 0)
		{
			return (args[0].obj_val as Character)?.GetKingdom();
		}
		return base.CalcTargetKingdom(target);
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
		if (character.prison_kingdom != null)
		{
			return false;
		}
		if (character == base.own_character)
		{
			return false;
		}
		if (character.GetKingdom() == own_kingdom)
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		if (mission_kingdom == null)
		{
			return null;
		}
		for (int i = 0; i < mission_kingdom.foreigners.Count; i++)
		{
			AddTarget(ref targets, mission_kingdom.foreigners[i]);
		}
		return targets;
	}

	public override void Run()
	{
		(base.target as Character).Imprison(base.own_character.mission_kingdom);
		base.Run();
	}
}
