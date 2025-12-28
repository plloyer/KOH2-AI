using System;
using System.Collections.Generic;

namespace Logic;

public class MercenarySpawner : Component
{
	public class RealmWaveItem
	{
		public int tier;

		public Realm realm;

		public RealmWaveItem(int t, Realm r)
		{
			tier = t;
			realm = r;
		}
	}

	public static bool global_spawn_enable = true;

	public Mercenary.Def def;

	private Kingdom kingdom;

	public static void EnableSpawn()
	{
		global_spawn_enable = true;
	}

	public static void DisableSpawn()
	{
		global_spawn_enable = false;
	}

	public MercenarySpawner(Kingdom kingdom)
		: base(kingdom)
	{
		this.kingdom = kingdom;
		def = base.game.defs.GetBase<Mercenary.Def>();
	}

	public override void OnStart()
	{
		if (obj.IsAuthority())
		{
			base.OnStart();
			if (Timer.Find(kingdom, "mercenary_spawn_timer") == null)
			{
				float num = base.game.Random(def.spawn_period * 0.2f, def.spawn_period);
				Timer.Start(kingdom, "mercenary_spawn_timer", def.spawn_initial_wait + num);
			}
		}
	}

	public void OnTimer()
	{
		float num = def.spawn_period / (float)Math.Max(1, kingdom.realms.Count);
		if (num == 0f)
		{
			num = 0.5f;
		}
		Timer.Start(kingdom, "mercenary_spawn_timer", num, restart: true);
		if (!kingdom.IsDefeated() && global_spawn_enable)
		{
			TrySpawn();
		}
	}

	private void TrySpawn()
	{
		if (string.IsNullOrEmpty(base.game.map_name))
		{
			return;
		}
		Mercenary.Def def = base.game.defs.GetBase<Mercenary.Def>();
		if (def != null && base.game.Random(0f, 100f) < def.spawn_chance)
		{
			int index = base.game.Random(0, kingdom.realms.Count);
			Realm realm = kingdom.realms[index];
			if (GetMercenariesCountInRealm(realm) < def.max_mercs_per_realm && GetMercenariesCountInKingdom(kingdom) < def.max_mercs_per_kingdom && GetTotalMercenariesCount(def) < def.max_mercs)
			{
				new Mercenary(base.game, GetNearbyRealm(realm, def.parent_realm_serch_limit).kingdom_id, realm, def);
			}
		}
	}

	public int GetTotalMercenariesCount(Mercenary.Def def)
	{
		int num = 0;
		foreach (Army army in FactionUtils.GetFactionKingdom(base.game, def.kingdom_key).armies)
		{
			if (army.mercenary != null)
			{
				num++;
			}
		}
		return num;
	}

	public int GetMercenariesCountInRealm(Realm r)
	{
		int num = 0;
		foreach (Army army in r.armies)
		{
			if (army.mercenary != null)
			{
				num++;
			}
		}
		return num;
	}

	public int GetMercenariesCountInKingdom(Kingdom k)
	{
		int num = 0;
		foreach (Army item in k.armies_in)
		{
			if (item.mercenary != null)
			{
				num++;
			}
		}
		return num;
	}

	public static Realm GetNearbyRealm(Realm current_realm, int search_limit = 25, int k_id = -1, Army army = null)
	{
		List<RealmWaveItem> list = new List<RealmWaveItem>();
		List<RealmWaveItem> list2 = new List<RealmWaveItem>();
		RealmWaveItem item = new RealmWaveItem(0, current_realm);
		list2.Add(item);
		list.Add(item);
		search_limit++;
		Realm result = null;
		float num = float.MaxValue;
		Kingdom k = null;
		if (k_id != -1)
		{
			k = current_realm.game.GetKingdom(k_id);
		}
		RelationUtils.Stance stance = RelationUtils.Stance.War;
		while (list.Count < search_limit && list2.Count > 0)
		{
			int num2;
			for (num2 = 0; num2 < list2.Count; num2++)
			{
				RealmWaveItem parent_item = list2[num2];
				int j = parent_item.realm.neighbors.Count - 1;
				while (j >= 0)
				{
					if (list.Find((RealmWaveItem x) => x.realm == parent_item.realm.neighbors[j]) == null)
					{
						RealmWaveItem realmWaveItem = new RealmWaveItem(parent_item.tier + 1, parent_item.realm.neighbors[j]);
						list.Add(realmWaveItem);
						list2.Add(realmWaveItem);
						if (k_id != -1 && army != null && !realmWaveItem.realm.IsSeaRealm())
						{
							float num3 = realmWaveItem.realm.castle.position.SqrDist(army.position);
							RelationUtils.Stance warStance = realmWaveItem.realm.GetWarStance(k);
							if (warStance >= stance && num3 < num)
							{
								num = num3;
								result = realmWaveItem.realm;
								stance = warStance;
							}
						}
					}
					if (list.Count >= search_limit)
					{
						break;
					}
					int num4 = j - 1;
					j = num4;
				}
				list2.RemoveAt(num2);
				num2--;
				if (list.Count >= search_limit)
				{
					break;
				}
			}
		}
		list.RemoveAll((RealmWaveItem x) => x.realm.id <= 0);
		if (list.Count == 0)
		{
			return current_realm;
		}
		if (list.Count == 1)
		{
			return list[0].realm;
		}
		int tier = list[list.Count - 1].tier;
		int randomWithDescendingPriority = GetRandomWithDescendingPriority(1, tier, current_realm.game);
		int num5 = 0;
		int num6 = list.Count - 1;
		for (int num7 = 1; num7 < list.Count; num7++)
		{
			if (randomWithDescendingPriority <= list[num7].tier)
			{
				num5 = num7;
				break;
			}
		}
		for (int num8 = list.Count - 1; num8 >= 0; num8--)
		{
			if (randomWithDescendingPriority >= list[num8].tier)
			{
				num6 = num8;
				break;
			}
		}
		if (num6 < num5)
		{
			num6 = num5;
		}
		if (k_id != -1 && army != null)
		{
			return result;
		}
		int index = current_realm.game.Random(num5, num6);
		return list[index].realm;
	}

	public static int GetRandomWithDescendingPriority(int min, int max, Game game)
	{
		float num = game.Random(0f, 1f);
		float num2 = 0.5f;
		for (int i = min; i <= max; i++)
		{
			if (num > num2)
			{
				return i;
			}
			num = game.Random(0f, 1f);
		}
		return min;
	}
}
