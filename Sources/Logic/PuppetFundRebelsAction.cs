namespace Logic;

public class PuppetFundRebelsAction : PuppetPlot
{
	public PuppetFundRebelsAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetFundRebelsAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		puppet?.GetArmy();
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.GetArmy()?.rebel != null)
		{
			return "is_rebel";
		}
		if (puppet.GetKingdom().rebellions.Count == 0)
		{
			return "no_rebels";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Run()
	{
		Kingdom kingdom = (base.target as Character).GetKingdom();
		if (kingdom.rebellions == null || kingdom.rebellions.Count == 0)
		{
			return;
		}
		int num = def.field.GetInt("max_rebel_armies");
		int num2 = def.field.GetInt("num_level_increases");
		for (int i = 0; i < kingdom.rebellions.Count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			Rebellion rebellion = kingdom.rebellions[i];
			for (int j = 0; j < rebellion.rebels.Count; j++)
			{
				if (num <= 0 && num2 <= 0)
				{
					break;
				}
				Rebel rebel = rebellion.rebels[j];
				Army army = rebel.army;
				if (rebel == null || army?.realm_in?.rebellionRisk == null)
				{
					continue;
				}
				if (num > 0 && army.battle == null)
				{
					if (army.realm_in.rebellionRisk.UpgradeUnits(army))
					{
						num--;
					}
					else if (army.rebel.CheckUnitReplenish(army.rebel.GetMaxUnitCount() - army.CountUnits()))
					{
						num--;
						army.rebel.NotifyListeners("reinforced");
					}
				}
				while (num2 > 0 && rebel.LevelUp())
				{
					num2--;
				}
			}
		}
		base.Run();
	}
}
