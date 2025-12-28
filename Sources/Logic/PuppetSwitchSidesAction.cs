using System.Collections.Generic;

namespace Logic;

public class PuppetSwitchSidesAction : PuppetPlot
{
	private List<Value> revoltLeaders = new List<Value>();

	public PuppetSwitchSidesAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetSwitchSidesAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (own_kingdom.GetFreeCourtSlotIndex() == -1)
		{
			return "_no_free_court_slot";
		}
		return base.Validate(quick_out);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.IsCrusader())
		{
			return "is_crusader";
		}
		if (puppet.IsPope())
		{
			return "is_pope";
		}
		if (puppet.IsPatriarch())
		{
			return "is_patriarch";
		}
		if (puppet.IsKing())
		{
			return "is_king";
		}
		if (puppet.IsHeir())
		{
			return "is_heir";
		}
		if (puppet.GetArmy()?.rebel != null)
		{
			return "is_rebel";
		}
		if (puppet.GetArmy()?.battle != null)
		{
			return "in_battle";
		}
		if (puppet.GetKingdom().IsEnemy(own_kingdom))
		{
			return "in_war";
		}
		CrownAuthority crownAuthority = puppet.GetKingdom().GetCrownAuthority();
		if (crownAuthority != null && crownAuthority.GetValue() > 0)
		{
			return "too_much_crown_authority";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Run()
	{
		base.own_character.DelPuppet(base.target as Character);
		PuppetFleeKingdom.Flee(base.target as Character, base.own_character, own_kingdom, keep_army: true);
		base.Run();
	}
}
