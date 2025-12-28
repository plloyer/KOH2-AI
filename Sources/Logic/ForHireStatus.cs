namespace Logic;

public class ForHireStatus : Status
{
	public ForHireStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new ForHireStatus(def);
	}

	public override bool IsIdle()
	{
		return true;
	}

	public bool Hire(int index = -1, bool for_free = false)
	{
		if (owner == null)
		{
			return false;
		}
		Kingdom kingdom = owner.GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		Character character = base.own_character;
		if (!for_free)
		{
			Resource cost = GetCost();
			if (!kingdom.resources.CanAfford(cost, 1f))
			{
				return false;
			}
			KingdomAI.Expense.Category expenseCategory = character.GetExpenseCategory();
			kingdom.SubResources(expenseCategory, cost);
			character.hire_cost = cost[ResourceType.Gold];
		}
		kingdom.AddCourtMember(character, index, is_hire: true);
		character.DelStatus<ForHireStatus>();
		return true;
	}

	private static int NumNonRoyalCourtMembersOfClass(Kingdom k, string class_name)
	{
		if (k?.court == null)
		{
			return 0;
		}
		if (class_name == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < k.court.Count; i++)
		{
			Character character = k.court[i];
			if (character != null && !character.IsDead() && !(character.class_name != class_name) && !character.IsKingOrPrince())
			{
				num++;
			}
		}
		return num;
	}

	public Resource GetCost()
	{
		return GetCost(game, GetKingdom(), base.own_character?.class_name);
	}

	public static Resource GetCost(Game game, Kingdom k, string className)
	{
		DT.Field field = game.defs.Get<Def>("ForHireStatus").field.FindChild("cost");
		Vars vars = new Vars();
		vars.Set("kingdom", k);
		vars.Set("class_name", className);
		vars.Set("num_existing", NumNonRoyalCourtMembersOfClass(k, className));
		Resource resource = Resource.Parse(field, vars);
		if (k != null)
		{
			float num = 1f - k.GetStat(Stats.ks_hire_knight_cost_discount_perc) / 100f;
			resource[ResourceType.Gold] *= num;
		}
		return resource;
	}
}
