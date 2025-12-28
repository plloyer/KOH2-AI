using System.Collections.Generic;

namespace Logic;

public class PuppetDeclareIndependenceAction : PuppetPlot
{
	private Kingdom independenceKingdom;

	public PuppetDeclareIndependenceAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetDeclareIndependenceAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.IsRoyalty())
		{
			return "is_royalty";
		}
		if (puppet.IsPatriarch())
		{
			return "is_patriarch";
		}
		if (puppet.IsCrusader())
		{
			return "is_crusader";
		}
		if (puppet.governed_castle == null)
		{
			return "not_a_governor";
		}
		if (puppet.GetKingdom().realms.Count <= 1)
		{
			return "not_enough_realms";
		}
		return base.ValidatePuppet(puppet);
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			independenceKingdom = GetIndependenceKingdom();
			return true;
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("independence_kingdom", independenceKingdom);
		outcome_vars.Set("independance_realm_turned_vassal", WillBecomeVassal());
	}

	private bool WillBecomeVassal()
	{
		if (independenceKingdom == null)
		{
			return false;
		}
		if (independenceKingdom.IsDefeated())
		{
			return own_kingdom.sovereignState == null;
		}
		return false;
	}

	public Kingdom GetIndependenceKingdom(Character puppet = null)
	{
		if (puppet == null)
		{
			puppet = base.target as Character;
		}
		Kingdom kingdom = (puppet?.governed_castle?.GetRealm())?.GetIndependenceKingdom();
		if (kingdom == null)
		{
			kingdom = own_kingdom;
		}
		return kingdom;
	}

	public List<Realm> GetIndependenceRealms()
	{
		Character character = base.target as Character;
		List<Realm> list = new List<Realm>();
		Realm realm = character.governed_castle.GetRealm();
		list.Add(realm);
		for (int i = 0; i < realm.neighbors.Count; i++)
		{
			if (list.Count + 1 >= realm.GetKingdom().realms.Count)
			{
				break;
			}
			Realm realm2 = realm.neighbors[i];
			if (realm2.kingdom_id == character.kingdom_id && !realm2.IsOccupied() && base.game.Random(0, 100) < character.GetClassLevel())
			{
				list.Add(realm2);
			}
		}
		return list;
	}

	public override void Run()
	{
		Character character = base.target as Character;
		character.GetKingdom();
		character.governed_castle.GetRealm();
		List<Realm> independenceRealms = GetIndependenceRealms();
		base.own_character.DelPuppet(character);
		character.DesertKingdom(createClone: false, null, dieIfNoNewKingdom: false, keep_army: true);
		bool num = WillBecomeVassal();
		independenceKingdom?.DeclareIndependenceOrJoin(independenceRealms, character, new List<Character> { character });
		if (num)
		{
			own_kingdom.AddVassalState(independenceKingdom);
		}
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "independance_realm")
		{
			return (base.target as Character)?.governed_castle.GetRealm();
		}
		return base.GetVar(key, vars, as_value);
	}
}
