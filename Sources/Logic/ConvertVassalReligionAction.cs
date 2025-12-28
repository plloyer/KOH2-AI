using System.Collections.Generic;

namespace Logic;

public class ConvertVassalReligionAction : Action
{
	private Realm next_target;

	public ConvertVassalReligionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ConvertVassalReligionAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Kingdom kingdom))
		{
			return false;
		}
		if (own_kingdom == null || !own_kingdom.vassalStates.Contains(kingdom))
		{
			return false;
		}
		if (kingdom.religion == own_kingdom.religion)
		{
			return false;
		}
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && character.cur_action is ConvertVassalReligionAction { is_active: not false } convertVassalReligionAction && convertVassalReligionAction.target == target)
			{
				return false;
			}
		}
		return true;
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		if (args != null && args.Count > 0)
		{
			return null;
		}
		return base.GetCost(target, vars);
	}

	public override List<OutcomeDef> DecideOutcomes()
	{
		return base.DecideOutcomes();
	}

	public override bool ValidateOutcome(OutcomeDef outcome)
	{
		return base.ValidateOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		return base.ApplyOutcome(outcome);
	}

	public override void Run()
	{
		Kingdom kingdom = own_kingdom;
		Kingdom kingdom2 = base.target as Kingdom;
		if (kingdom != null && kingdom2 != null)
		{
			kingdom2.SetReligion(kingdom.religion);
			base.Run();
		}
	}

	public static int sf_num_disloyal_realms_vassal(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("target").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = factor.field.Int(sf.vars);
		if (num == 0)
		{
			return 0;
		}
		Religion religion = (sf.vars as ChangeReligionAction)?.GetTartgetReligion();
		if (religion == null)
		{
			religion = ((sf.vars as Vars)?.obj.obj_val as ChangeReligionAction)?.GetTartgetReligion();
		}
		if (religion == null)
		{
			return 0;
		}
		int num2 = 0;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Realm realm = kingdom.realms[i];
			if (realm.pop_majority.kingdom != kingdom && realm.religion != religion)
			{
				num2++;
			}
		}
		return num2 * num;
	}

	public static int sf_leading_crusade_vassal(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("target").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		Character character = kingdom.game.religions.catholic.crusade?.leader;
		if (character != null && character.kingdom_id == kingdom.id)
		{
			return factor.field.Int(sf.vars);
		}
		return 0;
	}
}
