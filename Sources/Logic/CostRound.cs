using System;
using System.Collections.Generic;

namespace Logic;

public class Economy : Object
{
	public class Def : Logic.Def
	{
		public struct CostRound
		{
			public float threshold;

			public float round_to;
		}

		public CostRound[] cost_rounds;

		public float[] gold_from_excess_resources_perc;

		public float[] gold_from_excess_resources_cap;

		public static float gold_income_multiplier = 1f;

		public static float gold_expense_multiplier = 1f;

		public Resource income_multipliers;

		public Resource expense_multipliers;

		public IncomePerResource.Def[] incomes = new IncomePerResource.Def[13];

		public IncomePerResource.Def[] upkeeps = new IncomePerResource.Def[13];

		public int min_trade_level;

		public int min_trade_level_on_mission = 1;

		public int max_trade_level = 4;

		public List<DT.Field> trade_levels = new List<DT.Field>();

		public float time_spawn_or_despawn_trade_center;

		public float fist_time_spawn_trade_center_for_shattered_map;

		public float time_to_refresh_trade_center_realms;

		public float time_to_recheck_trade_routes;

		public float time_to_decay_trade_agreement;

		public float trade_route_min_time_before_close;

		public List<float> trade_route_capacity_thresholds = new List<float>();

		public float move_appeal_penalty_for_multiple_TC_around;

		public int merchant_max_import_goods_count;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			DT.Field field2 = field.FindChild("cost_round");
			if (field2?.children != null)
			{
				List<CostRound> list = new List<CostRound>();
				for (int i = 0; i < field2.children.Count; i++)
				{
					DT.Field obj = field2.children[i];
					DT.Field field3 = obj.FindChild("threshold");
					DT.Field field4 = obj.FindChild("round_to");
					if (field3 != null && field4 != null)
					{
						CostRound item = new CostRound
						{
							threshold = field3.Float(),
							round_to = field4.Float()
						};
						list.Add(item);
					}
				}
				if (list.Count > 0)
				{
					cost_rounds = list.ToArray();
				}
			}
			gold_from_excess_resources_perc = new float[13];
			gold_from_excess_resources_cap = new float[13];
			DT.Field field5 = field.FindChild("gold_from_excess_resources");
			if (field5?.children != null)
			{
				for (int j = 0; j < field5.children.Count; j++)
				{
					DT.Field field6 = field5.children[j];
					if (!string.IsNullOrEmpty(field6.key))
					{
						ResourceType type = Resource.GetType(field6.key);
						if (type == ResourceType.None || type >= ResourceType.COUNT)
						{
							Game.Log(field6.Path(include_file: true) + ": Invalid resource type", Game.LogType.Error);
							continue;
						}
						float num = field6.Float(0);
						float num2 = field6.Float(1);
						gold_from_excess_resources_perc[(int)type] = num;
						gold_from_excess_resources_cap[(int)type] = num2;
					}
				}
			}
			income_multipliers = Resource.Parse(field.FindChild("income_multipliers"));
			expense_multipliers = Resource.Parse(field.FindChild("expense_multipliers"));
			LoadTradeDef();
			time_spawn_or_despawn_trade_center = field.GetFloat("time_spawn_or_despawn_trade_center");
			fist_time_spawn_trade_center_for_shattered_map = field.GetFloat("fist_time_spawn_trade_center_for_shattered_map");
			time_to_refresh_trade_center_realms = field.GetFloat("time_to_refresh_trade_center_realms");
			time_to_recheck_trade_routes = field.GetFloat("time_to_recheck_trade_routes");
			time_to_decay_trade_agreement = field.GetFloat("time_to_decay_trade_agreement");
			trade_route_min_time_before_close = field.GetFloat("trade_route_min_time_before_close");
			move_appeal_penalty_for_multiple_TC_around = field.GetFloat("move_appeal_penalty_for_multiple_TC_around");
			DT.Field field7 = field.FindChild("trade_route_capacity_thresholds");
			int num3 = field7.NumValues();
			for (int k = 0; k < num3; k++)
			{
				trade_route_capacity_thresholds.Add(field7.Value(k));
			}
			merchant_max_import_goods_count = field.GetInt("merchant_max_import_goods_count");
			return true;
		}

		public float RoundCost(float val)
		{
			if (cost_rounds == null)
			{
				return val;
			}
			for (int num = cost_rounds.Length - 1; num >= 0; num--)
			{
				CostRound costRound = cost_rounds[num];
				if (!(val < costRound.threshold) || num <= 0)
				{
					float round_to = costRound.round_to;
					float num2 = Math.Sign(val);
					val = num2 * val;
					float num3 = val % round_to;
					num3 = ((!(num3 < round_to / 2f)) ? round_to : 0f);
					return num2 * (val - val % round_to + num3);
				}
			}
			return val;
		}

		public override bool Validate(Game game)
		{
			LoadIncomeDefs(game);
			return true;
		}

		private void LoadIncomeDefs(Game game)
		{
			DT.Field field = base.field.FindChild("income");
			DT.Field field2 = base.field.FindChild("upkeep");
			for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
			{
				string path = resourceType.ToString();
				DT.Field field3 = field?.FindChild(path);
				if (field3 == null)
				{
					incomes[(int)resourceType] = null;
				}
				else
				{
					IncomePerResource.Def def = new IncomePerResource.Def();
					def.Load(field3, resourceType, game);
					incomes[(int)resourceType] = def;
				}
				DT.Field field4 = field2?.FindChild(path);
				if (field4 == null)
				{
					upkeeps[(int)resourceType] = null;
					continue;
				}
				IncomePerResource.Def def2 = new IncomePerResource.Def();
				def2.is_upkeep = true;
				def2.Load(field4, resourceType, game);
				upkeeps[(int)resourceType] = def2;
			}
		}

