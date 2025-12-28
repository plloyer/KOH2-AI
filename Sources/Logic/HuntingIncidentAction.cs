using System.Collections.Generic;

namespace Logic;

public class HuntingIncidentAction : SpyPlot
{
	public HuntingIncidentAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new HuntingIncidentAction(owner as Character, def);
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
		if (base.own_character?.mission_kingdom == null)
		{
			return false;
		}
		if (character.kingdom_id != base.own_character.mission_kingdom.id)
		{
			return false;
		}
		if (!character.IsKing())
		{
			return false;
		}
		List<Character> puppets = base.own_character.puppets;
		if (puppets != null && puppets.Contains(character))
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
		AddTarget(ref targets, mission_kingdom.GetKing());
		return targets;
	}

	public override void Run()
	{
		if (base.target is Character character)
		{
			base.own_character.NotifyListeners("murder", character);
			character.Die(new DeadStatus("incident", character));
		}
		base.Run();
	}
}
