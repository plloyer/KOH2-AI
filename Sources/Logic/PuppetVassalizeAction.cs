namespace Logic;

public class PuppetVassalizeAction : PuppetPlot
{
	public PuppetVassalizeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetVassalizeAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character?.mission_kingdom != null && base.own_character.mission_kingdom.IsEnemy(own_kingdom))
		{
			return "_enemies";
		}
		return base.Validate(quick_out);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (!puppet.IsKing())
		{
			return "not_a_king";
		}
		if (puppet.IsCrusader())
		{
			return "is_crusader";
		}
		if (own_kingdom.sovereignState != null)
		{
			return "own_kingdom_is_vassal";
		}
		if (puppet.GetKingdom().sovereignState != null)
		{
			return "puppet_kingdom_already_vassal";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Run()
	{
		Character character = base.target as Character;
		Kingdom kingdom = character.GetKingdom();
		for (int num = kingdom.vassalStates.Count - 1; num >= 0; num--)
		{
			Kingdom kingdom2 = kingdom.vassalStates[0];
			kingdom.DelVassalState(kingdom2);
			if (!kingdom2.IsEnemy(own_kingdom))
			{
				own_kingdom.AddVassalState(kingdom2);
				kingdom2.FireEvent("became_vassal", null);
				Vars vars = new Vars();
				vars.Set("new_sovereign", own_kingdom);
				vars.Set("old_sovereign", kingdom);
				kingdom2.FireEvent("changed_sovereign", vars, kingdom2.id);
			}
		}
		own_kingdom.AddVassalState(kingdom);
		base.own_character.DelPuppet(character);
		base.Run();
	}
}
