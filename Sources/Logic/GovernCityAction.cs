using System.Collections.Generic;

namespace Logic;

public class GovernCityAction : Action
{
	public GovernCityAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new GovernCityAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsAlive())
		{
			return "dead";
		}
		if (own_kingdom == null)
		{
			return "no_kingodm";
		}
		if (character.sex != Character.Sex.Male)
		{
			return "not_a_male";
		}
		if (character.prison_kingdom != null)
		{
			return "imprisoned";
		}
		if (character.GetGovernedCastle() != null)
		{
			return "already_governing";
		}
		if (!own_kingdom.court.Contains(character))
		{
			return "not_in_court";
		}
		if (character.age < Character.Age.Young)
		{
			return "too_young";
		}
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		if (character.IsDeserting(out var _))
		{
			return "desrting_kingdom";
		}
		if (character.GetPreparingToGovernCastle() != null)
		{
			return "competing_action_running";
		}
		return "ok";
	}

	public override bool ValidateTarget(Object target)
	{
		if (!NeedsTarget())
		{
			return true;
		}
		if (target == null)
		{
			return false;
		}
		if (def.target == "own_realm")
		{
			Castle castle = (target as Realm)?.castle;
			if (castle == null)
			{
				return false;
			}
			if (!castle.CanSetGovernor(base.own_character))
			{
				return false;
			}
			if (castle.GetPreparingGovernor() != null)
			{
				return false;
			}
			return true;
		}
		return base.ValidateTarget(target);
	}

	public override void FillPossibleTargetVars(Vars vars)
	{
		Castle castle = (base.target as Realm)?.castle;
		vars.Set("castle", castle);
		vars.Set("governor", base.owner);
		if (castle != null && castle.governor != null)
		{
			vars.Set("additinalObject", castle.governor);
		}
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> possibleTargets = base.GetPossibleTargets();
		if (possibleTargets == null)
		{
			return null;
		}
		possibleTargets.Sort(delegate(Object x, Object y)
		{
			Castle castle = (x as Realm)?.castle;
			Castle castle2 = (y as Realm)?.castle;
			if (castle.governor == null && castle2.governor == null)
			{
				return 0;
			}
			if (castle.governor == null)
			{
				return -1;
			}
			return (castle2.governor == null) ? 1 : 0;
		});
		return possibleTargets;
	}

	public override bool NeedsTarget()
	{
		return base.NeedsTarget();
	}

	public override string GetTargetConfirmationMessageKey(object target)
	{
		Castle castle = (target as Realm)?.castle;
		if (castle != null && castle.governor != null)
		{
			return base.GetTargetConfirmationMessageKey(target);
		}
		return null;
	}

	public override void Prepare()
	{
		Castle castle = (base.target as Realm)?.castle;
		if (castle != null && castle.governor != null)
		{
			castle.governor.StopGoverning();
		}
		base.own_character.StopGoverning();
		base.Prepare();
		castle.FireEvent("begin_governor_assigment", base.own_character, castle.GetKingdom().id);
	}

	public override void Cancel(bool manual = false, bool notify = true)
	{
		Castle castle = (base.target as Realm)?.castle;
		base.Cancel(manual, notify);
		castle?.FireEvent("cancel_governor_assigment", base.own_character, castle.GetKingdom().id);
	}

	public override void Run()
	{
		Castle castle = (base.target as Realm)?.castle;
		if (castle != null)
		{
			base.own_character.Govern(castle);
			base.own_character.OnGovernAnalytics(own_kingdom, castle.GetRealm(), GetType().Name);
			base.Run();
		}
	}
}
