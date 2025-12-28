using System.Collections.Generic;

namespace Logic;

public class SpyMurderAction : SpyPlot
{
	public SpyMurderAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new SpyMurderAction(owner as Character, def);
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
		if (character.IsRoyalty())
		{
			if (!character.IsRoyalChild())
			{
				return false;
			}
			if (base.own_character.GetSkill("Assassination") == null)
			{
				return false;
			}
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
		for (int i = 0; i < mission_kingdom.court.Count; i++)
		{
			Character character = mission_kingdom.court[i];
			if (character != null && ValidateTarget(character))
			{
				AddTarget(ref targets, character);
			}
		}
		for (int j = 0; j < mission_kingdom.royalFamily.Children.Count; j++)
		{
			Character character2 = mission_kingdom.royalFamily.Children[j];
			if (character2 != null && (targets == null || !targets.Contains(character2)) && ValidateTarget(character2))
			{
				AddTarget(ref targets, character2);
			}
		}
		return targets;
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		if (outcome.key == "success")
		{
			if (base.target is Character character)
			{
				base.own_character.NotifyListeners("murder", character);
				character.Die(null, "incident");
			}
			return true;
		}
		return base.ApplyOutcome(outcome);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "target_character"))
		{
			if (key == "target_is_our_puppet")
			{
				return base.own_character.puppets.Contains(base.target as Character);
			}
			return base.GetVar(key, vars, as_value);
		}
		return base.target as Character;
	}
}
