using System.Collections.Generic;

namespace Logic;

public abstract class MercenaryMission
{
	public class Def : Logic.Def
	{
		public DT.Field cost_field;

		public DT.Field upkeep_field;

		public DT.Field upkeep_for_tooltip_field;

		public bool can_attack_rebels;

		public bool can_attack_war_kingdoms;

		public bool can_attack_crusade;

		public DT.Field validate_field;

		public DT.Field validate_resources_field;

		public OutcomeDef dismiss_outcomes;

		public override bool Load(Game game)
		{
			cost_field = base.field.FindChild("cost");
			upkeep_field = base.field.FindChild("upkeep");
			upkeep_for_tooltip_field = base.field.FindChild("upkeep_for_tooltip");
			can_attack_rebels = base.field.GetBool("can_attack_rebels", null, can_attack_rebels);
			can_attack_war_kingdoms = base.field.GetBool("can_attack_war_kingdoms", null, can_attack_war_kingdoms);
			can_attack_crusade = base.field.GetBool("can_attack_crusade", null, can_attack_crusade);
			validate_field = base.field.FindChild("validate");
			validate_resources_field = base.field.FindChild("validate_resources");
			DT.Field field = base.field.FindChild("dismiss_outcomes");
			if (field != null)
			{
				DT.Field defaults = base.field.FindChild("outcome_defaults");
				dismiss_outcomes = new OutcomeDef(game, field, defaults);
			}
			else
			{
				dismiss_outcomes = null;
			}
			return base.Load(game);
		}

		public Resource GetCost(IVars vars)
		{
			return Resource.Parse(cost_field, vars);
		}

		public Resource GetCost(Mercenary merc, Kingdom hire_kingdom)
		{
			Vars vars = new Vars();
			vars.Set("merc", merc);
			vars.Set("hire_kingdom", hire_kingdom);
			return GetCost(vars);
		}

		public Resource GetUpkeep(IVars vars)
		{
			return Resource.Parse(upkeep_field, vars);
		}

		public Resource GetUpkeep(Mercenary merc, Kingdom hire_kingdom)
		{
			Vars vars = new Vars();
			vars.Set("merc", merc);
			vars.Set("hire_kingdom", hire_kingdom);
			return GetUpkeep(vars);
		}

		public Resource GetUpkeepForTooltip(IVars vars)
		{
			return Resource.Parse(upkeep_for_tooltip_field, vars);
		}

		public Resource GetUpkeepForTooltip(Mercenary merc, Kingdom hire_kingdom)
		{
			Vars vars = new Vars();
			vars.Set("merc", merc);
			vars.Set("hire_kingdom", hire_kingdom);
			return GetUpkeepForTooltip(vars);
		}

		public bool Validate(IVars vars)
		{
			if (validate_field == null)
			{
				return true;
			}
			return validate_field.Bool(vars);
		}

		public bool Validate(Mercenary merc, Kingdom hire_kingdom)
		{
			Vars vars = new Vars();
			vars.Set("merc", merc);
			vars.Set("hire_kingdom", hire_kingdom);
			return Validate(vars);
		}

		public bool ValidateKingdomResources(Mercenary merc, Kingdom hire_kingdom)
		{
			Vars vars = new Vars();
			vars.Set("merc", merc);
			vars.Set("hire_kingdom", hire_kingdom);
			return ValidateKingdomResources(vars);
		}

		public bool ValidateKingdomResources(IVars vars)
		{
			if (validate_resources_field == null)
			{
				return true;
			}
			return validate_resources_field.Bool(vars);
		}

		public override Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return key switch
			{
				"cost" => GetCost(vars), 
				"valid" => Validate(vars), 
				"upkeep" => GetUpkeep(vars), 
				"valid_resources" => ValidateKingdomResources(vars), 
				"upkeep_for_tooltip" => GetUpkeepForTooltip(vars), 
				_ => base.GetVar(key, vars, as_value), 
			};
		}
	}

	public static List<Def> defs;

	public static void LoadDefs(Game game)
	{
		if (game != null)
		{
			defs = new List<Def>();
			List<Def> list = game.defs.GetDefs<Def>();
			if (list != null)
			{
				defs.AddRange(list);
			}
		}
	}
}
