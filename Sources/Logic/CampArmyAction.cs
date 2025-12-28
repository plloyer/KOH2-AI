using System;

namespace Logic;

public class CampArmyAction : Action
{
	public CampArmyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CampArmyAction(owner as Character, def);
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
		resource.Set(cost, 1f, ResourceType.Food, ResourceType.Workers);
		kingdom.SubResources(KingdomAI.Expense.Category.Military, resource);
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
			Resource resource2 = new Resource(base.own_character.GetKingdom().resources);
			resource2.Set(ResourceType.Workers, castle.population.GetWorkers());
			resource2.Set(ResourceType.Food, (int)castle.food_storage);
			return resource2;
		}
		case "damage_cost":
		{
			Army army = base.own_character.GetArmy();
			if (army == null)
			{
				return Value.Unknown;
			}
			Resource resource = new Resource();
			for (int i = 0; i < army.units.Count; i++)
			{
				resource.Add(army.units[i].GetHealCost(rounded: false), 1f);
			}
			for (int j = 0; j < 13; j++)
			{
				resource[(ResourceType)j] = (float)Math.Ceiling(resource[(ResourceType)j]);
			}
			if (resource.IsZero())
			{
				return Value.Unknown;
			}
			return resource;
		}
		case "damage":
		{
			Army army2 = base.own_character.GetArmy();
			if (army2 == null)
			{
				return Value.Unknown;
			}
			float num = 0f;
			for (int k = 0; k < army2.units.Count; k++)
			{
				num += army2.units[k].damage;
			}
			num /= (float)army2.units.Count;
			return num;
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
		if (!army.currently_on_land)
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
		if (character.IsInSpecialCourt() && character.IsRebel())
		{
			return "in_rebel";
		}
		if ((float)GetVar("damage") == 0f)
		{
			return "no_damage";
		}
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		return "ok";
	}

	public override void Run()
	{
		base.own_character.GetArmy()?.RestUnits();
		base.Run();
	}
}
