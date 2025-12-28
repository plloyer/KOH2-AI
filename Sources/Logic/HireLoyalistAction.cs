using System.Collections.Generic;

namespace Logic;

public class HireLoyalistAction : PrisonAction
{
	private Rebel.Def loyalist_def;

	private Rebel.Def rogue_def;

	public HireLoyalistAction(Kingdom owner, Def def)
		: base(owner, def)
	{
		loyalist_def = base.game.defs.Find<Rebel.Def>("Loyalists");
		rogue_def = base.game.defs.Find<Rebel.Def>("Rebels");
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.target is Character character && character.IsKing())
		{
			return "target_is_king";
		}
		if (Timer.Find(base.target, "hire_loyalist_cooldown") != null)
		{
			return "cooldown";
		}
		return base.Validate(quick_out);
	}

	public new static Action Create(Object owner, Def def)
	{
		return new HireLoyalistAction(owner as Kingdom, def);
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		if (args != null && args.Count > 0 && args[0].is_valid)
		{
			return args[0].Get<Kingdom>();
		}
		return base.CalcTargetKingdom(target);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		if (base.target is Character character)
		{
			outcome_vars.Set("goto_target", character.GetArmy());
			outcome_vars.Set("prisoner_kingdom", character.GetKingdom());
		}
	}

	public override void Run()
	{
		base.Run();
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Value>[] array = new List<Value>[def.arg_types.Count];
		array[0] = new List<Value>();
		if (base.game.kingdoms == null || own_kingdom == null)
		{
			return array;
		}
		foreach (Kingdom neighbor in own_kingdom.neighbors)
		{
			if (!neighbor.IsDefeated() && !neighbor.IsTeammate(own_kingdom))
			{
				array[0].Add(neighbor);
			}
		}
		return array;
	}

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (base.target is Character val)
		{
			List<Vars> list = new List<Vars>(possibleTargets.Count);
			if (possibleTargets == null)
			{
				return null;
			}
			{
				foreach (Value possibleTarget in possibleTargets)
				{
					Vars vars = new Vars(this);
					vars.Set("arg", possibleTarget);
					vars.Set("prisoner", val);
					vars.Set("rightTextKey", "HireLoyalistAction.picker_text");
					list.Add(vars);
				}
				return list;
			}
		}
		return null;
	}

	public override bool ApplyCost(bool check_first = true)
	{
		return true;
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		Resource cost = GetCost();
		switch (outcome.key)
		{
		case "early_success":
		{
			if (!(base.target is Character character2))
			{
				return false;
			}
			own_kingdom.SubResources(KingdomAI.Expense.Category.Military, cost);
			character2.Imprison(null, recall: true, send_state: true, "hired_as_loyalist", destroy_if_free: false);
			character2.DisbandArmy();
			Rebel rebel2 = character2.TurnIntoRebel("GeneralLoyalists", null, own_kingdom, target_kingdom.GetWeightedRebelliosRealm());
			if (rebel2 == null)
			{
				return false;
			}
			character2.OnPrisonActionAnalytics("hired_as_loyalist_early_success", cost);
			rebel2.Start();
			character2.GetSpecialCourtKingdom()?.DelSpecialCourtMember(character2);
			return true;
		}
		case "go_rogue":
		{
			own_kingdom.SubResources(KingdomAI.Expense.Category.Military, cost);
			Character character = base.target as Character;
			character.Imprison(null, recall: true, send_state: true, null, destroy_if_free: false);
			character.DisbandArmy();
			Rebel rebel = character.TurnIntoRebel("GeneralRebels", null, null, target_kingdom.GetWeightedRebelliosRealm());
			character.OnPrisonActionAnalytics("hired_as_loyalist_went_rogue", cost);
			character.GetSpecialCourtKingdom()?.DelSpecialCourtMember(character);
			return rebel != null;
		}
		case "trick":
		{
			own_kingdom.SubResources(KingdomAI.Expense.Category.Military, cost);
			Character obj2 = base.target as Character;
			obj2.Imprison(null);
			obj2.OnPrisonActionAnalytics("hired_as_loyalist_tricked", cost);
			return true;
		}
		case "refusal":
		{
			Character obj = base.target as Character;
			Timer.Start(obj, "hire_loyalist_cooldown", def.field.GetFloat("fail_cooldown"), restart: true);
			obj.FireEvent("force_refresh_actions", null);
			obj.OnPrisonActionAnalytics("hired_as_loyalist_refused", cost);
			return true;
		}
		default:
			return base.ApplyOutcome(outcome);
		}
	}
}
