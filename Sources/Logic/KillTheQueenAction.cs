using System.Collections.Generic;

namespace Logic;

public class KillTheQueenAction : SpyPlot
{
	private Character character;

	public KillTheQueenAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new KillTheQueenAction(owner as Character, def);
	}

	public override bool NeedsTarget()
	{
		return true;
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
		if (character != base.own_character.mission_kingdom.royalFamily.Spouse)
		{
			return false;
		}
		if (!character.IsQueen())
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
		if (mission_kingdom.royalFamily.Spouse == null)
		{
			return null;
		}
		AddTarget(ref targets, mission_kingdom.royalFamily.Spouse);
		return targets;
	}

	public override void Prepare()
	{
		character = base.target as Character;
		base.Prepare();
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

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "target_character")
		{
			return character;
		}
		return base.GetVar(key, vars, as_value);
	}
}
