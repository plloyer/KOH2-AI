using System.Collections.Generic;

namespace Logic;

public class SecureMercenariesAction : MerchantOpportunity
{
	public SecureMercenariesAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new SecureMercenariesAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_mission_kingdom";
		}
		return base.Validate(quick_out);
	}

	public override bool NeedsTarget()
	{
		return true;
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Army army))
		{
			return false;
		}
		if (army.mercenary == null)
		{
			return false;
		}
		if (army.mercenary.former_owner_id != 0)
		{
			return false;
		}
		if (army.IsHiredMercenary())
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		for (int i = 0; i < mission_kingdom.realms.Count; i++)
		{
			Realm realm = mission_kingdom.realms[i];
			for (int j = 0; j < realm.armies.Count; j++)
			{
				Army army = realm.armies[j];
				AddTarget(ref targets, army);
			}
		}
		for (int k = 0; k < mission_kingdom.externalBorderRealms.Count; k++)
		{
			Realm realm2 = mission_kingdom.externalBorderRealms[k];
			for (int l = 0; l < realm2.armies.Count; l++)
			{
				Army army2 = realm2.armies[l];
				AddTarget(ref targets, army2);
			}
		}
		return targets;
	}

	public override void Cancel(bool manual = false, bool notify = true)
	{
		base.Cancel(manual, notify);
	}

	public override void Run()
	{
		Army army = base.target as Army;
		army.mercenary.SendState<Mercenary.OriginState>();
		army.NotifyListeners("mercenary_changed");
		Realm realm = null;
		for (int i = 0; i < own_kingdom.realms.Count; i++)
		{
			Realm realm2 = own_kingdom.realms[i];
			if (realm == null || realm.castle.position.Dist(army.position) > realm2.castle.position.Dist(army.position))
			{
				realm = realm2;
			}
		}
		army.MoveTo(Army.GetRandomRealmPoint(realm));
		base.Run();
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		if (def.cost == null)
		{
			return null;
		}
		if (vars == null)
		{
			if (target == null)
			{
				vars = this;
			}
			else
			{
				Vars vars2 = new Vars(this);
				vars2.Set("target", target);
				vars = vars2;
			}
		}
		return Resource.Parse(def.cost, vars);
	}
}
