namespace Logic;

public class PuppetSupportPretenderToTheThroneAction : PuppetPlot
{
	public PuppetSupportPretenderToTheThroneAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetSupportPretenderToTheThroneAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		_ = base.own_character.mission_kingdom;
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (own_kingdom.GetStat(Stats.ks_allow_PuppetSupportPretenderToTheThroneAction) == 0f)
		{
			return "no_traiditon_unlock";
		}
		if (puppet.IsRoyalty())
		{
			return "is_royalty";
		}
		if (puppet == puppet.GetKingdom()?.royalFamily?.GetCrownPretender() && own_kingdom == puppet.GetKingdom()?.royalFamily?.GetCrownPretenderKingdomLoyalTo())
		{
			return "already_a_pretender";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Run()
	{
		if (base.target is Character character)
		{
			bool num = character.masters != null && character.masters.Contains(base.own_character);
			base.own_character.mission_kingdom.royalFamily.SetPretender(character, own_kingdom, was_puppet: true);
			if (!num)
			{
				base.own_character.AddPuppet(character);
			}
		}
		base.Run();
	}
}
