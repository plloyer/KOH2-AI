using System.Collections.Generic;

namespace Logic;

public class HealArmyUnitAction : Action
{
	public HealArmyUnitAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new HealArmyUnitAction(owner as Character, def);
	}

	public override bool CheckCost(Object target)
	{
		Resource cost = GetCost(target);
		if (cost == null)
		{
			return true;
		}
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		if (base.own_character == null)
		{
			return kingdom.resources.CanAfford(cost, 1f);
		}
		Resource resource = new Resource(kingdom.resources);
		Castle castleIn = base.own_character.GetCastleIn();
		if (castleIn != null)
		{
			resource.Set(ResourceType.Workers, castleIn.population.GetWorkers());
			resource.Set(ResourceType.Food, castleIn.food_storage);
		}
		return resource.CanAfford(cost, 1f);
	}

	public override bool ApplyCost(bool check_first = true)
	{
		Resource cost = GetCost(base.target);
		if (cost == null)
		{
			return true;
		}
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		if (base.own_character == null)
		{
			return false;
		}
		Resource resource = new Resource(kingdom.resources);
		Castle castleIn = base.own_character.GetCastleIn();
		if (castleIn != null)
		{
			resource.Set(ResourceType.Workers, castleIn.population.GetWorkers());
			resource.Set(ResourceType.Food, castleIn.food_storage);
		}
		if (check_first && !resource.CanAfford(cost, 1f))
		{
			return false;
		}
		if (castleIn != null)
		{
			castleIn.population.RemoveVillagers((int)cost.Get(ResourceType.Workers), Population.Type.Worker);
			castleIn.AddFood(-(int)cost.Get(ResourceType.Food));
		}
		kingdom.SubResources(KingdomAI.Expense.Category.Military, cost);
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "available":
		{
			Castle castle = base.own_character?.GetCastleIn();
			if (castle == null)
			{
				return Value.Null;
			}
			Resource resource = new Resource(base.own_character.GetKingdom().resources);
			resource.Set(ResourceType.Workers, castle.population.GetWorkers());
			resource.Set(ResourceType.Food, (int)castle.food_storage);
			return resource;
		}
		case "damage_cost":
		{
			Army army = base.own_character.GetArmy();
			if (army == null)
			{
				return Value.Unknown;
			}
			if (army.units == null || army.units.Count == 0)
			{
				return Value.Unknown;
			}
			if (args == null || args.Count < 1)
			{
				return Value.Unknown;
			}
			if (!args[0].is_number)
			{
				return Value.Unknown;
			}
			int int_val = args[0].int_val;
			if (int_val < 0 || int_val >= army.units.Count)
			{
				return Value.Unknown;
			}
			Resource healCost = army.units[int_val].GetHealCost();
			if (healCost.IsZero())
			{
				return Value.Unknown;
			}
			return healCost;
		}
		case "damage":
		{
			Army army2 = base.own_character.GetArmy();
			if (army2 == null)
			{
				return Value.Unknown;
			}
			if (army2 == null)
			{
				return Value.Unknown;
			}
			if (army2.units == null || army2.units.Count == 0)
			{
				return Value.Unknown;
			}
			if (args == null || args.Count < 1)
			{
				return Value.Unknown;
			}
			if (!args[0].is_number)
			{
				return Value.Unknown;
			}
			int int_val2 = args[0].int_val;
			if (int_val2 < 0 || int_val2 >= army2.units.Count)
			{
				return Value.Unknown;
			}
			return army2.units[int_val2].damage;
		}
		case "is_disorder":
		{
			Realm realm = (base.own_character?.GetArmy())?.castle?.GetRealm();
			if (realm == null)
			{
				return false;
			}
			return realm.IsDisorder();
		}
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (possibleTargets == null)
		{
			return null;
		}
		if (possibleTargets.Count == 0)
		{
			return null;
		}
		List<Unit> list = base.own_character?.GetArmy()?.units;
		if (list == null)
		{
			return null;
		}
		List<Vars> list2 = new List<Vars>(possibleTargets.Count);
		for (int i = 0; i < possibleTargets.Count; i++)
		{
			Vars vars = new Vars();
			if ((int)possibleTargets[i] < 0 || (int)possibleTargets[i] > list.Count)
			{
				list2.Add(vars);
				continue;
			}
			Unit unit = list[possibleTargets[i]];
			vars.obj = new Value(unit.def);
			list2.Add(vars);
		}
		return list2;
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Unit> list = base.own_character?.GetArmy()?.units;
		if (list == null)
		{
			return null;
		}
		List<Value> list2 = new List<Value>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			Unit unit = list[i];
			if (unit != null && unit.damage != 0f)
			{
				list2.Add(i);
			}
		}
		return new List<Value>[1] { list2 };
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.cur_action is CampArmyAction)
		{
			return "in_action";
		}
		Army army = character.GetArmy();
		if (army == null)
		{
			return "no_army";
		}
		if (army.castle == null)
		{
			return "not_in_castle";
		}
		if (army.is_in_water)
		{
			return "in_water";
		}
		if (army.battle != null)
		{
			return "in_battle";
		}
		if (army.realm_in.IsDisorder())
		{
			return "in_disorder";
		}
		if ((float)GetVar("damage") == 0f)
		{
			return "no_damage";
		}
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		return base.Validate(quick_out);
	}

	public override void Run()
	{
		Army army = base.own_character.GetArmy();
		if (army == null || args == null || args.Count < 1)
		{
			return;
		}
		int num = args[0].Int();
		if (num >= 0 && num < army.units.Count)
		{
			Unit unit = army.units[num];
			if (unit != null)
			{
				unit.SetDamage(0f);
				base.Run();
			}
		}
	}
}
