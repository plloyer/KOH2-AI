using System;

namespace Logic;

public class PuppetStealTreasuryAction : PuppetPlot
{
	private float goldAmount;

	public PuppetStealTreasuryAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetStealTreasuryAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.GetArmy()?.rebel != null)
		{
			return "is_rebel";
		}
		return base.ValidatePuppet(puppet);
	}

	private void CalcGoldStolen()
	{
		goldAmount = 0f;
		if (base.target is Character character)
		{
			goldAmount = def.field.GetFloat("min_gold_stolen");
			goldAmount = Math.Max(goldAmount, character.GetKingdom().resources[ResourceType.Gold] * def.field.GetFloat("gold_stolen_perc") / 100f);
		}
	}

	public override void Run()
	{
		Character character = base.target as Character;
		if (goldAmount <= 0f)
		{
			CalcGoldStolen();
		}
		Kingdom.in_AI_spend = true;
		character.GetKingdom().SubResources(KingdomAI.Expense.Category.Other, ResourceType.Gold, goldAmount);
		Kingdom.in_AI_spend = false;
		if (def.field.GetBool("give_gold_to_spy_kingdom"))
		{
			own_kingdom.AddResources(KingdomAI.Expense.Category.Espionage, ResourceType.Gold, goldAmount);
		}
		base.own_character.DelPuppet(character);
		PuppetFleeKingdom.Flee(character, base.own_character);
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "gold_amount")
		{
			if (goldAmount <= 0f)
			{
				CalcGoldStolen();
			}
			return goldAmount;
		}
		return base.GetVar(key, vars, as_value);
	}
}
