namespace Logic;

public class Bounty
{
	public class Def : Logic.Def
	{
		public string name;

		public float[] per_tier_amount;

		public int num_bounty_choices = 3;

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = dt_def.field;
			num_bounty_choices = field.GetInt("num_bounty_choices", null, num_bounty_choices);
			DT.Field field2 = field.FindChild("per_tier_amount");
			if (field2 != null)
			{
				int num = field2.NumValues();
				per_tier_amount = new float[num];
				for (int i = 0; i < num; i++)
				{
					per_tier_amount[i] = field2.Value(i);
				}
			}
			return true;
		}
	}

	public Def def;

	public int cur_tier = -1;

	public int kingdom_id;

	public Rebel rebel;

	public Bounty(Rebel rebel)
	{
		if (rebel != null)
		{
			this.rebel = rebel;
			def = rebel.game.defs.GetBase<Def>();
		}
	}

	public void AddBounty(Kingdom k, int new_tier)
	{
		if (k == null || new_tier <= cur_tier || def == null)
		{
			return;
		}
		if (new_tier >= def.per_tier_amount.Length)
		{
			Game.Log($"AddBounty({new_tier} exeed max alloued tier of {def.per_tier_amount.Length})", Game.LogType.Message);
			return;
		}
		float num = def.per_tier_amount[new_tier];
		if (!(k.resources[ResourceType.Gold] < num))
		{
			if (kingdom_id > 0 && cur_tier != -1)
			{
				rebel.game.GetKingdom(kingdom_id)?.AddResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, def.per_tier_amount[cur_tier]);
				k.NotifyListeners("rebel_bounty_outbound");
			}
			kingdom_id = k.id;
			cur_tier = new_tier;
			k.SubResources(KingdomAI.Expense.Category.Economy, ResourceType.Gold, num);
			rebel.NotifyListeners("rebel_bounty_changed");
		}
	}

	public void Claim(Kingdom k)
	{
		if (k != null && def != null)
		{
			k.AddResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, def.per_tier_amount[cur_tier]);
			Kingdom kingdom = rebel.game.GetKingdom(kingdom_id);
			if (kingdom != null)
			{
				k.NotifyListeners("rebel_bounty_claimed", k);
				k.AddRelationModifier(kingdom, "rel_bounty_fulfilled", rebel);
			}
			cur_tier = -1;
			kingdom_id = 0;
			rebel.NotifyListeners("rebel_bounty_claimed");
		}
	}

	private float GetAtractivness()
	{
		return 1f;
	}

	public static float[] CalcBounty(Rebel r)
	{
		Def def = r.game.defs.GetBase<Def>();
		if (r == null || def == null)
		{
			return new float[3] { 100f, 110f, 120f };
		}
		if (r.level + def.num_bounty_choices >= def.per_tier_amount.Length)
		{
			return new float[3] { 100f, 110f, 120f };
		}
		float[] array = new float[def.num_bounty_choices];
		for (int i = r.level; i < def.num_bounty_choices; i++)
		{
			array[i] = def.per_tier_amount[r.level + i];
		}
		return array;
	}
}
