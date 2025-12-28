namespace Logic;

public class PuppetVassalizeSmallKingdomAction : PuppetPlot
{
	public PuppetVassalizeSmallKingdomAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetVassalizeSmallKingdomAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = base.own_character?.mission_kingdom;
		if (kingdom == null)
		{
			return "no_mission_kingdom";
		}
		if (kingdom != null && kingdom.IsEnemy(own_kingdom))
		{
			return "_enemies";
		}
		if ((float)kingdom.realms.Count * def.field.GetFloat("we_are_times_larger", null, 1f) > (float)own_kingdom.realms.Count)
		{
			return "too_large";
		}
		if ((float)kingdom.realms.Count > def.field.GetFloat("max_target_kingdom_size"))
		{
			return "too_large";
		}
		if ((float)kingdom.DistanceToKingdom(own_kingdom) > def.field.GetFloat("max_distance_to_them"))
		{
			return "too_far_away";
		}
		if (own_kingdom.sovereignState != null)
		{
			return "own_kingdom_is_vassal";
		}
		if (kingdom.sovereignState != null)
		{
			return "puppet_kingdom_already_vassal";
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
		if (character.IsKing())
		{
			base.own_character.DelPuppet(character);
		}
		base.Run();
	}
}
