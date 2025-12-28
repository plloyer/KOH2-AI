using System;
using System.Collections.Generic;

namespace Logic;

public class Inheritance : Component
{
	public class Def : Logic.Def
	{
		public float kingdom_provinces_divider = 3f;

		public int min_provinces = 1;

		public int max_provinces = 3;

		public float timeout_after = 300f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			kingdom_provinces_divider = field.GetFloat("kingdom_provinces_divider", null, kingdom_provinces_divider);
			min_provinces = field.GetInt("min_provinces", null, min_provinces);
			max_provinces = field.GetInt("max_provinces", null, max_provinces);
			timeout_after = field.GetFloat("timeout_after", null, timeout_after);
			return true;
		}
	}

	private Kingdom k;

	private Def def;

	public Character currentPrincess;

	public Kingdom currentKingdom;

	public List<Realm> realms = new List<Realm>();

	public List<Character> princesses = new List<Character>();

	public Inheritance(Kingdom k)
		: base(k)
	{
		def = k.game.defs.GetBase<Def>();
		this.k = k;
	}

	public void AddInheriancePrincess(Character princess)
	{
		if (k.IsAuthority() && princesses != null && princess.IsMarried())
		{
			princesses.Add(princess);
			k.SendState<Kingdom.InheritancePricessesState>();
		}
	}

	public void CalcInheritanceRealm(Realm r, Realm rStart, int depth, object param, ref bool push_neighbors, ref bool stop)
	{
		if (r.IsOwnStance(k))
		{
			realms.Add(r);
			int num = (int)param;
			if (realms.Count >= num)
			{
				stop = true;
			}
		}
	}

	public List<Realm> CalcInheritanceRealms(Kingdom kingdom)
	{
		realms.Clear();
		if (!k.IsAuthority() || k.IsEnemy(kingdom) || kingdom.IsDefeated())
		{
			return realms;
		}
		if (k.realms.Count <= 1)
		{
			return realms;
		}
		int num = (int)Math.Floor((float)k.realms.Count / def.kingdom_provinces_divider);
		if (num < def.min_provinces)
		{
			num = def.min_provinces;
		}
		if (num > def.max_provinces)
		{
			num = def.max_provinces;
		}
		if (num > k.realms.Count - 1)
		{
			num = k.realms.Count - 1;
		}
		num = base.game.Random(1, num + 1);
		Realm closestRealmOfKingdom = kingdom.GetClosestRealmOfKingdom(k.id);
		base.game.RealmWave(closestRealmOfKingdom, 0, CalcInheritanceRealm, num);
		return realms;
	}

	public void StartInheritanceProcess(bool from_state = false)
	{
		if (currentKingdom.is_player)
		{
			UpdateAfter(def.timeout_after);
			currentKingdom.NotifyListeners("princess_inheritance", k);
			return;
		}
		Offer cachedOffer = Offer.GetCachedOffer("PrincessClaimInheritanceOffer", currentKingdom, k);
		cachedOffer.SetArg(0, currentPrincess);
		cachedOffer.SetArg(1, new Value(realms));
		Kingdom kingdom = cachedOffer.from as Kingdom;
		Kingdom kingdom2 = cachedOffer.to as Kingdom;
		bool flag = kingdom != null && kingdom2 != null && kingdom.allies.Contains(kingdom2);
		if (cachedOffer.Validate() != "ok" || !cachedOffer.CheckThreshold("propose") || flag)
		{
			currentKingdom.AddRelationModifier(k, "rel_abstain_from_claiming_inheritence", null);
			HandleNextPrincess();
		}
		else
		{
			cachedOffer.Send();
		}
	}

	public Time GetExpireTime()
	{
		return tmNextUpdate;
	}

	public override void OnUpdate()
	{
		HandleNextPrincess();
	}

	public void HandleNextPrincess()
	{
		if (!k.IsAuthority())
		{
			k.SendEvent(new Kingdom.InheritanceNextPrincessEvent());
			return;
		}
		if (IsRegisteredForUpdate())
		{
			StopUpdating();
		}
		if (princesses == null || princesses.Count == 0)
		{
			currentPrincess = null;
			currentKingdom = null;
			k.SendState<Kingdom.InheritanceState>();
			return;
		}
		currentPrincess = princesses[0];
		princesses.RemoveAt(0);
		currentKingdom = (currentPrincess?.GetMarriage())?.kingdom_husband;
		if (currentKingdom == null || currentKingdom.IsDefeated() || currentKingdom.IsEnemy(k))
		{
			HandleNextPrincess();
			return;
		}
		CalcInheritanceRealms(currentKingdom);
		if (realms.Count == 0)
		{
			Vars vars = new Vars();
			vars.Set("princess", currentPrincess);
			currentKingdom.FireEvent("inheritance_insufficient_realms", vars, currentKingdom.id);
			HandleNextPrincess();
		}
		else
		{
			k.SendState<Kingdom.InheritanceState>();
			StartInheritanceProcess();
		}
	}

	public void Abstain()
	{
		if (!k.IsAuthority())
		{
			k.SendEvent(new Kingdom.InheritanceAbstainEvent());
			return;
		}
		currentKingdom.AddRelationModifier(k, "rel_abstain_from_claiming_inheritence", null);
		HandleNextPrincess();
	}
}
