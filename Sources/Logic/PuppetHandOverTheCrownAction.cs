using System;
using System.Collections.Generic;

namespace Logic;

public class PuppetHandOverTheCrownAction : PuppetPlot
{
	private List<Realm> leftRealms;

	private int leftRealmChecks;

	private List<Realm> realms;

	private float gold;

	public PuppetHandOverTheCrownAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetHandOverTheCrownAction(owner as Character, def);
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
		if (puppet.GetKingdom().is_player && puppet.GetKingdom().realms.Count <= def.field.GetInt("min_player_realms"))
		{
			return "not_enough_realms";
		}
		if (!War.CanStart(own_kingdom, puppet.GetKingdom()))
		{
			return "cant_start_war";
		}
		return base.ValidatePuppet(puppet);
	}

	public void DecidePlayerLeftRealms(Realm r, Realm rStart, int depth, object param, ref bool push_neighbors, ref bool stop)
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
			base.game.RealmWave(kingdom.GetCapital(), 0, DecidePlayerLeftRealms);
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
			gold = (base.target as Character).GetKingdom().resources[ResourceType.Gold] * def.field.GetFloat("gold_percent") / 100f;
			gold = Math.Max(gold, 0f);
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
			outcome_vars.Set("gold", gold);
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
		using (new Kingdom.CacheRBS("PuppetHandOverTheCrownAction"))
		{
			for (int i = 0; i < realms.Count; i++)
			{
				realms[i].SetKingdom(own_kingdom.id);
			}
		}
		if (!kingdom.IsDefeated())
		{
			kingdom.StartWarWith(own_kingdom, War.InvolvementReason.CrownHandedOverByPuppet);
		}
		kingdom.SubResources(KingdomAI.Expense.Category.Economy, ResourceType.Gold, gold);
		own_kingdom.AddResources(KingdomAI.Expense.Category.Espionage, ResourceType.Gold, gold);
		base.own_character.DelPuppet(character);
		character.DesertKingdom(createClone: false, null, dieIfNoNewKingdom: true, keep_army: true);
	}
}
