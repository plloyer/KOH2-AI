using System;
using System.Collections.Generic;

namespace Logic;

public class KingdomTradeRanking : Component, IVars
{
	public class Def : Logic.Def
	{
		public float min;

		public float max;

		public float smoothness = 1f;

		public float inflex;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			min = field.GetFloat("min", null, 10f);
			max = field.GetFloat("max", null, 50f);
			inflex = field.GetFloat("inflex", null, 70f);
			smoothness = field.GetFloat("smoothness", null, 20f);
			return true;
		}
	}

	internal class KingdomTradePowerRank
	{
		public Kingdom kingdom;

		public float tradePower;

		public KingdomTradePowerRank()
		{
		}

		public KingdomTradePowerRank(Kingdom kingdom, float tradePower)
		{
			this.kingdom = kingdom;
			this.tradePower = tradePower;
		}
	}

	public Dictionary<int, int> ranking;

	public int maxRank = 1;

	private List<KingdomTradePowerRank> tempRanking;

	public Def def;

	private Vars temp_vars = new Vars();

	private bool init;

	public KingdomTradeRanking(Economy economy)
		: base(economy)
	{
	}

	public void Init()
	{
		UpdateInBatch(base.game.update_5sec);
		ranking = new Dictionary<int, int>();
		tempRanking = new List<KingdomTradePowerRank>();
		PopulateRanking();
		if (base.game != null && base.game.defs != null)
		{
			InitDef();
			init = true;
		}
	}

	public void Shutdown()
	{
		StopUpdating();
		ranking = null;
		tempRanking = null;
	}

	private void InitDef()
	{
		def = base.game.defs.Get<Def>("KingdomTradeRanking");
	}

	private int CompareTradePowers(KingdomTradePowerRank k1, KingdomTradePowerRank k2)
	{
		if (!(k1.tradePower > k2.tradePower))
		{
			if (k1.tradePower != k2.tradePower)
			{
				return -1;
			}
			return 0;
		}
		return 1;
	}

	private void PopulateRanking()
	{
		if (base.game.kingdoms == null)
		{
			Warning("No kingdoms");
			return;
		}
		tempRanking.Clear();
		ranking.Clear();
		for (int i = 0; i < base.game.kingdoms.Count; i++)
		{
			Kingdom kingdom = base.game.kingdoms[i];
			if (!kingdom.IsDefeated())
			{
				ranking.Add(kingdom.id, 1);
				tempRanking.Add(new KingdomTradePowerRank
				{
					kingdom = kingdom,
					tradePower = kingdom.GetStat(Stats.ks_commerce)
				});
			}
		}
	}

	private void SortRanking()
	{
		tempRanking.Sort((KingdomTradePowerRank k1, KingdomTradePowerRank k2) => -1 * k1.tradePower.CompareTo(k2.tradePower));
		int num = 1;
		int num2 = 0;
		for (int num3 = 0; num3 < tempRanking.Count - 1; num3++)
		{
			num2++;
			KingdomTradePowerRank kingdomTradePowerRank = tempRanking[num3];
			KingdomTradePowerRank kingdomTradePowerRank2 = tempRanking[num3 + 1];
			ranking[kingdomTradePowerRank.kingdom.id] = num;
			if (kingdomTradePowerRank2.tradePower != kingdomTradePowerRank.tradePower)
			{
				num += num2;
				num2 = 0;
			}
		}
		if (tempRanking.Count > 0)
		{
			ranking[tempRanking[tempRanking.Count - 1].kingdom.id] = num;
		}
		maxRank = num;
	}

	public float GetBaseGold(int kId)
	{
		if (def == null)
		{
			return 0f;
		}
		if (ranking == null)
		{
			return 0f;
		}
		int value = 0;
		ranking.TryGetValue(kId, out value);
		float num = ((value == maxRank || maxRank <= 1) ? 0f : ((1f - (float)(value - 1) / (float)(maxRank - 1)) * 100f));
		double num2 = Math.Atan((0f - def.inflex) / def.smoothness);
		double num3 = Math.Atan((num - def.inflex) / def.smoothness);
		double value2 = Math.Atan((100f - def.inflex) / def.smoothness);
		float num4 = (float)((num3 - num2) / (Math.Abs(num2) + Math.Abs(value2)) * (double)(def.max - def.min) + (double)def.min);
		if (!(num4 < 0f))
		{
			return num4;
		}
		return 0f;
	}

	public override void OnUpdate()
	{
		if (!init)
		{
			InitDef();
		}
		PopulateRanking();
		SortRanking();
	}

	public Value GetVarKingdoms(string key, Kingdom ownKingdom, Kingdom tarKingdom)
	{
		temp_vars.Set("target_kingdom", tarKingdom);
		temp_vars.Set("own_kingdom", ownKingdom);
		return GetVar(key);
	}

	public Value GetVarKingdomRealm(string key, Kingdom kingdom, Realm realm)
	{
		temp_vars.Set("target_realm", realm);
		temp_vars.Set("own_kingdom", kingdom);
		return GetVar(key);
	}

	public Value GetVarKingdoms(string key, Kingdom ownKingdom, Kingdom tarKingdom, Character merchant)
	{
		temp_vars.Set("merchant_class_level", merchant.GetClassLevel("Merchant"));
		return GetVarKingdoms(key, ownKingdom, tarKingdom);
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Kingdom kingdom = temp_vars.GetVar("target_kingdom").Get<Kingdom>();
		Kingdom kingdom2 = temp_vars.GetVar("own_kingdom").Get<Kingdom>();
		Realm realm = temp_vars.GetVar("target_realm").Get<Realm>();
		if (kingdom == null && realm != null)
		{
			kingdom = realm.GetKingdom();
		}
		switch (key)
		{
		case "distance":
		{
			float num = base.game.KingdomAndRealmDistance(kingdom2.id, realm.id);
			if (num < 0f)
			{
				kingdom.RecalcKingdomDistances();
				num = base.game.KingdomAndRealmDistance(kingdom2.id, realm.id);
			}
			if (num < 0f)
			{
				num = 0f;
			}
			return (float)Math.Sqrt(num);
		}
		case "relationship":
			return (float)Math.Pow((0f - kingdom2.GetRelationship(kingdom)) / RelationUtils.Def.maxRelationship, 2.0);
		case "merchant_class_level":
			return temp_vars.Get("merchant_class_level");
		case "power_x":
			return kingdom2.GetStat(Stats.ks_commerce);
		case "power_y":
			return kingdom.GetStat(Stats.ks_commerce);
		default:
			return Value.Unknown;
		}
	}
}