		private void LoadTradeDef()
		{
			DT.Field trade_field = base.field.FindChild("trade");
			LoadTradeLevels(trade_field);
		}

		private void LoadTradeLevels(DT.Field trade_field)
		{
			trade_levels.Clear();
			if (trade_field == null)
			{
				return;
			}
			int num = 0;
			min_trade_level = 0;
			min_trade_level_on_mission = 1;
			while (true)
			{
				DT.Field field = trade_field.FindChild($"level_{num}");
				if (field != null)
				{
					trade_levels.Add(field);
					max_trade_level = num;
					num++;
					continue;
				}
				break;
			}
		}

		public float IncomeMultiplier(ResourceType rt)
		{
			float num = ((income_multipliers == null) ? 1f : income_multipliers[rt]);
			if (num <= 0f)
			{
				num = 1f;
			}
			if (rt == ResourceType.Gold && gold_income_multiplier > 0f)
			{
				num *= gold_income_multiplier;
			}
			return num;
		}

		public float ExpenseMultiplier(ResourceType rt)
		{
			float num = ((expense_multipliers == null) ? 1f : expense_multipliers[rt]);
			if (num <= 0f)
			{
				num = 1f;
			}
			if (rt == ResourceType.Gold && gold_expense_multiplier > 0f)
			{
				num *= gold_expense_multiplier;
			}
			return num;
		}
	}

	public Def def;

	public KingdomTradeRanking trade_ranking;

	public List<Realm> tradeCenterRealms = new List<Realm>();

	public Time nextTCSpawnDespawn;

	public Time nextTCRefresh;

	private float trCheckKingdomsAtATime = 10f;

	private int trCurrentKingdom;

	public TradeCenter.Def tcDef;

	public const float update_cycle = 5f;

	private Queue<Realm> tcQueRealms = new Queue<Realm>();

	private Queue<int> tcQueDistances = new Queue<int>();

	private Dictionary<int, float> spawnAppeal;

	private List<Realm> moveToRealms = new List<Realm>();

	private List<Realm> moveTCs = new List<Realm>();

	private static Vars tmp_trade_level_vars = new Vars();

	private static Kingdom tmp_trade_level_vars_src_kingdom = null;

	private static Kingdom tmp_trade_level_vars_tgt_kingdom = null;

	private static long tmp_trade_level_vars_frame = -1L;

	public void Init(bool new_game)
	{
		def = game.defs.Get<Def>("Economy");
		if (game.kingdoms == null)
		{
			Warning("No kingdoms");
			return;
		}
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom.IsRegular())
			{
				Incomes.CreateForKingdom(kingdom);
				if (kingdom.def != null)
				{
					kingdom.SetResources(Resource.Parse(kingdom.def.FindChild("starting_resources"), kingdom, no_null: true), send_state: false);
				}
			}
		}
		trade_ranking.Init();
		if (!new_game)
		{
			RefreshTradeCenters();
		}
		UpdateAfter(5f);
	}

	public void Shutdown()
	{
		StopUpdating();
		tradeCenterRealms.Clear();
		nextTCSpawnDespawn = (nextTCRefresh = Time.Zero);
		trCurrentKingdom = 0;
		trade_ranking.Shutdown();
	}

	public override void OnInit()
	{
		base.OnInit();
		trade_ranking = new KingdomTradeRanking(this);
	}

	public override void OnUpdate()
	{
		if (IsAuthority())
		{
			if (nextTCSpawnDespawn <= game.time)
			{
				nextTCSpawnDespawn = game.time + def.time_spawn_or_despawn_trade_center * 60f;
				nextTCRefresh = game.time + def.time_to_refresh_trade_center_realms;
				SpawnDespawnTC();
			}
			if (nextTCRefresh <= game.time)
			{
				nextTCRefresh = game.time + def.time_to_refresh_trade_center_realms;
				RefreshTradeCenters();
			}
		}
		UpdateAfter(5f);
	}

	public void RefreshTradeCenters()
	{
		if (game == null || game.realms == null)
		{
			return;
		}
		if (tcDef == null)
		{
			tcDef = game.defs.Find<TradeCenter.Def>("TradeCenter");
			if (tcDef == null)
			{
				return;
			}
		}
		for (int i = 0; i < game.realms.Count; i++)
		{
			game.realms[i].wave_depth = 0;
			game.realms[i].wave_prev = null;
		}
		tcQueRealms.Clear();
		tcQueDistances.Clear();
		for (int j = 0; j < tradeCenterRealms.Count; j++)
		{
			Realm realm = tradeCenterRealms[j];
			if (realm != null)
			{
				TradeCenter tradeCenter = realm.GetComponent<TradeCenter>();
				if (tradeCenter == null)
				{
					tradeCenter = new TradeCenter(realm);
				}
				tradeCenter.CleanRealms();
				tcQueRealms.Enqueue(realm);
				tcQueDistances.Enqueue(0);
				tradeCenter.belongingRealms.Clear();
				tradeCenter.AddBelongingRealm(realm, 0);
				realm.wave_depth = (int)(realm.GetAppeal() * (100f + tradeCenter.realm.GetKingdom().GetStat(Stats.ks_increase_TC_range)) / 100f);
			}
		}
		while (tcQueRealms.Count > 0)
		{
			Realm realm2 = tcQueRealms.Dequeue();
			int num = tcQueDistances.Dequeue();
			float num2 = realm2.wave_depth;
			TradeCenter tradeCenter2 = realm2.tradeCenter;
			int num3 = 0;
			if (num2 <= 0f)
			{
				continue;
			}
			foreach (Realm neighbor in realm2.neighbors)
			{
				if (neighbor == null || neighbor.IsTradeCenter())
				{
					continue;
				}
				float num4 = 1f;
				if (!neighbor.IsSeaRealm())
				{
					float num5 = RelationUtils.Def.maxRelationship;
					float num6 = RelationUtils.Def.maxRelationship;
					if (neighbor.kingdom_id != realm2.kingdom_id && !realm2.IsSeaRealm())
					{
						Kingdom kingdom = realm2.GetKingdom();
						Kingdom kingdom2 = neighbor.GetKingdom();
						num5 = kingdom.GetRelationship(kingdom2);
					}
					if (neighbor.kingdom_id != realm2.tradeCenter.realm.kingdom_id)
					{
						Kingdom kingdom3 = realm2.tradeCenter.realm.GetKingdom();
						Kingdom kingdom4 = neighbor.GetKingdom();
						if (kingdom3.IsEnemy(neighbor))
						{
							num4 = 0f;
						}
						else
						{
							num6 = kingdom3.GetRelationship(kingdom4);
						}
					}
					num4 *= (tcDef.tc_spread_relation_base + num5) / tcDef.tc_spread_relation_total;
					num4 *= (tcDef.tc_spread_relation_base + num6) / tcDef.tc_spread_relation_total;
					num3 = (int)(tcDef.tc_spread_appeal_fade_perc / 100f * num2 * num4 - tcDef.tc_spread_appeal_fade_const);
				}
				else
				{
					num3 = (int)(num2 - tcDef.tc_spread_appeal_fade_sea_const * (100f - tradeCenter2.realm.GetKingdom().GetStat(Stats.ks_tc_sea_spread_penalty_reduced_perc)) / 100f);
				}
				if (neighbor.wave_depth >= num3)
				{
					continue;
				}
				neighbor.wave_depth = num3;
				if (!neighbor.IsSeaRealm())
				{
					if (neighbor.tradeCenter != null)
					{
						neighbor.tradeCenter.DelBelongingRealm(neighbor);
					}
					tradeCenter2.AddBelongingRealm(neighbor, num + 1);
				}
				else
				{
					neighbor.tradeCenter = tradeCenter2;
				}
				tcQueRealms.Enqueue(neighbor);
				tcQueDistances.Enqueue(num + 1);
			}
		}
		for (int k = game.landRealmsCount; k < game.realms.Count; k++)
		{
			if (game.realms[k].IsSeaRealm())
			{
				game.realms[k].tradeCenter = null;
			}
		}
		game.SendState<Game.TradeCentersState>();
	}

	public void AddTradeCenterRealm(Realm r, bool send_state = true)
	{
		tradeCenterRealms.Add(r);
		if (send_state)
		{
			game.SendState<Game.TradeCentersState>();
		}
	}

	public void DelTradeCenterRealm(Realm r, bool send_state = true)
	{
		if (r != null)
		{
			tradeCenterRealms.Remove(r);
			for (TradeCenter component = r.GetComponent<TradeCenter>(); component != null; component = r.GetComponent<TradeCenter>())
			{
				r.RemoveComponent(component);
			}
			r.tradeCenter?.Clean();
			r.tradeCenter = null;
			if (send_state)
			{
				game.SendState<Game.TradeCentersState>();
			}
		}
	}

	private List<Realm> GetTCMoveRealms(bool force = false)
	{
		moveToRealms.Clear();
		for (int i = 0; i < tradeCenterRealms.Count; i++)
		{
			Realm realm = tradeCenterRealms[i];
			float num = spawnAppeal[realm.id];
			for (int j = 0; j < realm.neighbors.Count; j++)
			{
				Realm realm2 = realm.neighbors[j];
				if (realm2.IsSeaRealm() || realm2.IsTradeCenter())
				{
					continue;
				}
				float num2 = spawnAppeal[realm2.id];
				if ((force || num2 > num) && !moveToRealms.Contains(realm2))
				{
					moveToRealms.Add(realm2);
				}
				for (int k = 0; k < realm2.neighbors.Count; k++)
				{
					Realm realm3 = realm2.neighbors[k];
					if (!realm3.IsSeaRealm() && !realm3.IsTradeCenter())
					{
						float num3 = spawnAppeal[realm3.id];
						if ((force || num3 > num) && !moveToRealms.Contains(realm3))
						{
							moveToRealms.Add(realm3);
						}
					}
				}
			}
		}
		return moveToRealms;
	}

	public bool TryMoveTradeCenter(Realm forcedRealm = null)
	{
		List<Realm> tCMoveRealms = GetTCMoveRealms(forcedRealm != null);
		int num = game.Random(0, tCMoveRealms.Count);
		for (int i = 0; i < tCMoveRealms.Count; i++)
		{
			Realm realm = tCMoveRealms[(i + num) % tCMoveRealms.Count];
			if (forcedRealm != null && realm != forcedRealm)
			{
				continue;
			}
			moveTCs.Clear();
			for (int j = 0; j < realm.neighbors.Count; j++)
			{
				Realm realm2 = realm.neighbors[j];
				if (realm2.IsTradeCenter() && !moveTCs.Contains(realm2))
				{
					moveTCs.Add(realm2);
				}
				for (int k = 0; k < realm2.neighbors.Count; k++)
				{
					Realm realm3 = realm2.neighbors[k];
					if (realm3.IsTradeCenter() && !moveTCs.Contains(realm3))
					{
						moveTCs.Add(realm3);
					}
				}
			}
			if (moveTCs.Count == 1)
			{
				moveTCs[0].tradeCenter.MoveTo(realm);
				return true;
			}
			if (forcedRealm == null)
			{
				float num2 = spawnAppeal[realm.id] - def.move_appeal_penalty_for_multiple_TC_around * (float)(moveTCs.Count - 1);
				float num3 = 0f;
				for (int l = 0; l < moveTCs.Count; l++)
				{
					num3 = Math.Max(num3, spawnAppeal[moveTCs[l].id]);
				}
				if (num2 <= num3)
				{
					continue;
				}
			}
			for (int m = 0; m < moveTCs.Count; m++)
			{
				Realm r = moveTCs[m];
				DelTradeCenterRealm(r);
			}
			AddTradeCenterRealm(realm);
			RefreshTradeCenters();
			realm.NotifyListeners("trade_centers_merged", moveTCs);
			return true;
		}
		if (forcedRealm != null)
		{
			game.Log(forcedRealm.name + "(" + forcedRealm.town_name + ") is too far from any trade centers");
		}
		return false;
	}

	public void SpawnDespawnTC(bool force = false, Realm forcedRealm = null, Realm forcedMoveRealm = null)
	{
		if (!IsAuthority())
		{
			return;
		}
		if (forcedRealm != null && !forcedRealm.IsTradeCenter() && !CanHaveTradeCenter(forcedRealm))
		{
			game.Log(forcedRealm.name + "(" + forcedRealm.town_name + ") is too close to other trade centers and cant become one");
			return;
		}
		if (spawnAppeal == null)
		{
			spawnAppeal = new Dictionary<int, float>(game.realms.Count);
		}
		Realm realm = null;
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm2 = game.realms[i];
			if (!realm2.IsSeaRealm())
			{
				float appeal = realm2.GetAppeal();
				spawnAppeal[realm2.id] = appeal;
				if ((realm2.IsTradeCenter() || tradeCenterRealms.Count == 0) && (realm == null || appeal < spawnAppeal[realm.id]))
				{
					realm = realm2;
				}
			}
		}
		if ((!force && TryMoveTradeCenter(forcedMoveRealm)) || forcedMoveRealm != null)
		{
			return;
		}
		Realm realm3 = null;
		for (int j = 0; j < game.realms.Count; j++)
		{
			Realm realm4 = game.realms[j];
			if (!realm4.IsSeaRealm() && !realm4.IsTradeCenter() && (realm3 == null || spawnAppeal[realm3.id] < spawnAppeal[realm4.id]) && CanHaveTradeCenter(realm4, realm))
			{
				realm3 = realm4;
			}
		}
		if (tradeCenterRealms.Count == 0)
		{
			forcedRealm = realm;
		}
		if ((!force && (realm3 == null || realm == null)) || (!force && !(spawnAppeal[realm.id] < spawnAppeal[realm3.id])))
		{
			return;
		}
		if ((forcedRealm == null && CanHaveTradeCenter(realm3) && (float)game.Random(0, 100) < 100f * (float)(tcDef.max - tradeCenterRealms.Count) / (float)(tcDef.max - tcDef.min)) || (forcedRealm != null && !forcedRealm.IsTradeCenter()))
		{
			if (forcedRealm != null)
			{
				realm3 = forcedRealm;
			}
			AddTradeCenterRealm(realm3);
			realm3.NotifyListeners("trade_center_spawned");
		}
		else
		{
			if (forcedRealm != null)
			{
				realm = forcedRealm;
			}
			realm.NotifyListeners("trade_center_despawned");
			DelTradeCenterRealm(realm);
		}
		RefreshTradeCenters();
	}

	public bool CheckTCInNeighbors(Realm r, int distance, Realm ignoreTCRealm = null)
	{
		if (r == null)
		{
			return false;
		}
		if (r.wave_depth >= distance)
		{
			return false;
		}
		r.wave_depth = distance;
		if (distance <= 0)
		{
			return false;
		}
		if (tradeCenterRealms.Contains(r) && r != ignoreTCRealm)
		{
			return true;
		}
		for (int i = 0; i < r.neighbors.Count; i++)
		{
			if (CheckTCInNeighbors(r.neighbors[i], distance - 1, ignoreTCRealm))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanHaveTradeCenter(Realm r, Realm ignoreTCRealm = null)
	{
		if (r == ignoreTCRealm)
		{
			return false;
		}
		if (r == null || r.castle == null)
		{
			return false;
		}
		if (r.IsTradeCenter())
		{
			return false;
		}
		if (tcDef == null)
		{
			tcDef = game.defs.Find<TradeCenter.Def>("TradeCenter");
		}
		if (tcDef.min_realm_distance_between > 0)
		{
			for (int i = 0; i < game.realms.Count; i++)
			{
				game.realms[i].wave_depth = -1;
			}
			if (CheckTCInNeighbors(r, tcDef.min_realm_distance_between, ignoreTCRealm))
			{
				return false;
			}
		}
		return true;
	}

	public void InitTradeCenterRealms(ProvinceFeatureDistribution distribution, List<int> blacklistedKingdoms)
	{
		tcDef = game.defs.Find<TradeCenter.Def>("TradeCenter");
		if (!IsAuthority())
		{
			return;
		}
		nextTCSpawnDespawn = game.time + def.time_spawn_or_despawn_trade_center * 60f;
		nextTCRefresh = game.time + def.time_to_refresh_trade_center_realms;
		int num = game.Random(tcDef.min, tcDef.max + 1);
		tradeCenterRealms.Clear();
		foreach (KeyValuePair<RealmFeaturesPreset.Def, List<RealmData>> preset in distribution.presets)
		{
			RealmFeaturesPreset.Def key = preset.Key;
			if (!key.features.Contains("tradeCenter"))
			{
				continue;
			}
			List<RealmData> value = preset.Value;
			for (int i = 0; i < value.Count; i++)
			{
				if (!blacklistedKingdoms.Contains(value[i].realm.kingdom_id) && CanHaveTradeCenter(value[i].realm) && value[i].desired_preset == key)
				{
					tradeCenterRealms.Add(value[i].realm);
				}
			}
		}
		List<Tuple<Realm, float>> list = new List<Tuple<Realm, float>>(game.realms.Count);
		for (int j = 0; j < game.realms.Count; j++)
		{
			if (!game.realms[j].IsSeaRealm())
			{
				list.Add(new Tuple<Realm, float>(game.realms[j], game.realms[j].GetPotentialCommerse()));
			}
		}
		list.Sort((Tuple<Realm, float> d1, Tuple<Realm, float> d2) => d2.Item2.CompareTo(d1.Item2));
		int num2 = tradeCenterRealms.Count;
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			if (num2 >= num)
			{
				break;
			}
			if (!blacklistedKingdoms.Contains(list[num3].Item1.kingdom_id) && CanHaveTradeCenter(list[num3].Item1))
			{
				tradeCenterRealms.Add(list[num3].Item1);
				num2++;
			}
		}
		game.SendState<Game.TradeCentersState>();
		RefreshTradeCenters();
	}

	public void InitTCTimingParametersForShatterdMap()
	{
		nextTCSpawnDespawn = game.time + def.fist_time_spawn_trade_center_for_shattered_map * 60f;
	}

	private bool MatchTempVars(Kingdom src_kingdom, Kingdom tgt_kingdom)
	{
		if (tmp_trade_level_vars_tgt_kingdom != tgt_kingdom)
		{
			return false;
		}
		if (tmp_trade_level_vars_src_kingdom != src_kingdom)
		{
			return false;
		}
		if (tmp_trade_level_vars_frame != game.frame)
		{
			return false;
		}
		return true;
	}

	public void OnRelationChanged(Kingdom k1, Kingdom k2, KingdomAndKingdomRelation rel)
	{
		if (MatchTempVars(k1, k2) || MatchTempVars(k2, k1))
		{
			tmp_trade_level_vars.Set("relationship", rel.GetRelationship());
		}
	}

	public void OnKingdomDistancesChanged(Kingdom k)
	{
		if (k == tmp_trade_level_vars_src_kingdom || k == tmp_trade_level_vars_tgt_kingdom)
		{
			tmp_trade_level_vars_src_kingdom = null;
			tmp_trade_level_vars_tgt_kingdom = null;
			tmp_trade_level_vars_frame = -1L;
		}
	}

	private IVars GetTradeLevelVars(Kingdom src_kingdom, Kingdom tgt_kingdom, Character trader = null, int trade_level = -1)
	{
		Vars vars = tmp_trade_level_vars;
		vars.Set("obj", trader);
		vars.Set("trade_level", trade_level);
		if (src_kingdom == null)
		{
			src_kingdom = trader?.GetKingdom();
		}
		if (!MatchTempVars(src_kingdom, tgt_kingdom))
		{
			vars.Set("src_kingdom", src_kingdom);
			vars.Set("tgt_kingdom", tgt_kingdom);
			vars.Set("relationship", (src_kingdom == null) ? new Value(int.MaxValue) : new Value(src_kingdom.GetRelationship(tgt_kingdom)));
			vars.Set("distance", (src_kingdom == null) ? new Value(int.MaxValue) : new Value(src_kingdom.DistanceToKingdom(tgt_kingdom)));
			tmp_trade_level_vars_src_kingdom = src_kingdom;
			tmp_trade_level_vars_tgt_kingdom = tgt_kingdom;
			tmp_trade_level_vars_frame = game.frame;
		}
		return vars;
	}

	public DT.Field GetTradeLevelField(int trade_level)
	{
		if (trade_level < 0 || trade_level >= def.trade_levels.Count)
		{
			return null;
		}
		return def.trade_levels[trade_level];
	}

	private bool ResolveTradeParams(Character trader, ref Kingdom tgt_kingdom, ref int trade_level)
	{
		if (tgt_kingdom == null)
		{
			tgt_kingdom = trader?.mission_kingdom;
			if (tgt_kingdom == null)
			{
				return false;
			}
		}
		if (trade_level < 0 && trader != null)
		{
			trade_level = trader.trade_level;
		}
		if (trade_level < 0 || trade_level > def.trade_levels.Count)
		{
			return false;
		}
		return true;
	}

	public Value GetTradeLevelValue(string key, Kingdom src_kingdom, Kingdom tgt_kingdom, Character trader = null, int trade_level = -1)
	{
		if (!ResolveTradeParams(trader, ref tgt_kingdom, ref trade_level))
		{
			return Value.Unknown;
		}
		DT.Field tradeLevelField = GetTradeLevelField(trade_level);
		if (tradeLevelField == null)
		{
			return Value.Unknown;
		}
		IVars tradeLevelVars = GetTradeLevelVars(src_kingdom, tgt_kingdom, trader, trade_level);
		Value var = tradeLevelVars.GetVar(key);
		if (!var.is_unknown)
		{
			return var;
		}
		return tradeLevelField.GetValue(key, tradeLevelVars);
	}

	public float GetTradeLevelFloat(string key, Kingdom src_kingdom, Kingdom tgt_kingdom, Character trader = null, int trade_level = -1)
	{
		return GetTradeLevelValue(key, src_kingdom, tgt_kingdom, trader, trade_level).Float();
	}

	public int GetMinMerchantTradeLevel()
	{
		return def.min_trade_level;
	}

	public int GetMinMerchantTradeLevelOnMission()
	{
		return def.min_trade_level_on_mission;
	}

	public int GetMaxMerchantTradeLevel()
	{
		return def.max_trade_level;
	}

	public float CalcTradeProfit(Kingdom src_kingdom, Kingdom tgt_kingdom, Character trader = null, int trade_level = -1)
	{
		return GetTradeLevelFloat("profit", src_kingdom, tgt_kingdom, trader, trade_level);
	}

	public float CalcForeignTradeProfit(Kingdom src_kingdom, Kingdom tgt_kingdom, Character trader, int trade_level = -1)
	{
		if (trader == null)
		{
			return 0f;
		}
		return GetTradeLevelFloat("foreign_profit", src_kingdom, tgt_kingdom, trader, trade_level);
	}

	public float CalcCommerceForTrader(Character trader, Kingdom tgt_kingdom = null, int trade_level = -1)
	{
		return GetTradeLevelFloat("commerce", trader.GetKingdom(), tgt_kingdom, trader, trade_level);
	}

	public float CalcImportGoodCommerce(Character trader, string good_name)
	{
		if (string.IsNullOrEmpty(good_name))
		{
			return 0f;
		}
		Resource.Def def = game.defs.Find<Resource.Def>(good_name);
		if (def == null)
		{
			return 0f;
		}
		DT.Field field = def.field?.FindChild("import_upkeep.commerce");
		if (field == null)
		{
			return 0f;
		}
		Kingdom tgt_kingdom = null;
		int trade_level = -1;
		if (!ResolveTradeParams(trader, ref tgt_kingdom, ref trade_level))
		{
			return 0f;
		}
		IVars tradeLevelVars = GetTradeLevelVars(trader.GetKingdom(), tgt_kingdom, trader, trade_level);
		return field.Float(tradeLevelVars);
	}

	public float CalcImportGoodGoldUpkeep(Character trader, string good_name)
	{
		if (string.IsNullOrEmpty(good_name))
		{
			return 0f;
		}
		Resource.Def def = game.defs.Find<Resource.Def>(good_name);
		if (def == null)
		{
			return 0f;
		}
		DT.Field field = def.field?.FindChild("import_upkeep.gold");
		if (field == null)
		{
			return 0f;
		}
		Kingdom tgt_kingdom = null;
		int trade_level = -1;
		if (!ResolveTradeParams(trader, ref tgt_kingdom, ref trade_level))
		{
			return 0f;
		}
		IVars tradeLevelVars = GetTradeLevelVars(trader.GetKingdom(), tgt_kingdom, trader, trade_level);
		return field.Float(tradeLevelVars);
	}

	public Resource.Def GetGoodDef(string good_name)
	{
		if (string.IsNullOrEmpty(good_name))
		{
			return null;
		}
		Resource.Def def = game.defs.Find<Resource.Def>(good_name);
		if (def != null && def.field != null)
		{
			return def;
		}
		return null;
	}

	public override void OnUnloadMap()
	{
		while (tradeCenterRealms.Count > 0)
		{
			if (tradeCenterRealms[0] == null)
			{
				tradeCenterRealms.Remove(tradeCenterRealms[0]);
			}
			DelTradeCenterRealm(tradeCenterRealms[0]);
		}
	}

	protected override void OnDestroy()
	{
		if (game.economy == this)
		{
			game.economy = null;
		}
		base.OnDestroy();
	}

	public Economy(Game game)
		: base(game)
	{
	}

	public static float CalculateGoldFromPassiveTrade(Kingdom k, IncomeModifier mod)
	{
		k.CalculateGoldFromPassiveTrade();
		return k.goldFromPassiveTrade;
	}

	public static float CalculateGoldFromImportantRelatives(Kingdom k, IncomeModifier mod)
	{
		k.CalculateGoldFromImportantRelatives();
		return k.goldFromImportantRelatives;
	}

	public static float CalculateGoldFromMerchants(Kingdom k, IncomeModifier mod)
	{
		k.CalculateGoldFromMerchants();
		return k.goldFromMerchants + k.goldFromRoyalMerchants;
	}

	public static float CalculateGoldFromGoods(Kingdom k, IncomeModifier mod)
	{
		k.CalculateGoldFromGoods();
		return k.goldFromGoods;
	}

	public static float CalculateGoldFromForeignMerchants(Kingdom k, IncomeModifier mod)
	{
		k.CalculateGoldFromForeignMerchants();
		return k.goldFromForeignMerchants;
	}

	public static float CalculateUntaxedGoldFromTradeCenters(Kingdom k, IncomeModifier mod)
	{
		k.CalculateUntaxedGoldFromTradeCenters();
		return k.untaxGoldFromTradeCenters;
	}

	public static float CalcVassalGold(Kingdom k, IncomeModifier mod)
	{
		k.CalcVassalGold();
		return k.goldFromVassals;
	}

	public static float CalcVassalBooks(Kingdom k, IncomeModifier mod)
	{
		k.CalcVassalBooks();
		return k.booksFromVassals;
	}

	public static float CalcVassalPiety(Kingdom k, IncomeModifier mod)
	{
		k.CalcVassalPiety();
		return k.pietyFromVassals;
	}

	public static float CalcImportAndExportGold(Kingdom k, IncomeModifier mod)
	{
		k.CalcImportAndExportGold();
		return k.goldFromFoodExport;
	}

	public static float CalculateGoldFromExcessResources(Kingdom k, IncomeModifier mod)
	{
		k.CalculateGoldFromExcessResources();
		return k.goldFromExcessResources;
	}

	public static float CalcGoldFromJizya(Kingdom k, IncomeModifier mod)
	{
		k.taxGoldFromJizya = 0f;
		if (k.religion == null || !k.religion.def.muslim)
		{
			k.percGoldFromJizya = 0f;
			return 0f;
		}
		k.percGoldFromJizya = k.GetStat(Stats.ks_jizya_tax_perc);
		if (k.percGoldFromJizya == 0f)
		{
			return 0f;
		}
		float num = k.percGoldFromJizya * 0.01f;
		for (int i = 0; i < k.realms.Count; i++)
		{
			Realm realm = k.realms[i];
			if (realm.religion == null || !realm.religion.def.muslim)
			{
				float untaxed_value = realm.incomes[ResourceType.Gold].value.untaxed_value;
				k.taxGoldFromJizya += untaxed_value * num;
			}
		}
		return k.taxGoldFromJizya;
	}

	public static float CalcCrownAuthorityGoldPerc(Kingdom k, IncomeModifier mod)
	{
		return k.GetCrownAuthority()?.GetIncomeModifier() ?? 0f;
	}

	public static float CalcCorruptionGoldPerc(Kingdom k, IncomeModifier mod)
	{
		CrownAuthority crownAuthority = k.GetCrownAuthority();
		if (crownAuthority == null)
		{
			return 0f;
		}
		float corruptionModifier = crownAuthority.GetCorruptionModifier();
		float stat = k.GetStat(Stats.ks_corruption);
		float num = corruptionModifier + stat;
		if (num > k.corruption_max)
		{
			num = k.corruption_max;
		}
		if (num < k.corruption_min)
		{
			num = k.corruption_min;
		}
		k.percCorruption = num;
		return 0f - num;
	}

	public static float CalcWagesUpkeep(Kingdom k, IncomeModifier mod)
	{
		k.CalcWages();
		return k.wageGoldTotal;
	}

	public static float CalcBuildingsUpkeep(Kingdom k, IncomeModifier mod)
	{
		return k.upkeepBuildings;
	}

	public static float CalcArmiesGoldUpkeep(Kingdom k, IncomeModifier mod)
	{
		return k.armies_upkeep[ResourceType.Gold];
	}

	public static float CalcBribesUpkeep(Kingdom k, IncomeModifier mod)
	{
		k.upkeepBribes = k.CalcBribesUpkeep();
		return k.upkeepBribes;
	}

	public static float CalcSupportPretendersUpkeep(Kingdom k, IncomeModifier mod)
	{
		k.upkeepSupportPretender = k.CalcSupportPretendersUpkeep();
		return k.upkeepSupportPretender;
	}

	public static float CalcRuinRelationsUpkeep(Kingdom k, IncomeModifier mod)
	{
		return k.CalcRuinRelationsUpkeep();
	}

	public static float CalcSowDissentUpkeep(Kingdom k, IncomeModifier mod)
	{
		return k.CalcSowDissentUpkeep();
	}

	public static float CalcBolsterCultureUpkeep(Kingdom k, IncomeModifier mod)
	{
		return k.CalcBolsterCultureUpkeep();
	}

	public static float CalcBolsterInfluenceUpkeep(Kingdom k, IncomeModifier mod)
	{
		return k.CalcBolsterInfluenceUpkeep();
	}

	public static float CalcOccupationsUpkeep(Kingdom k, IncomeModifier mod)
	{
		k.upkeepOccupations = k.CalcOccupationsUpkeep();
		return k.upkeepOccupations;
	}

	public static float CalcDisorderUpkeep(Kingdom k, IncomeModifier mod)
	{
		k.upkeepDisorder = k.CalcDisorderUpkeep();
		return k.upkeepDisorder;
	}

	public static float CalcStatusResources(Kingdom k, IncomeModifier mod)
	{
		Status.Def def = Logic.Def.Get<Status.Def>(mod.def.field.GetRef("status"));
		if (def == null)
		{
			return 0f;
		}
		string path = mod.def.field.GetString("key", k, "upkeep");
		DT.Field field = def.field.FindChild(path);
		if (field == null)
		{
			return 0f;
		}
		ResourceType rt = mod.location.parent.def.rt;
		float num = 0f;
		for (int i = 0; i < k.court.Count; i++)
		{
			Character character = k.court[i];
			if (character == null)
			{
				continue;
			}
			Status status = character.FindStatus(def);
			if (status != null)
			{
				Resource resource = Resource.Parse(field, status);
				if (!(resource == null))
				{
					float num2 = resource[rt];
					num += num2;
				}
			}
		}
		return num;
	}

	public static float CalcActionResources(Kingdom k, IncomeModifier mod)
	{
		Action.Def def = Logic.Def.Get<Action.Def>(mod.def.field.GetRef("action"));
		if (def == null)
		{
			return 0f;
		}
		string path = mod.def.field.GetString("key", k, "upkeep");
		DT.Field field = def.field.FindChild(path);
		if (field == null)
		{
			return 0f;
		}
		ResourceType rt = mod.location.parent.def.rt;
		float num = 0f;
		for (int i = 0; i < k.court.Count; i++)
		{
			Character character = k.court[i];
			if (character == null)
			{
				continue;
			}
			Action action = character.actions?.FindActive(def);
			if (action != null)
			{
				Resource resource = Resource.Parse(field, action);
				if (!(resource == null))
				{
					float num2 = resource[rt];
					num += num2;
				}
			}
		}
		return num;
	}

	public static float CalcHelpTheWeakUpkeep(Kingdom k, IncomeModifier mod)
	{
		k.upkeepHelpTheWeak = k.CalcHelpTheWeakUpkeep();
		return k.upkeepHelpTheWeak;
	}

	public static float CalcUpkeepGoldFromGoodsImport(Kingdom k, IncomeModifier mod)
	{
		return k.upkeepGoldFromGoodsImport;
	}

	public static float CalcUpkeepGoldFromFoodImport(Kingdom k, IncomeModifier mod)
	{
		return k.upkeepGoldFromFoodImport;
	}

	public static float CalcSovereignTax(Kingdom k, IncomeModifier mod)
	{
		k.CalcSovereignTaxGold();
		return k.taxForSovereignGold;
	}

	public static float CalcSovereignTaxBooks(Kingdom k, IncomeModifier mod)
	{
		k.CalcSovereignTaxBooks();
		return k.taxForSovereignBooks;
	}

	public static float CalcSovereignTaxPiety(Kingdom k, IncomeModifier mod)
	{
		k.CalcSovereignTaxPiety();
		return k.taxForSovereignPiety;
	}

	public static float CalcJihadUpkeep(Kingdom k, IncomeModifier mod)
	{
		k.upkeepJihad = k.CalcJihadUpkeep();
		return k.upkeepJihad;
	}

	public static float CalcPaganBeliefsUpkeep(Kingdom k, IncomeModifier mod)
	{
		if (k?.religion?.def == null)
		{
			return 0f;
		}
		k.upkeepPaganBeliefs = k.religion.def.CalcPaganBliefsUpkeep(k);
		return k.upkeepPaganBeliefs;
	}

	public static float CalcInflationPenalty(Kingdom k, IncomeModifier mod)
	{
		k.inflation = k.CalcInflationPenalty();
		return k.inflation;
	}

	public static float CalcPopulationGold(Realm r, IncomeModifier mod)
	{
		Population population = r.castle?.population;
		if (population == null)
		{
			return 0f;
		}
		population.Recalc(clamp: true);
		float num = 0f;
		for (Population.Type type = Population.Type.Rebel; type < Population.Type.TOTAL; type++)
		{
			int num2 = population.Count(type, check_up_to_date: false);
			if (num2 != 0)
			{
				num += (float)num2 * r.castle.production_coef[(int)type, 1] * (100f + r.GetKingdom().GetStat(Stats.ks_gold_per_population_bonus_perc)) * 0.01f;
			}
		}
		return num;
	}

	public static float CalcTownGoldFromTradeCenter(Settlement s, IncomeModifier mod)
	{
		Realm realm = s.GetRealm();
		if (realm == null || !realm.IsTradeCenter())
		{
			return 0f;
		}
		float trade_center_gold_castle = s.def.trade_center_gold_castle;
		trade_center_gold_castle *= 1f + realm.GetKingdom().GetStat(Stats.ks_gold_from_own_TCs_perc) / 100f;
		s.production_from_Trade_center.Add(ResourceType.Gold, trade_center_gold_castle);
		return trade_center_gold_castle;
	}

	public static float CalcTradeCenterGoldFromCommerce(Settlement s, IncomeModifier mod)
	{
		Realm realm = s.GetRealm();
		if (realm?.castle == null || !realm.IsTradeCenter())
		{
			return 0f;
		}
		return realm.castle.def.trade_center_gold_per_commerce * realm.incomes[ResourceType.Trade].value.untaxed_value * (1f + realm.GetKingdom().GetStat(Stats.ks_gold_from_own_TCs_perc) / 100f);
	}

	public static float CalcTradeCenterGoldFromGoods(Settlement s, IncomeModifier mod)
	{
		Realm realm = s.GetRealm();
		if (realm?.castle == null || !realm.IsTradeCenter())
		{
			return 0f;
		}
		return (float)realm.goods_produced.Count * realm.castle.def.trade_center_gold_per_good * (1f + realm.GetKingdom().GetStat(Stats.ks_gold_from_own_TCs_perc) / 100f);
	}

	public static float CalcSettlementGoldFromTradeCenter(Settlement s, IncomeModifier mod)
	{
		Realm realm = s.GetRealm();
		if (realm == null || !realm.IsTradeCenter())
		{
			return 0f;
		}
		float num = 0f;
		num = ((!s.coastal) ? s.def.trade_center_gold_settlement : s.def.trade_center_gold_coastal);
		num *= 1f + realm.GetKingdom().GetStat(Stats.ks_gold_from_own_TCs_perc) / 100f;
		s.production_from_Trade_center.Add(ResourceType.Gold, num);
		return num;
	}

	public static float CalcBooksPerCleric(Kingdom k, IncomeModifier mod)
	{
		return k.CalcBooksFromClerics();
	}

	public static float CalcBooksFromFaith(Kingdom k, IncomeModifier mod)
	{
		float taxed_value = k.incomes.per_resource[5].value.taxed_value;
		float num = k.GetStat(Stats.ks_piety_converted_to_books_perc) / 100f;
		return taxed_value * num;
	}

	public static float CalcBooksFromImportantRelatives(Kingdom k, IncomeModifier mod)
	{
		k.CalcBooksFromImportantRelatives();
		return k.booksFromImportantRelatives;
	}

	public static float CalcTownBooksFromTradeCenter(Settlement s, IncomeModifier mod)
	{
		Realm realm = s.GetRealm();
		if (realm == null || !realm.IsTradeCenter())
		{
			return 0f;
		}
		float trade_center_books_castle = s.def.trade_center_books_castle;
		s.production_from_Trade_center.Add(ResourceType.Books, trade_center_books_castle);
		return trade_center_books_castle;
	}

	public static float CalcFaithFromClerics(Kingdom k, IncomeModifier mod)
	{
		k.faithFromClerics = k.CalcFaithFromClerics();
		return k.faithFromClerics;
	}

	public static float CalcFoodFromImportAndExport(Kingdom k, IncomeModifier mod)
	{
		k.CalcFoodFromImportAndExport();
		return k.foodFromImport;
	}

	public static float CalcArmiesFoodUpkeep(Kingdom k, IncomeModifier mod)
	{
		return k.armies_upkeep[ResourceType.Food];
	}

	public static float CalcFoodExportUpkeep(Kingdom k, IncomeModifier mod)
	{
		return k.upkeepFoodFromExport;
	}

	public static float CalcGarrisonFoodUpkeep(Realm r, IncomeModifier mod)
	{
		r.CalcGarrisonUpkep();
		return r.upkeepGarrison[ResourceType.Food];
	}

	public static float CalcAllocattedCommerce(Kingdom k, IncomeModifier mod)
	{
		return k.CalcAllocattedCommerce();
	}
}
