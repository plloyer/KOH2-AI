using System.Collections.Generic;

namespace Logic;

public class ReassignGovernCityAction : Action
{
	public ReassignGovernCityAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ReassignGovernCityAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
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
		if (!own_kingdom.court.Contains(character))
		{
			return "not_in_court";
		}
		if (character.age < Character.Age.Young)
		{
			return "too_young";
		}
		if (character.GetGovernedCastle() == null)
		{
			return "not_governing";
		}
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		if (character.IsPope())
		{
			return "pope";
		}
		if (character.IsDeserting(out var _))
		{
			return "desrting_kingdom";
		}
		if (CompetingActionRunning())
		{
			return "competing_action_running";
		}
		return "ok";
	}

	protected virtual bool CompetingActionRunning()
	{
		if (base.own_character?.actions?.Find("GovernCityAction")?.is_active ?? false)
		{
			return true;
		}
		if (base.own_character?.actions?.Find("AssignNewGovernCityAction")?.is_active ?? false)
		{
			return true;
		}
		return false;
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
			Realm realm = target as Realm;
			Castle castle = realm?.castle;
			if (castle == null)
			{
				return false;
			}
			if (castle.governor == base.own_character)
			{
				return false;
			}
			if (!castle.CanSetGovernor(base.own_character))
			{
				return false;
			}
			if (castle.battle != null)
			{
				return false;
			}
			for (int i = 0; i < own_kingdom.court.Count; i++)
			{
				Character character = own_kingdom.court[i];
				if (character == null)
				{
					continue;
				}
				List<Action> list = character?.actions.active;
				if (list == null)
				{
					continue;
				}
				for (int j = 0; j < list.Count; j++)
				{
					Action action = list[j];
					if ((action as GovernCityAction)?.target == realm)
					{
						return false;
					}
					if ((action as ReassignGovernCityAction)?.target == realm)
					{
						return false;
					}
				}
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
		castle.FireEvent("begin_governor_assigment", base.own_character, castle.GetKingdom().id);
		base.Prepare();
	}

	public override void Cancel(bool manual = false, bool notify = true)
	{
		Castle castle = (base.target as Realm)?.castle;
		castle?.FireEvent("cancel_governor_assigment", base.own_character, castle.GetKingdom().id);
		base.Cancel(manual, notify);
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
