using System;

namespace Logic;

public class ArmyResupplyCastleAction : Action
{
	public Resource applied_cost;

	public ArmyResupplyCastleAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ArmyResupplyCastleAction(owner as Character, def);
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

	public Resource GetFullCost(IVars vars = null)
	{
		if (def.cost == null)
		{
			return null;
		}
		if (vars == null)
		{
			if (base.target == null)
			{
				vars = this;
			}
			else
			{
				Vars vars2 = new Vars(this);
				vars2.Set("target", base.target);
				vars = vars2;
			}
		}
		else if (base.target != null && vars.GetVar("target").is_unknown)
		{
			Vars vars3 = new Vars(vars);
			vars3.Set("target", base.target);
			vars = vars3;
		}
		return Resource.Parse(def.field.FindChild("cost_full"), vars);
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
		applied_cost = cost;
		resource.Set(cost, 1f, ResourceType.Food, ResourceType.Workers);
		kingdom.SubResources(KingdomAI.Expense.Category.Military, resource);
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "potential_supplies":
			return PotentialSupplies(GetCost(base.target));
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
			Resource resource2 = new Resource();
			for (int i = 0; i < army.units.Count; i++)
			{
				resource2.Add(army.units[i].GetHealCost(rounded: false), 1f);
			}
			for (int j = 0; j < 13; j++)
			{
				resource2[(ResourceType)j] = (float)Math.Ceiling(resource2[(ResourceType)j]);
			}
			if (resource2.IsZero())
			{
				return Value.Unknown;
			}
			return resource2;
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
		Army army = character.GetArmy();
		if (army == null)
		{
			return "no_army";
		}
		if (army.castle == null)
		{
			return "no_castle";
		}
		if (PotentialSupplies(GetCost(base.target)) < 1f)
		{
			return "no_supplies";
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
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		return "ok";
	}

	public override void Run()
	{
		base.own_character.GetArmy()?.AddSupplies(PotentialSupplies(applied_cost));
		base.Run();
	}

	public float PotentialSupplies(Resource cur_cost)
	{
		if (cur_cost == null)
		{
			return 0f;
		}
		Army army = base.own_character.GetArmy();
		Vars vars = new Vars(this);
		vars.Set("target", base.target);
		Resource resource = Resource.Parse(def.field.FindChild("cost_full"), vars);
		if (resource == null)
		{
			return 0f;
		}
		float num = 0f;
		for (ResourceType resourceType = ResourceType.Gold; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num2 = resource.Get(resourceType);
			if (num2 != 0f)
			{
				float num3 = cur_cost.Get(resourceType);
				float num4 = (num2 - num3) / num2;
				if (num4 > num)
				{
					num = num4;
				}
			}
		}
		return army.MissingSupplies() * (1f - num);
	}
}
