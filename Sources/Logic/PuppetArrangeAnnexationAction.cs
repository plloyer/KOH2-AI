using System.Collections.Generic;

namespace Logic;

public class PuppetArrangeAnnexationAction : PuppetPlot
{
	private List<Realm> leftRealms;

	private int leftRealmChecks;

	private List<Realm> realms;

	public PuppetArrangeAnnexationAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetArrangeAnnexationAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		Kingdom kingdom = puppet.GetKingdom();
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (kingdom.is_player && kingdom.realms.Count <= def.field.GetInt("min_player_realms"))
		{
			return "not_enough_realms";
		}
		if (kingdom.sovereignState != own_kingdom)
		{
			return "not_a_vassal";
		}
		if (kingdom.GetCrownAuthority().GetValue() >= 0)
		{
			return "too_high_crown_athority";
		}
		int num = def.field.GetInt("added_comparison_realms", null, 3);
		float num2 = def.field.GetFloat("kingdoms_comparison_perc", null, 70f);
		if ((float)(kingdom.realms.Count + num) > (float)(own_kingdom.realms.Count + num) * num2 / 100f)
		{
			return "too_big";
		}
		if (puppet.GetArmy()?.rebel != null)
		{
			return "is_rebel";
		}
		return base.ValidatePuppet(puppet);
	}

	public void DecideLeftoverRealms(Realm r, Realm rStart, int depth, object param, ref bool push_neighbors, ref bool stop)
	{
		if (!r.IsOwnStance((base.target as Character).GetKingdom()))
		{
			return;
		}
		if (leftRealms == null)
		{
			leftRealms = new List<Realm>();
		}
		r.GetKingdom();
		if (r.GetKingdom().is_player && leftRealms.Count < def.field.GetInt("min_player_leftover_realms"))
		{
			leftRealms.Add(r);
		}
		else if (leftRealmChecks > 0)
		{
			if ((float)base.game.Random(0, 100) < def.field.GetFloat("leftover_realm_chance", r))
			{
				leftRealms.Add(r);
			}
		}
		else
		{
			stop = true;
		}
		leftRealmChecks--;
	}

	private void CalcRealms()
	{
		realms = new List<Realm>();
		Kingdom kingdom = (base.target as Character).GetKingdom();
		List<Realm> list = kingdom.realms;
		leftRealms?.Clear();
		if (kingdom.is_player || list.Count > 1)
		{
			leftRealmChecks = def.field.GetInt("min_checks_for_leftover_realms");
			base.game.RealmWave(kingdom.GetCapital(), 0, DecideLeftoverRealms);
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if ((leftRealms == null || !leftRealms.Contains(list[num])) && !list[num].IsOccupied())
			{
				realms.Add(list[num]);
			}
		}
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			CalcRealms();
			return true;
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		if (realms != null)
		{
			int num = (base.target as Character).GetKingdom().realms.Count - realms.Count;
			outcome_vars.Set("realms", realms);
			outcome_vars.Set("no_valid_realms", realms.Count == 0);
			outcome_vars.Set("has_leftover_realms", realms.Count > 0 && num > 0);
			outcome_vars.Set("leftover_realms_count", num);
		}
	}

	public override void Run()
	{
		base.Run();
		if (realms.Count == 0)
		{
			return;
		}
		Character character = base.target as Character;
		Kingdom kingdom = character.GetKingdom();
		using (new Kingdom.CacheRBS("PuppetArrangeAnnexationAction"))
		{
			for (int i = 0; i < realms.Count; i++)
			{
				realms[i].SetKingdom(own_kingdom.id);
			}
		}
		own_kingdom.DelVassalState(kingdom);
		base.own_character.DelPuppet(character);
		base.Run();
	}
}
