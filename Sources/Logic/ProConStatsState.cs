using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Logic.ExtensionMethods;
using UnityEngine;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Game, dynamic = false)]
public class Game : Object, RemoteVars.IListener
{
	public enum State
	{
		InLobby,
		LoadingMap,
		Running,
		Quitting
	}

	public class NotificationObject
	{
		public Object obj;

		public string message;

		public object param;

		public bool process_triggers;

		public bool profile;

		public NotificationObject(Object obj, string message, object param, bool process_triggers, bool profile)
		{
			this.obj = obj;
			this.message = message;
			this.param = param;
			this.process_triggers = process_triggers;
			this.profile = true;
		}
	}

	public enum LogType
	{
		Message,
		Warning,
		Error
	}

	public struct ProfileScope : IDisposable
	{
		public class Stats
		{
			public long num_calls;

			public long total_ticks;

			public long min_ticks;

			public long max_ticks;

			public float total_ms => ProfTicksToMillis(total_ticks);

			public float min_ms => ProfTicksToMillis(min_ticks);

			public float max_ms => ProfTicksToMillis(max_ticks);

			public float avg_ms
			{
				get
				{
					if (num_calls >= 1)
					{
						return total_ms / (float)num_calls;
					}
					return 0f;
				}
			}

			public void Clear()
			{
				num_calls = 0L;
				total_ticks = 0L;
				min_ticks = (max_ticks = 0L);
			}

			public void AddCall(long ticks)
			{
				if (num_calls == 0L)
				{
					min_ticks = (max_ticks = ticks);
				}
				else if (ticks < min_ticks)
				{
					min_ticks = ticks;
				}
				else if (ticks > max_ticks)
				{
					max_ticks = ticks;
				}
				num_calls++;
				total_ticks += ticks;
			}

			public override string ToString()
			{
				long num = num_calls;
				string text = "Calls: " + num.ToString().PadLeft(6);
				if (num > 0)
				{
					text = text + ", Total: " + total_ms.ToString("F3").PadLeft(9) + "ms";
				}
				if (num > 1)
				{
					text += string.Format(", Avg: {0} ms ({1:F3} - {2:F3})", avg_ms.ToString("F3").PadLeft(9), min_ms, max_ms);
				}
				return text;
			}
		}

		public string name;

		public long start_ticks;

		public bool log;

		public float log_threshold;

		public Stats stats;

		public long Ticks => prof_timer.ElapsedTicks - start_ticks;

		public float Millis => (float)(Ticks * 1000) / (float)Stopwatch.Frequency;

		public ProfileScope(string name, bool log, float log_threshold, Stats stats)
		{
			this.name = name;
			this.log = log;
			this.log_threshold = log_threshold;
			this.stats = stats;
			if (log_threshold >= 0f)
			{
				BeginProfileSection(name);
			}
			if (prof_timer == null)
			{
				prof_timer = Stopwatch.StartNew();
			}
			start_ticks = prof_timer.ElapsedTicks;
		}

		public void Dispose()
		{
			long elapsedTicks = prof_timer.ElapsedTicks;
			if (log_threshold >= 0f)
			{
				EndProfileSection(name);
			}
			if (stats != null)
			{
				stats.AddCall(elapsedTicks - start_ticks);
			}
			if (log)
			{
				float num = ProfTicksToMillis(elapsedTicks - start_ticks);
				if (!(num < log_threshold))
				{
					Log($"{name} took {num} ms", (!(log_threshold <= 0f)) ? LogType.Warning : LogType.Message);
				}
			}
		}

		public void LogElapsed()
		{
			Log($"{name} took {Millis} ms", LogType.Message);
		}
	}

	public struct BenchmarkResult
	{
		public string name;

		public long reps;

		public long total_ticks;

		public long overhead_ticks;

		public long allocated;

		public static string Ticks2Str(long ticks, long reps = 1L)
		{
			long num = Stopwatch.Frequency * reps;
			if (ticks > num)
			{
				return ((float)ticks / (float)num).ToString("F3") + " s";
			}
			if (ticks > num / 1000)
			{
				return ((float)(ticks * 1000) / (float)num).ToString("F3") + " ms";
			}
			if (ticks > num / 1000000)
			{
				return ((float)(ticks * 1000000) / (float)num).ToString("F3") + " us";
			}
			return ((float)(ticks * 1000000000) / (float)num).ToString("F3") + " ns";
		}

		public static string Mem2Str(long bytes, long reps = 1L)
		{
			if (bytes == 0L)
			{
				return "---";
			}
			float num = (float)bytes / (float)reps;
			if (num < 1024f)
			{
				return num.ToString("F3") + " B";
			}
			if (num < 1048576f)
			{
				return (num / 1024f).ToString("F3") + " K";
			}
			return (num / 1048576f).ToString("F3") + " M";
		}

		public override string ToString()
		{
			return $"[x{reps}] {name}: {Ticks2Str(total_ticks - overhead_ticks, reps)} / {Mem2Str(allocated, reps)}";
		}
	}

	public enum SavesRoot
	{
		Root,
		Single,
		Multi
	}

	public enum GameStateDumpFileType
	{
		Local,
		Client,
		Server
	}

	public class CampaignRules : Component, IVars
	{
		public class ScoreModifers
		{
			public float scorePerGold;

			public float scorePerRealm;

			public float scorePerFame;

			public float scorePerUniqueResource;

			public float scorePerArmyStrength;

			public void Parse(List<DT.Field> scoring_fields)
			{
				if (scoring_fields == null)
				{
					return;
				}
				scorePerGold = 0f;
				scorePerRealm = 0f;
				scorePerFame = 0f;
				scorePerUniqueResource = 0f;
				scorePerArmyStrength = 0f;
				for (int i = 0; i < scoring_fields.Count; i++)
				{
					switch (scoring_fields[i].key)
					{
					case "gold":
						scorePerGold = 1f;
						break;
					case "realms":
						scorePerRealm = 1f;
						break;
					case "fame":
						scorePerFame = 1f;
						break;
					case "unique_resources":
						scorePerUniqueResource = 1f;
						break;
					case "army_strength":
						scorePerArmyStrength = 1f;
						break;
					}
				}
			}

			public void Parse(string scoring_key)
			{
				if (scoring_key != null)
				{
					scorePerGold = 0f;
					scorePerRealm = 0f;
					scorePerFame = 0f;
					scorePerUniqueResource = 0f;
					scorePerArmyStrength = 0f;
					switch (scoring_key)
					{
					case "gold":
						scorePerGold = 1f;
						break;
					case "realms":
						scorePerRealm = 1f;
						break;
					case "fame":
						scorePerFame = 1f;
						break;
					case "unique_resources":
						scorePerUniqueResource = 1f;
						break;
					case "army_strength":
						scorePerArmyStrength = 1f;
						break;
					}
				}
			}
		}

		public class KingdomResizeUtils
		{
			private enum GrowthScore
			{
				SameCulture = 1,
				SameSubculture = 2,
				SameReligion = 4,
				SameSubreligion = 8,
				SecondNeighbor = 16,
				LogicNeighbor = 32,
				SameNames = 64,
				ImmediateNeighbor = 128,
				Historical = 256,
				Invalid = -512
			}

			private Game game;

			public KingdomResizeUtils(Game game)
			{
				this.game = game;
			}

			public void ReturnAllRealmsToInitKingdoms()
			{
				using (new Kingdom.CacheRBS("ReturnAllRealmsToInitKingdoms"))
				{
					foreach (Realm realm in game.realms)
					{
						if (realm.kingdom_id != realm.init_kingdom_id)
						{
							realm.SetKingdom(realm.init_kingdom_id, ignore_victory: true, check_cancel_battle: false, via_diplomacy: true, send_state: false, from_gameplay: false);
						}
					}
				}
			}

			public void GrowKingdoms(List<Kingdom> kingdoms, int to_size)
			{
				List<IEnumerator> list = new List<IEnumerator>(kingdoms.Count);
				foreach (Kingdom kingdom in kingdoms)
				{
					list.Add(GrowKingdomEnumerator(kingdom, to_size));
				}
				bool flag = false;
				while (!flag)
				{
					flag = true;
					foreach (IEnumerator item in list)
					{
						flag &= !item.MoveNext();
					}
				}
			}

			public void GrowKingdom(Kingdom k, int to_size, Predicate<Realm> WillNotUseRealm = null)
			{
				using (new Kingdom.CacheRBS("GrowKingdom"))
				{
					IEnumerator enumerator = GrowKingdomEnumerator(k, to_size, WillNotUseRealm);
					while (enumerator.MoveNext())
					{
					}
				}
			}

			public void ShrinkKingdom(Kingdom kingdom, int toSize, Predicate<Realm> keepRealmFunc)
			{
				using (new Kingdom.CacheRBS("ShrinkKingdom"))
				{
					while (kingdom.realms.Count > toSize)
					{
						Realm realm = FindMostDisconnectedRealm(kingdom, keepRealmFunc);
						if (realm == null)
						{
							break;
						}
						bool flag = false;
						foreach (Realm item in realm.logicNeighborsRestricted)
						{
							if (item.kingdom_id != kingdom.id && !IsPlayerDesignatedKingdom(item.GetKingdom()) && item.GetKingdom() != game.rules.targetKingdom)
							{
								realm.SetKingdom(item.kingdom_id, ignore_victory: true, check_cancel_battle: false, via_diplomacy: true, send_state: false, from_gameplay: false);
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							foreach (Kingdom neighbor in kingdom.neighbors)
							{
								if (!IsPlayerDesignatedKingdom(neighbor.GetKingdom()))
								{
									realm.SetKingdom(neighbor.id, ignore_victory: true, check_cancel_battle: false, via_diplomacy: true, send_state: false, from_gameplay: false);
									flag = true;
									break;
								}
							}
						}
						if (!flag)
						{
							Game.Log("Failed to shrink " + kingdom.Name + "!", LogType.Warning);
							break;
						}
					}
				}
			}

			public void UpdateReligionAndLoyaltyOfModifiedRealms(bool force = false)
			{
				for (int i = 0; i < game.realms.Count; i++)
				{
					Realm realm = game.realms[i];
					if (!force && realm.kingdom_id != realm.init_kingdom_id)
					{
						realm.SetReligion(realm.GetKingdom().religion);
						if (realm.kingdom_id != realm.pop_majority.kingdom.id)
						{
							realm.AdjustPopMajority(-100f, realm.GetKingdom());
						}
					}
					if (force && realm.GetKingdom() != null)
					{
						realm.SetReligion(realm.GetKingdom().religion);
						realm.AdjustPopMajority(-100f, realm.GetKingdom());
					}
				}
			}

			private IEnumerator GrowKingdomEnumerator(Kingdom k, int to_size, Predicate<Realm> WillNotUseRealm = null)
			{
				if (k.realms.Count >= to_size)
				{
					yield break;
				}
				List<int> permanent_scores = new List<int>(game.realms.Count);
				foreach (Realm realm in game.realms)
				{
					if (WillNotUseRealm != null && WillNotUseRealm(realm))
					{
						permanent_scores.Add(-5120);
					}
					else
					{
						permanent_scores.Add(0);
					}
				}
				ScoreRealms(game.realms, permanent_scores, (Realm r) => DefaultForKingdom(r, k), GrowthScore.Historical);
				ScoreRealms(game.realms, permanent_scores, (Realm r) => r.name == k.Name || r.town_name == k.Name, GrowthScore.SameNames);
				ScoreRealms(game.realms, permanent_scores, (Realm r) => r.religion == k.religion, GrowthScore.SameSubreligion);
				ScoreRealms(game.realms, permanent_scores, (Realm r) => SameReligiousGroup(r, k), GrowthScore.SameReligion);
				ScoreRealms(game.realms, permanent_scores, (Realm r) => r.culture == k.culture, GrowthScore.SameSubculture);
				ScoreRealms(game.realms, permanent_scores, (Realm r) => SameCulturalGroup(game.cultures, r, k), GrowthScore.SameCulture);
				for (int i = k.realms.Count; i < to_size; i++)
				{
					List<int> scores = new List<int>(permanent_scores);
					if (WillNotUseRealm == null)
					{
						ScoreRealms(game.realms, scores, (Realm r) => ImpossibleToTransfer(r, k) || r == game.religions.catholic.hq_realm || IsPlayerDesignatedKingdom(r.GetKingdom()) || (r == game.religions.orthodox.hq_realm && r.init_kingdom_id != k.id) || IsFromTargetKingdom(r), GrowthScore.Invalid);
					}
					else
					{
						ScoreRealms(game.realms, scores, (Realm r) => ImpossibleToTransfer(r, k), GrowthScore.Invalid);
					}
					ScoreImmediateNeighbors(k, scores);
					ScoreLogicNeighbors(k, scores);
					ScoreSecondNeighbors(k, scores);
					(int, int) tuple = FindHighestScore(scores);
					if (tuple.Item2 < 0)
					{
						break;
					}
					game.realms[tuple.Item1].SetKingdom(k.id, ignore_victory: true, check_cancel_battle: false, via_diplomacy: true, send_state: false, from_gameplay: false);
					yield return null;
				}
			}

			private bool IsFromTargetKingdom(Realm r)
			{
				int num = game.rules.targetKingdom?.id ?? 0;
				if (num == 0)
				{
					return false;
				}
				return (r?.GetKingdom()?.id ?? 0) == num;
			}

			private bool IsPlayerDesignatedKingdom(Kingdom kingdom)
			{
				if (kingdom == null)
				{
					return false;
				}
				for (int i = 0; i < game.campaign.playerDataPersistent.Length; i++)
				{
					if (game.campaign.GetKingdomName(i) == kingdom.Name)
					{
						return true;
					}
				}
				return false;
			}

			public Realm FindMostDisconnectedRealm(Kingdom kingdom, Predicate<Realm> keepRealmFunc)
			{
				if (kingdom.realms.Count <= 1)
				{
					return null;
				}
				Realm result = null;
				int num = 0;
				foreach (Realm realm in kingdom.realms)
				{
					if (keepRealmFunc(realm))
					{
						continue;
					}
					int num2 = 0;
					foreach (Realm neighbor in realm.neighbors)
					{
						if (neighbor.IsSeaRealm() || (neighbor.kingdom_id != kingdom.id && !IsPlayerDesignatedKingdom(neighbor.GetKingdom())))
						{
							num2++;
						}
					}
					if (num2 == realm.neighbors.Count)
					{
						return realm;
					}
					if (num2 > num)
					{
						result = realm;
						num = num2;
					}
				}
				return result;
			}

			private static bool ImpossibleToTransfer(Realm r, Kingdom k)
			{
				if (!r.IsSeaRealm())
				{
					return r.kingdom_id == k.id;
				}
				return true;
			}

			private static bool DefaultForKingdom(Realm r, Kingdom k)
			{
				return k.id == r.init_kingdom_id;
			}

			private static bool NeighborOfKingdom(Realm r)
			{
				return r.GetKingdom()?.externalBorderRealms.Contains(r) ?? false;
			}

			private static bool SameReligiousGroup(Realm r, Kingdom k)
			{
				if ((!r.is_christian || !k.is_christian) && (!r.is_muslim || !k.is_muslim))
				{
					if (r.is_pagan)
					{
						return k.is_pagan;
					}
					return false;
				}
				return true;
			}

			private bool SameCulturalGroup(Cultures c, Realm r, Kingdom k)
			{
				return c.GetGroup(r.culture) == c.GetGroup(k.culture);
			}

			private static void ScoreRealms(List<Realm> realms, List<int> scores, Predicate<Realm> predicate, GrowthScore score)
			{
				for (int i = 0; i < realms.Count; i++)
				{
					scores[i] += (int)(predicate(realms[i]) ? score : ((GrowthScore)0));
				}
			}

			private static void ScoreImmediateNeighbors(Kingdom k, List<int> scores)
			{
				foreach (Realm realm in k.realms)
				{
					foreach (Realm neighbor in realm.neighbors)
					{
						if (neighbor.id > 0 && neighbor.kingdom_id != k.id)
						{
							scores[neighbor.id - 1] += 128;
						}
					}
				}
			}

			private static void ScoreLogicNeighbors(Kingdom k, List<int> scores)
			{
				foreach (Realm externalBorderRealm in k.externalBorderRealms)
				{
					if (externalBorderRealm.id > 0)
					{
						scores[externalBorderRealm.id - 1] += 32;
					}
				}
			}

			private static void ScoreSecondNeighbors(Kingdom k, List<int> scores)
			{
				foreach (Realm externalBorderRealm in k.externalBorderRealms)
				{
					foreach (Realm neighbor in externalBorderRealm.neighbors)
					{
						if (!ImpossibleToTransfer(neighbor, k))
						{
							scores[neighbor.id - 1] += 16;
						}
					}
				}
			}

			private (int index, int score) FindHighestScore(List<int> scores)
			{
				(int, int) result = (-1, int.MinValue);
				for (int i = 0; i < scores.Count; i++)
				{
					if (result.Item2 < scores[i])
					{
						result = (i, scores[i]);
					}
				}
				return result;
			}
		}

		private class IdAndScore
		{
			public int id;

			public float score;

			public IdAndScore(int kid, float score)
			{
				id = kid;
				this.score = score;
			}
		}

		private class PlayerWarLimits
		{
			public enum Type
			{
				Time,
				Generations,
				Always,
				Never
			}

			public Type type;

			public int time;

			public PlayerWarLimits(string s)
			{
				if (s == null)
				{
					type = Type.Always;
				}
				else if (s == "always")
				{
					type = Type.Always;
				}
				else if (s == "never")
				{
					type = Type.Never;
				}
				else if (s.Contains("g"))
				{
					type = Type.Generations;
					string s2 = s.Replace("g", "");
					time = int.Parse(s2);
				}
				else if (s.Contains("m"))
				{
					type = Type.Time;
					string s3 = s.Replace("m", "");
					time = int.Parse(s3);
				}
				else
				{
					type = Type.Always;
				}
			}
		}

		public class TimeLimits
		{
			public enum Type
			{
				None,
				Time,
				Generations
			}

			public Type type;

			public int value;

			public TimeLimits(string s)
			{
				if (s == null || s == "none")
				{
					type = Type.None;
				}
				else if (s.Contains("g"))
				{
					type = Type.Generations;
					string s2 = s.Replace("g", "");
					value = int.Parse(s2);
				}
				else if (s.Contains("m"))
				{
					type = Type.Time;
					string s3 = s.Replace("m", "");
					value = int.Parse(s3);
				}
				else
				{
					type = Type.None;
				}
			}
		}

		private ScoreModifers _mainGoalScoreModifiers;

		private ScoreModifers prestigeVictoryModifiers;

		private ScoreModifers kingdomAdvantagesModifiers;

		public KingdomResizeUtils kingdom_resize;

		private bool loaded;

		public string main_goal;

		public Value gold_goal;

		public Value realms_goal;

		public Value goods_goal;

		public int target_kingdom_size;

		public int starting_gold = -1;

		public int starting_gold_multiplier = 1;

		public int max_unlockable_tiers = 4;

		public int ai_difficulty = 1;

		public int min_ai_difficulty;

		public int max_ai_difficulty = 3;

		public Value team_size;

		public int on_player_destroyed_pick_count;

		public Value time_limit;

		public TimeLimits time_limits;

		public CharacterAge.Def aging_def;

		public bool knight_aging;

		private PlayerWarLimits player_war_limits;

		public bool playerwars_enable_loosing_players;

		public bool starting_trade_center_for_players = true;

		public float min_speed = 1f;

		public float max_speed = 1f;

		public int pause_refresh;

		public int unpause_cooldown;

		public int espionage_vs_players_max_severity = 1;

		public int espionage_vs_AI_max_severity = 2;

		public Kingdom targetKingdom;

		public Team winningTeam;

		public string end_game_reason;

		public Kingdom singlePlayerWinner;

		public bool end_game_triggered;

		public bool early_end_triggered;

		public static int useDefaultKingdomSize;

		public ScoreModifers mainGoalScoreModifiers
		{
			get
			{
				if (_mainGoalScoreModifiers == null)
				{
					_mainGoalScoreModifiers = new ScoreModifers();
					mainGoalScoreModifiers.Parse(GetMainGoalScoring());
				}
				return _mainGoalScoreModifiers;
			}
		}

		public CampaignRules(Object obj)
			: base(obj)
		{
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (!(key == "game_type_target"))
			{
				if (key == "destroy_goal")
				{
					return targetKingdom;
				}
				if (base.game?.campaign != null)
				{
					Value var = base.game.campaign.GetVar(key);
					if (!var.is_unknown)
					{
						return var;
					}
					DT.Field field = base.game.campaign.FindMatchingOption(key);
					if (field != null)
					{
						return base.game.campaign.GetOptionValue(field);
					}
				}
				return Value.Unknown;
			}
			return GetGameTypeTarget();
		}

		public string GetGameTypeTarget()
		{
			return main_goal switch
			{
				"HaveXGold" => gold_goal, 
				"HaveXRealms" => realms_goal, 
				"WarForGoods" => goods_goal, 
				"DestroyKingdom" => targetKingdom?.Name, 
				_ => null, 
			};
		}

		public void Load()
		{
			if (loaded)
			{
				return;
			}
			if (base.game.campaign == null)
			{
				if (isComingFromTitle)
				{
					Game.Log("Missing campaign data coming from the title screen!", LogType.Warning);
					return;
				}
				base.game.campaign = Campaign.CreateSinglePlayerCampaign(base.game.map_name, base.game.map_period);
			}
			main_goal = GetVar("main_goal");
			gold_goal = GetVar("gold_goal");
			realms_goal = GetVar("realms_goal");
			goods_goal = GetVar("goods_goal");
			target_kingdom_size = GetVar("target_kingdom_size");
			starting_gold = GetVar("starting_gold").Int(-1);
			starting_gold_multiplier = GetVar("starting_gold_multiplier").Int(1);
			min_ai_difficulty = 0;
			max_ai_difficulty = 3;
			string text = GetVar("ai_difficulty").String();
			switch (text)
			{
			case "easy":
				ai_difficulty = min_ai_difficulty;
				break;
			case "normal":
				ai_difficulty = 1;
				break;
			case "hard":
				ai_difficulty = 2;
				break;
			case "very_hard":
				ai_difficulty = max_ai_difficulty;
				break;
			}
			base.game.NotifyListeners("ai_difficulty_changed", text);
			team_size = GetVar("team_size");
			on_player_destroyed_pick_count = GetVar("on_player_destroyed_pick_count");
			time_limit = GetVar("time_limit");
			time_limits = new TimeLimits(time_limit);
			max_unlockable_tiers = base.game.campaign.GetVar("max_unlockable_tiers").Int(4);
			aging_def = ExtractAgingDef();
			knight_aging = ExtractKinghtAging();
			player_war_limits = new PlayerWarLimits(GetVar("playerwars_limit_time").String());
			playerwars_enable_loosing_players = GetVar("playerwars_enable_loosing_players").Bool();
			min_speed = GetVar("min_speed").Float(1f);
			max_speed = GetVar("max_speed").Float(1f);
			pause_refresh = GetVar("pause_refresh").Int();
			unpause_cooldown = GetVar("unpause_cooldown").Int();
			switch (GetVar("espionage_role").String())
			{
			case "full":
				espionage_vs_players_max_severity = 2;
				break;
			case "limited":
				espionage_vs_players_max_severity = 1;
				break;
			case "minimal":
				espionage_vs_players_max_severity = 0;
				break;
			}
			_mainGoalScoreModifiers = new ScoreModifers();
			mainGoalScoreModifiers.Parse(GetMainGoalScoring());
			prestigeVictoryModifiers = new ScoreModifers();
			prestigeVictoryModifiers.Parse(GetRuleScoring("goal_prestige_victory"));
			kingdomAdvantagesModifiers = new ScoreModifers();
			kingdomAdvantagesModifiers.Parse(GetRuleScoring("goal_kingdom_advantages"));
			end_game_triggered = base.game.campaign.GetVar("end_game_triggered").Bool();
			early_end_triggered = base.game.campaign.GetVar("early_end_triggered").Bool();
			end_game_reason = base.game.campaign.GetVar("end_game_reason").String();
			if (kingdom_resize == null)
			{
				kingdom_resize = new KingdomResizeUtils(base.game);
			}
			winningTeam = null;
			loaded = true;
		}

		public void Clear()
		{
			targetKingdom = null;
			winningTeam = null;
			end_game_reason = null;
			singlePlayerWinner = null;
			end_game_triggered = false;
			early_end_triggered = false;
		}

		public void Unload()
		{
			Clear();
			loaded = false;
		}

		public void Reload()
		{
			Unload();
			Load();
		}

		private bool ExtractKinghtAging()
		{
			Value var = GetVar("knight_aging");
			if (var.is_unknown)
			{
				return false;
			}
			return var.Bool();
		}

		private CharacterAge.Def ExtractAgingDef()
		{
			Value var = base.game.campaign.GetVar("aging_speed");
			if (var.is_unknown)
			{
				return base.game.defs.GetBase<CharacterAge.Def>();
			}
			List<CharacterAge.Def> defs = base.game.defs.GetDefs<CharacterAge.Def>();
			if (defs == null || defs.Count == 0)
			{
				return base.game.defs.GetBase<CharacterAge.Def>();
			}
			string value = var.String().ToLowerInvariant();
			for (int i = 0; i < defs.Count; i++)
			{
				CharacterAge.Def def = defs[i];
				if (def.field.key.ToLowerInvariant().Contains(value))
				{
					return def;
				}
			}
			return base.game.defs.GetBase<CharacterAge.Def>();
		}

		private List<DT.Field> GetMainGoalScoring()
		{
			if (base.game.campaign == null)
			{
				return null;
			}
			return base.game.campaign.FindMatchingOption("main_goal")?.FindChild("scoring")?.children;
		}

		private List<DT.Field> GetRuleScoring(string rule_id)
		{
			if (base.game.campaign == null)
			{
				return null;
			}
			return base.game.campaign.GetVarDef(rule_id)?.FindChild("scoring")?.children;
		}

		public void ApplyCampaignRules(bool new_game)
		{
			if (new_game)
			{
				BalancePlayerKingdoms();
				int kingdomSizeWhenShatteredMap = GetKingdomSizeWhenShatteredMap();
				if (kingdomSizeWhenShatteredMap > 0)
				{
					CreateShatteredMap(kingdomSizeWhenShatteredMap);
				}
				if (main_goal == "DestroyKingdom")
				{
					DecideTargetKingdom();
				}
				for (int i = 0; i < base.game.kingdoms.Count; i++)
				{
					Kingdom kingdom = base.game.kingdoms[i];
					if (kingdom.IsDefeated())
					{
						continue;
					}
					if (kingdom.def != null)
					{
						kingdom.SetResources(Resource.Parse(kingdom.def.FindChild("starting_resources"), kingdom, no_null: true), send_state: false);
					}
					if (!IsPlayer(kingdom))
					{
						kingdom.total_earned.Set(kingdom.resources, 1f, ResourceType.Trade);
						continue;
					}
					float num = kingdom.resources[ResourceType.Gold];
					if (starting_gold >= 0)
					{
						num = starting_gold;
					}
					if (starting_gold_multiplier != 1)
					{
						num *= (float)starting_gold_multiplier;
					}
					kingdom.SetResources(ResourceType.Gold, num, send_state: false);
					kingdom.total_earned.Set(kingdom.resources, 1f, ResourceType.Trade);
					kingdom.InvalidateIncomes();
				}
			}
			ApplyAIDifficulty();
		}

		public int GetKingdomSize()
		{
			string text = GetVar("pick_kingdom").String();
			int num = GetVar("kingdom_size").Int();
			if (num == 0 && text != null && text.Contains("province"))
			{
				return 1;
			}
			if (num == 0)
			{
				return useDefaultKingdomSize;
			}
			return num;
		}

		public int GetKingdomSizeWhenShatteredMap()
		{
			string text = GetVar("kingdom_size").String();
			if (text != null && text.Contains("_shattered"))
			{
				string[] array = text.Split('_');
				if (array.Length != 0)
				{
					return int.Parse(array[0]);
				}
			}
			return -1;
		}

		private List<int> FindSuitableTargetKingdoms(List<int> playerKingdomIds, int preferredSizeMin, int preferredSizeMax, int minDist)
		{
			List<int> list = new List<int>(16);
			int num = 3;
			for (int i = minDist + num; i < num * 15; i += num)
			{
				for (int j = 0; j < base.game.kingdoms.Count; j++)
				{
					Kingdom kingdom = base.game.kingdoms[j];
					if (kingdom?.realms != null && kingdom.realms.Count != 0 && !IsPlayer(kingdom) && TargetKingdomAppropriateDist(playerKingdomIds, kingdom, minDist, i) && kingdom.realms.Count >= preferredSizeMin && kingdom.realms.Count <= preferredSizeMax)
					{
						list.Add(kingdom.id);
					}
				}
				if (list.Count > 0)
				{
					break;
				}
			}
			return list;
		}

		private void BalancePlayerKingdoms()
		{
			if (!obj.IsAuthority())
			{
				return;
			}
			int kingdomSize = GetKingdomSize();
			bool flag = false;
			bool flag2 = false;
			starting_trade_center_for_players = true;
			bool flag3 = false;
			string text = GetVar("kingdom_standing").String();
			switch (text)
			{
			case "historical":
				flag = true;
				flag2 = true;
				starting_trade_center_for_players = true;
				flag3 = true;
				break;
			case "challenging":
				flag = false;
				flag2 = false;
				starting_trade_center_for_players = false;
				flag3 = false;
				break;
			case "challenging_religion":
				flag = false;
				flag2 = false;
				starting_trade_center_for_players = true;
				flag3 = true;
				break;
			case "challenging_vassals":
				flag = true;
				flag2 = true;
				starting_trade_center_for_players = true;
				flag3 = false;
				break;
			case "challenging_trade":
				flag = true;
				flag2 = true;
				starting_trade_center_for_players = false;
				flag3 = true;
				break;
			default:
				Game.Log("Unrecognized value for religious_standing: " + text, LogType.Error);
				break;
			}
			if (MapIsShattered())
			{
				flag = false;
				flag2 = false;
				starting_trade_center_for_players = false;
				flag3 = false;
			}
			bool flag4 = false;
			string text2 = null;
			List<string> characterClassList = CharacterFactory.GetCharacterClassList(base.game, includeBaseClasses: true, includeSubclasses: false);
			List<string> characterClassList2 = CharacterFactory.GetCharacterClassList(base.game, includeBaseClasses: true, includeSubclasses: false, includeBase: false, filterRandomlySelectableAtStart: true);
			string text3 = GetVar("starting_king_class").String();
			switch (text3)
			{
			case "historical":
				flag4 = false;
				break;
			case "random_same":
				flag4 = true;
				text2 = characterClassList2[base.game.Random(0, characterClassList2.Count - 1)];
				break;
			case "random":
				flag4 = true;
				text2 = "random";
				break;
			default:
			{
				flag4 = true;
				bool flag5 = false;
				string text4 = char.ToUpperInvariant(text3[0]) + text3.Substring(1);
				foreach (string item in characterClassList)
				{
					if (text4 == item)
					{
						flag5 = true;
						text2 = item;
					}
				}
				if (!flag5)
				{
					Game.Log("Unrecognized value for starting_king_class: " + text3, LogType.Error);
				}
				break;
			}
			}
			List<Kingdom> list = new List<Kingdom>();
			for (int i = 0; i < base.game.campaign.playerDataPersistent.Length; i++)
			{
				RemoteVars remoteVars = base.game.campaign.playerDataPersistent[i];
				string kingdomName = base.game.campaign.GetKingdomName(i);
				if (string.IsNullOrEmpty(kingdomName))
				{
					continue;
				}
				Kingdom kingdom = base.game.GetKingdom(kingdomName);
				if (kingdom == null)
				{
					Game.Log("Invalid starting kingdom: " + kingdomName, LogType.Error);
					continue;
				}
				if (kingdom.IsDefeated())
				{
					Realm realm = base.game.GetRealm(kingdomName);
					if (realm == null)
					{
						Game.Log("Failed to find realm: " + kingdomName, LogType.Error);
						continue;
					}
					kingdom.DeclareIndependenceOrJoin(new List<Realm> { realm }, null, null, null, null, go_to_war: false, bonusses: false);
				}
				string text5 = remoteVars?.GetVar("origin_realm").String();
				Realm origin_realm = ((text5 != null) ? base.game.GetRealm(text5) : kingdom.GetCapital());
				if (origin_realm != null)
				{
					kingdom.GetKing()?.Govern(origin_realm?.castle);
				}
				if (kingdom.IsCaliphate() && !flag)
				{
					kingdom.caliphate = false;
					kingdom.SendState<Kingdom.ReligionState>();
				}
				if (!flag3)
				{
					for (int num = kingdom.vassalStates.Count - 1; num >= 0; num--)
					{
						kingdom.DelVassalState(kingdom.vassalStates[num]);
					}
					kingdom.SetSovereignState(null);
				}
				if (!flag2 && kingdom.is_orthodox && !kingdom.subordinated && !kingdom.is_ecumenical_patriarchate)
				{
					Character patriarch = kingdom.patriarch;
					(kingdom.religion as Orthodox).SetSubordinated(kingdom, subordinated: true, null, check_authority: false);
					kingdom.SendState<Kingdom.ReligionState>();
					kingdom.DelCourtMember(patriarch, send_state: false, kill_or_throneroom: false);
					kingdom.SendState<Kingdom.CourtState>();
				}
				if (flag4)
				{
					RoyalFamily royalFamily = kingdom.royalFamily;
					if (royalFamily.Spouse == null)
					{
						royalFamily.SetSpouse(CharacterFactory.CreateQueen(kingdom, royalFamily.Sovereign));
					}
					for (int num2 = royalFamily.Children.Count - 1; num2 >= 0; num2--)
					{
						royalFamily.RemoveChild(royalFamily.Children[num2]);
					}
					royalFamily.SetHeir(null);
				}
				if (text2 != null)
				{
					Character king = kingdom.GetKing();
					if (king != null)
					{
						kingdom.GetKing().SetClass((text2 == "random") ? characterClassList2[base.game.Random(0, characterClassList2.Count - 1)] : text2);
						king.GenerateNewSkills(force_new: true);
						king.age = Character.Age.Adult;
						king.SendState<Character.AgeState>();
					}
					else
					{
						Game.Log($"{kingdom}: King is null at the start of a game!", LogType.Error);
					}
				}
				if (kingdomSize != useDefaultKingdomSize)
				{
					kingdom_resize.ShrinkKingdom(kingdom, 1, (Realm r) => r == origin_realm);
					list.Add(kingdom);
				}
			}
			kingdom_resize.GrowKingdoms(list, kingdomSize);
			kingdom_resize.UpdateReligionAndLoyaltyOfModifiedRealms();
		}

		public bool MapIsShattered()
		{
			string text = GetVar("kingdom_size").String();
			if (text != null && text.Contains("_shattered"))
			{
				return true;
			}
			return false;
		}

		private void CreateShatteredMap(int kingdomSize)
		{
			RemoveScatteredRealms();
			HasScatteredRealms();
			List<Kingdom> list = new List<Kingdom>(base.game.kingdoms);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				Kingdom kingdom = list[num];
				List<Realm> keepRealms = new List<Realm>();
				keepRealms.Add(kingdom.GetCapital());
				int num2 = 30;
				while (kingdom.realms.Count > kingdomSize && num2-- > 0)
				{
					Realm realm = kingdom.GetCapital();
					Kingdom kingdom2 = base.game.GetKingdom(realm.name);
					if (kingdom.realms.Count > keepRealms.Count)
					{
						realm = kingdom_resize.FindMostDisconnectedRealm(kingdom, (Realm rl) => keepRealms.Contains(rl));
						kingdom2 = base.game.GetKingdom(realm.name);
					}
					if (kingdom2 == kingdom)
					{
						if (kingdom.realms.Count == 2)
						{
							realm = kingdom.GetCapital();
							kingdom2 = base.game.GetKingdom(realm.name);
							kingdom2.DeclareIndependenceOrJoin(new List<Realm> { realm }, null, null, null, null, go_to_war: false, bonusses: false);
						}
						else
						{
							keepRealms.Add(realm);
						}
					}
					else
					{
						kingdom2.DeclareIndependenceOrJoin(new List<Realm> { realm }, null, null, null, null, go_to_war: false, bonusses: false);
					}
				}
			}
			RemoveScatteredRealms();
			SpecialTreatmentKingdoms(kingdomSize);
			List<Kingdom> list2 = new List<Kingdom>();
			foreach (Kingdom kingdom3 in base.game.kingdoms)
			{
				if (!list2.Contains(kingdom3) && kingdom3.realms.Count == kingdomSize)
				{
					list2.Add(kingdom3);
				}
			}
			int num3 = HasKingdomLargerThanSize(list, kingdomSize);
			int num4 = 30;
			while (num3 > 0 && num4-- > 0)
			{
				for (int num5 = kingdomSize; num5 > 0; num5--)
				{
					foreach (Kingdom kingdom4 in base.game.kingdoms)
					{
						if (!list2.Contains(kingdom4) && kingdom4.realms.Count == kingdomSize)
						{
							list2.Add(kingdom4);
						}
						if (kingdom4.realms.Count == 0 || kingdom4.realms.Count != num5 || num5 == kingdomSize)
						{
							continue;
						}
						bool flag = true;
						int num6 = num5;
						while (flag)
						{
							if (!list2.Contains(kingdom4) && kingdom4.realms.Count == kingdomSize)
							{
								list2.Add(kingdom4);
							}
							GrowKingdom(kingdom4, num6, kingdomSize, list2);
							if (num6 != kingdom4.realms.Count)
							{
								num6 = kingdom4.realms.Count;
								flag = true;
							}
							else
							{
								flag = false;
							}
						}
					}
				}
				int num7 = HasKingdomLargerThanSize(list, kingdomSize);
				if (num3 != num7)
				{
					num3 = num7;
					kingdom_resize.UpdateReligionAndLoyaltyOfModifiedRealms(force: true);
					continue;
				}
				break;
			}
		}

		private void SpecialTreatmentKingdoms(int kingdomSize)
		{
			Kingdom papalKingdom = base.game.GetKingdom("Papal_States");
			kingdom_resize.ShrinkKingdom(papalKingdom, kingdomSize, (Realm r) => r == papalKingdom.GetCapital());
			kingdom_resize.GrowKingdom(papalKingdom, kingdomSize);
			for (int num = 0; num < base.game.campaign.playerDataPersistent.Length; num++)
			{
				_ = base.game.campaign.playerDataPersistent[num];
				string kingdomName = base.game.campaign.GetKingdomName(num);
				if (string.IsNullOrEmpty(kingdomName))
				{
					continue;
				}
				Kingdom playerKingdom = base.game.GetKingdom(kingdomName);
				if (playerKingdom != null)
				{
					kingdom_resize.ShrinkKingdom(playerKingdom, kingdomSize, (Realm r) => r == playerKingdom.GetCapital());
					kingdom_resize.GrowKingdom(playerKingdom, kingdomSize);
				}
			}
		}

		private int HasKingdomLargerThanSize(List<Kingdom> Kingdoms, int size)
		{
			string text = "";
			int num = 0;
			foreach (Kingdom kingdom in base.game.kingdoms)
			{
				if (kingdom.realms.Count > 0 && kingdom.realms.Count != size)
				{
					text = text + kingdom.Name + " " + kingdom.realms.Count + " ";
					num++;
				}
			}
			UnityEngine.Debug.LogWarning(text + "Total " + num);
			return num;
		}

		private void GrowKingdom(Kingdom o_Kingdom, int fromSize, int toSize, List<Kingdom> shatteredMapKingdoms)
		{
			int shattered_map_unification_size = GetVar("shattered_map_unification_size").int_val;
			Predicate<Realm> willNotUseRealm = delegate(Realm r)
			{
				if (o_Kingdom.realms.Contains(r))
				{
					return true;
				}
				int num = 0;
				foreach (Realm realm in o_Kingdom.realms)
				{
					if (base.game.RealmDistance(realm.id, r.id) > shattered_map_unification_size)
					{
						num++;
					}
				}
				if (num == o_Kingdom.realms.Count)
				{
					return true;
				}
				foreach (Kingdom shatteredMapKingdom in shatteredMapKingdoms)
				{
					if (shatteredMapKingdom.realms.Contains(r))
					{
						return true;
					}
				}
				return false;
			};
			kingdom_resize.GrowKingdom(o_Kingdom, toSize, willNotUseRealm);
			if (o_Kingdom.realms.Count == toSize && !shatteredMapKingdoms.Contains(o_Kingdom))
			{
				shatteredMapKingdoms.Add(o_Kingdom);
			}
		}

		private void RemoveScatteredRealms()
		{
			for (int i = 0; i < base.game.kingdoms.Count; i++)
			{
				Kingdom kingdom = base.game.kingdoms[i];
				if (kingdom.realms.Count < 2)
				{
					continue;
				}
				Realm capital = kingdom.GetCapital();
				for (int num = kingdom.realms.Count - 1; num >= 0; num--)
				{
					Realm realm = kingdom.realms[num];
					if (realm != capital && base.game.RealmDistance(capital.id, realm.id, goThroughSeas: true, useLogicNeighbors: false, int.MaxValue, sameKingdom: true) == -1)
					{
						Kingdom kingdom2 = base.game.GetKingdom(realm.name);
						if (kingdom2 != kingdom)
						{
							kingdom2.DeclareIndependenceOrJoin(new List<Realm> { realm }, null, null, null, null, go_to_war: false, bonusses: false);
						}
						else
						{
							base.game.GetKingdom(capital.name).DeclareIndependenceOrJoin(new List<Realm> { capital }, null, null, null, null, go_to_war: false, bonusses: false);
							capital = kingdom.GetCapital();
							num = kingdom.realms.Count;
						}
					}
				}
			}
		}

		private bool HasScatteredRealms()
		{
			bool result = false;
			string text = "";
			int num = 0;
			foreach (Kingdom kingdom in base.game.kingdoms)
			{
				if (kingdom.realms.Count < 2)
				{
					continue;
				}
				Realm capital = kingdom.GetCapital();
				for (int num2 = kingdom.realms.Count - 1; num2 >= 0; num2--)
				{
					Realm realm = kingdom.realms[num2];
					if (realm != capital && base.game.RealmDistance(capital.id, realm.id, goThroughSeas: true, useLogicNeighbors: false, int.MaxValue, sameKingdom: true) == -1)
					{
						text = text + "kingdom " + kingdom.Name + " _ capital " + capital.name + " _ realm " + realm.name + " ";
						result = true;
						num++;
					}
				}
			}
			UnityEngine.Debug.LogWarning(text + "TotalScattered " + num);
			return result;
		}

		private void DecideTargetKingdom()
		{
			if (!obj.IsAuthority() || base.game?.campaign == null)
			{
				return;
			}
			List<int> playerKingdomIds = new List<int>();
			for (int i = 0; i < base.game.campaign.playerDataPersistent.Length; i++)
			{
				string kingdomName = base.game.campaign.GetKingdomName(i);
				if (!string.IsNullOrEmpty(kingdomName))
				{
					Kingdom kingdom = obj.game.GetKingdom(kingdomName);
					if (kingdom != null)
					{
						playerKingdomIds.Add(kingdom.id);
					}
				}
			}
			int num = 6;
			int num2 = 8;
			target_kingdom_size = GetVar("target_kingdom_size");
			if (base.game.kingdoms != null && base.game.kingdoms.Count > 0 && base.game.kingdoms[0]?.def != null)
			{
				string key = "small_kingdom_size";
				switch (target_kingdom_size)
				{
				case 1:
					key = "tiny_kingdom_size";
					break;
				case 2:
					key = "small_kingdom_size";
					break;
				case 3:
					key = "medium_kingdom_size";
					break;
				case 4:
					key = "large_kingdom_size";
					break;
				case 5:
					key = "empire_kingdom_size";
					break;
				}
				num = base.game.kingdoms[0].def.GetValue(0, key);
				num2 = base.game.kingdoms[0].def.GetValue(1, key);
			}
			int target_kingdom_dist = GetVar("target_kingdom_dist").int_val;
			int minDist = Math.Max(target_kingdom_dist, num / 2);
			List<int> list = FindSuitableTargetKingdoms(playerKingdomIds, num, num2, minDist);
			if (list.Count == 0)
			{
				list = FindSuitableTargetKingdoms(playerKingdomIds, 1, base.game.realms.Count, minDist);
			}
			if (list.Count > 0)
			{
				Kingdom kingdom2 = base.game.GetKingdom(list[base.game.Random(0, list.Count)]);
				targetKingdom = kingdom2;
				Predicate<Realm> willNotUseRealm = delegate(Realm r)
				{
					foreach (int item in playerKingdomIds)
					{
						Kingdom kingdom3 = base.game.GetKingdom(item);
						int num4 = 0;
						foreach (Realm realm2 in kingdom3.realms)
						{
							if (base.game.RealmDistance(realm2.id, r.id) > target_kingdom_dist)
							{
								num4++;
							}
						}
						if (num4 != kingdom3.realms.Count)
						{
							return true;
						}
					}
					return false;
				};
				if (targetKingdom.realms.Count < num)
				{
					kingdom_resize.GrowKingdom(targetKingdom, base.game.Random(num, num2 + 1), willNotUseRealm);
				}
				else if (targetKingdom.realms.Count > num2)
				{
					kingdom_resize.ShrinkKingdom(targetKingdom, base.game.Random(num, num2 + 1), (Realm r) => targetKingdom.GetCapital() == r);
				}
				for (int num3 = 0; num3 < targetKingdom.realms.Count; num3++)
				{
					Realm realm = targetKingdom.realms[num3];
					realm.SetReligion(targetKingdom.religion);
					realm.AdjustPopMajority(-100f, targetKingdom);
				}
				base.game.SendState<TargetKingdomState>();
				Game.Log($"Target kingdom: {targetKingdom} with {targetKingdom.realms.Count} realms", LogType.Message);
				base.game.NotifyListeners("destroy_kingdom_target_selected", targetKingdom);
			}
			else
			{
				Game.Log("Failed to pick a target kingdom!", LogType.Error);
			}
		}

		private bool TargetKingdomAppropriateDist(List<int> plKingdomIds, Kingdom tKingdom, int mDist, int MDist)
		{
			string text = tKingdom.Name;
			foreach (int plKingdomId in plKingdomIds)
			{
				Kingdom kingdom = base.game.GetKingdom(plKingdomId);
				int num = int.MaxValue;
				foreach (Realm realm in kingdom.realms)
				{
					foreach (Realm realm2 in tKingdom.realms)
					{
						int num2 = base.game.RealmDistance(realm.id, realm2.id);
						num = ((num2 < num) ? num2 : num);
					}
				}
				if (num < mDist || num >= MDist)
				{
					return false;
				}
				text = text + " " + num;
			}
			return true;
		}

		private void ConvertPlayerProvinceCultureAndReligion()
		{
			for (int i = 0; i < base.game.campaign.playerDataPersistent.Length; i++)
			{
				string kingdomName = base.game.campaign.GetKingdomName(i);
				if (string.IsNullOrEmpty(kingdomName))
				{
					continue;
				}
				Kingdom kingdom = base.game.GetKingdom(kingdomName);
				for (int j = 0; j < kingdom.realms.Count; j++)
				{
					Realm realm = kingdom.realms[j];
					if (!realm.pop_majority.HasFullMajority(kingdom))
					{
						realm.AdjustPopMajority(-100f, kingdom);
					}
					if (realm.religion != kingdom.religion)
					{
						realm.SetReligion(kingdom.religion);
					}
				}
			}
		}

		private void ApplyAIDifficulty()
		{
			if (base.game.ProvincesAlwaysInitiallyConverted())
			{
				ConvertPlayerProvinceCultureAndReligion();
			}
		}

		public bool KnightAging()
		{
			return knight_aging;
		}

		public void SetAgingDef(string def_id)
		{
			List<CharacterAge.Def> defs = base.game.defs.GetDefs<CharacterAge.Def>();
			if (defs == null || defs.Count == 0)
			{
				return;
			}
			def_id = def_id.ToLowerInvariant();
			for (int i = 0; i < defs.Count; i++)
			{
				CharacterAge.Def def = defs[i];
				if (def.field.key.ToLowerInvariant().Contains(def_id))
				{
					aging_def = def;
					break;
				}
			}
		}

		public CharacterAge.Def GetAgingDef()
		{
			if (aging_def != null)
			{
				return aging_def;
			}
			CharacterAge.Def def = base.game.defs.Get<CharacterAge.Def>("NormalAging");
			if (def == null)
			{
				def = base.game.defs.GetBase<CharacterAge.Def>();
			}
			return def;
		}

		public bool IsPlayer(Kingdom kingdom)
		{
			if (kingdom.is_player)
			{
				return true;
			}
			if (base.game != null && base.game.campaign != null)
			{
				return base.game.campaign.IsPlayerControlledKingdom(kingdom);
			}
			return false;
		}

		public string ValidateWarAllowed(Kingdom source, Kingdom target)
		{
			if (source == null || target == null)
			{
				return "invalid_kingdoms";
			}
			if (source.is_player && target.is_player)
			{
				if (player_war_limits.type == PlayerWarLimits.Type.Never)
				{
					return "_player_wars_disabled";
				}
				if (base.game.teams.Get(source) == base.game.teams.Get(target))
				{
					return "_multiplayer_team";
				}
				if ((player_war_limits.type == PlayerWarLimits.Type.Time && (float)player_war_limits.time > base.game.session_time.minutes) | (player_war_limits.type == PlayerWarLimits.Type.Generations && player_war_limits.time > source.generationsPassed))
				{
					if (!playerwars_enable_loosing_players)
					{
						return "_player_wars_disabled";
					}
					List<Multiplayer.PlayerData> all = Multiplayer.CurrentPlayers.GetAll();
					if (all != null)
					{
						float num = 0f;
						float num2 = float.PositiveInfinity;
						float num3 = float.PositiveInfinity;
						float kingdomScore = GetKingdomScore(source, "main_goal");
						List<IdAndScore> list = new List<IdAndScore>(all.Count);
						for (int i = 0; i < all.Count; i++)
						{
							Kingdom kingdom = base.game.GetKingdom(all[i].kingdomId);
							if (kingdom == null)
							{
								base.game.Warning($"Null kingdom for Multiplayer.CurrentPlayers[{i}] = {all[i].kingdomId}");
								continue;
							}
							float kingdomScore2 = GetKingdomScore(kingdom, "main_goal");
							list.Add(new IdAndScore(all[i].kingdomId, kingdomScore2));
							if (kingdomScore2 > num)
							{
								num = kingdomScore2;
							}
							if (kingdomScore2 < num2)
							{
								num2 = kingdomScore2;
							}
						}
						if (kingdomScore != num2)
						{
							return "_player_wars_disabled";
						}
						list.Sort((IdAndScore s1, IdAndScore s2) => s1.score.CompareTo(s2.score));
						num3 = ((list.Count <= 2) ? num : list[1].score);
						if ((num3 - num2) / num3 * 100f < 20f)
						{
							return "_player_wars_disabled";
						}
					}
				}
			}
			return "ok";
		}

		public int GetPlayerRepicsLeftCount(Kingdom playerKingdom)
		{
			if (playerKingdom == null)
			{
				return 0;
			}
			if (!IsPlayer(playerKingdom))
			{
				return 0;
			}
			if (on_player_destroyed_pick_count <= 0)
			{
				return 0;
			}
			int num = on_player_destroyed_pick_count;
			if (base.game.campaign != null && base.game.campaign.playerDataPersistent != null)
			{
				int playerIndex = base.game.campaign.GetPlayerIndex(playerKingdom);
				if (playerIndex >= 0 && playerIndex < base.game.campaign.playerDataPersistent.Length)
				{
					num = base.game.campaign.playerDataPersistent[playerIndex].GetVar("repicks").Int();
				}
			}
			return on_player_destroyed_pick_count - num;
		}

		public bool PlayerCanRepick(Kingdom playerKingdom)
		{
			if (GetPlayerRepicsLeftCount(playerKingdom) > 0)
			{
				foreach (Kingdom kingdom in base.game.kingdoms)
				{
					if (IsAvailableForPicking(kingdom))
					{
						return true;
					}
				}
			}
			return false;
		}

		public void SetPlayerEliminated(Kingdom playerKingdom)
		{
			Log($"{playerKingdom} Player eliminated");
			if (base.game?.campaign != null)
			{
				int playerIndexForKingdom = base.game.campaign.GetPlayerIndexForKingdom(playerKingdom.Name);
				if (playerIndexForKingdom >= 0)
				{
					base.game.campaign.playerDataPersistent[playerIndexForKingdom].Set("repicks", base.game.rules.on_player_destroyed_pick_count);
					base.game.campaign.SetSlotState(playerIndexForKingdom, Campaign.SlotState.Eliminated);
				}
				else if (Multiplayer.LogEnabled(2))
				{
					Multiplayer.Log($"Fial to find player with {playerKingdom} during SetPlayerEliminated", 2);
				}
			}
			base.game.NotifyListeners("trigger_autosave", "player_eliminated");
		}

		public void ChangeMainGoal(string new_goal)
		{
			if (base.game != null && base.game.campaign != null && base.game.IsAuthority() && base.game.campaign.ValidateValue("main_goal", new_goal))
			{
				Value var = base.game.campaign.GetVar("team_size");
				base.game.campaign.campaignData.Set("main_goal", new_goal);
				main_goal = new_goal;
				base.game.campaign.campaignData.Set("team_size", var);
				Reload();
				base.game.NotifyListeners("main_goal_changed");
			}
		}

		public List<Object> GetPickableKingdoms()
		{
			List<Object> list = new List<Object>();
			List<Object> list2 = new List<Object>();
			if (base.game.multiplayer == null)
			{
				return list;
			}
			Multiplayer.PlayerData playerData = base.game.multiplayer.playerData;
			List<Multiplayer.PlayerData> all = Multiplayer.CurrentPlayers.GetAll();
			int maxKingdomRepickSize = GetMaxKingdomRepickSize();
			foreach (Kingdom kingdom in base.game.kingdoms)
			{
				if (!IsAvailableForPicking(kingdom, maxKingdomRepickSize))
				{
					continue;
				}
				list.Add(kingdom);
				bool flag = false;
				foreach (Multiplayer.PlayerData item in all)
				{
					if (item.team != playerData.team)
					{
						RelationUtils.Stance stance = KingdomAndKingdomRelation.GetStance(base.game.GetKingdom(item.kingdomId), kingdom);
						if (stance.IsAlliance() || stance.IsTrade() || stance.IsMarriage())
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					list2.Add(kingdom);
				}
			}
			if (list2.Count > 0)
			{
				return list2;
			}
			return list;
		}

		public int GetMaxKingdomRepickSize()
		{
			int val = int.MaxValue;
			int num = int.MaxValue;
			foreach (Kingdom kingdom in base.game.kingdoms)
			{
				if (!kingdom.IsDefeated() && IsPlayer(kingdom))
				{
					num = Math.Min(num, kingdom.realms.Count);
				}
				if (IsAvailableForPicking(kingdom))
				{
					val = Math.Min(val, kingdom.realms.Count);
				}
			}
			Value var = GetVar("kingdom_size");
			int num2 = ((var == 0) ? 2 : var.Int());
			if (num != int.MaxValue)
			{
				num2 = Math.Max(num2, num / 2);
			}
			return Math.Max(val, num2);
		}

		public bool IsAvailableForPicking(Kingdom kNew, int max_size = -1)
		{
			if (kNew == null || base.game.multiplayer == null)
			{
				return false;
			}
			if (kNew.IsDefeated())
			{
				return false;
			}
			if (IsPlayer(kNew))
			{
				return false;
			}
			if (kNew.IsPapacy())
			{
				return false;
			}
			if (kNew == targetKingdom)
			{
				return false;
			}
			if (max_size >= 0 && kNew.realms.Count > max_size)
			{
				return false;
			}
			Team team = base.game.teams.Get(base.game.multiplayer.playerData.id);
			if (team == null)
			{
				return false;
			}
			foreach (Player player in team.players)
			{
				if (kNew.IsEnemy(base.game.GetKingdom(player.kingdom_id)))
				{
					return false;
				}
			}
			return true;
		}

		private int GetTeamGoods(int teamId)
		{
			Team team = base.game.teams[teamId];
			if (team == null)
			{
				return 0;
			}
			List<Resource.Def> list = new List<Resource.Def>();
			foreach (Player item in team?.players)
			{
				Kingdom kingdom = base.game.GetKingdom(item.kingdom_id);
				if (kingdom == null)
				{
					continue;
				}
				foreach (KeyValuePair<string, Resource.Def> item2 in kingdom.goods_produced)
				{
					Resource.Def value = item2.Value;
					if (!list.Contains(value))
					{
						list.Add(value);
					}
				}
			}
			return list.Count;
		}

		private bool ValidateEndGameLimit()
		{
			if (time_limits.type == TimeLimits.Type.Time && time_limits.value - base.game.session_time.milliseconds / 1000 / 60 <= 0)
			{
				return true;
			}
			if (time_limits.type == TimeLimits.Type.Generations)
			{
				int num = -1;
				for (int i = 0; i < base.game.campaign.playerDataPersistent.Length; i++)
				{
					string kingdomName = base.game.campaign.GetKingdomName(i);
					if (string.IsNullOrEmpty(kingdomName))
					{
						continue;
					}
					Kingdom kingdom = base.game.GetKingdom(kingdomName);
					if (kingdom != null)
					{
						if (num == -1)
						{
							num = kingdom.generationsPassed;
						}
						else if (kingdom != null && kingdom.generationsPassed < num)
						{
							num = kingdom.generationsPassed;
						}
					}
				}
				if (num >= time_limits.value)
				{
					return true;
				}
			}
			return false;
		}

		private float GetTeamScore(Kingdom k)
		{
			float result = 0f;
			Team team = base.game.teams.Get(k);
			if (team != null)
			{
				result = GetTeamScore(team.id);
			}
			return result;
		}

		private int GetTeamRealms(Kingdom k)
		{
			if (k == null)
			{
				return 0;
			}
			Team team = base.game.teams.Get(k);
			if (team != null)
			{
				int num = 0;
				{
					foreach (Player player in team.players)
					{
						Kingdom kingdom = base.game.GetKingdom(player.kingdom_id);
						if (kingdom != null)
						{
							num += kingdom.realms.Count;
						}
					}
					return num;
				}
			}
			return k.realms.Count;
		}

		private List<IdAndScore> GetTies(List<IdAndScore> scores, bool sortInput = false)
		{
			if (scores == null || scores.Count < 2)
			{
				return null;
			}
			if (sortInput)
			{
				scores.Sort((IdAndScore s1, IdAndScore s2) => s2.score.CompareTo(s1.score));
			}
			List<IdAndScore> list = new List<IdAndScore>(scores.Count);
			list.Add(scores[0]);
			for (int num = 1; num < scores.Count; num++)
			{
				if (scores[0].score == scores[num].score)
				{
					list.Add(scores[num]);
				}
			}
			if (list.Count > 1)
			{
				return list;
			}
			return null;
		}

		public float GetKingdomScore(Kingdom k, ScoreModifers scoreModifers)
		{
			float num = 0f;
			if (scoreModifers == null)
			{
				return num;
			}
			k.UpdateRealmTags();
			num += GetKingdomScorePerType(k, scoreModifers, "gold", updateRealmTags: false);
			num += GetKingdomScorePerType(k, scoreModifers, "realms", updateRealmTags: false);
			num += GetKingdomScorePerType(k, scoreModifers, "fame", updateRealmTags: false);
			num += GetKingdomScorePerType(k, scoreModifers, "unique_resources", updateRealmTags: false);
			return num + GetKingdomScorePerType(k, scoreModifers, "army_strength", updateRealmTags: false);
		}

		public float GetKingdomScore(Kingdom k, string rule_id)
		{
			ScoreModifers ruleScoreModifers = GetRuleScoreModifers(rule_id);
			return GetKingdomScore(k, ruleScoreModifers);
		}

		public float GetKingdomScorePerType(Kingdom k, string rule_id, string score_type)
		{
			ScoreModifers ruleScoreModifers = GetRuleScoreModifers(rule_id);
			return GetKingdomScorePerType(k, ruleScoreModifers, score_type);
		}

		public float GetKingdomScorePerType(Kingdom k, ScoreModifers scoreModifers, string score_type, bool updateRealmTags = true)
		{
			float num = 0f;
			if (scoreModifers == null)
			{
				return num;
			}
			if (updateRealmTags)
			{
				k.UpdateRealmTags();
			}
			switch (score_type)
			{
			case "gold":
				num += k.resources[ResourceType.Gold] * scoreModifers.scorePerGold;
				break;
			case "realms":
			{
				int num2 = 0;
				for (int i = 0; i < k.realms.Count; i++)
				{
					Realm realm = k.realms[i];
					if (!realm.IsOccupied() && !realm.IsDisorder())
					{
						num2++;
					}
				}
				num += (float)num2 * scoreModifers.scorePerRealm;
				break;
			}
			case "fame":
				num += k.fame * scoreModifers.scorePerFame;
				break;
			case "unique_resources":
				num += (float)k.goods_produced.Count * scoreModifers.scorePerUniqueResource;
				break;
			case "army_strength":
				num += k.armyStrength * scoreModifers.scorePerArmyStrength;
				break;
			}
			return num;
		}

		public ScoreModifers GetRuleScoreModifers(string key)
		{
			if (!(key == "goal_prestige_victory"))
			{
				if (key == "goal_kingdom_advantages")
				{
					return kingdomAdvantagesModifiers;
				}
				return mainGoalScoreModifiers;
			}
			return prestigeVictoryModifiers;
		}

		public float GetTeamScore(int teamId, string rule_key = null)
		{
			ScoreModifers scoreModifers = ((!string.IsNullOrEmpty(rule_key)) ? GetRuleScoreModifers(rule_key) : mainGoalScoreModifiers);
			float num = 0f;
			if (scoreModifers == null)
			{
				return num;
			}
			if (rule_key == "WarForGoods" || main_goal == "WarForGoods")
			{
				return GetTeamGoods(teamId);
			}
			foreach (Player player in base.game.teams[teamId].players)
			{
				Kingdom kingdom = base.game.GetKingdom(player.kingdom_id);
				if (kingdom != null && !kingdom.IsDefeated())
				{
					num += GetKingdomScore(kingdom, scoreModifers);
				}
			}
			return num;
		}

		public float GetTeamScorePerType(int teamId, string rule_key, string score_type)
		{
			ScoreModifers scoreModifers = ((!string.IsNullOrEmpty(rule_key)) ? GetRuleScoreModifers(rule_key) : mainGoalScoreModifiers);
			float num = 0f;
			if (scoreModifers == null)
			{
				return num;
			}
			if (rule_key == "WarForGoods" || main_goal == "WarForGoods")
			{
				return GetTeamGoods(teamId);
			}
			foreach (Player player in base.game.teams[teamId].players)
			{
				Kingdom kingdom = base.game.GetKingdom(player.kingdom_id);
				if (kingdom != null && !kingdom.IsDefeated())
				{
					num += GetKingdomScorePerType(kingdom, scoreModifers, score_type);
				}
			}
			return num;
		}

		public bool HasActivePlayers()
		{
			if (base.game?.campaign?.playerDataPersistent == null)
			{
				return false;
			}
			for (int i = 0; i < base.game.campaign.playerDataPersistent.Length; i++)
			{
				if (base.game.campaign.GetSlotState(i) == Campaign.SlotState.Joined)
				{
					return true;
				}
			}
			return false;
		}

		public bool GetLastTeamStanding(out int winning_team_id)
		{
			if (base.game == null || base.game.teams == null || base.game.teams.teams == null || base.game.teams.teams.Count < 2)
			{
				winning_team_id = -1;
				return false;
			}
			if (!base.game.IsMultiplayer())
			{
				winning_team_id = -1;
				return false;
			}
			int num = 0;
			int num2 = 0;
			int num3 = -1;
			for (int i = 0; i < base.game.teams.teams.Count; i++)
			{
				Team team = base.game.teams.teams[i];
				if (team == null || team.players.Count == 0)
				{
					continue;
				}
				num++;
				if (team.NumActivePlayers(base.game.campaign) > 0)
				{
					num2++;
					if (num3 < 0)
					{
						num3 = team.id;
					}
				}
			}
			if (num <= 1 || num2 > 1)
			{
				winning_team_id = -1;
				return false;
			}
			winning_team_id = num3;
			return true;
		}

		public bool CheckMainGoalAchieved(Kingdom k, Kingdom defeatedKingdom = null)
		{
			if (!end_game_triggered)
			{
				if (GetTeamRealms(k) >= base.game.landRealmsCount)
				{
					SetWinner(k, "Conquest");
					Log(string.Concat(k, "Victory: Conquest"));
					return true;
				}
				if (main_goal == "HaveXGold" && GetTeamScore(k) >= (float)gold_goal.int_val && IsPlayer(k))
				{
					SetWinner(k, main_goal);
					Log(string.Concat(k, "Victory: Greedy Kings"));
					return true;
				}
				if (main_goal == "HaveXRealms" && GetTeamScore(k) >= (float)realms_goal.int_val && IsPlayer(k))
				{
					SetWinner(k, main_goal);
					Log(string.Concat(k, "Victory: Peasants Rush"));
					return true;
				}
				if (main_goal == "FirstBlood" && defeatedKingdom != null && IsPlayer(defeatedKingdom))
				{
					SetWinner(k, main_goal);
					Log(string.Concat(k, "Victory: First Blood"));
					return true;
				}
				if (main_goal == "DestroyKingdom" && targetKingdom != null && defeatedKingdom != null && defeatedKingdom.id == targetKingdom.id)
				{
					SetWinner(k, main_goal);
					Log(string.Concat(k, "Victory: Destroy Kingdom"));
					return true;
				}
				if (main_goal == "WarForGoods" && GetTeamScore(k) >= (float)goods_goal.int_val && IsPlayer(k))
				{
					SetWinner(k, main_goal);
					Log(string.Concat(k, "Victory: War for Goods"));
					return true;
				}
			}
			else if (k.realms != null && k.realms.Count >= base.game.landRealmsCount)
			{
				SetWinner(k, "Conquest");
				Log(string.Concat(k, "Victory: Conquest"));
				return true;
			}
			return false;
		}

		public bool OnValidateEndGame(Kingdom k, Kingdom defeatedKingdom = null)
		{
			if (!base.game.IsAuthority())
			{
				return false;
			}
			if (base.game.state != State.Running)
			{
				return false;
			}
			singlePlayerWinner = null;
			winningTeam = null;
			if (k != null && k.type == Kingdom.Type.Regular && CheckMainGoalAchieved(k, defeatedKingdom))
			{
				TriggerEndGame();
				return true;
			}
			if (defeatedKingdom != null && IsPlayer(defeatedKingdom))
			{
				if (!base.game.IsMultiplayer())
				{
					Log(string.Concat(defeatedKingdom, " End Game. Player eliminated"));
					SetEndGameReason("PlayerEliminated");
					TriggerEndGame();
					return true;
				}
				if (on_player_destroyed_pick_count == 0)
				{
					SetPlayerEliminated(defeatedKingdom);
					defeatedKingdom.FireEvent("defeat", "PlayerEliminated");
				}
				else if (!PlayerCanRepick(defeatedKingdom))
				{
					SetPlayerEliminated(defeatedKingdom);
					if (GetPlayerRepicsLeftCount(defeatedKingdom) > 0)
					{
						defeatedKingdom.FireEvent("defeat", "PlayerEliminatedCantRepick");
					}
					else
					{
						defeatedKingdom.FireEvent("defeat", "PlayerEliminated");
					}
				}
				else
				{
					defeatedKingdom.FireEvent("defeat", "KingdomDefeated");
				}
				if (!HasActivePlayers())
				{
					SetEndGameReason("PlayerEliminated");
					TriggerEndGame();
					Log(string.Concat(defeatedKingdom, " End Game. Players eliminated"));
					return true;
				}
			}
			if (!CampaignUtils.IsCoop(base.game.campaign) && GetLastTeamStanding(out var winning_team_id) && end_game_reason != "LastTeamStanding" && winning_team_id > -1)
			{
				SetWinner(winning_team_id, "LastTeamStanding");
				Log(string.Concat(k, " Victory: LastTeamStanding"));
				TriggerEndGame();
				return true;
			}
			if (!end_game_triggered && !early_end_triggered && ValidateEndGameLimit())
			{
				TriggerEarlyEnd();
				Log("End Game: EndGameLimit");
				return true;
			}
			return false;
		}

		private void TriggerEndGame()
		{
			base.game.campaign.campaignData.Set("end_game_triggered", true);
			base.game?.campaign?.SetState(Campaign.State.Closed);
			end_game_triggered = true;
		}

		private void TriggerEarlyEnd()
		{
			SetEndGameReason("EndGameLimit");
			base.game.campaign.campaignData.Set("early_end_triggered", true);
			base.game?.campaign?.SetState(Campaign.State.Closed);
			early_end_triggered = true;
		}

		public void SetWinner(Kingdom k, string reason)
		{
			if (!base.game.IsMultiplayer())
			{
				singlePlayerWinner = k;
			}
			Team team = base.game.teams.Get(k);
			if (team != null)
			{
				winningTeam = team;
			}
			SetEndGameReason(reason);
			base.game?.campaign?.SetState(Campaign.State.Closed);
			int playerIndex = base.game.campaign.GetPlayerIndex(k);
			base.game.campaign.SetVar(RemoteVars.DataType.PersistentPlayerData, playerIndex, "victor", true);
		}

		public void SetWinner(int team_id, string reason)
		{
			Team team = base.game.teams[team_id];
			if (team != null)
			{
				winningTeam = team;
				for (int i = 0; i < team.players.Count; i++)
				{
					Player player = team.players[i];
					int playerIndex = base.game.campaign.GetPlayerIndex(player.id);
					base.game.campaign.SetVar(RemoteVars.DataType.PersistentPlayerData, playerIndex, "victor", true);
				}
			}
			SetEndGameReason(reason);
		}

		public void SetEndGameReason(string reason)
		{
			end_game_reason = reason;
			base.game?.campaign?.campaignData.Set("end_game_reason", reason);
		}
	}

	public enum LoadedGameType
	{
		Continue,
		LoadFromMainMenu,
		LoadFromInGameMenu,
		QuickLoad,
		ContinueMultiplayerHost,
		ContinueMultiplayerClient,
		Invalid
	}

	public delegate void RealmWaveCallback(Realm r, Realm rStart, int depth, object param, ref bool push_neighbors, ref bool stop);

	public delegate void RealmWaveWithListCallback(Realm r, List<Realm> rStart, int depth, object param, ref bool push_neighbors, ref bool stop);

	public enum CheatLevel
	{
		None,
		Low,
		Medium,
		High
	}

	public class Stats
	{
		private bool AchievementsAllowed()
		{
			return ModManager.Get()?.IsVanillaGame() ?? false;
		}

		public void SetIntStat(string name, int val)
		{
			if (AchievementsAllowed())
			{
				Coroutine.Start("SetStatCoro", THQNORequest.SetStatCoro(name, val));
			}
		}

		public int GetIntStat(string name)
		{
			THQNORequest intStat = THQNORequest.GetIntStat(name);
			if (intStat.error != null)
			{
				Log("GetStat " + name + " unsuccessful! Error: " + intStat.error, LogType.Warning);
				return 0;
			}
			return intStat.result.int_val;
		}

		public void IncIntStat(string name, int val)
		{
			if (AchievementsAllowed())
			{
				LogWithoutStackTrace($"Achievement stat '{name}': +{val}", LogType.Message);
				if (val != 0)
				{
					Coroutine.Start("IncStatCoro", THQNORequest.IncStatCoro(name, val));
				}
			}
		}

		public void SetAchievement(string name)
		{
			if (AchievementsAllowed())
			{
				LogWithoutStackTrace("Unlocking Achievement '" + name + "'", LogType.Message);
				Coroutine.Start("SetAchievementCoro", THQNORequest.SetAchievementCoro(name));
			}
		}

		public void ClearAchievement(string name)
		{
			LogWithoutStackTrace("Clearing Achievement '" + name + "'", LogType.Message);
			Coroutine.Start("ClearAchievementCoro", THQNORequest.ClearAchievementCoro(name));
		}

		public void CheckAchievement(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				Coroutine.Start("CheckAchievementCoro", THQNORequest.CheckAchievementCoro(name));
			}
		}

		public bool GetAchievement(string name)
		{
			THQNORequest achievement = THQNORequest.GetAchievement(name);
			if (achievement.error != null)
			{
				Log("GetAchievement " + name + " unsuccessful! Error: " + achievement.error, LogType.Warning);
				return false;
			}
			return achievement.result.Bool();
		}
	}

	public class Teams : Component
	{
		public List<Team> teams;

		public Team this[int teamId]
		{
			get
			{
				foreach (Team team in teams)
				{
					if (team.id == teamId)
					{
						return team;
					}
				}
				return null;
			}
		}

		public Teams(Object obj)
			: base(obj)
		{
			teams = new List<Team>();
		}

		public void Evaluate()
		{
			if (base.game?.campaign?.playerDataPersistent == null || base.game?.rules == null)
			{
				return;
			}
			if (teams == null)
			{
				teams = new List<Team>();
			}
			teams.Clear();
			for (int i = 0; i < base.game.campaign.playerDataPersistent.Length; i++)
			{
				string text = base.game.campaign.playerDataPersistent[i].GetVar("id").String();
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				int team = CampaignUtils.GetTeam(base.game.campaign, i);
				if (team < 0)
				{
					Game.Log($"Got invalid team id value {team} from player_vars {base.game.campaign.playerDataPersistent[i]}", LogType.Warning);
					continue;
				}
				Team team2 = this[team];
				if (team2 == null)
				{
					team2 = new Team(team);
					teams.Add(team2);
				}
				Kingdom kingdom = Multiplayer.CurrentPlayers.GetKingdomByGUID(text);
				if (kingdom == null)
				{
					string kingdomName = base.game.campaign.GetKingdomName(i);
					if (!string.IsNullOrEmpty(kingdomName))
					{
						kingdom = base.game.GetKingdom(kingdomName);
					}
				}
				int num = kingdom?.id ?? 0;
				if (!string.IsNullOrEmpty(text) && num > 0 && num <= base.game.kingdoms.Count)
				{
					team2.players.Add(new Player(base.game, text, num));
				}
			}
		}

		public Team Get(Kingdom k)
		{
			if (k == null)
			{
				return null;
			}
			foreach (Team team in teams)
			{
				foreach (Player player in team.players)
				{
					if (player.kingdom_id == k.id)
					{
						return team;
					}
				}
			}
			return null;
		}

		public Team Get(string player_id)
		{
			foreach (Team team in teams)
			{
				foreach (Player player in team.players)
				{
					if (player.id == player_id)
					{
						return team;
					}
				}
			}
			return null;
		}

		public Player GetPlayerById(string player_id)
		{
			for (int i = 0; i < teams.Count; i++)
			{
				Team team = teams[i];
				for (int j = 0; j < team.players.Count; j++)
				{
					Player player = team.players[j];
					if (player.id == player_id)
					{
						return player;
					}
				}
			}
			return null;
		}
	}

	public class Team : IVars
	{
		public int id;

		public List<Player> players;

		public Team(int id)
		{
			this.id = id;
			players = new List<Player>();
		}

		public bool HasKingdom(int kingdom_id)
		{
			foreach (Player player in players)
			{
				if (player.kingdom_id == kingdom_id)
				{
					return true;
				}
			}
			return false;
		}

		public int NumActivePlayers(Campaign campaign)
		{
			int num = 0;
			for (int i = 0; i < players.Count; i++)
			{
				Player player = players[i];
				if (campaign.GetSlotState(player.id) == Campaign.SlotState.Joined)
				{
					num++;
				}
			}
			return num;
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return "Player.team.name";
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return key switch
			{
				"name" => GetNameKey(), 
				"team_id" => id, 
				"team_number" => id + 1, 
				_ => Value.Unknown, 
			};
		}

		public override string ToString()
		{
			string text = $"Team {id}: ";
			if (players != null)
			{
				foreach (Player player in players)
				{
					text += $" [{player}]";
				}
			}
			return text;
		}
	}

	public class Player : IVars
	{
		public string id;

		public int kingdom_id;

		public Game game;

		public Player(Game game, string id, int kingdom_id)
		{
			this.id = id;
			this.game = game;
			this.kingdom_id = kingdom_id;
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return key switch
			{
				"kingdom" => GetKingdom(), 
				"id" => id, 
				"name" => "#" + ExtractPlayerName(), 
				"team_name" => "Player.team.name", 
				"team_id" => GetTeamId(), 
				"team_number" => GetTeamId() + 1, 
				"score" => GetScore(), 
				"current_generation" => GetgenerationPassed() + 1, 
				"generations_passed" => GetgenerationPassed(), 
				"connection_status" => GetConnectionStatusKey(), 
				"is_defeated" => IsDefeated(), 
				"is_eliminated" => IsEliminated(), 
				"has_generation_limit" => HasGenerationLimit(), 
				"repicks_are_enabled" => GetRepicksEnabled(), 
				"repicks_left" => GetRepicksLeft(), 
				"is_local_player" => IsLocalPlayer(), 
				_ => Value.Unknown, 
			};
		}

		public Kingdom GetKingdom()
		{
			if (game == null)
			{
				return null;
			}
			return game.GetKingdom(kingdom_id);
		}

		public bool GetRepicksEnabled()
		{
			if (game == null)
			{
				return false;
			}
			if (game.rules == null)
			{
				return false;
			}
			return game.rules.on_player_destroyed_pick_count > 0;
		}

		public int GetRepicksLeft()
		{
			if (!GetRepicksEnabled())
			{
				return 0;
			}
			if (game.campaign == null || game.campaign.playerDataPersistent == null)
			{
				return 0;
			}
			int playerIndex = game.campaign.GetPlayerIndex(id);
			if (playerIndex < 0)
			{
				return 0;
			}
			return game.rules.on_player_destroyed_pick_count - game.campaign.playerDataPersistent[playerIndex].GetVar("repicks").Int();
		}

		public bool HasGenerationLimit()
		{
			return game.rules.time_limits.type == CampaignRules.TimeLimits.Type.Generations;
		}

		public int GetgenerationPassed()
		{
			if (game == null)
			{
				return 0;
			}
			return game.GetKingdom(kingdom_id).generationsPassed;
		}

		public string ExtractPlayerName()
		{
			string text = "";
			if (game == null)
			{
				return text;
			}
			Multiplayer.PlayerData byGUID = Multiplayer.CurrentPlayers.GetByGUID(id);
			if (byGUID != null)
			{
				text = byGUID.name;
			}
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			if (game != null && game.campaign != null && game.campaign.playerDataPersistent != null)
			{
				for (int i = 0; i < game.campaign.playerDataPersistent.Length; i++)
				{
					string text2 = game.campaign.playerDataPersistent[i].GetVar("id").String();
					if (!string.IsNullOrEmpty(text2) && text2 == id)
					{
						text = game.campaign.playerDataPersistent[i].GetVar("name").String();
						if (!string.IsNullOrEmpty(text))
						{
							return text;
						}
					}
				}
			}
			return "";
		}

		public int GetTeamId()
		{
			if (game == null)
			{
				return -1;
			}
			return (game.teams?.Get(id))?.id ?? (-1);
		}

		public float GetScore()
		{
			if (game == null)
			{
				return 0f;
			}
			return game.rules.GetKingdomScore(game.GetKingdom(kingdom_id), "main_goal");
		}

		public string GetConnectionStatusKey()
		{
			if (IsOffline())
			{
				return "Player.connection.not_connected";
			}
			if (CampaignUtils.IsPlayerLoaded(game, id))
			{
				return "Player.connection.playing";
			}
			return "Player.connection.loading";
		}

		public bool IsOffline()
		{
			if (game == null || game.multiplayer == null)
			{
				return true;
			}
			if (game.multiplayer.type != Multiplayer.Type.Server && !game.multiplayer.IsOnline())
			{
				return true;
			}
			return Multiplayer.CurrentPlayers.GetKingdomByGUID(id) == null;
		}

		public bool IsDefeated()
		{
			return GetPlayerKingdom()?.IsDefeated() ?? true;
		}

		public bool IsEliminated()
		{
			return game.campaign.GetSlotState(id) == Campaign.SlotState.Eliminated;
		}

		public Kingdom GetPlayerKingdom()
		{
			Kingdom kingdom = Multiplayer.CurrentPlayers.GetKingdomByGUID(id);
			if (kingdom != null)
			{
				return kingdom;
			}
			if (game == null || game.campaign == null || game.campaign.playerDataPersistent == null)
			{
				return kingdom;
			}
			for (int i = 0; i < game.campaign.playerDataPersistent.Length; i++)
			{
				if (game.campaign.playerDataPersistent[i].GetVar("id").String() == id)
				{
					string kingdomName = game.campaign.GetKingdomName(i);
					if (!string.IsNullOrEmpty(kingdomName))
					{
						kingdom = game.GetKingdom(kingdomName);
						break;
					}
				}
			}
			return kingdom;
		}

		public bool IsLocalPlayer()
		{
			return game.GetLocalPlayerKingdom() == GetKingdom();
		}

		public override string ToString()
		{
			return $"Player (id: {id}, kid: {kingdom_id})";
		}
	}

	public class Pings
	{
		public struct Ping
		{
			public int uid;

			public long sent_time;

			public long elapsed => Millis - sent_time;

			public override string ToString()
			{
				return $"Ping {uid}, {elapsed}ms";
			}
		}

		private Game game;

		private Dictionary<int, int> last_client_roundtrip_ms;

		private Dictionary<int, long> last_kingdom_received_time;

		private long game_start_time;

		private List<Ping> pending;

		private int last_uid;

		private long last_sent_time;

		public int last_round_trip_ms;

		private long last_pong_time;

		public static long Millis => prof_timer.ElapsedMilliseconds;

		public Pings(Game game)
		{
			this.game = game;
		}

		public long TimeSinceLastPing(int kingdom_id)
		{
			if (last_kingdom_received_time == null)
			{
				return Millis - game_start_time;
			}
			if (!last_kingdom_received_time.TryGetValue(kingdom_id, out var value))
			{
				return Millis - game_start_time;
			}
			return Millis - value;
		}

		public long TimeSinceLastPong()
		{
			return Millis - last_pong_time;
		}

		public void OnGameStarted()
		{
			game_start_time = Millis;
		}

		public void OnUpdate()
		{
			if (Millis - last_sent_time < 1000)
			{
				return;
			}
			last_sent_time = Millis;
			if (game.state != State.Running || !game.IsMultiplayer())
			{
				return;
			}
			if (game.IsAuthority())
			{
				game.campaign.UpdatePlayersAI(game);
				return;
			}
			Ping item = new Ping
			{
				uid = ++last_uid,
				sent_time = last_sent_time
			};
			if (pending == null)
			{
				pending = new List<Ping>();
			}
			pending.Add(item);
			game.SendEvent(new PingEvent(item.uid, last_round_trip_ms));
		}

		public void OnPing(Multiplayer sender, int uid, int round_trip_ms)
		{
			if (last_client_roundtrip_ms == null)
			{
				last_client_roundtrip_ms = new Dictionary<int, int>();
			}
			last_client_roundtrip_ms[sender.playerData.pid] = round_trip_ms;
			if (last_kingdom_received_time == null)
			{
				last_kingdom_received_time = new Dictionary<int, long>();
			}
			last_kingdom_received_time[sender.playerData.kingdomId] = Millis;
			sender.SendObjEvent(game, new PongEvent(uid));
		}

		public void OnPong(int uid)
		{
			for (int i = 0; i < pending.Count; i++)
			{
				Ping ping = pending[i];
				if (ping.uid == uid)
				{
					last_round_trip_ms = (int)(Millis - ping.sent_time);
					pending.RemoveAt(i);
					break;
				}
			}
			last_pong_time = Millis;
		}

		public override string ToString()
		{
			if (game == null)
			{
				return "null game";
			}
			if (game.state != State.Running)
			{
				return "not playing";
			}
			if (!game.IsMultiplayer())
			{
				return "not multiplayer";
			}
			if (!game.IsAuthority())
			{
				return $"{last_round_trip_ms}ms";
			}
			if (last_client_roundtrip_ms == null)
			{
				return "none";
			}
			string text = "";
			foreach (KeyValuePair<int, int> last_client_roundtrip_m in last_client_roundtrip_ms)
			{
				int key = last_client_roundtrip_m.Key;
				int value = last_client_roundtrip_m.Value;
				if (text != "")
				{
					text += ", ";
				}
				text += $"{key}: {value}ms";
			}
			return text;
		}
	}

	[Serialization.State(11)]
	public class SessionTimeState : Serialization.ObjectState
	{
		public float session_time;

		public float real_time_played;

		public float real_time_total;

		public static SessionTimeState Create()
		{
			return new SessionTimeState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Game game = obj as Game;
			session_time = game.session_time.seconds;
			real_time_played = game.real_time_played.seconds + game.unscaled_unpaused_time_acc;
			real_time_total = game.real_time_total.seconds + game.unscaled_total_time_acc;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(session_time, "session_time");
			ser.WriteFloat(real_time_played, "real_time_played");
			ser.WriteFloat(real_time_total, "real_time_total");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			session_time = ser.ReadFloat("session_time");
			real_time_played = ser.ReadFloat("real_time_played");
			real_time_total = ser.ReadFloat("real_time_total");
			if (real_time_played <= 0f)
			{
				real_time_played = session_time;
			}
			if (real_time_total <= 0f)
			{
				real_time_total = real_time_played;
			}
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			Time time = Time.Zero + session_time;
			game.session_time_offset = time - game.time;
			game.real_time_played = Time.Zero + real_time_played;
			game.real_time_total = Time.Zero + real_time_total;
			game.real_time_total_per_frame = game.real_time_total;
		}
	}

	[Serialization.State(12)]
	public class SeedState : Serialization.ObjectState
	{
		public int seed;

		public static SeedState Create()
		{
			return new SeedState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Game game = obj as Game;
			seed = game.seed;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(seed, "seed");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			seed = ser.Read7BitUInt("seed");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Game).seed = seed;
		}
	}

	[Serialization.State(13)]
	public class FamousPeopleState : Serialization.ObjectState
	{
		public float time_to_next;

		public List<string> non_available_characters;

		public static FamousPeopleState Create()
		{
			return new FamousPeopleState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Game game = obj as Game;
			FamousPersonSpawner component = game.GetComponent<FamousPersonSpawner>();
			if (component == null)
			{
				return false;
			}
			time_to_next = component.next_update - game.time;
			non_available_characters = new List<string>();
			for (int i = 0; i < component.non_available_famous_people.Count; i++)
			{
				non_available_characters.Add(component.non_available_famous_people[i].field.key);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(time_to_next, "time_to_next");
			int count = non_available_characters.Count;
			ser.Write7BitSigned(count, "count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteStr(non_available_characters[i], "character_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			time_to_next = ser.ReadFloat("time_to_next");
			int num = ser.Read7BitSigned("count");
			non_available_characters = new List<string>();
			for (int i = 0; i < num; i++)
			{
				string item = ser.ReadStr("character_", i);
				non_available_characters.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			FamousPersonSpawner component = game.GetComponent<FamousPersonSpawner>();
			if (component == null)
			{
				return;
			}
			if (time_to_next > 0f)
			{
				component.StopUpdating();
				component.UpdateAfter(time_to_next);
			}
			if (component.non_available_famous_people == null)
			{
				component.non_available_famous_people = new List<FamousPerson.Def>();
			}
			else
			{
				component.non_available_famous_people.Clear();
			}
			for (int i = 0; i < non_available_characters.Count; i++)
			{
				FamousPerson.Def def = game.defs.Get<FamousPerson.Def>(non_available_characters[i]);
				if (def != null)
				{
					component.non_available_famous_people.Add(def);
				}
			}
		}
	}

	[Serialization.State(14)]
	public class TradeCentersState : Serialization.ObjectState
	{
		public struct Data
		{
			public int rid;

			public List<NID> belongingRealms;

			public List<int> belongingRealmsDistances;

			public Data(int rid, List<NID> belongingRealms, List<int> belongingRealmsDistances)
			{
				this.rid = rid;
				this.belongingRealms = belongingRealms;
				this.belongingRealmsDistances = belongingRealmsDistances;
			}
		}

		public List<Data> centersData;

		public static TradeCentersState Create()
		{
			return new TradeCentersState();
		}

		public static bool IsNeeded(Object obj)
		{
			List<Realm> list = (obj as Game).economy?.tradeCenterRealms;
			if (list != null)
			{
				return list.Count > 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			List<Realm> list = (obj as Game).economy?.tradeCenterRealms;
			if (list == null || list.Count <= 0)
			{
				return false;
			}
			centersData = new List<Data>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				Realm realm = list[i];
				TradeCenter tradeCenter = realm.tradeCenter;
				if (tradeCenter != null)
				{
					List<NID> list2 = new List<NID>(tradeCenter.belongingRealms.Count);
					List<int> list3 = new List<int>(tradeCenter.belongingRealms.Count);
					for (int j = 0; j < tradeCenter.belongingRealms.Count; j++)
					{
						list2.Add(tradeCenter.belongingRealms[j]);
						list3.Add(tradeCenter.belongingRealms[j].tradeCenterDistance);
					}
					centersData.Add(new Data(realm.id, list2, list3));
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((centersData != null) ? centersData.Count : 0);
			ser.Write7BitUInt(num, "count_tcs");
			for (int i = 0; i < num; i++)
			{
				_ = centersData[i];
				ser.Write7BitUInt(centersData[i].rid, "realm", i);
				ser.Write7BitUInt(centersData[i].belongingRealms.Count, "count_brs", i);
				for (int j = 0; j < centersData[i].belongingRealms.Count; j++)
				{
					ser.WriteNID<Realm>(centersData[i].belongingRealms[j], "brs_" + j + "_", i);
					ser.Write7BitUInt(centersData[i].belongingRealmsDistances[j], "brs_distance_" + j + "_", i);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count_tcs");
			centersData = new List<Data>(num);
			for (int i = 0; i < num; i++)
			{
				int rid = ser.Read7BitUInt("realm", i);
				int num2 = ser.Read7BitUInt("count_brs", i);
				List<NID> list = new List<NID>(num2);
				List<int> list2 = new List<int>(num2);
				for (int j = 0; j < num2; j++)
				{
					list.Add(ser.ReadNID<Realm>("brs_" + j + "_", i));
					list2.Add(ser.Read7BitUInt("brs_distance_" + j + "_", i));
				}
				centersData.Add(new Data(rid, list, list2));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			List<Realm> list = game.economy?.tradeCenterRealms;
			if (list == null)
			{
				Log("Economy not created when appying TradeCentersState", LogType.Error);
				return;
			}
			for (int i = 0; i < game.realms.Count; i++)
			{
				game.economy?.DelTradeCenterRealm(game.realms[i], send_state: false);
			}
			if (centersData == null)
			{
				return;
			}
			for (int j = 0; j < centersData.Count; j++)
			{
				int rid = centersData[j].rid;
				Realm realm = game.GetRealm(rid);
				if (realm == null)
				{
					Log("Unknown realm id " + rid + " when appying TradeCentersState", LogType.Error);
					continue;
				}
				list.Add(realm);
				TradeCenter tradeCenter = realm.GetComponent<TradeCenter>();
				if (tradeCenter == null)
				{
					tradeCenter = new TradeCenter(realm);
				}
				else
				{
					tradeCenter.CleanRealms();
				}
				realm.tradeCenter = tradeCenter;
				for (int k = 0; k < centersData[j].belongingRealms.Count; k++)
				{
					tradeCenter.AddBelongingRealm(centersData[j].belongingRealms[k].Get<Realm>(game), centersData[j].belongingRealmsDistances[k]);
				}
			}
		}
	}

	[Serialization.State(15)]
	public class CurrentPlayersState : Serialization.ObjectState
	{
		private int count;

		private List<Multiplayer.PlayerData> currentPlayers = new List<Multiplayer.PlayerData>();

		public static CurrentPlayersState Create()
		{
			return new CurrentPlayersState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			currentPlayers = new List<Multiplayer.PlayerData>(Multiplayer.CurrentPlayers.GetAll());
			count = currentPlayers.Count;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < count; i++)
			{
				currentPlayers[i].Write(ser, i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			count = ser.Read7BitUInt("count");
			for (int i = 0; i < count; i++)
			{
				Multiplayer.PlayerData playerData = new Multiplayer.PlayerData();
				playerData.Read(ser, i);
				currentPlayers.Add(playerData);
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (isLoadingSaveGame)
			{
				return;
			}
			bool flag = false;
			Game game = obj as Game;
			Multiplayer.CurrentPlayers.Clear();
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				Multiplayer.PlayerData playerData = currentPlayers[i];
				playerData.owner = game.multiplayer;
				if (playerData.pid == game.multiplayer.playerData.pid)
				{
					if (game.multiplayer.playerData.kingdomName != playerData.kingdomName)
					{
						flag = true;
					}
					game.multiplayer.playerData = playerData;
				}
				Multiplayer.CurrentPlayers.Add(playerData);
				if (flag)
				{
					flag = false;
					game.GetKingdom(playerData.kingdomId).NotifyListeners("players_changed", null, may_trigger: false, profile: false);
				}
			}
			game.teams.Evaluate();
			game.multiplayer.NotifyListeners("players_changed", null, may_trigger: false, profile: false);
		}
	}

	[Serialization.State(16)]
	public class TargetKingdomState : Serialization.ObjectState
	{
		public string targetKingdomName;

		public static TargetKingdomState Create()
		{
			return new TargetKingdomState();
		}

		public static bool IsNeeded(Object obj)
		{
			Game game = obj as Game;
			if (game.rules != null && game.rules.main_goal == "DestroyKingdom")
			{
				return game.rules.targetKingdom != null;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Game game = obj as Game;
			targetKingdomName = game.rules.targetKingdom?.Name ?? null;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(targetKingdomName, "targetKingdomName");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			targetKingdomName = ser.ReadStr("targetKingdomName");
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			if (game.rules == null)
			{
				game.rules = new CampaignRules(game);
			}
			game.rules.targetKingdom = game.GetKingdom(targetKingdomName);
			game.NotifyListeners("destroy_kingdom_target_selected", game.rules.targetKingdom);
		}
	}

	[Serialization.State(17)]
	public class PopMajorityUpdateState : Serialization.ObjectState
	{
		public int last_pop_majority_update_realm_id;

		public float next_pop_majority_update_time;

		public static PopMajorityUpdateState Create()
		{
			return new PopMajorityUpdateState();
		}

		public static bool IsNeeded(Object obj)
		{
			Game game = obj as Game;
			if (!(game.next_pop_majority_update_time != Time.Zero))
			{
				return game.last_pop_majority_update_realm_id != 0;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Game game = obj as Game;
			last_pop_majority_update_realm_id = game.last_pop_majority_update_realm_id;
			next_pop_majority_update_time = game.next_pop_majority_update_time - game.time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(last_pop_majority_update_realm_id, "last_pop_majority_update_realm_id");
			ser.WriteFloat(next_pop_majority_update_time, "next_pop_majority_update_time");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			last_pop_majority_update_realm_id = ser.Read7BitUInt("last_pop_majority_update_realm_id");
			next_pop_majority_update_time = ser.ReadFloat("next_pop_majority_update_time");
		}

		public override void ApplyTo(Object obj)
		{
			Game obj2 = obj as Game;
			obj2.last_pop_majority_update_realm_id = last_pop_majority_update_realm_id;
			obj2.next_pop_majority_update_time = obj2.time + next_pop_majority_update_time;
		}
	}

	[Serialization.State(18)]
	public class GameSpeedState : Serialization.ObjectState
	{
		public float speed;

		public int pid;

		public static GameSpeedState Create()
		{
			return new GameSpeedState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Game game = obj as Game;
			speed = game.speed;
			pid = game.last_speed_control_pid;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(speed, "speed");
			ser.Write7BitUInt(pid + 1, "pid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			speed = ser.ReadFloat("speed");
			pid = ser.Read7BitUInt("pid") - 1;
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			if (!isLoadingSaveGame)
			{
				game.SetSpeed(speed, pid);
			}
		}
	}

	[Serialization.State(19)]
	public class PauseState : Serialization.ObjectState
	{
		public struct Request
		{
			public string id;

			public int pid;

			public float elapsed;

			public float timeout;
		}

		public struct Cooldown
		{
			public int pid;

			public float remaining;
		}

		public List<Request> requests;

		public int resume_pid;

		public List<Cooldown> cooldowns;

		public static PauseState Create()
		{
			return new PauseState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Game game = obj as Game;
			if (game?.pause == null)
			{
				return false;
			}
			requests = new List<Request>(game.pause.requests.Count);
			Time time = game.pause.time;
			for (int i = 0; i < game.pause.requests.Count; i++)
			{
				Pause.Request request = game.pause.requests[i];
				Request item = new Request
				{
					id = request.reason,
					pid = ((request.pid < 0) ? (-1) : request.pid),
					elapsed = time - request.paused_time,
					timeout = ((request.timeout_time == Time.Zero) ? (-1f) : (request.timeout_time - request.paused_time))
				};
				requests.Add(item);
			}
			resume_pid = -2;
			if (game.pause.Resume != null)
			{
				resume_pid = ((game.pause.Resume.pid < 0) ? (-1) : game.pause.Resume.pid);
			}
			for (int j = 0; j < game.pause.cooldowns.Length; j++)
			{
				Time time2 = game.pause.cooldowns[j];
				if (!(time2 == Time.Zero) && !(time2 <= time))
				{
					if (cooldowns == null)
					{
						cooldowns = new List<Cooldown>();
					}
					cooldowns.Add(new Cooldown
					{
						pid = j + 1,
						remaining = time2 - time
					});
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((requests != null) ? requests.Count : 0);
			ser.Write7BitUInt(num, "requests");
			for (int i = 0; i < num; i++)
			{
				Request request = requests[i];
				ser.WriteStr(request.id, "id", i);
				ser.Write7BitUInt(request.pid + 1, "pid", i);
				ser.WriteFloat(request.elapsed, "elapsed", i);
				ser.WriteFloat(request.timeout, "timeout", i);
			}
			ser.Write7BitUInt(resume_pid + 2, "resume_pid");
			num = ((cooldowns != null) ? cooldowns.Count : 0);
			ser.Write7BitUInt(num, "cooldowns");
			for (int j = 0; j < num; j++)
			{
				Cooldown cooldown = cooldowns[j];
				ser.Write7BitUInt(cooldown.pid, "cd_pid", j);
				ser.WriteFloat(cooldown.remaining, "cd_remaining", j);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("requests");
			if (num > 0)
			{
				requests = new List<Request>(num);
				for (int i = 0; i < num; i++)
				{
					string text = ser.ReadStr("id", i);
					int pid = ser.Read7BitUInt("pid", i) - 1;
					float elapsed = ser.ReadFloat("elapsed", i);
					float timeout = ser.ReadFloat("timeout", i);
					Request item = new Request
					{
						id = text,
						pid = pid,
						elapsed = elapsed,
						timeout = timeout
					};
					requests.Add(item);
				}
			}
			if (Serialization.cur_version >= 16)
			{
				resume_pid = ser.Read7BitUInt("resume_pid") - 2;
			}
			num = ser.Read7BitUInt("cooldowns");
			if (num > 0)
			{
				cooldowns = new List<Cooldown>(num);
				for (int j = 0; j < num; j++)
				{
					int pid2 = ser.Read7BitUInt("cd_pid", j);
					float remaining = ser.ReadFloat("cd_remaining", j);
					Cooldown item2 = new Cooldown
					{
						pid = pid2,
						remaining = remaining
					};
					cooldowns.Add(item2);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (isLoadingSaveGame)
			{
				return;
			}
			Game game = obj as Game;
			if (game?.pause == null)
			{
				return;
			}
			game.pause.Reset(apply: false);
			Time time = game.pause.time;
			if (requests != null)
			{
				for (int i = 0; i < requests.Count; i++)
				{
					Request request = requests[i];
					Pause.Request.Def def = game.pause.FindDef(request.id);
					if (def == null)
					{
						Log("Unknown pause request '" + request.id + "' ignored", LogType.Warning);
						continue;
					}
					Time time2 = time - request.elapsed;
					Time timeout_time = ((request.timeout < 0f) ? Time.Zero : (time2 + request.timeout));
					Pause.Request item = new Pause.Request
					{
						pause = game.pause,
						def = def,
						pid = request.pid,
						paused_time = time2,
						timeout_time = timeout_time
					};
					game.pause.requests.Add(item);
				}
			}
			if (resume_pid > -2)
			{
				Pause.Request.Def def2 = game.pause.FindDef("GameResumed");
				if (def2 != null)
				{
					game.pause.Resume = new Pause.Request
					{
						pause = game.pause,
						def = def2,
						pid = resume_pid
					};
				}
			}
			if (cooldowns != null)
			{
				for (int j = 0; j < cooldowns.Count; j++)
				{
					Cooldown cooldown = cooldowns[j];
					Time time3 = time + cooldown.remaining;
					game.pause.cooldowns[cooldown.pid - 1] = time3;
				}
			}
			game.pause.Update();
		}
	}

	[Serialization.State(20)]
	public class GreatPowersState : Serialization.ObjectState
	{
		public List<NID> great_power_kingdoms = new List<NID>();

		public static GreatPowersState Create()
		{
			return new GreatPowersState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			List<Kingdom> list = (obj as Game).great_powers.TopKingdoms();
			for (int i = 0; i < list.Count; i++)
			{
				great_power_kingdoms.Add(list[i]);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(great_power_kingdoms.Count, "count");
			for (int i = 0; i < great_power_kingdoms.Count; i++)
			{
				ser.WriteNID(great_power_kingdoms[i], "kingdom_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				great_power_kingdoms.Add(ser.ReadNID("kingdom_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			List<Kingdom> list = new List<Kingdom>();
			for (int i = 0; i < great_power_kingdoms.Count; i++)
			{
				if (great_power_kingdoms[i].GetObj(game) is Kingdom item)
				{
					list.Add(item);
				}
			}
			game.great_powers.SetTopKingdoms(list, send_state: false);
		}
	}

	[Serialization.State(21)]
	public class GameLoadedState : Serialization.ObjectState
	{
		public static GameLoadedState Create()
		{
			return new GameLoadedState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Game).OnGameStarted("load_game");
		}
	}

	[Serialization.State(22)]
	public class KingdomRankingsState : Serialization.ObjectState
	{
		private struct Row
		{
			public int kingdom_id;

			public float score;

			public int rank;

			public int rank_group;

			public int fame;
		}

		private List<List<Row>> rankingsData = new List<List<Row>>();

		public static KingdomRankingsState Create()
		{
			return new KingdomRankingsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			List<KingdomRanking> rankings = (obj as Game).rankings.rankings;
			for (int i = 0; i < rankings.Count; i++)
			{
				rankingsData.Add(new List<Row>());
				KingdomRanking kingdomRanking = rankings[i];
				for (int j = 0; j < kingdomRanking.rows.Count; j++)
				{
					KingdomRanking.Row row = kingdomRanking.rows[j];
					rankingsData[i].Add(new Row
					{
						kingdom_id = row.kingdom.id,
						score = row.score,
						rank = row.rank,
						rank_group = row.rank_group,
						fame = row.fame
					});
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(rankingsData.Count, "rankings_count");
			for (int i = 0; i < rankingsData.Count; i++)
			{
				List<Row> list = rankingsData[i];
				ser.Write7BitUInt(list.Count, "ranking_row_count_" + i);
				for (int j = 0; j < list.Count; j++)
				{
					Row row = list[j];
					ser.Write7BitUInt(row.kingdom_id, "kingdom_id_" + i + "_" + j);
					ser.WriteFloat(row.score, "score_" + i + "_" + j);
					ser.Write7BitUInt(row.rank, "rank_" + i + "_" + j);
					ser.Write7BitUInt(row.rank_group, "rank_group_" + i + "_" + j);
					ser.Write7BitUInt(row.fame, "fame_" + i + "_" + j);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("rankings_count");
			for (int i = 0; i < num; i++)
			{
				List<Row> list = new List<Row>();
				rankingsData.Add(list);
				int num2 = ser.Read7BitUInt("ranking_row_count_" + i);
				for (int j = 0; j < num2; j++)
				{
					list.Add(new Row
					{
						kingdom_id = ser.Read7BitUInt("kingdom_id_" + i + "_" + j),
						score = ser.ReadFloat("score_" + i + "_" + j),
						rank = ser.Read7BitUInt("rank_" + i + "_" + j),
						rank_group = ser.Read7BitUInt("rank_group_" + i + "_" + j),
						fame = ser.Read7BitUInt("fame_" + i + "_" + j)
					});
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			List<KingdomRanking> rankings = game.rankings.rankings;
			if (rankingsData.Count != rankings.Count)
			{
				Log("Incorrect amount of rankings: " + rankingsData.Count, LogType.Error);
				return;
			}
			for (int i = 0; i < rankings.Count; i++)
			{
				List<Row> list = rankingsData[i];
				KingdomRanking kingdomRanking = rankings[i];
				kingdomRanking.rows.Clear();
				for (int j = 0; j < list.Count; j++)
				{
					Row row = list[j];
					KingdomRanking.Row item = new KingdomRanking.Row
					{
						kingdom = game.GetKingdom(row.kingdom_id),
						score = row.score,
						rank = row.rank,
						rank_group = row.rank_group,
						fame = row.fame
					};
					kingdomRanking.rows.Add(item);
				}
			}
		}
	}

	[Serialization.State(23)]
	public class CheatLevelState : Serialization.ObjectState
	{
		private int cheat_level;

		public static CheatLevelState Create()
		{
			return new CheatLevelState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			cheat_level = (int)Game.cheat_level;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(cheat_level, "cheat_level");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			cheat_level = ser.Read7BitUInt("cheat_level");
		}

		public override void ApplyTo(Object obj)
		{
			if (!isLoadingSaveGame)
			{
				Game.cheat_level = (CheatLevel)cheat_level;
			}
		}
	}

	[Serialization.State(24)]
	public class ProConStatsState : Serialization.ObjectState
	{
		private Dictionary<string, ProsAndCons.Tracker.Track_OfferStats> stats = new Dictionary<string, ProsAndCons.Tracker.Track_OfferStats>();

		private Dictionary<string, ProsAndCons.Tracker.Track_OfferStats> stats_player = new Dictionary<string, ProsAndCons.Tracker.Track_OfferStats>();

		public static ProConStatsState Create()
		{
			return new ProConStatsState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Game game))
			{
				return false;
			}
			return game.GetDevSettingsDef().track_stats;
		}

		private void CopyStats(Dictionary<string, ProsAndCons.Tracker.Track_OfferStats> src, Dictionary<string, ProsAndCons.Tracker.Track_OfferStats> dest)
		{
			if (src == null || dest == null)
			{
				return;
			}
			dest.Clear();
			foreach (KeyValuePair<string, ProsAndCons.Tracker.Track_OfferStats> item in src)
			{
				ProsAndCons.Tracker.Track_OfferStats value = new ProsAndCons.Tracker.Track_OfferStats
				{
					thresholds = new Dictionary<string, ProsAndCons.Tracker.Track_ThresholdStats>()
				};
				foreach (KeyValuePair<string, ProsAndCons.Tracker.Track_ThresholdStats> threshold in item.Value.thresholds)
				{
					ProsAndCons.Tracker.Track_ThresholdStats value2 = new ProsAndCons.Tracker.Track_ThresholdStats
					{
						pass = new Dictionary<string, long>(),
						fail = new Dictionary<string, long>(),
						count_pass = threshold.Value.count_pass,
						count_fail = threshold.Value.count_fail,
						count = threshold.Value.count
					};
					foreach (KeyValuePair<string, long> item2 in threshold.Value.pass)
					{
						value2.pass.Add(item2.Key, item2.Value);
					}
					foreach (KeyValuePair<string, long> item3 in threshold.Value.fail)
					{
						value2.fail.Add(item3.Key, item3.Value);
					}
					value.thresholds.Add(threshold.Key, value2);
				}
				dest.Add(item.Key, value);
			}
		}

		public override bool InitFrom(Object obj)
		{
			CopyStats(ProsAndCons.Tracker.stats, stats);
			CopyStats(ProsAndCons.Tracker.stats_player, stats_player);
			if (stats.Count == 0)
			{
				return stats_player.Count != 0;
			}
			return true;
		}

		public void WriteBodyStats(Serialization.IWriter ser, Dictionary<string, ProsAndCons.Tracker.Track_OfferStats> stats, string statsName)
		{
			ser.Write7BitUInt(stats.Count, statsName + "_count");
			int num = 0;
			foreach (KeyValuePair<string, ProsAndCons.Tracker.Track_OfferStats> stat in stats)
			{
				string text = statsName + "_" + num;
				ser.WriteStr(stat.Key, text + "_name");
				ser.Write7BitUInt((int)stat.Value.count, text + "_count");
				ser.Write7BitUInt(stat.Value.thresholds.Count, text + "_tresholds_count");
				int num2 = 0;
				foreach (KeyValuePair<string, ProsAndCons.Tracker.Track_ThresholdStats> threshold in stat.Value.thresholds)
				{
					string text2 = text + "_treshold_" + num2;
					ser.WriteStr(threshold.Key, text2 + "_name");
					ser.Write7BitUInt((int)threshold.Value.count_pass, text2 + "_count_pass");
					ser.Write7BitUInt((int)threshold.Value.count_fail, text2 + "_count_fail");
					ser.Write7BitUInt((int)threshold.Value.count, text2 + "_count");
					ser.Write7BitUInt(threshold.Value.pass.Count, text2 + "_pass_count");
					int num3 = 0;
					foreach (KeyValuePair<string, long> item in threshold.Value.pass)
					{
						string text3 = text2 + "_pass_" + num3;
						ser.WriteStr(item.Key, text3 + "_name");
						ser.Write7BitUInt((int)item.Value, text3 + "_points");
						num3++;
					}
					ser.Write7BitUInt(threshold.Value.fail.Count, text2 + "_fail_count");
					int num4 = 0;
					foreach (KeyValuePair<string, long> item2 in threshold.Value.fail)
					{
						string text4 = text2 + "_fail_" + num4;
						ser.WriteStr(item2.Key, text4 + "_name");
						ser.Write7BitUInt((int)item2.Value, text4 + "_points");
						num4++;
					}
					num2++;
				}
				num++;
			}
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			WriteBodyStats(ser, stats, "stats");
			WriteBodyStats(ser, stats_player, "stats_player");
		}

		public void ReadBodyStats(Serialization.IReader ser, Dictionary<string, ProsAndCons.Tracker.Track_OfferStats> stats, string statsName)
		{
			stats.Clear();
			int num = ser.Read7BitUInt(statsName + "_count");
			for (int i = 0; i < num; i++)
			{
				string text = statsName + "_" + i;
				ProsAndCons.Tracker.Track_OfferStats value = new ProsAndCons.Tracker.Track_OfferStats
				{
					thresholds = new Dictionary<string, ProsAndCons.Tracker.Track_ThresholdStats>()
				};
				string key = ser.ReadStr(text + "_name");
				value.count = ser.Read7BitUInt(text + "_count");
				int num2 = ser.Read7BitUInt(text + "_tresholds_count");
				for (int j = 0; j < num2; j++)
				{
					string text2 = text + "_treshold_" + j;
					ProsAndCons.Tracker.Track_ThresholdStats value2 = new ProsAndCons.Tracker.Track_ThresholdStats
					{
						pass = new Dictionary<string, long>(),
						fail = new Dictionary<string, long>()
					};
					string key2 = ser.ReadStr(text2 + "_name");
					value2.count_pass = ser.Read7BitUInt(text2 + "_count_pass");
					value2.count_fail = ser.Read7BitUInt(text2 + "_count_fail");
					value2.count = ser.Read7BitUInt(text2 + "_count");
					int num3 = ser.Read7BitUInt(text2 + "_pass_count");
					for (int k = 0; k < num3; k++)
					{
						string text3 = text2 + "_pass_" + k;
						value2.pass.Add(ser.ReadStr(text3 + "_name"), ser.Read7BitUInt(text3 + "_points"));
					}
					int num4 = ser.Read7BitUInt(text2 + "_fail_count");
					for (int l = 0; l < num4; l++)
					{
						string text4 = text2 + "_fail_" + l;
						value2.fail.Add(ser.ReadStr(text4 + "_name"), ser.Read7BitUInt(text4 + "_points"));
					}
					value.thresholds.Add(key2, value2);
				}
				stats.Add(key, value);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			ReadBodyStats(ser, stats, "stats");
			ReadBodyStats(ser, stats_player, "stats_player");
		}

		public override void ApplyTo(Object obj)
		{
			CopyStats(stats, ProsAndCons.Tracker.stats);
			CopyStats(stats_player, ProsAndCons.Tracker.stats_player);
		}
	}

	[Serialization.State(25)]
	public class DevSettingsState : Serialization.ObjectState
	{
		private bool gai;

		private bool rai;

		private float apd;

		public static DevSettingsState Create()
		{
			return new DevSettingsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Game game = obj as Game;
			gai = game.ai.enabled;
			rai = Rebel.enabled;
			apd = Action.force_prepare_duration;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(gai, "gai");
			ser.WriteBool(rai, "rai");
			ser.WriteFloat(apd, "apd");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			gai = ser.ReadBool("gai");
			rai = ser.ReadBool("rai");
			apd = ser.ReadFloat("apd");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Game).ai.enabled = gai;
			Rebel.enabled = rai;
			Action.force_prepare_duration = apd;
		}
	}

	[Serialization.State(26)]
	public class ActionStatsState : Serialization.ObjectState
	{
		private Dictionary<string, Action.Tracker.Track_ActionStats> stats = new Dictionary<string, Action.Tracker.Track_ActionStats>();

		public static ActionStatsState Create()
		{
			return new ActionStatsState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Game game))
			{
				return false;
			}
			return game.GetDevSettingsDef().track_stats;
		}

		private void CopyStats(Dictionary<string, Action.Tracker.Track_ActionStats> src, Dictionary<string, Action.Tracker.Track_ActionStats> dest)
		{
			if (src == null || dest == null)
			{
				return;
			}
			dest.Clear();
			foreach (KeyValuePair<string, Action.Tracker.Track_ActionStats> item in src)
			{
				dest.Add(value: new Action.Tracker.Track_ActionStats
				{
					has_outcomes = item.Value.has_outcomes,
					disabled_in_ai = item.Value.disabled_in_ai,
					count = item.Value.count,
					count_success = item.Value.count_success,
					count_fail = item.Value.count_fail,
					count_running = item.Value.count_running
				}, key: item.Key);
			}
		}

		public override bool InitFrom(Object obj)
		{
			CopyStats(Action.Tracker.stats, stats);
			return stats.Count != 0;
		}

		public void WriteBodyStats(Serialization.IWriter ser, Dictionary<string, Action.Tracker.Track_ActionStats> stats, string statsName)
		{
			ser.Write7BitUInt(stats.Count, statsName + "_count");
			int num = 0;
			foreach (KeyValuePair<string, Action.Tracker.Track_ActionStats> stat in stats)
			{
				string text = statsName + "_" + num;
				ser.WriteStr(stat.Key, text + "_name");
				ser.WriteBool(stat.Value.has_outcomes, text + "_has_outcomes");
				ser.WriteBool(stat.Value.disabled_in_ai, text + "_disabled_in_ai");
				ser.Write7BitUInt((int)stat.Value.count, text + "_count");
				ser.Write7BitUInt((int)stat.Value.count_success, text + "_count_success");
				ser.Write7BitUInt((int)stat.Value.count_fail, text + "_count_fail");
				ser.Write7BitUInt((int)Math.Max(0L, stat.Value.count_running), text + "_count_running");
				num++;
			}
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			WriteBodyStats(ser, stats, "stats");
		}

		public void ReadBodyStats(Serialization.IReader ser, Dictionary<string, Action.Tracker.Track_ActionStats> stats, string statsName)
		{
			stats.Clear();
			int num = ser.Read7BitUInt(statsName + "_count");
			for (int i = 0; i < num; i++)
			{
				string text = statsName + "_" + i;
				Action.Tracker.Track_ActionStats value = default(Action.Tracker.Track_ActionStats);
				string key = ser.ReadStr(text + "_name");
				value.has_outcomes = ser.ReadBool(text + "_has_outcomes");
				value.disabled_in_ai = ser.ReadBool(text + "_disabled_in_ai");
				value.count = ser.Read7BitUInt(text + "_count");
				value.count_success = ser.Read7BitUInt(text + "_count_success");
				value.count_fail = ser.Read7BitUInt(text + "_count_fail");
				value.count_running = ser.Read7BitUInt(text + "_count_running");
				stats.Add(key, value);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			ReadBodyStats(ser, stats, "stats");
		}

		public override void ApplyTo(Object obj)
		{
			CopyStats(stats, Action.Tracker.stats);
		}
	}

	[Serialization.State(27)]
	public class TradeCentersTimesState : Serialization.ObjectState
	{
		public float next_sd_delta;

		public float next_refresh_delta;

		public static TradeCentersTimesState Create()
		{
			return new TradeCentersTimesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Game game))
			{
				return false;
			}
			next_sd_delta = game.economy.nextTCSpawnDespawn - game.time;
			next_refresh_delta = game.economy.nextTCRefresh - game.time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(next_sd_delta, "next_sd_delta");
			ser.WriteFloat(next_refresh_delta, "next_refresh_delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			next_sd_delta = ser.ReadFloat("next_sd_delta");
			next_refresh_delta = ser.ReadFloat("next_refresh_delta");
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			if (game?.economy != null)
			{
				game.economy.nextTCSpawnDespawn = game.time + next_sd_delta;
				game.economy.nextTCRefresh = game.time + next_refresh_delta;
			}
		}
	}

	[Serialization.Event(27)]
	public class GameSpeedEvent : Serialization.ObjectEvent
	{
		private float speed;

		public GameSpeedEvent()
		{
		}

		public static GameSpeedEvent Create()
		{
			return new GameSpeedEvent();
		}

		public GameSpeedEvent(float speed)
		{
			this.speed = speed;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(speed, "speed");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			speed = ser.ReadFloat("speed");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Game).SetSpeed(speed, sender.playerData.pid);
		}
	}

	[Serialization.Event(28)]
	public class GamePauseEvent : Serialization.ObjectEvent
	{
		public bool pause;

		public string reason;

		public GamePauseEvent()
		{
		}

		public static GamePauseEvent Create()
		{
			return new GamePauseEvent();
		}

		public GamePauseEvent(bool pause, string reason = null)
		{
			this.pause = pause;
			this.reason = reason;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(pause, "pause");
			ser.WriteStr(reason, "reason");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			pause = ser.ReadBool("pause");
			reason = ser.ReadStr("reason");
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			if (game?.pause != null)
			{
				if (reason == "GameResumed")
				{
					game.pause.RefreshResumeRequest(sender.playerData.pid);
				}
				else if (pause)
				{
					game.pause.AddRequest(reason, sender.playerData.pid);
				}
				else
				{
					game.pause.DelRequest(reason, sender.playerData.pid);
				}
			}
		}
	}

	[Serialization.Event(29)]
	public class SwitchKingdomEvent : Serialization.ObjectEvent
	{
		private int pid;

		private int kid;

		private bool repick;

		public SwitchKingdomEvent()
		{
		}

		public static SwitchKingdomEvent Create()
		{
			return new SwitchKingdomEvent();
		}

		public SwitchKingdomEvent(int pid, int kid, bool repick)
		{
			this.pid = pid;
			this.kid = kid;
			this.repick = repick;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(pid, "pid");
			ser.Write7BitUInt(kid, "kid");
			ser.WriteBool(repick, "repick");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			pid = ser.Read7BitUInt("pid");
			kid = ser.Read7BitUInt("kid");
			repick = ser.ReadBool("repick");
		}

		public override void ApplyTo(Object obj)
		{
			Game obj2 = obj as Game;
			obj2.SetAnyKingdom(kNew: obj2.GetKingdom(kid), pid: pid, repick: repick);
		}
	}

	[Serialization.Event(30)]
	public class PingEvent : Serialization.ObjectEvent
	{
		public int uid;

		public int last_round_trip_ms;

		public PingEvent()
		{
		}

		public static PingEvent Create()
		{
			return new PingEvent();
		}

		public PingEvent(int uid, int last_round_trip_ms)
		{
			this.uid = uid;
			this.last_round_trip_ms = last_round_trip_ms;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(uid, "uid");
			ser.Write7BitUInt(last_round_trip_ms, "last_round_trip_ms");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			uid = ser.Read7BitUInt("uid");
			last_round_trip_ms = ser.Read7BitUInt("last_round_trip_ms");
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			if (game?.pings != null)
			{
				game.pings.OnPing(sender, uid, last_round_trip_ms);
			}
		}
	}

	[Serialization.Event(31)]
	public class PongEvent : Serialization.ObjectEvent
	{
		public int uid;

		public PongEvent()
		{
		}

		public static PongEvent Create()
		{
			return new PongEvent();
		}

		public PongEvent(int uid)
		{
			this.uid = uid;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(uid, "uid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			uid = ser.Read7BitUInt("uid");
		}

		public override void ApplyTo(Object obj)
		{
			Game game = obj as Game;
			if (game?.pings != null)
			{
				game.pings.OnPong(uid);
			}
		}
	}

	public string type;

	public State state;

	public bool isInVideoMode;

	public DT dt;

	public Defs defs;

	public string defs_map;

	public Dictionary<string, Timer.Def> timer_defs;

	private IEngine engine;

	public static Action<string> fnBeginProfile = null;

	public static Action<string> fnEndProfile = null;

	public static Stopwatch prof_timer = null;

	public Scheduler scheduler;

	public float session_time_offset;

	public float unscaled_unpaused_time_acc;

	public float unscaled_total_time_acc;

	public Scheduler.UpdateBatch update_half_sec;

	public Scheduler.UpdateBatch update_1sec;

	public Scheduler.UpdateBatch update_5sec;

	public Scheduler.UpdateBatch update_10sec;

	private System.Random rnd = new System.Random();

	public Campaign campaign;

	public GameRules game_rules;

	public Stats stats;

	public IListener analytics;

	public static Vars anal_vars = new Vars();

	public IListener tutorial_listener;

	public List<Game> subgames = new List<Game>();

	public Vars vars = new Vars();

	public uint num_created_objects;

	public Object first_object;

	public Object last_object;

	public Object cur_obj;

	public int num_objects;

	public Dictionary<Type, int> num_objects_by_type = new Dictionary<Type, int>();

	public static bool isDefaultLobbyFilterEnabled = true;

	public static bool isComingFromTitle = false;

	public static bool isJoiningGame = false;

	public static bool isLoadingSaveGame = false;

	public static bool fullGameStateReceived = false;

	public static bool isLoadingSaveGameForClientOnly = false;

	public bool mapLoaded;

	public Time time_unscaled = Time.Zero;

	public Time real_time_played = Time.Zero;

	public Time real_time_total = Time.Zero - 1f;

	public Time real_time_total_per_frame = Time.Zero - 1f;

	public long updates;

	public const string save_name_none = "no-name";

	public string save_name = "no-name";

	private static string persistent_data_path = null;

	private static string project_name = null;

	public List<Object> starts = new List<Object>();

	public string map_name;

	public string map_period;

	public string map_from_save_id;

	public int political_data_session_time;

	public Point world_size = Point.Invalid;

	public List<Realm> realms;

	public List<Kingdom> kingdoms;

	public Dictionary<string, Kingdom> kingdoms_by_name;

	public DT.Field kingdoms_csv;

	public int landRealmsCount;

	public short[,] realm_id_map;

	public PathFinding path_finding;

	public HeightsGrid heights = new HeightsGrid();

	public PassabilityGrid passability = new PassabilityGrid();

	public TerrainTypesInfo terrain_types;

	public ClimateZoneInfo climate_zones;

	public CampaignRules rules;

	public Economy economy;

	public Religions religions;

	public Cultures cultures;

	public EmperorOfTheWorld emperorOfTheWorld;

	public AI ai;

	public KingdomRankings rankings;

	public GreatPowers great_powers;

	public Dictionary<Resource.Def, List<int>> kingdoms_with_resource = new Dictionary<Resource.Def, List<int>>();

	public List<Rebellion> rebellions;

	public int last_pop_majority_update_realm_id;

	public Time next_pop_majority_update_time = Time.Zero;

	public Teams teams;

	public int seed;

	public bool fow;

	public Pings pings;

	public float speed = 1f;

	public Pause pause;

	public int last_speed_control_pid = -1;

	public int kingdoms_at_war;

	public bool new_game;

	public LoadedGameType load_game = LoadedGameType.Invalid;

	public bool send_analytics = true;

	private static Queue<Realm> tmp_que_realms = new Queue<Realm>();

	private static Queue<Kingdom> tmp_que_kingdoms = new Queue<Kingdom>();

	private static Queue<int> tmp_distances = new Queue<int>();

	private static HashSet<Realm> tmp_processed_realms = new HashSet<Realm>();

	private static HashSet<Kingdom> tmp_processed_kingdoms = new HashSet<Kingdom>();

	private bool force_endless_game;

	private DevSettings.Def dev_settings;

	public static CheatLevel cheat_level = CheatLevel.None;

	private static string branch_name = null;

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public static string defs_path => "defs/";

	public Multiplayer multiplayer { get; private set; }

	public Time time => scheduler.Time;

	public float time_f => time - Time.Zero;

	public long frame => scheduler.Frame;

	public Time session_time => time + session_time_offset;

	public static string maps_path => "maps/";

	public Game(string type, IEngine engine)
		: base((Game)null)
	{
		if (prof_timer == null)
		{
			prof_timer = Stopwatch.StartNew();
		}
		scheduler = new Scheduler(this);
		update_half_sec = new Scheduler.UpdateBatch(scheduler, "HalfSec", 0.5f);
		update_1sec = new Scheduler.UpdateBatch(scheduler, "1Sec", 1f);
		update_5sec = new Scheduler.UpdateBatch(scheduler, "5Sec", 5f);
		update_10sec = new Scheduler.UpdateBatch(scheduler, "10Sec", 10f);
		this.type = type;
		game = this;
		this.engine = engine;
		visuals = engine;
		Serialization.Init();
		defs = new Defs(this);
		timer_defs = new Dictionary<string, Timer.Def>();
		OnInit();
	}

	public Game(string type, Game base_game)
		: base((Game)null)
	{
		scheduler = new Scheduler(this);
		update_half_sec = new Scheduler.UpdateBatch(scheduler, "HalfSec", 0.5f);
		update_1sec = new Scheduler.UpdateBatch(scheduler, "1Sec", 1f);
		update_5sec = new Scheduler.UpdateBatch(scheduler, "5Sec", 5f);
		update_10sec = new Scheduler.UpdateBatch(scheduler, "10Sec", 10f);
		this.type = type;
		game = base_game;
		engine = base_game.engine;
		visuals = base_game.visuals;
		dt = base_game.dt;
		defs = base_game.defs;
		timer_defs = base_game.timer_defs;
		Init();
		InitFrom(base_game);
		base_game.subgames.Add(this);
	}

	private void OnKingsGameStarted(string started_from)
	{
		if (IsAuthority())
		{
			pings?.OnGameStarted();
			campaign?.UpdatePlayersAI(this);
		}
		campaign.AddVarsListener(this);
		OnGameStartedFame(started_from);
		great_powers.TopKingdoms(!great_powers.IsRegisteredForUpdate());
	}

	public void OnGameStarted(string started_from)
	{
		DelayedDestroyTimer.OnGameStarted(this, started_from);
		OnKingsGameStarted(started_from);
	}

	public override string ToString()
	{
		return "[" + state.ToString() + "]" + type;
	}

	public bool IsMain()
	{
		if (game != null)
		{
			return game == this;
		}
		return true;
	}

	public Game GetMain()
	{
		Game game = this;
		while (!game.IsMain())
		{
			game = game.game;
		}
		return game;
	}

	public bool IsQuitting()
	{
		return state == State.Quitting;
	}

	public bool IsRunning()
	{
		if (state == State.Running)
		{
			return !isLoadingSaveGame;
		}
		return false;
	}

	public void UnloadMap()
	{
		NotifyListeners("unloading_map");
		map_name = null;
		realms = null;
		landRealmsCount = 0;
		game_rules = null;
		if (rules != null)
		{
			rules.Unload();
		}
		if (type != "battle_view")
		{
			kingdoms = null;
			kingdoms_by_name = null;
		}
		if (path_finding != null)
		{
			path_finding.Destroy();
			path_finding = null;
		}
		terrain_types = null;
		climate_zones = null;
		realm_id_map = null;
		heights.Dispose();
		passability.Dispose();
		world_size = Point.Invalid;
		speed = 1f;
		last_speed_control_pid = -1;
		pause?.Reset(apply: false);
		kingdoms_at_war = 0;
		cur_obj = first_object;
		while (cur_obj != null)
		{
			Object obj = cur_obj;
			cur_obj = cur_obj.next_in_game;
			if (base.obj_state == ObjState.Destroying)
			{
				obj.Destroy(forceDestroyInstantly: true);
			}
			else
			{
				obj.OnUnloadMap();
			}
		}
		SetNid(0, update_registry: false);
		if (ai != null)
		{
			ai.enabled = true;
		}
		Rebel.enabled = true;
		Action.force_prepare_duration = 0f;
		fullGameStateReceived = false;
		ShutdownComponents();
	}

	public void OnQuitGameAnalytics(string reason)
	{
		Vars vars = new Vars();
		vars.Set("gameName", save_name ?? "no-name");
		vars.Set("quitAction", reason);
		vars.Set("menuLocation", (GetLocalPlayerKingdom() != null) ? "in_game" : "title_screen");
		NotifyListeners("analytics_game_quit", vars);
	}

	public void Quit()
	{
		OnQuitGameAnalytics("quit");
		NotifyListeners("quitting");
		state = State.Quitting;
		campaign?.DelVarsListener(this);
		CampaignUtils.ClearMultiplayerRegistry(multiplayer);
		if (multiplayer != null)
		{
			multiplayer.initial_waiting_finished = false;
		}
		DestroyMultiplayer();
		Multiplayer.CurrentPlayers.Clear();
		if (rules != null)
		{
			rules.Unload();
		}
		UnloadMap();
	}

	public Multiplayer CreateMultiplayer(Multiplayer.Type type, int player_index)
	{
		if (!IsMain())
		{
			return multiplayer;
		}
		if (multiplayer != null)
		{
			Error($"Replacing existing multiplayer {multiplayer}");
			DestroyMultiplayer();
		}
		multiplayer = new Multiplayer(this, type);
		multiplayer.connectionReason = Multiplayer.ConnectionReason.InGame;
		multiplayer.SetPid(player_index + 1);
		if (type == Multiplayer.Type.Server)
		{
			Multiplayer.CurrentPlayers.Add(multiplayer.playerData);
		}
		return multiplayer;
	}

	public void DestroyMultiplayer()
	{
		if (multiplayer != null)
		{
			multiplayer.ShutDown();
			multiplayer = null;
		}
	}

	public Timer StartTimer(string name, float duration, bool restart = false)
	{
		return Timer.Start(this, name, duration, restart);
	}

	public override void OnTimer(Timer timer)
	{
		NotifyListeners("on_timer", timer);
		string name = timer.name;
		if (name == "session_time_sync")
		{
			if (IsAuthority())
			{
				SendState<SessionTimeState>();
				Timer.Start(this, "session_time_sync", 10f, restart: true);
			}
		}
		else
		{
			base.OnTimer(timer);
		}
	}

	public static float map(float val, float vmin, float vmax, float rmin, float rmax)
	{
		return rmin + (val - vmin) / (vmax - vmin) * (rmax - rmin);
	}

	public static float clamp(float val, float vmin, float vmax)
	{
		if (val < vmin)
		{
			return vmin;
		}
		if (val > vmax)
		{
			return vmax;
		}
		return val;
	}

	public static float map_clamp(float val, float vmin, float vmax, float rmin, float rmax)
	{
		val = clamp(val, vmin, vmax);
		return rmin + (val - vmin) / (vmax - vmin) * (rmax - rmin);
	}

	public static float map_clamp_pow(float val, float vmin, float vmax, float rmin, float rmax, float power)
	{
		val = clamp(val, vmin, vmax);
		float num = (float)Math.Pow((val - vmin) / (vmax - vmin), power);
		return rmin + (rmax - rmin) * num;
	}

	public int Random(int min, int max)
	{
		return rnd.Next(min, max);
	}

	public T Random<T>(List<T> v, Func<T, int> weight = null, bool alloc = true, T err = default(T))
	{
		int w;
		return Random(out w, v, weight, alloc, err);
	}

	public T Random<T>(out int w, List<T> v, Func<T, int> weight = null, bool alloc = true, T err = default(T))
	{
		w = 0;
		if (v == null)
		{
			return err;
		}
		int count = v.Count;
		if (count == 0)
		{
			return err;
		}
		if (weight == null)
		{
			return v[w = game.Random(0, count)];
		}
		int[] array = (alloc ? new int[count] : null);
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			int num2 = weight(v[i]);
			if (alloc)
			{
				array[i] = num2;
			}
			num += num2;
		}
		if (num == 0)
		{
			return err;
		}
		int num3 = game.Random(0, num);
		for (int j = 0; j < count; j++)
		{
			int num4 = (alloc ? array[j] : weight(v[j]));
			if (num3 < num4)
			{
				w = num4;
				return v[j];
			}
			num3 -= num4;
		}
		return err;
	}

	public T Random<T>(T err, (T option, int weight)[] v)
	{
		if (v == null)
		{
			return err;
		}
		int num = v.Length;
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			num2 += v[i].weight;
		}
		if (num2 == 0)
		{
			return err;
		}
		int num3 = game.Random(0, num2);
		for (int j = 0; j < num; j++)
		{
			if (num3 < v[j].weight)
			{
				return v[j].option;
			}
			num3 -= v[j].weight;
		}
		return err;
	}

	public float Random(float min, float max)
	{
		return (float)((double)min + rnd.NextDouble() * (double)(max - min));
	}

	public float Random100CheatedUp(float luck)
	{
		return 100f * (float)Math.Pow(Random(0f, 1f), 1f / luck);
	}

	public float Random100CheatedDown(float luck)
	{
		return 100f * (float)Math.Pow(Random(0f, 1f), luck);
	}

	public double Random()
	{
		return rnd.NextDouble();
	}

	public float Clamp(float v, float min, float max)
	{
		if (v < min)
		{
			return min;
		}
		if (v > max)
		{
			return max;
		}
		return v;
	}

	public static float CeilingByOffset(float v, int dot_offset)
	{
		double num = Math.Pow(10.0, dot_offset);
		return (float)(Math.Ceiling((double)v / num) * num);
	}

	public float Map(float v, float vmin, float vmax, float rmin, float rmax, bool clamp = false)
	{
		if (vmax == vmin)
		{
			return rmin;
		}
		if (clamp)
		{
			v = Clamp(v, vmin, vmax);
		}
		return rmin + (v - vmin) * (rmax - rmin) / (vmax - vmin);
	}

	public float Map3(float v, float vmin, float vmid, float vmax, float rmin, float rmid, float rmax, bool clamp = false)
	{
		if (vmax <= vmin)
		{
			return rmin;
		}
		if (clamp)
		{
			v = Clamp(v, vmin, vmax);
		}
		if (v < vmid)
		{
			vmax = vmid;
			rmax = rmid;
		}
		else
		{
			vmin = vmid;
			rmin = rmid;
		}
		if (vmax == vmin)
		{
			return rmin;
		}
		return rmin + (v - vmin) / (vmax - vmin) * (rmax - rmin);
	}

	public static bool Match(string s, string pattern, bool case_insensitive = false, char wildcard = '*')
	{
		if (string.IsNullOrEmpty(pattern))
		{
			return string.IsNullOrEmpty(s);
		}
		if (pattern.Length == 1 && pattern[0] == wildcard)
		{
			return true;
		}
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		if (pattern.Length == 2 && pattern[0] == wildcard && pattern[1] == wildcard)
		{
			return true;
		}
		return Match(s, 0, s.Length, pattern, 0, pattern.Length, case_insensitive, wildcard);
	}

	public static bool Match(string s, int idx, int end_idx, string pattern, int pat_idx, int pat_end_idx, bool case_insensitive, char wildcard)
	{
		while (true)
		{
			if (pat_idx >= pat_end_idx)
			{
				if (idx >= end_idx)
				{
					return true;
				}
				return false;
			}
			char c = pattern[pat_idx];
			if (c == wildcard)
			{
				break;
			}
			if (idx >= end_idx)
			{
				return false;
			}
			char c2 = s[idx];
			if (case_insensitive)
			{
				c = char.ToLowerInvariant(c);
				c2 = char.ToLowerInvariant(c2);
			}
			if (c != c2)
			{
				return false;
			}
			idx++;
			pat_idx++;
		}
		pat_idx++;
		while (pat_idx < pat_end_idx && pattern[pat_idx] == wildcard)
		{
			pat_idx++;
		}
		if (pat_idx >= pat_end_idx)
		{
			return true;
		}
		while (idx < end_idx)
		{
			if (Match(s, idx, end_idx, pattern, pat_idx, pat_end_idx, case_insensitive, wildcard))
			{
				return true;
			}
			idx++;
		}
		return false;
	}

	public void Log(Object obj, string msg, LogType type)
	{
		if (engine != null)
		{
			engine.Log(obj, msg, type);
			return;
		}
		if (obj != null)
		{
			msg = "[" + obj.ToString() + "]: " + msg;
		}
		Log(msg, type);
	}

	public static float ProfTicksToMillis(long ticks)
	{
		return (float)(ticks * 1000) / (float)Stopwatch.Frequency;
	}

	public static ProfileScope Profile(string section, bool log = false, float log_threshold = 0f, ProfileScope.Stats stats = null)
	{
		return new ProfileScope(section, log, log_threshold, stats);
	}

	public static void BeginProfileSection(string section)
	{
		if (fnBeginProfile != null)
		{
			fnBeginProfile(section);
		}
	}

	public static void EndProfileSection(string section)
	{
		if (fnEndProfile != null)
		{
			fnEndProfile(section);
		}
	}

	public static BenchmarkResult Benchmark(string name, System.Action action, long repetitions = -100L, bool use_profiler = true, bool log = true, bool calc_overhead = true)
	{
		long frequency = Stopwatch.Frequency;
		long num = frequency / 1000;
		using (ProfileScope profileScope = Profile("pre-warm", log: false, -1f))
		{
			for (int i = 0; i < 8; i++)
			{
				action();
				if (profileScope.Ticks >= num)
				{
					break;
				}
			}
		}
		GC.Collect();
		long num2 = ((repetitions < 0) ? (frequency * -repetitions / 1000) : long.MaxValue);
		if (repetitions < 0)
		{
			repetitions = long.MaxValue;
		}
		long num3 = 0L;
		long ticks;
		long allocated;
		using (ProfileScope profileScope2 = Profile(name, log: false, use_profiler ? 0f : (-1f)))
		{
			long totalMemory = GC.GetTotalMemory(forceFullCollection: false);
			do
			{
				num3++;
				action();
			}
			while (num3 < repetitions && profileScope2.Ticks < num2);
			ticks = profileScope2.Ticks;
			allocated = GC.GetTotalMemory(forceFullCollection: false) - totalMemory;
		}
		GC.Collect();
		long overhead_ticks = 0L;
		if (calc_overhead)
		{
			overhead_ticks = Benchmark("calc overhead", delegate
			{
			}, num3, use_profiler: false, log: false, calc_overhead: false).total_ticks;
		}
		BenchmarkResult result = new BenchmarkResult
		{
			name = name,
			reps = num3,
			total_ticks = ticks,
			overhead_ticks = overhead_ticks,
			allocated = allocated
		};
		if (log)
		{
			Log(result.ToString(), LogType.Message);
		}
		return result;
	}

	public void AddObject(Object obj)
	{
		if (++num_created_objects == 0)
		{
			Warning("Object UIDs overflow");
			num_created_objects++;
		}
		obj.uid = num_created_objects;
		if (last_object == null)
		{
			first_object = obj;
		}
		else
		{
			last_object.next_in_game = obj;
			obj.prev_in_game = last_object;
		}
		last_object = obj;
		num_objects++;
		Type type = obj.rtti.type;
		if (type != null)
		{
			int value = 0;
			num_objects_by_type.TryGetValue(type, out value);
			num_objects_by_type[type] = value + 1;
		}
	}

	public void RemoveObject(Object obj)
	{
		if (obj.next_in_game != null)
		{
			obj.next_in_game.prev_in_game = obj.prev_in_game;
		}
		else
		{
			last_object = obj.prev_in_game;
		}
		if (obj.prev_in_game != null)
		{
			obj.prev_in_game.next_in_game = obj.next_in_game;
		}
		else
		{
			first_object = obj.next_in_game;
		}
		obj.next_in_game = null;
		obj.prev_in_game = null;
		num_objects--;
		if (num_objects > 0 && first_object == null)
		{
			Error("Objects list messed up when deleting " + obj.ToString());
		}
		Type type = obj.rtti.type;
		if (type != null)
		{
			int value = 0;
			num_objects_by_type.TryGetValue(type, out value);
			if (value > 0)
			{
				num_objects_by_type[type] = value - 1;
			}
		}
	}

	public List<int> GetRandomIndexes(int num)
	{
		List<int> list = new List<int>(num);
		for (int i = 0; i < num; i++)
		{
			list.Add(i);
		}
		Shuffle(list);
		return list;
	}

	public void Shuffle<T>(IList<T> list)
	{
		for (int i = 0; i < list.Count - 1; i++)
		{
			Swap(list, i, Random(i, list.Count));
		}
	}

	public void Swap<T>(IList<T> list, int i, int j)
	{
		T value = list[i];
		list[i] = list[j];
		list[j] = value;
	}

	public Object FindObjectByUIDVerySlow(int uid)
	{
		for (Object obj = first_object; obj != null; obj = obj.next_in_game)
		{
			if (obj.uid == uid)
			{
				return obj;
			}
		}
		return null;
	}

	public int CountObjects()
	{
		int num = 0;
		for (Object obj = first_object; obj != null; obj = obj.next_in_game)
		{
			num++;
		}
		return num;
	}

	public bool SkipMapDef(string path, string name, bool is_directory)
	{
		switch (name)
		{
		case "kingdoms.csv":
		case "realms.csv":
		case "cultures.csv":
		case "coa.csv":
		case "camera_paths.def":
			return true;
		default:
			if (defs_map == null && name == "settlements.def")
			{
				char c = '/';
				if (!path.EndsWith(c + "europe" + c, StringComparison.Ordinal) && !path.EndsWith(c + "test" + c, StringComparison.Ordinal))
				{
					return true;
				}
				return false;
			}
			if (is_directory && name != defs_map && name != "europe" && name != "test" && name != "openfield" && !name.StartsWith("battle", StringComparison.Ordinal))
			{
				return true;
			}
			return false;
		}
	}

	public void LoadDefs(string map_name, bool profile = true)
	{
		if (dt == null)
		{
			dt = new DT();
			dt.context.game = this;
		}
		else
		{
			dt.Reset();
		}
		defs_map = map_name;
		CoAMapping.ClearAll();
		if (profile)
		{
			BeginProfileSection("Load defs");
		}
		dt.LoadDir(defs_path);
		if (profile)
		{
			EndProfileSection("Load defs");
		}
		if (profile)
		{
			BeginProfileSection("Load map defs");
		}
		dt.LoadDir(maps_path, SkipMapDef);
		if (profile)
		{
			EndProfileSection("Load map defs");
		}
		if (profile)
		{
			BeginProfileSection("Load mod defs");
		}
		dt.LoadActiveMods(SkipMapDef);
		if (profile)
		{
			EndProfileSection("Load mod defs");
		}
		NotifyListeners("defs_loaded");
		if (profile)
		{
			BeginProfileSection("DT.PostProcess");
		}
		dt.PostProcess();
		if (profile)
		{
			EndProfileSection("DT.PostProcess");
		}
		NotifyListeners("dt_processed");
		Campaign.campaign_vars_def = dt.Find("CampaignVars");
		Campaign.singleplayer_campaign_vars_def = dt.Find("SinglePlayerCampaignVars");
		if (profile)
		{
			BeginProfileSection("Load logic defs");
		}
		timer_defs.Clear();
		defs.Load();
		pause?.LoadDefs();
		if (profile)
		{
			EndProfileSection("Load logic defs");
		}
		if (base.started)
		{
			if (profile)
			{
				BeginProfileSection("Call OnDefsReloaded()");
			}
			cur_obj = first_object;
			while (cur_obj != null)
			{
				Object obj = cur_obj;
				cur_obj = cur_obj.next_in_game;
				obj.OnDefsReloaded();
			}
			if (profile)
			{
				EndProfileSection("Call OnDefsReloaded()");
			}
		}
		ModManager.Get()?.UpdateBaseChecksumValidity();
		NotifyListeners("defs_processed");
	}

	public static string PersistentDataPath()
	{
		if (string.IsNullOrEmpty(persistent_data_path))
		{
			persistent_data_path = Application.persistentDataPath;
		}
		return persistent_data_path;
	}

	public static string ProjectName()
	{
		if (string.IsNullOrEmpty(project_name))
		{
			project_name = "Kings";
		}
		return project_name;
	}

	public static string GetSavesRootDir(SavesRoot root)
	{
		string text = PersistentDataPath();
		try
		{
			text = ((!(ProjectName() == "Kings2")) ? (text + "/Saves") : (text + "/Saves2"));
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError(ex.Message);
			return null;
		}
		switch (root)
		{
		case SavesRoot.Single:
			text += "/SinglePlayer";
			break;
		case SavesRoot.Multi:
			text += "/MultiPlayer";
			break;
		}
		try
		{
			return Directory.CreateDirectory(text).FullName;
		}
		catch (Exception ex2)
		{
			UnityEngine.Debug.LogError(ex2.Message);
			return null;
		}
	}

	public string GetTextFile(string name)
	{
		if (engine == null)
		{
			return null;
		}
		return engine.GetTextFile(name);
	}

	public byte[] GetBinaryFile(string name)
	{
		if (engine == null)
		{
			return null;
		}
		return engine.GetBinaryFile(name);
	}

	public bool IsMultiplayer()
	{
		if (campaign != null)
		{
			return campaign.IsMultiplayerCampaign();
		}
		return false;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Value result = this.vars.Get(key, as_value);
		if (!result.is_unknown)
		{
			return result;
		}
		switch (key)
		{
		case "religions":
			return religions;
		case "campaign_rules":
			return rules;
		case "is_multiplayer":
			return IsMultiplayer();
		case "is_multiplayer_str":
			return IsMultiplayer().ToString();
		case "time_hours":
			return session_time.hours;
		case "time_seconds":
			return (int)session_time.seconds;
		case "real_time_played_seconds":
			return (int)(real_time_played.seconds + unscaled_unpaused_time_acc);
		case "real_time_total_seconds":
			return (int)(real_time_total.seconds + unscaled_total_time_acc);
		case "campaign":
			return new Value(campaign);
		case "num_players":
			return Multiplayer.CurrentPlayers.Count();
		case "is_loading_save_game":
			return isLoadingSaveGame;
		case "is_quitting":
			return IsQuitting();
		case "is_running":
			return IsRunning();
		default:
			if (religions != null)
			{
				result = religions.GetVar(key, vars, as_value);
				if (!result.is_unknown)
				{
					return result;
				}
			}
			return Value.Unknown;
		}
	}

	private void CallStarts()
	{
		while (starts.Count > 0)
		{
			starts[0].Start();
		}
	}

	protected override void OnStart()
	{
		CallStarts();
		base.OnStart();
	}

	protected override void OnDestroy()
	{
		UnloadMap();
		DestroyComponents();
		if (game != this)
		{
			game.subgames.Remove(this);
		}
		scheduler.game = null;
		scheduler = null;
		update_half_sec = null;
		update_1sec = null;
		update_5sec = null;
		update_10sec = null;
		defs = null;
		if (dt?.context?.game == this)
		{
			dt.context.game = null;
		}
		dt = null;
		base.OnDestroy();
	}

	public void Update(float elapsed_scaled, float elapsed_unscaled)
	{
		updates++;
		if (real_time_total.milliseconds < 0)
		{
			elapsed_unscaled = elapsed_scaled;
			real_time_total = Time.Zero;
			real_time_total_per_frame = Time.Zero;
		}
		time_unscaled += elapsed_unscaled;
		unscaled_total_time_acc += elapsed_unscaled;
		real_time_total_per_frame += elapsed_unscaled;
		if (unscaled_total_time_acc >= 1f)
		{
			real_time_total += unscaled_total_time_acc;
			unscaled_total_time_acc = 0f;
		}
		if (elapsed_scaled > 0f && subgames.Count == 0 && state == State.Running)
		{
			Expression.num_calcs = 0;
			unscaled_unpaused_time_acc += elapsed_unscaled;
			if (unscaled_unpaused_time_acc >= 1f)
			{
				real_time_played += unscaled_unpaused_time_acc;
				unscaled_unpaused_time_acc = 0f;
			}
			BeginProfileSection("Scheduler.Update");
			scheduler.Update(elapsed_scaled);
			EndProfileSection("Scheduler.Update");
			if (game == this)
			{
				BeginProfileSection("UpdatePopMajorities");
				UpdatePopMajorities();
				EndProfileSection("UpdatePopMajorities");
				BeginProfileSection("CoopThread.UpdateAll");
				CoopThread.UpdateAll(Math.Min(0.35f + 0.35f * speed + 0.05f * speed * speed, 30f));
				EndProfileSection("CoopThread.UpdateAll");
			}
		}
		BeginProfileSection("Game.CallStarts");
		CallStarts();
		EndProfileSection("Game.CallStarts");
		for (int i = 0; i < subgames.Count; i++)
		{
			subgames[i].Update(elapsed_scaled, elapsed_unscaled);
		}
		if (multiplayer != null)
		{
			BeginProfileSection("Multiplayer Update");
			multiplayer.OnUpdate();
			EndProfileSection("Multiplayer Update");
		}
		BeginProfileSection("THQNORequest.Update()");
		THQNORequest.UpdateAll();
		EndProfileSection("THQNORequest.Update()");
		BeginProfileSection("Request.ProcessAll()");
		Request.ProcessAll();
		EndProfileSection("Request.ProcessAll()");
		BeginProfileSection("Coroutine.UpdateAll()");
		Coroutine.UpdateAll();
		EndProfileSection("Coroutine.UpdateAll()");
		pings?.OnUpdate();
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("seed", seed);
		dump.Append("session_time", session_time.ToString());
		dump.Append("session_time_offset", session_time_offset);
		if (economy != null && economy.tradeCenterRealms.Count > 0)
		{
			dump.OpenSection("trade_centers");
			for (int i = 0; i < economy.tradeCenterRealms.Count; i++)
			{
				dump.Append(economy.tradeCenterRealms[i]?.name);
			}
			dump.CloseSection("trade_centers");
		}
		if (Multiplayer.CurrentPlayers.Count() > 0)
		{
			dump.OpenSection("current_players");
			List<Multiplayer.PlayerData> all = Multiplayer.CurrentPlayers.GetAll();
			for (int j = 0; j < all.Count; j++)
			{
				if (all[j] != null)
				{
					dump.OpenSection("pid", all[j].pid);
					dump.Append("name", all[j].name);
					dump.Append("kingdom", all[j].kingdomName);
					dump.Append("team", all[j].team);
					dump.CloseSection("pid", all[j].pid);
				}
			}
			dump.CloseSection("current_players");
		}
		dump.Append("campaign", campaign?.ToString());
		dump.Append("target_kingdom", rules?.targetKingdom);
		List<Kingdom> list = great_powers?.TopKingdoms();
		if (list != null && list.Count > 0)
		{
			dump.OpenSection("great_powers");
			for (int k = 0; k < list.Count; k++)
			{
				dump.Append(list[k]?.Name);
			}
			dump.CloseSection("great_powers");
		}
		base.DumpInnerState(dump, verbosity);
	}

	public void OnVarChanged(RemoteVars vars, string key, Value old_val, Value new_val)
	{
		if (key == "slot_state")
		{
			ValidateEndGame();
		}
	}

	public static bool IsPlaying()
	{
		if (MainThreadUpdates.main_thread == null)
		{
			return false;
		}
		if (!MainThreadUpdates.IsMainThread())
		{
			return true;
		}
		return Application.isPlaying;
	}

	private string GenerateGameState(int verbosity = 0)
	{
		StateDump stateDump = new StateDump();
		for (Serialization.ObjectType objectType = Serialization.ObjectType.Game; objectType < Serialization.ObjectType.COUNT; objectType++)
		{
			foreach (KeyValuePair<int, Object> item in game.multiplayer.objects.Registry(objectType).objects.OrderBy((KeyValuePair<int, Object> key) => key.Key))
			{
				item.Value?.DumpState(stateDump, verbosity);
			}
		}
		return stateDump.ToString();
	}

	public void DumpGameStateClient(byte[] serversChecksum)
	{
		if (multiplayer == null)
		{
			Error("Multiplayer is null");
			return;
		}
		if (multiplayer.type != Multiplayer.Type.Client)
		{
			Error("Attempting to call DumpGameStateClient() from multiplayer of type " + multiplayer.type);
			return;
		}
		byte[] gameStateChecksum = GetGameStateChecksum();
		Log("Game state dump generated");
		if (!CompareBytes(gameStateChecksum, serversChecksum))
		{
			multiplayer.SendGameStateDump();
		}
	}

	public void DumpGameStateServer()
	{
		if (multiplayer == null)
		{
			Error("Multiplayer is null");
			return;
		}
		if (multiplayer.playerData == null)
		{
			Error("PlayerData is null");
			return;
		}
		if (multiplayer.type != Multiplayer.Type.Server)
		{
			Error("Attempting to call DumpGameStateServer() from multiplayer of type " + multiplayer.type);
			return;
		}
		byte[] gameStateChecksum = GetGameStateChecksum();
		Log("Game state dump generated");
		multiplayer.SendBeginGameStateDump(gameStateChecksum);
		game.DumpGameStateToFile(multiplayer.playerData, GameStateDumpFileType.Server);
	}

	public void DumpGameStateLocally()
	{
		if (multiplayer != null)
		{
			if (multiplayer.playerData == null)
			{
				Error("PlayerData is null");
				return;
			}
			multiplayer.playerData.gameState = GenerateGameState();
			DumpGameStateToFile(multiplayer.playerData, GameStateDumpFileType.Local);
			CopyToClipboard(multiplayer.playerData.gameState);
		}
	}

	private byte[] GetGameStateChecksum()
	{
		multiplayer.playerData.gameState = GenerateGameState();
		Checksum.BeginChecksum();
		Checksum.FeedChecksum(multiplayer.playerData.gameState);
		return Checksum.EndChecksum();
	}

	public void DumpGameStateToFile(Multiplayer.PlayerData playerData, GameStateDumpFileType fileType)
	{
		string gameStateDumpDir = GetGameStateDumpDir();
		if (gameStateDumpDir == null)
		{
			return;
		}
		Directory.CreateDirectory(gameStateDumpDir);
		string gameStateDumpFilePath = GetGameStateDumpFilePath(playerData.name, fileType);
		if (gameStateDumpFilePath == null)
		{
			return;
		}
		try
		{
			using FileStream fileStream = new FileStream(gameStateDumpFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			StreamReader streamReader = new StreamReader(fileStream);
			using StreamWriter streamWriter = new StreamWriter(fileStream);
			streamReader.ReadToEnd();
			fileStream.SetLength(0L);
			streamWriter.Write(playerData.gameState);
		}
		catch (Exception ex)
		{
			Error(ex.Message);
		}
	}

	public string GetGameStateDumpFilePath(string playerName, GameStateDumpFileType fileType)
	{
		string gameStateDumpDir = GetGameStateDumpDir();
		if (gameStateDumpDir == null)
		{
			return null;
		}
		string path = playerName + "_" + fileType.ToString().ToLowerInvariant() + ".txt";
		return System.IO.Path.Combine(gameStateDumpDir, path);
	}

	private string GetGameStateDumpDir()
	{
		return campaign?.Dir("GameStateDump");
	}

	private bool CompareBytes(byte[] array1, byte[] array2)
	{
		int num = array1.Length;
		if (num != array2.Length)
		{
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			if (array1[i] != array2[i])
			{
				return false;
			}
		}
		return true;
	}

	public static string GetLoadGameLocation(LoadedGameType type)
	{
		switch (type)
		{
		case LoadedGameType.Continue:
		case LoadedGameType.LoadFromMainMenu:
		case LoadedGameType.QuickLoad:
		case LoadedGameType.ContinueMultiplayerHost:
		case LoadedGameType.ContinueMultiplayerClient:
			return "main_menu";
		case LoadedGameType.LoadFromInGameMenu:
			return "in_game";
		default:
			return "unknown";
		}
	}

	public bool True()
	{
		return true;
	}

	public bool False()
	{
		return false;
	}

	public override void OnInit()
	{
		base.OnInit();
		if (type == "world_view")
		{
			SetNid(1, update_registry: false);
			teams = new Teams(this);
			rules = new CampaignRules(this);
			stats = new Stats();
			economy = new Economy(this);
			emperorOfTheWorld = new EmperorOfTheWorld(this);
			religions = new Religions(this);
			ai = new AI();
			cultures = new Cultures(this);
			rankings = new KingdomRankings(this);
			great_powers = new GreatPowers(this);
			pause = new Pause(this);
			pings = new Pings(this);
			scheduler.RegisterAfterSeconds(this, 60f, exact: true);
			Timer.Start(this, "session_time_sync", 10f, restart: true);
		}
	}

	public void InitFrom(Game base_game)
	{
		if (type == "battle_view")
		{
			realms = base_game.realms;
			kingdoms = base_game.kingdoms;
			kingdoms_by_name = base_game.kingdoms_by_name;
		}
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		if (state == State.Running)
		{
			if (!IsAuthority())
			{
				scheduler.RegisterAfterSeconds(this, 60f, exact: true);
			}
			else if (!ValidateEndGame())
			{
				scheduler.RegisterAfterSeconds(this, 60f, exact: true);
			}
		}
	}

	public bool IsUnloadingMap()
	{
		return map_name == null;
	}

	public void UpdatePopMajorities()
	{
		if (landRealmsCount <= 0 || realms == null || realms.Count <= 0 || time < next_pop_majority_update_time)
		{
			return;
		}
		float pop_majority_update_interval = game.cultures.def.pop_majority_update_interval;
		if (next_pop_majority_update_time == Time.Zero)
		{
			next_pop_majority_update_time = Time.Zero + pop_majority_update_interval;
			return;
		}
		pop_majority_update_interval /= (float)landRealmsCount;
		while (time >= next_pop_majority_update_time)
		{
			last_pop_majority_update_realm_id++;
			if (last_pop_majority_update_realm_id > landRealmsCount)
			{
				last_pop_majority_update_realm_id = 1;
			}
			Realm realm = GetRealm(last_pop_majority_update_realm_id);
			if (realm != null && !realm.IsSeaRealm())
			{
				realm.UpdatePopMajority();
				next_pop_majority_update_time += pop_majority_update_interval;
			}
		}
	}

	public void LoadMapDef(string map_name)
	{
		DT.Field field = dt.Find("Maps." + map_name);
		if (field != null)
		{
			world_size = field.GetPoint("size");
		}
	}

	public void ResolvePeriod()
	{
		DT.Field field = dt.Find("Periods." + map_name);
		if (field == null)
		{
			map_period = "";
			return;
		}
		if (string.IsNullOrEmpty(map_period))
		{
			map_period = field.String();
		}
		if (!string.IsNullOrEmpty(map_period) && field.FindChild(map_period) == null)
		{
			Error("Undefined period '" + map_period + "' for map '" + map_name + "'");
			map_period = "";
		}
		else
		{
			if (map_period != null || field.children == null)
			{
				return;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!string.IsNullOrEmpty(field2.key))
				{
					map_period = field2.key;
					break;
				}
			}
		}
	}

	public void LoadPFData(string map_name)
	{
		path_finding = new PathFinding(this, map_name);
	}

	public void LoadHeights(string map_name)
	{
		string moddedAssetPath = ModManager.GetModdedAssetPath(maps_path + map_name + "/heights.bin", allow_unmodded_path: true);
		try
		{
			heights.Load(moddedAssetPath);
		}
		catch (Exception ex)
		{
			Error("Error loading " + moddedAssetPath + ": " + ex.Message);
			heights.Dispose();
		}
	}

	public void LoadPassability(string map_name)
	{
		string moddedAssetPath = ModManager.GetModdedAssetPath(maps_path + map_name + "/passability.bin", allow_unmodded_path: true);
		try
		{
			passability.Load(moddedAssetPath);
		}
		catch (Exception ex)
		{
			Error("Error loading " + moddedAssetPath + ": " + ex.Message);
			passability.Dispose();
		}
	}

	public void AddKingdom(Kingdom k)
	{
		if (k == null)
		{
			return;
		}
		using (Profile("Game.AddKingdom"))
		{
			kingdoms.Add(k);
			if (!kingdoms_by_name.ContainsKey(k.Name))
			{
				kingdoms_by_name.Add(k.Name, k);
			}
			else
			{
				Error("Duplicated kingdom name: " + k.Name);
			}
			k.id = kingdoms.Count;
			k.SetNid(k.id);
			if (k.ai != null)
			{
				k.ai.Init();
			}
		}
	}

	public static string ResolvePeriodSuffix(string suffix, DT.Field field)
	{
		switch (suffix)
		{
		case "E":
			return "early";
		case "M":
			return "mid";
		case "L":
			return "late";
		default:
			Log(field.Path(include_file: true) + ": Unknown period suffix: '" + suffix + "'", LogType.Error);
			return null;
		}
	}

	public static bool ResolveKingdomCSVName(DT.Field f, out string name, out string period)
	{
		if (!ResolveNameAndPeriod(f, out name, out period))
		{
			return false;
		}
		if (name.StartsWith("NamePeriod_", StringComparison.Ordinal))
		{
			name = name.Substring(11);
		}
		return true;
	}

	public static bool ResolveNameAndPeriod(DT.Field f, out string name, out string period)
	{
		int num = f.key.IndexOf('.');
		if (num < 0)
		{
			name = f.key;
			period = null;
			return true;
		}
		name = f.key.Substring(0, num);
		string suffix = f.key.Substring(num + 1);
		period = ResolvePeriodSuffix(suffix, f);
		if (period == null)
		{
			return false;
		}
		return true;
	}

	public static bool MatchPeriod(string period, string map_period)
	{
		if (period == null)
		{
			return true;
		}
		return period == map_period;
	}

	public static void ProcessStringList(string s, Action<string> cb)
	{
		if (string.IsNullOrEmpty(s))
		{
			return;
		}
		int num = 0;
		while (true)
		{
			int num2 = s.IndexOf('/', num);
			if (num2 < 0)
			{
				break;
			}
			string obj = s.Substring(num, num2 - num);
			cb(obj);
			num = num2 + 1;
		}
		string obj2 = s.Substring(num);
		cb(obj2);
	}

	public static void AddStringsToList(string s, List<string> lst)
	{
		ProcessStringList(s, delegate(string ss)
		{
			lst.Add(ss);
		});
	}

	public static string FieldPath(DT.Field field, string key)
	{
		string text = "";
		if (field != null)
		{
			text += field.Path(include_file: true);
		}
		if (!string.IsNullOrEmpty(key))
		{
			text = text + "." + key;
		}
		return text;
	}

	public bool ValidateDefID(string id, string type, DT.Field src_field = null, string src_key = null, bool allow_empty = true)
	{
		if (string.IsNullOrEmpty(id))
		{
			if (allow_empty)
			{
				return true;
			}
			Log(FieldPath(src_field, src_key) + ": " + type + " def not specified", LogType.Error);
			return false;
		}
		DT.Def def = dt?.FindDef(id);
		if (def == null)
		{
			Log(FieldPath(src_field, src_key) + ": Invalid " + type + " def: '" + id + "'", LogType.Error);
			return false;
		}
		if (def.field.BaseRoot().key != type)
		{
			Log(FieldPath(src_field, src_key) + ": Invalid " + type + " def: '" + id + "'", LogType.Error);
			return false;
		}
		return true;
	}

	public bool ValidateDefIDs(List<string> ids, string type, DT.Field src_field = null, string src_key = null, bool allow_empty = true)
	{
		if (ids == null || ids.Count == 0)
		{
			if (allow_empty)
			{
				return true;
			}
			Log(FieldPath(src_field, src_key) + ": " + type + " def(s) not specified", LogType.Error);
			return false;
		}
		bool result = true;
		for (int i = 0; i < ids.Count; i++)
		{
			string id = ids[i];
			if (!ValidateDefID(id, type, src_field, src_key, allow_empty: false))
			{
				result = false;
			}
		}
		return result;
	}

	public static void SetCSVBase(DT.Field f, DT.Field bf)
	{
		if (f.based_on != null)
		{
			Log($"{f.Path(include_file: true)}: Changing base from {f.based_on} to {bf}", LogType.Error);
		}
		f.based_on = bf;
		if (f.children != null && bf?.children != null)
		{
			for (int i = 0; i < f.children.Count; i++)
			{
				DT.Field field = f.children[i];
				DT.Field based_on = bf.FindChild(field.key);
				field.based_on = based_on;
			}
		}
	}

	public bool LoadRealmsCSV(string map_name, string map_period)
	{
		DT.Field field = DT.ReadMapsCsv(null, map_name + "/realms.csv", '\0', null, "-");
		if (field?.children == null)
		{
			return false;
		}
		DT.Field field2 = dt.Find("Realm");
		if (field2 == null)
		{
			Error("Realm def not found");
			return false;
		}
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field3 = field.children[i];
			if (string.IsNullOrEmpty(field3.key) || !ResolveNameAndPeriod(field3, out var name, out var period) || !MatchPeriod(period, map_period))
			{
				continue;
			}
			if (name.StartsWith("Name_", StringComparison.Ordinal))
			{
				name = name.Substring(5);
			}
			Realm realm = GetRealm(name);
			if (realm != null)
			{
				if (period == null && realm.csv_field != null)
				{
					Log(field3.Path(include_file: true) + ": Duplicated realm name: '" + name + "'", LogType.Error);
					continue;
				}
				SetCSVBase(field3, realm.csv_field);
				realm.csv_field = field3;
			}
			else
			{
				Log("Realm '" + name + "' is not exported, please re-export map", LogType.Warning);
				realm = new Realm(this);
				realm.name = name;
				realm.def = field2;
				realm.csv_field = field3;
				realms.Add(realm);
				realm.id = realms.Count;
			}
		}
		return true;
	}

	public void LoadRealms(string map_name, string map_period)
	{
		using (Profile("Game.LoadRealms"))
		{
			realms = new List<Realm>();
			kingdoms = null;
			kingdoms_by_name = null;
			List<DT.Field> list = new List<DT.Field>();
			List<DT.Field> list2 = new List<DT.Field>();
			using (Profile("Load Realm DTs"))
			{
				DT.Field field = dt.Find("Realms." + map_name);
				if (field?.children != null)
				{
					for (int i = 0; i < field.children.Count; i++)
					{
						DT.Field field2 = field.children[i];
						if (!string.IsNullOrEmpty(field2.key))
						{
							if (field2.GetBool("isSeaRealm"))
							{
								list2.Add(field2);
							}
							else
							{
								list.Add(field2);
							}
						}
					}
				}
			}
			using (Profile("Create land realms"))
			{
				for (int j = 0; j < list.Count; j++)
				{
					DT.Field field3 = list[j];
					Realm realm = GetRealm(field3.key);
					if (realm == null)
					{
						realm = new Realm(this);
						realm.name = field3.key;
						realms.Add(realm);
						realm.id = realms.Count;
					}
					realm.def = field3;
				}
			}
			using (Profile("LoadRealmsCSV"))
			{
				LoadRealmsCSV(map_name, map_period);
			}
			landRealmsCount = realms.Count;
			using (Profile("Set land realm NIDs"))
			{
				for (int k = 0; k < landRealmsCount; k++)
				{
					Realm realm2 = realms[k];
					realm2.SetNid(realm2.id);
				}
			}
			using (Profile("Create sea realms"))
			{
				for (int l = 0; l < list2.Count; l++)
				{
					DT.Field field4 = list2[l];
					Realm realm3 = new Realm(this);
					realm3.def = field4;
					realm3.name = field4.key;
					realms.Add(realm3);
					realm3.id = -realms.Count;
					realm3.SetNid(-realm3.id);
					realm3.kingdom_id = 0;
				}
			}
			using (Profile("LoadNeighborsAndRegions"))
			{
				for (int m = 0; m < realms.Count; m++)
				{
					realms[m].LoadNeighborsAndRegions();
				}
			}
			using (Profile("SetLogicNeighborsAll"))
			{
				for (int n = 0; n < realms.Count; n++)
				{
					realms[n].SetLogicNeighborsAll();
				}
			}
		}
	}

	public bool LoadKingdomsCSV(string map_name, string map_period)
	{
		kingdoms_csv = DT.ReadMapsCsv(null, map_name + "/kingdoms.csv", '\0', null, "-");
		if (kingdoms_csv?.children == null)
		{
			return false;
		}
		DT.Field field = dt.Find("Kingdom");
		if (field == null)
		{
			Error("Kingdom def not found");
			return false;
		}
		for (int i = 0; i < kingdoms_csv.children.Count; i++)
		{
			DT.Field field2 = kingdoms_csv.children[i];
			if (string.IsNullOrEmpty(field2.key) || !ResolveKingdomCSVName(field2, out var name, out var period) || !MatchPeriod(period, map_period))
			{
				continue;
			}
			Kingdom kingdom = GetKingdom(name);
			if (kingdom != null)
			{
				if (period == null && kingdom.csv_field != null)
				{
					Log(field2.Path(include_file: true) + ": Duplicated kingdom name: '" + name + "'", LogType.Error);
					continue;
				}
				SetCSVBase(field2, kingdom.csv_field);
				kingdom.csv_field = field2;
			}
			else
			{
				kingdom = new Kingdom(this);
				kingdom.Name = (kingdom.ActiveName = name);
				kingdom.def = field;
				kingdom.csv_field = field2;
				AddKingdom(kingdom);
			}
		}
		return true;
	}

	public bool LoadPapacyCSV(string map_name, string map_period)
	{
		DT.Field field = DT.ReadMapsCsv(null, map_name + "/papacy.csv", '\0', null);
		if (field?.children == null)
		{
			return false;
		}
		if (religions?.catholic == null)
		{
			return false;
		}
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field2 = field.children[i];
			if (MatchPeriod(ResolvePeriodSuffix(field2.key.Substring(field2.key.Length - 1), field2), map_period))
			{
				religions.catholic.papacy_csv = field2;
				return true;
			}
		}
		return false;
	}

	public DT.Field GetKingdomCSV(string kingdom_csv_key)
	{
		List<DT.Field> list = kingdoms_csv?.children;
		if (list == null)
		{
			return null;
		}
		for (int i = 0; i < list.Count; i++)
		{
			DT.Field field = list[i];
			if (field != null && !(field.key != kingdom_csv_key))
			{
				return field;
			}
		}
		for (int j = 0; j < realms.Count; j++)
		{
			Realm realm = realms[j];
			if (realm != null && !(realm.csv_field?.key != kingdom_csv_key))
			{
				return realm.csv_field;
			}
		}
		return null;
	}

	public void LoadKingdoms(string map_name, string period)
	{
		using (Profile("Game.LoadKingdoms"))
		{
			kingdoms = new List<Kingdom>();
			kingdoms_by_name = new Dictionary<string, Kingdom>();
			DT.Field field = dt.Find("Kingdom");
			if (field == null)
			{
				Error("Kingdom def not found");
				return;
			}
			using (Profile("Create kingdoms for land realms"))
			{
				for (int i = 0; i < landRealmsCount; i++)
				{
					Realm realm = realms[i];
					Kingdom kingdom = new Kingdom(this);
					kingdom.Name = (kingdom.ActiveName = realm.name);
					kingdom.def = field;
					AddKingdom(kingdom);
				}
			}
			using (Profile("Load kingdoms def"))
			{
				DT.Field field2 = dt.Find("Kingdoms." + map_name);
				if (field2 != null)
				{
					List<string> list = field2.Keys();
					for (int j = 0; j < list.Count; j++)
					{
						string path = list[j];
						DT.Field field3 = field2.FindChild(path);
						string key = field3.key;
						Kingdom kingdom2 = GetKingdom(key);
						if (kingdom2 == null)
						{
							kingdom2 = new Kingdom(this);
							kingdom2.Name = (kingdom2.ActiveName = key);
							AddKingdom(kingdom2);
						}
						kingdom2.def = field3;
					}
				}
			}
			using (Profile("LoadKingdomsCSV"))
			{
				LoadKingdomsCSV(map_name, period);
			}
			using (Profile("LoadPapacyCSV"))
			{
				LoadPapacyCSV(map_name, period);
			}
			using (Profile("Finish loading kingdoms"))
			{
				for (int k = 0; k < kingdoms.Count; k++)
				{
					Kingdom kingdom3 = kingdoms[k];
					if (kingdom3.id <= game.landRealmsCount)
					{
						Realm realm2 = game.GetRealm(kingdom3.id);
						if (realm2 != null)
						{
							if (kingdom3.csv_field == null)
							{
								kingdom3.csv_field = realm2.csv_field;
							}
							else
							{
								SetCSVBase(kingdom3.csv_field.BaseRoot(), realm2.csv_field);
							}
						}
					}
					kingdom3.Load();
				}
			}
			if (game.type == "world_view" && !string.IsNullOrEmpty(game.map_name))
			{
				using (Profile("BuildFactionKingdoms"))
				{
					FactionUtils.BuildFactionKingdoms(this);
				}
			}
			using (Profile("LoadCoAIndicesAndColors"))
			{
				LoadCoAIndicesAndColors(map_name, period);
			}
			using (Profile("Finish loading realms"))
			{
				for (int l = 0; l < realms.Count; l++)
				{
					realms[l].Load();
				}
			}
			using (Profile("Init relations"))
			{
				for (int m = 0; m < kingdoms.Count; m++)
				{
					KingdomAndKingdomRelation.InitRelations(kingdoms[m]);
				}
			}
			using (Profile("load capital provinces and vassalage"))
			{
				for (int n = 0; n < kingdoms.Count; n++)
				{
					Kingdom kingdom4 = kingdoms[n];
					kingdom4.LoadCapitalProvince();
					kingdom4.LoadVassalage();
				}
			}
			using (Profile("Recalc kingdom neighbors and distances"))
			{
				for (int num = 0; num < kingdoms.Count; num++)
				{
					Kingdom kingdom5 = kingdoms[num];
					foreach (Realm realm3 in kingdom5.realms)
					{
						realm3.RecheckIfBorder();
					}
					kingdom5.RecalcKingdomDistances();
					kingdom5.ResetNeighbors();
				}
				for (int num2 = 0; num2 < kingdoms.Count; num2++)
				{
					kingdoms[num2].ResetSecondaryNeighbors();
				}
			}
		}
	}

	public void LoadKingdomCoAIndicesAndColors(CoAMapping map, Kingdom k, string suffix)
	{
		int num = map.Get(k.ActiveName, suffix);
		if (num > 0)
		{
			k.CoAIndex = num;
		}
		CoAMapping.KingdomColors colors = map.GetColors(k.ActiveName, suffix);
		if (colors != null)
		{
			k.map_color = colors.map_color;
			k.primary_army_color = colors.primary_army_color;
			k.secondary_army_color = colors.secondary_army_color;
		}
	}

	public void LoadCoAIndicesAndColors(string map_name, string period)
	{
		string suffix = null;
		if (!string.IsNullOrEmpty(period))
		{
			suffix = dt.GetString("Periods." + map_name + "." + period);
		}
		CoAMapping coAMapping = new CoAMapping();
		coAMapping.Load(map_name);
		for (int i = 0; i < kingdoms.Count; i++)
		{
			Kingdom k = kingdoms[i];
			LoadKingdomCoAIndicesAndColors(coAMapping, k, suffix);
		}
	}

	public void LoadRealmIDMap(string map_name)
	{
		using (Profile("Game.LoadRealmIDMap"))
		{
			try
			{
				byte[] array = File.ReadAllBytes(maps_path + map_name + "/realms.bin");
				int num = (int)Math.Sqrt(array.Length / 2);
				realm_id_map = Serialization.ToArray<short>(array, num, num);
			}
			catch (Exception ex)
			{
				Error("Error loading realm id map: " + ex.Message);
				realm_id_map = null;
			}
		}
	}

	public void LoadTerrainTypes(string map_name)
	{
		terrain_types = new TerrainTypesInfo();
		terrain_types.SetWorldSize(world_size);
		terrain_types.Load(dt, map_name);
	}

	public void LoadClimateZones(string map_name)
	{
		climate_zones = new ClimateZoneInfo();
		climate_zones.SetWorldSize(world_size);
		climate_zones.Load(dt, map_name);
	}

	public void LoadSettlements(string map_name, bool new_game, ProvinceFeatureDistribution distribution)
	{
		DT.Field field = dt.Find("Settlements." + map_name);
		if (field == null || field.children == null)
		{
			return;
		}
		DT.Field randsField = dt.Find("SettlementsRandomizationWeights");
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field2 = field.children[i];
			if (string.IsNullOrEmpty(field2.key))
			{
				continue;
			}
			Point point = field2.GetPoint("position");
			if (point == Point.Invalid)
			{
				Error("Settlement " + field2.key + " has no position");
				continue;
			}
			Settlement settlement = null;
			string text = field2.base_path;
			if (distribution != null)
			{
				text = distribution.GetPrefredSettlementType(field2);
			}
			if (text == "Settlement")
			{
				text = Settlement.GetRandomType(randsField, game.GetRealm(game.RealmIDAt(point)));
			}
			string text2 = Settlement.ParseType(dt, text);
			if (text2 != null)
			{
				settlement = Settlement.Create(this, point, text2);
				settlement.Load(field2);
			}
			else
			{
				Error("Settlement " + field2.key + " has unknown type");
			}
		}
		if (new_game && game.IsAuthority())
		{
			GenerateProvinceFeatureTags(distribution);
		}
	}

	private void InitIdeas(KingdomAdvantage.Def adv_def)
	{
	}

	private void InitStructures()
	{
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm != null && realm.castle != null)
			{
				realm.castle.InitStructures();
			}
		}
	}

	private void InitFamousPersonSpawner()
	{
		new FamousPersonSpawner(this);
	}

	private void InitKingdomRankingsTimers()
	{
		GetComponent<KingdomRankings>()?.InitNotificationTimers();
	}

	private void InitQuests(bool new_game)
	{
		if (IsAuthority() && new_game)
		{
			for (int i = 0; i < kingdoms.Count; i++)
			{
				kingdoms[i]?.quests?.AddBaseQuests();
			}
		}
	}

	public void StartGame(bool new_game, string map_name = null, string fullPath = null)
	{
		if (multiplayer == null)
		{
			if (campaign != null && campaign.IsMultiplayerCampaign())
			{
				Error("Starting multiplayer game without a multiplayer");
			}
			CreateMultiplayer(Multiplayer.Type.Server, 0);
			if (multiplayer == null)
			{
				Error("CreateMuliplayer failed on StartGame");
				return;
			}
		}
		multiplayer.StartGame(new_game, map_name, fullPath);
	}

	public void LoadMap(string map_name, string period, bool new_game)
	{
		_ = state;
		this.new_game = new_game;
		state = State.LoadingMap;
		UnloadMap();
		map_name = map_name.ToLowerInvariant();
		if (defs_map != null && defs_map != map_name)
		{
			LoadDefs(map_name);
		}
		if (!isComingFromTitle && !isJoiningGame && (!isLoadingSaveGame || multiplayer == null || multiplayer.type != Multiplayer.Type.Server))
		{
			Log("No multiplayer on LoadMap, creating one");
			CreateMultiplayer(Multiplayer.Type.Server, 0);
			_ = multiplayer;
		}
		this.map_name = map_name;
		map_period = period;
		map_from_save_id = null;
		political_data_session_time = 0;
		if (map_name == null)
		{
			return;
		}
		if (type == "world_view")
		{
			BeginProfileSection("ResolvePeriod");
			ResolvePeriod();
			EndProfileSection("ResolvePeriod");
		}
		if (campaign == null)
		{
			campaign = Campaign.CreateSinglePlayerCampaign(map_name, map_period);
		}
		if (multiplayer != null && multiplayer.logLevel == Multiplayer.LogLevel.Custom)
		{
			multiplayer.receiveLogWriter?.WriteLine("Loading Map");
		}
		session_time_offset = Time.Zero - time;
		real_time_played = Time.Zero;
		unscaled_unpaused_time_acc = 0f;
		real_time_total = Time.Zero - 1f;
		real_time_total_per_frame = Time.Zero - 1f;
		unscaled_total_time_acc = 0f;
		if (type == "world_view")
		{
			SetNid(1);
		}
		if (new_game)
		{
			seed = rnd.Next();
		}
		NotifyListeners("loading_map", map_name);
		BeginProfileSection("LoadMapDef");
		LoadMapDef(map_name);
		EndProfileSection("LoadMapDef");
		BeginProfileSection("LoadPFData");
		LoadPFData(map_name);
		EndProfileSection("LoadPFData");
		if (type == "battle_view")
		{
			if (game.campaign == null)
			{
				game.campaign = Campaign.CreateSinglePlayerCampaign(map_name, map_period);
			}
			BeginProfileSection("LoadHeights");
			LoadHeights(map_name);
			EndProfileSection("LoadHeights");
			BeginProfileSection("LoadPassability");
			LoadPassability(map_name);
			EndProfileSection("LoadPassability");
		}
		if (type == "world_view")
		{
			BeginProfileSection("LoadCultures");
			cultures?.LoadDefaults(map_name);
			EndProfileSection("LoadCultures");
			BeginProfileSection("LoadRealms");
			LoadRealms(map_name, map_period);
			EndProfileSection("LoadRealms");
			BeginProfileSection("LoadKingdoms");
			LoadKingdoms(map_name, map_period);
			EndProfileSection("LoadKingdoms");
			BeginProfileSection("LoadRealmIDMap");
			LoadRealmIDMap(map_name);
			EndProfileSection("LoadRealmIDMap");
			BeginProfileSection("LoadTerrainTypes");
			LoadTerrainTypes(map_name);
			EndProfileSection("LoadTerrainTypes");
			BeginProfileSection("LoadClimateZones");
			LoadClimateZones(map_name);
			EndProfileSection("LoadClimateZones");
			ProvinceFeatureDistribution distribution = null;
			if (new_game && game.IsAuthority())
			{
				BeginProfileSection("ProvinceFeatureDistribution.AnalyzeRealms");
				distribution = ProvinceFeatureDistribution.AnalyzeRealms(game, map_name);
				EndProfileSection("ProvinceFeatureDistribution.AnalyzeRealms");
			}
			BeginProfileSection("LoadSettlements");
			LoadSettlements(map_name, new_game, distribution);
			EndProfileSection("LoadSettlements");
			if (new_game && IsAuthority())
			{
				BeginProfileSection("PatchEmptySettlmentsNavigation");
				path_finding.UpdateEmptySettlementsPathData();
				EndProfileSection("PatchEmptySettlmentsNavigation");
			}
			BeginProfileSection("InitRealmsAndKingdoms");
			InitRealmsAndKingdoms(new_game);
			EndProfileSection("InitRealmsAndKingdoms");
			if (new_game && IsAuthority())
			{
				BeginProfileSection("InitStructures");
				InitStructures();
				EndProfileSection("InitStructures");
			}
			BeginProfileSection("Religions.Init");
			religions.Init(new_game);
			EndProfileSection("Religions.Init");
			BeginProfileSection("FamousPersonSpawner");
			InitFamousPersonSpawner();
			EndProfileSection("FamousPersonSpawner");
			BeginProfileSection("Quest.Init");
			InitQuests(new_game);
			EndProfileSection("Quest.Init");
			BeginProfileSection("RankingMessages.Init");
			InitKingdomRankingsTimers();
			EndProfileSection("RankingMessages.Init");
			rules.Load();
			BeginProfileSection("CampaignRules.Apply");
			rules.ApplyCampaignRules(new_game);
			EndProfileSection("CampaignRules.Apply");
			if (new_game)
			{
				BeginProfileSection("InitTradeCenters");
				InitTradeCenters(distribution);
				EndProfileSection("InitTradeCenters");
			}
			if (new_game && IsAuthority())
			{
				BeginProfileSection("GenerateKingdomRelations");
				GenerateKingdomRelations();
				EndProfileSection("GenerateKingdomRelations");
			}
		}
		rebellions = new List<Rebellion>();
		game_rules = new GameRules(this);
		if (multiplayer != null && multiplayer.logLevel == Multiplayer.LogLevel.Custom)
		{
			multiplayer.receiveLogWriter?.WriteLine("Finished Loading Map");
		}
		NotifyListeners("map_loaded");
		if (campaign != null && campaign.IsMultiplayerCampaign())
		{
			if (!IsInternalBranch())
			{
				cheat_level = CheatLevel.None;
			}
			if (!CampaignUtils.AreAllJoinedPlayersInGame(this))
			{
				pause.AddRequest("WaitingForPlayersPause");
			}
		}
		if (isJoiningGame)
		{
			NotifyListeners("joining_map_loaded");
		}
		mapLoaded = true;
		state = State.Running;
		campaign.AddVarsListener(this);
		scheduler.RegisterAfterSeconds(this, 60f, exact: true);
	}

	private void OnGameStartedFame(string started_from)
	{
		if (started_from != "load_game")
		{
			return;
		}
		for (int i = 0; i < game.realms.Count; i++)
		{
			realms[i].InvalidateIncomes();
		}
		for (int j = 0; j < kingdoms.Count; j++)
		{
			Kingdom kingdom = kingdoms[j];
			if (!kingdom.IsDefeated() && kingdom.type == Kingdom.Type.Regular)
			{
				kingdom.fameObj.CalcFame();
				kingdom.RefreshAdvantages(create: true);
			}
		}
	}

	public void OnStartedAnalytics()
	{
		send_analytics = true;
		if (new_game)
		{
			GetLocalPlayerKingdom()?.OnNewGameAnalytics();
		}
		else
		{
			GetLocalPlayerKingdom()?.OnLoadGameAnalytics();
		}
	}

	public void OnFullGameStateReceived(bool isServer)
	{
		if (multiplayer == null)
		{
			return;
		}
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"{this} received full game state", 2);
		}
		fullGameStateReceived = true;
		if (!isServer && (isServer || !isLoadingSaveGameForClientOnly))
		{
			OnGameStarted("new_game");
		}
		if (isServer)
		{
			multiplayer.NotifyListeners("full_game_state_received", isServer);
		}
		else
		{
			multiplayer.NotifyListenersDelayed("full_game_state_received", isServer, process_triggers: false, profile: false);
		}
		isLoadingSaveGameForClientOnly = false;
		if (IsMultiplayer())
		{
			MainThreadUpdates.Perform(delegate
			{
				CampaignUtils.SetCampaignVarGameLoaded(this);
			});
		}
	}

	public Realm GetRealm(int rid)
	{
		if (rid < 0)
		{
			rid = -rid;
		}
		if (realms == null || rid == 0 || rid > realms.Count)
		{
			return null;
		}
		return realms[rid - 1];
	}

	public Realm GetRealm(string name)
	{
		if (realms == null)
		{
			return null;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm.name == name)
			{
				return realm;
			}
		}
		return null;
	}

	public Castle GetCastle(string name)
	{
		if (realms == null)
		{
			return null;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm.castle?.name == name || realm.castle?.customName == name)
			{
				return realm.castle;
			}
		}
		return null;
	}

	public Kingdom GetKingdom(int kid)
	{
		if (kingdoms == null || kid <= 0 || kid > kingdoms.Count)
		{
			return null;
		}
		return kingdoms[kid - 1];
	}

	public Kingdom GetKingdom(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		if (kingdoms_by_name == null)
		{
			return null;
		}
		Kingdom value = null;
		kingdoms_by_name.TryGetValue(name, out value);
		return value;
	}

	public int GetLocalPlayerKingdomId()
	{
		if (multiplayer == null || multiplayer.playerData == null)
		{
			return 0;
		}
		return multiplayer.playerData.kingdomId;
	}

	public Kingdom GetLocalPlayerKingdom()
	{
		return GetKingdom(GetLocalPlayerKingdomId());
	}

	public int GetNumAliveKingdoms()
	{
		if (kingdoms == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < kingdoms.Count; i++)
		{
			if (!kingdoms[i].IsDefeated())
			{
				num++;
			}
		}
		return num;
	}

	public void ValidatePlayersAndKingdoms()
	{
		if (kingdoms == null)
		{
			Warning("Kingdoms list is null!");
			return;
		}
		for (int i = 0; i < kingdoms.Count; i++)
		{
			Kingdom kingdom = kingdoms[i];
			if (!kingdom.IsDefeated())
			{
				if (kingdom.is_player && kingdom.ai.enabled != KingdomAI.EnableFlags.Disabled)
				{
					Warning($"Kingdom {kingdom} is controlled by player, but AI is ({kingdom.ai.enabled})!");
				}
				if (!kingdom.is_player && kingdom.ai.enabled == KingdomAI.EnableFlags.Disabled)
				{
					Warning($"Kingdom {kingdom} is not controlled by player, but AI is ({kingdom.ai.enabled})!");
				}
			}
		}
	}

	public int RealmIDAt(Point pt)
	{
		if (realm_id_map == null)
		{
			return 0;
		}
		int length = realm_id_map.GetLength(0);
		int length2 = realm_id_map.GetLength(1);
		int num = (int)(pt.x * (float)length / world_size.x);
		int num2 = (int)(pt.y * (float)length2 / world_size.y);
		if (num < 0)
		{
			num = 0;
		}
		else if (num >= length)
		{
			num = length - 1;
		}
		if (num2 < 0)
		{
			num2 = 0;
		}
		else if (num2 >= length2)
		{
			num2 = length2 - 1;
		}
		return realm_id_map[num, num2];
	}

	public int GetNearbyLandRealm(Point pt, float max_dist = 16f, float rstep = 2f, float astep = 30f)
	{
		if (realm_id_map == null)
		{
			return 0;
		}
		int num = RealmIDAt(pt);
		if (num > 0)
		{
			return num;
		}
		for (float num2 = rstep; num2 <= max_dist; num2 += rstep)
		{
			Point point = new Point(num2);
			for (float num3 = 0f; num3 < 360f; num3 += astep)
			{
				Point pt2 = pt + point.GetRotated(num3);
				num = RealmIDAt(pt2);
				if (num > 0)
				{
					return num;
				}
			}
		}
		return 0;
	}

	public int GetNearbyRealm(Point pt, float max_dist = 16f, float rstep = 2f, float astep = 30f)
	{
		if (realm_id_map == null)
		{
			return 0;
		}
		int num = RealmIDAt(pt);
		if (num != 0)
		{
			return num;
		}
		for (float num2 = rstep; num2 <= max_dist; num2 += rstep)
		{
			Point point = new Point(num2);
			for (float num3 = 0f; num3 < 360f; num3 += astep)
			{
				Point pt2 = pt + point.GetRotated(num3);
				num = RealmIDAt(pt2);
				if (num != 0)
				{
					return num;
				}
			}
		}
		return 0;
	}

	public Realm GetRealm(Point pt)
	{
		int rid = RealmIDAt(pt);
		return GetRealm(rid);
	}

	public TerrainType GetTerrainType(Point pt)
	{
		if (terrain_types == null)
		{
			return TerrainType.Plains;
		}
		return terrain_types.GetTerrainType(pt);
	}

	public ClimateZoneType GetClimateZone(Point pt)
	{
		if (climate_zones == null)
		{
			return ClimateZoneType.Temperate;
		}
		return climate_zones.GetZoneType(pt);
	}

	public Kingdom GetKingdom(Point pt)
	{
		Realm realm = GetRealm(pt);
		if (realm == null)
		{
			return null;
		}
		return GetKingdom(realm.kingdom_id);
	}

	public void InitTradeCenters(ProvinceFeatureDistribution distribution)
	{
		if (rules.MapIsShattered())
		{
			economy.InitTCTimingParametersForShatterdMap();
			return;
		}
		List<int> list = new List<int>();
		if (!rules.starting_trade_center_for_players)
		{
			for (int i = 0; i < game.campaign.playerDataPersistent.Length; i++)
			{
				string kingdomName = game.campaign.GetKingdomName(i);
				if (!string.IsNullOrEmpty(kingdomName))
				{
					Kingdom kingdom = game.GetKingdom(kingdomName);
					if (kingdom != null)
					{
						list.Add(kingdom.id);
					}
				}
			}
		}
		economy.InitTradeCenterRealms(distribution, list);
	}

	public void GenerateKingdomRelations()
	{
		if (kingdoms == null)
		{
			return;
		}
		DT.Def def = game.dt.FindDef("RelationUtils.InitialRelationship");
		if (def == null || def.field == null)
		{
			return;
		}
		float min = def.field.GetFloat("rnd_min", null, RelationUtils.Def.minRelationship);
		float max = def.field.GetFloat("rnd_max", null, RelationUtils.Def.maxRelationship);
		float num = def.field.GetFloat("no_relations_distance", null, 5f);
		float num2 = def.field.GetFloat("not_neighbour_modifier", null, 0.5f);
		float num3 = def.field.GetFloat("war_threshold", null, -500f);
		float num4 = def.field.GetFloat("chance_join_existing_war", null, 50f);
		float num5 = def.field.GetFloat("trade_thresold", null, 500f);
		int num6 = def.field.GetInt("trades_base", null, 1);
		int num7 = def.field.GetInt("provinces_per_trade", null, 1);
		float num8 = def.field.GetFloat("trades_max_distance", null, 500f);
		float num9 = def.field.GetFloat("marriage_bonus", null, 250f);
		int maxPlayerInitialWars = GetMaxPlayerInitialWars();
		List<Kingdom> list = new List<Kingdom>();
		for (int i = 1; i <= kingdoms.Count; i++)
		{
			Kingdom kingdom = kingdoms[i - 1];
			int num10 = num6 + (int)Math.Round((double)kingdom.realms.Count / (double)num7, MidpointRounding.AwayFromZero);
			list.Clear();
			if (kingdom.IsDefeated())
			{
				continue;
			}
			for (int j = i + 1; j <= kingdoms.Count; j++)
			{
				Kingdom kingdom2 = kingdoms[j - 1];
				if (kingdom2.IsDefeated())
				{
					continue;
				}
				KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, kingdom2, calc_fade: true, create_if_not_found: true);
				int num11 = kingdom.DistanceToKingdom(kingdom2);
				kingdomAndKingdomRelation.peace_time = new Time((long)(RelationUtils.Def.truce_time * 60f * -1000f));
				if ((float)num11 >= num)
				{
					kingdomAndKingdomRelation.perm_relationship = 0f;
				}
				else
				{
					kingdomAndKingdomRelation.perm_relationship = Random(min, max);
					if (!kingdom.neighbors.Contains(kingdom2))
					{
						kingdomAndKingdomRelation.perm_relationship *= num2;
					}
				}
				for (int k = 0; k < kingdom.marriages.Count; k++)
				{
					if (kingdom.marriages[k].GetOtherKingdom(kingdom) == kingdom2)
					{
						kingdomAndKingdomRelation.stance |= RelationUtils.Stance.Marriage;
						kingdomAndKingdomRelation.perm_relationship += num9;
						break;
					}
				}
				if (kingdom.sovereignState == kingdom2 || kingdom2.sovereignState == kingdom)
				{
					kingdomAndKingdomRelation.stance |= RelationUtils.Stance.AnyVassalage;
				}
				kingdomAndKingdomRelation.OnChanged(kingdom, kingdom2);
				if (kingdomAndKingdomRelation.perm_relationship >= num5 && (float)num11 <= num8)
				{
					list.Add(kingdom2);
				}
			}
			while (kingdom.tradeAgreementsWith.Count < num10 && list.Count > 0)
			{
				Kingdom kingdom3 = list[game.Random(0, list.Count)];
				list.Remove(kingdom3);
				int num12 = num6 + (int)Math.Round((double)kingdom3.realms.Count / (double)num7, MidpointRounding.AwayFromZero);
				if (kingdom3.tradeAgreementsWith.Count < num12)
				{
					kingdom.SetStance(kingdom3, RelationUtils.Stance.Trade);
				}
			}
		}
		for (int l = 1; l <= kingdoms.Count; l++)
		{
			Kingdom kingdom4 = kingdoms[l - 1];
			if (kingdom4.IsDefeated() || kingdom4.IsPapacy())
			{
				continue;
			}
			int playerIndex = campaign.GetPlayerIndex(kingdom4);
			bool flag = playerIndex == -1 || kingdom4.wars.Count < maxPlayerInitialWars;
			for (int m = l + 1; m <= kingdoms.Count; m++)
			{
				Kingdom kingdom5 = kingdoms[m - 1];
				if (kingdom5.IsDefeated() || kingdom5.IsPapacy())
				{
					continue;
				}
				int playerIndex2 = campaign.GetPlayerIndex(kingdom5);
				if (playerIndex != -1 && playerIndex2 != -1 && CampaignUtils.GetTeam(campaign, playerIndex) == CampaignUtils.GetTeam(campaign, playerIndex2))
				{
					continue;
				}
				bool flag2 = playerIndex2 == -1 || kingdom5.wars.Count < maxPlayerInitialWars;
				KingdomAndKingdomRelation kingdomAndKingdomRelation2 = KingdomAndKingdomRelation.Get(kingdom4, kingdom5);
				if (kingdomAndKingdomRelation2.stance.IsMarriage() || kingdomAndKingdomRelation2.stance.IsAnyVassalage() || !(kingdomAndKingdomRelation2.perm_relationship < num3) || !kingdom4.neighbors.Contains(kingdom5))
				{
					continue;
				}
				bool flag3 = false;
				if ((float)game.Random(0, 100) < num4)
				{
					if (flag2)
					{
						int num13 = game.Random(0, kingdom4.wars.Count);
						for (int n = 0; n < kingdom4.wars.Count; n++)
						{
							War war = kingdom4.wars[(n + num13) % kingdom4.wars.Count];
							int side = war.GetSide(kingdom4);
							int side2 = war.EnemySide(side);
							if (war.CanJoin(kingdom5, side2) && (playerIndex2 == -1 || !war.InitialWarContainsPlayersOfTeamOnSide(CampaignUtils.GetTeam(campaign, playerIndex2), side)))
							{
								flag3 = true;
								war.Join(kingdom5, side2, War.InvolvementReason.GameStartRelations);
								break;
							}
						}
					}
					if (flag && !flag3)
					{
						int num14 = game.Random(0, kingdom5.wars.Count);
						for (int num15 = 0; num15 < kingdom5.wars.Count; num15++)
						{
							War war2 = kingdom5.wars[(num15 + num14) % kingdom5.wars.Count];
							int side3 = war2.GetSide(kingdom5);
							int side4 = war2.EnemySide(side3);
							if (war2.CanJoin(kingdom4, side4) && (playerIndex == -1 || !war2.InitialWarContainsPlayersOfTeamOnSide(CampaignUtils.GetTeam(campaign, playerIndex), side3)))
							{
								flag3 = true;
								war2.Join(kingdom4, side4, War.InvolvementReason.GameStartRelations);
								break;
							}
						}
					}
				}
				if (!flag3 && (campaign == null || (flag && flag2)) && (!kingdom5.IsVassal() || game.Random(0, 4) <= 0))
				{
					kingdom4.StartWarWith(kingdom5, War.InvolvementReason.GameStartRelations);
				}
			}
		}
		for (int num16 = 1; num16 <= kingdoms.Count; num16++)
		{
			Kingdom kingdom6 = kingdoms[num16 - 1];
			if (kingdom6.diplomacyReasons.Count > 0)
			{
				kingdom6.diplomacyReasons.Clear();
				kingdom6.SendState<Kingdom.DiplomacyReasonsState>();
			}
		}
	}

	public void InitEmperorOfTheWorld(bool new_game)
	{
		emperorOfTheWorld.Init(new_game);
	}

	public void InitRealmsAndKingdoms(bool new_game)
	{
		if (realms == null || kingdoms == null)
		{
			return;
		}
		BeginProfileSection("Init Realms");
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm == null)
			{
				Warning("Realm " + i + " is null");
				continue;
			}
			if (realm.castle == null && realm.id > 0)
			{
				realm.Warning("Realm has no castle");
			}
			realm.SetLogicNeighborsRestricted();
			realm.InitPopMajority();
		}
		EndProfileSection("Init Realms");
		BeginProfileSection("Init Kingdoms");
		for (int j = 0; j < kingdoms.Count; j++)
		{
			Kingdom kingdom = kingdoms[j];
			kingdom.wars = new List<War>();
			kingdom.IsDefeated();
		}
		EndProfileSection("Init Kingdoms");
		if (OfferGenerator.instance == null)
		{
			OfferGenerator.instance = new OfferGenerator(this);
		}
		InitComponents(new_game);
	}

	public void InitComponents(bool new_game)
	{
		BeginProfileSection("Economy.Init");
		economy.Init(new_game);
		EndProfileSection("Economy.Init");
		BeginProfileSection("KingdomRankings.Init");
		rankings.Init(new_game);
		EndProfileSection("KingdomRankings.Init");
		BeginProfileSection("GreatPowers.Init");
		great_powers.Init(new_game);
		EndProfileSection("GreatPowers.Init");
		BeginProfileSection("EmperorOfTheWorld.Init");
		InitEmperorOfTheWorld(new_game);
		EndProfileSection("EmperorOfTheWorld.Init");
		BeginProfileSection("Init AI");
		ai?.Init(this);
		EndProfileSection("Init AI");
	}

	public void ShutdownComponents()
	{
		ai?.Shutdown();
		economy?.Shutdown();
		rankings?.Shutdown();
		great_powers?.Shutdown();
		emperorOfTheWorld?.Shutdown();
	}

	public void DestroyComponents()
	{
		cultures?.OnDestroy();
		cultures = null;
	}

	private void GenerateProvinceFeatureTags(ProvinceFeatureDistribution distribution)
	{
		for (int i = 0; i < landRealmsCount; i++)
		{
			Realm r = realms[i];
			GenerateProvinceFeatureTags(r, distribution);
		}
	}

	private void GenerateProvinceFeatureTags(Realm r, ProvinceFeatureDistribution distribution)
	{
		if (r == null || r.settlements == null || r.settlements.Count == 0)
		{
			return;
		}
		if (distribution != null)
		{
			List<string> desiredProvinceFeaturesType = distribution.GetDesiredProvinceFeaturesType(r);
			if (desiredProvinceFeaturesType != null)
			{
				for (int i = 0; i < desiredProvinceFeaturesType.Count; i++)
				{
					if (!r.features.Contains(desiredProvinceFeaturesType[i]))
					{
						r.features.Add(desiredProvinceFeaturesType[i]);
					}
				}
				return;
			}
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int j = 0; j < r.settlements.Count; j++)
		{
			Settlement settlement = r.settlements[j];
			if (settlement != null && settlement.def != null && settlement.def.enable_features != null && settlement.def.enable_features.Length != 0)
			{
				int value = 0;
				dictionary.TryGetValue(settlement.def.id, out value);
				dictionary[settlement.def.id] = value + 1;
			}
		}
		foreach (KeyValuePair<string, int> item3 in dictionary)
		{
			string key = item3.Key;
			int value2 = item3.Value;
			Settlement.Def def = defs.Get<Settlement.Def>(key);
			List<string> list = new List<string>(def.enable_features);
			int num = 0;
			for (int k = 0; k < value2; k++)
			{
				if (list.Count <= 0)
				{
					break;
				}
				int index = Random(0, list.Count);
				string text = list[index];
				list.RemoveAt(index);
				ProvinceFeature.Def def2 = game.defs.Get<ProvinceFeature.Def>(text);
				if (def2 == null || (def2.requre_costal && !r.HasCostalCastle()) || (def2.requre_distant_port && !r.HasDistantPort()) || r.features.Contains(text) || (float)Random(0, 100) >= def2.GetSpawnChance(r))
				{
					continue;
				}
				r.features.Add(text);
				if (++num >= def.max_allowed_features_per_settlement_type)
				{
					break;
				}
				if (def.exclusive_features != null && def.exclusive_features.Contains(text))
				{
					for (int l = 0; l < def.exclusive_features.Count; l++)
					{
						string item = def.exclusive_features[l];
						list.Remove(item);
					}
				}
			}
			if (num <= 0 && def.default_features != null && def.default_features.Count > 0)
			{
				int index2 = Random(0, def.default_features.Count);
				string item2 = def.default_features[index2];
				if (!r.features.Contains(item2))
				{
					r.features.Add(item2);
				}
			}
		}
	}

	public Realm RealmWave(Realm rStart, int max_depth, RealmWaveCallback callback = null, object param = null, bool use_logic_neighbors = false)
	{
		if (rStart == null)
		{
			return null;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			realm.wave_depth = -1;
			realm.wave_prev = null;
			realm.wave_eval = 0f;
		}
		Queue<Realm> queue = new Queue<Realm>(64);
		queue.Enqueue(rStart);
		rStart.wave_depth = 0;
		while (queue.Count > 0)
		{
			Realm realm2 = queue.Dequeue();
			int wave_depth = realm2.wave_depth;
			bool push_neighbors = true;
			bool stop = false;
			callback?.Invoke(realm2, rStart, wave_depth, param, ref push_neighbors, ref stop);
			if (stop)
			{
				return realm2;
			}
			if (!push_neighbors || (max_depth > 0 && wave_depth >= max_depth))
			{
				continue;
			}
			List<Realm> list = (use_logic_neighbors ? realm2.logicNeighborsRestricted : realm2.neighbors);
			for (int j = 0; j < list.Count; j++)
			{
				Realm realm3 = list[j];
				if (realm3.wave_depth < 0)
				{
					realm3.wave_depth = wave_depth + 1;
					realm3.wave_prev = realm2;
					realm3.wave_eval = realm2.wave_eval;
					queue.Enqueue(realm3);
				}
			}
		}
		return null;
	}

	public Realm RealmWave(List<Realm> rStart, int max_depth, RealmWaveWithListCallback callback = null, object param = null, bool use_logic_neighbors = false)
	{
		if (rStart == null)
		{
			return null;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			realm.wave_depth = -1;
			realm.wave_prev = null;
			realm.wave_eval = 0f;
		}
		Queue<Realm> queue = new Queue<Realm>(64);
		for (int j = 0; j < rStart.Count; j++)
		{
			queue.Enqueue(rStart[j]);
			rStart[j].wave_depth = 0;
		}
		while (queue.Count > 0)
		{
			Realm realm2 = queue.Dequeue();
			int wave_depth = realm2.wave_depth;
			bool push_neighbors = true;
			bool stop = false;
			callback?.Invoke(realm2, rStart, wave_depth, param, ref push_neighbors, ref stop);
			if (stop)
			{
				return realm2;
			}
			if (!push_neighbors || (max_depth > 0 && wave_depth >= max_depth))
			{
				continue;
			}
			List<Realm> list = (use_logic_neighbors ? realm2.logicNeighborsRestricted : realm2.neighbors);
			for (int k = 0; k < list.Count; k++)
			{
				Realm realm3 = list[k];
				if (realm3.wave_depth < 0)
				{
					realm3.wave_depth = wave_depth + 1;
					realm3.wave_prev = realm2;
					realm3.wave_eval = realm2.wave_eval;
					queue.Enqueue(realm3);
				}
			}
		}
		return null;
	}

	public int RealmDistance(int rid1, int rid2, bool goThroughSeas = true, bool useLogicNeighbors = false, int maxDepth = int.MaxValue, bool sameKingdom = false)
	{
		if (maxDepth < 1)
		{
			return -1;
		}
		Realm realm = GetRealm(rid1);
		if (realm == null || realm.id == 0)
		{
			return -1;
		}
		Kingdom kingdom = realm.GetKingdom();
		int item = 0;
		tmp_que_realms.Clear();
		tmp_distances.Clear();
		tmp_processed_realms.Clear();
		tmp_que_realms.Enqueue(realm);
		tmp_distances.Enqueue(item);
		tmp_processed_realms.Add(realm);
		while (tmp_que_realms.Count > 0)
		{
			realm = tmp_que_realms.Dequeue();
			item = tmp_distances.Dequeue() + 1;
			if (item > maxDepth)
			{
				return -1;
			}
			List<Realm> list = realm.neighbors;
			if (useLogicNeighbors && !goThroughSeas)
			{
				list = realm.logicNeighborsRestricted;
			}
			foreach (Realm item2 in list)
			{
				if (item2 != null && !tmp_processed_realms.Contains(item2) && (!item2.IsSeaRealm() || goThroughSeas) && (!sameKingdom || kingdom == item2.GetKingdom()))
				{
					if (item2.id == rid2)
					{
						return item;
					}
					tmp_que_realms.Enqueue(item2);
					tmp_distances.Enqueue(item);
					tmp_processed_realms.Add(item2);
				}
			}
		}
		return -1;
	}

	public int KingdomDistance(int kid1, int kid2, int maxDepth = int.MaxValue)
	{
		Kingdom kingdom = GetKingdom(kid1);
		if (kingdom == null)
		{
			return -1;
		}
		int item = 0;
		tmp_que_kingdoms.Clear();
		tmp_distances.Clear();
		tmp_processed_kingdoms.Clear();
		tmp_que_kingdoms.Enqueue(kingdom);
		tmp_distances.Enqueue(item);
		tmp_processed_kingdoms.Add(kingdom);
		while (tmp_que_kingdoms.Count > 0)
		{
			kingdom = tmp_que_kingdoms.Dequeue();
			item = tmp_distances.Dequeue() + 1;
			if (item >= maxDepth)
			{
				return -1;
			}
			foreach (Kingdom neighbor in kingdom.neighbors)
			{
				if (neighbor != null && !tmp_processed_kingdoms.Contains(neighbor))
				{
					if (neighbor.id == kid2)
					{
						return item;
					}
					tmp_que_kingdoms.Enqueue(neighbor);
					tmp_distances.Enqueue(item);
					tmp_processed_kingdoms.Add(neighbor);
				}
			}
		}
		return -1;
	}

	public Realm ClosestKingomRealmToKingdom(int kid1, int kid2)
	{
		Kingdom kingdom = GetKingdom(kid2);
		Kingdom kingdom2 = GetKingdom(kid1);
		if (kingdom2 == null || kingdom2.id == 0 || kingdom2.realms.Count == 0 || kingdom == null || kingdom.id == 0 || kingdom.realms.Count == 0)
		{
			return null;
		}
		foreach (Realm realm2 in kingdom2.game.realms)
		{
			realm2.wave_depth = -1;
		}
		int item = 0;
		tmp_que_realms.Clear();
		tmp_distances.Clear();
		tmp_processed_realms.Clear();
		foreach (Realm realm3 in kingdom.realms)
		{
			tmp_processed_realms.Add(realm3);
			if (realm3.IsBorder())
			{
				tmp_que_realms.Enqueue(realm3);
				tmp_distances.Enqueue(item);
			}
		}
		while (tmp_que_realms.Count > 0)
		{
			Realm realm = tmp_que_realms.Dequeue();
			item = tmp_distances.Dequeue() + 1;
			foreach (Realm item2 in realm.logicNeighborsRestricted)
			{
				if (item2 != null && item2.kingdom_id != kingdom.id && !tmp_processed_realms.Contains(item2))
				{
					if (kingdom2.realms.Contains(item2))
					{
						item2.wave_depth = item;
						return item2;
					}
					item2.wave_depth = item;
					tmp_que_realms.Enqueue(item2);
					tmp_distances.Enqueue(item);
					tmp_processed_realms.Add(item2);
				}
			}
		}
		return null;
	}

	public int KingdomAndRealmDistance(int kid, int rid, int maxDepth = int.MaxValue)
	{
		Kingdom kingdom = GetKingdom(kid);
		if (kingdom == null || kingdom.id == 0)
		{
			return -1;
		}
		int item = 0;
		tmp_que_realms.Clear();
		tmp_distances.Clear();
		tmp_processed_realms.Clear();
		foreach (Realm realm2 in kingdom.realms)
		{
			if (realm2.id == rid)
			{
				return 0;
			}
			tmp_que_realms.Enqueue(realm2);
			tmp_distances.Enqueue(item);
			tmp_processed_realms.Add(realm2);
		}
		Realm realm = null;
		while (tmp_que_realms.Count > 0)
		{
			realm = tmp_que_realms.Dequeue();
			item = tmp_distances.Dequeue() + 1;
			if (item >= maxDepth)
			{
				return -1;
			}
			foreach (Realm item2 in realm.logicNeighborsRestricted)
			{
				if (item2 != null && !tmp_processed_realms.Contains(item2))
				{
					if (item2.id == rid)
					{
						return item;
					}
					tmp_que_realms.Enqueue(item2);
					tmp_distances.Enqueue(item);
					tmp_processed_realms.Add(item2);
				}
			}
		}
		return -1;
	}

	public int GetKingdomId()
	{
		if (multiplayer == null)
		{
			return 0;
		}
		return multiplayer.playerData.kingdomId;
	}

	public override Kingdom GetKingdom()
	{
		int kingdomId = GetKingdomId();
		return GetKingdom(kingdomId);
	}

	public Kingdom GetPlayerKingdom(string player_id)
	{
		Multiplayer.PlayerData byGUID = Multiplayer.CurrentPlayers.GetByGUID(player_id);
		if (byGUID == null)
		{
			return null;
		}
		return GetKingdom(byGUID.kingdomId);
	}

	public Kingdom GetPlayerKingdom(int pid)
	{
		Multiplayer.PlayerData byPID = Multiplayer.CurrentPlayers.GetByPID(pid);
		if (byPID == null)
		{
			return null;
		}
		return GetKingdom(byPID.kingdomId);
	}

	public void SetAnyKingdom(int pid, Kingdom kNew, bool repick = false)
	{
		if (multiplayer == null)
		{
			return;
		}
		if (kNew == null || pid <= 0)
		{
			Warning($"Setting Kingdom ({kNew}) for player with PID: {pid}");
			return;
		}
		Multiplayer.PlayerData byPID = Multiplayer.CurrentPlayers.GetByPID(pid);
		if (byPID == null)
		{
			Warning($"Null player data for player with PID: {pid}");
			return;
		}
		if (!kNew.IsAuthority())
		{
			SendEvent(new SwitchKingdomEvent(pid, kNew.id, repick));
			return;
		}
		Kingdom kingdom = GetKingdom(byPID.kingdomId);
		bool num = kingdom?.is_local_player ?? false;
		if (campaign != null && campaign.playerDataPersistent != null)
		{
			int playerIndex = campaign.GetPlayerIndex(byPID.id);
			if (playerIndex >= 0 && playerIndex < campaign.playerDataPersistent.Length)
			{
				campaign.SetPlayerKingdomName(playerIndex, kNew.Name, null);
				if (repick && kingdom != null && kingdom != kNew && kingdom.IsDefeated())
				{
					int num2 = campaign.playerDataPersistent[playerIndex].GetVar("repicks").Int();
					Campaign obj = campaign;
					if (obj != null)
					{
						obj.playerDataPersistent[playerIndex].Set("repicks", num2 + 1);
					}
				}
			}
		}
		game.multiplayer.UpdatePlayerInCurrentPlayers(pid, kNew.Name);
		if (num)
		{
			kingdom?.NotifyListeners("local_player_changed");
			kNew?.NotifyListeners("local_player_changed");
		}
		if (repick)
		{
			kNew.NotifyListeners("repick", kingdom);
		}
	}

	public void SetKingdom(Kingdom kNew, bool repick = false)
	{
		if (kNew != null && multiplayer != null && multiplayer.playerData != null)
		{
			SetAnyKingdom(multiplayer.playerData.pid, kNew, repick);
		}
	}

	public void SetForceEndlessGame(bool val)
	{
		force_endless_game = val;
	}

	public bool IsEndlessGame()
	{
		if (force_endless_game)
		{
			return true;
		}
		if (dev_settings != null)
		{
			return dev_settings.force_endless_game;
		}
		return false;
	}

	public bool ValidateEndGame(Kingdom k = null, Kingdom defeatedKingdom = null)
	{
		if (!IsAuthority())
		{
			return false;
		}
		if (rules == null)
		{
			return false;
		}
		if (IsEndlessGame())
		{
			return false;
		}
		if (!Kingdom.CacheRBS.RequestEndGame(k, defeatedKingdom, this))
		{
			return false;
		}
		bool num = rules.OnValidateEndGame(k, defeatedKingdom);
		if (num)
		{
			EndGame(rules.end_game_reason);
		}
		return num;
	}

	public void EndGame(string reason = null)
	{
		if (!AssertAuthority() || rules == null)
		{
			return;
		}
		bool flag = rules.main_goal != "None" && rules.main_goal == reason;
		HashSet<int> hashSet = new HashSet<int>();
		if (!game.IsMultiplayer())
		{
			Kingdom kingdom = game.GetKingdom();
			if (rules.singlePlayerWinner != null)
			{
				if (kingdom == rules.singlePlayerWinner)
				{
					kingdom.FireEvent("victory", reason);
					hashSet.Add(kingdom.id);
					if (flag)
					{
						rules.ChangeMainGoal("None");
					}
					return;
				}
				if (rules.main_goal == "DestroyKingdom")
				{
					kingdom.FireEvent("defeat", reason);
					hashSet.Add(kingdom.id);
					if (flag)
					{
						rules.ChangeMainGoal("None");
					}
					return;
				}
			}
			else
			{
				kingdom.FireEvent("defeat", reason);
				hashSet.Add(kingdom.id);
			}
		}
		if (rules.winningTeam != null)
		{
			foreach (Team team in teams.teams)
			{
				foreach (Player player in team.players)
				{
					Kingdom kingdomByGUID = Multiplayer.CurrentPlayers.GetKingdomByGUID(player.id);
					if (kingdomByGUID != null && !hashSet.Contains(kingdomByGUID.id))
					{
						if (rules.winningTeam != null && rules.winningTeam.id == team.id)
						{
							kingdomByGUID.FireEvent("victory", reason);
						}
						else
						{
							kingdomByGUID.FireEvent("defeat", reason);
						}
						hashSet.Add(kingdomByGUID.id);
					}
				}
			}
		}
		else
		{
			string id = "defeat";
			foreach (Team team2 in teams.teams)
			{
				foreach (Player player2 in team2.players)
				{
					Kingdom kingdom2 = GetKingdom(player2.kingdom_id);
					if (kingdom2 != null && !hashSet.Contains(kingdom2.id))
					{
						kingdom2.FireEvent(id, reason);
						hashSet.Add(kingdom2.id);
					}
				}
			}
		}
		if (flag)
		{
			rules.ChangeMainGoal("None");
		}
	}

	public void ForceEndGame(Kingdom winner, string reason)
	{
		if (AssertAuthority() && winner != null && rules != null && !IsEndlessGame())
		{
			rules.SetWinner(winner, reason);
			campaign.campaignData.Set("end_game_triggered", true);
			rules.end_game_triggered = true;
			rules.SetEndGameReason(reason);
			EndGame(reason);
		}
	}

	private int GetInfluence(Realm r, int kingdom_id)
	{
		if (r.influence.kingdom_id == kingdom_id)
		{
			return r.influence.influence;
		}
		return 0;
	}

	public bool IsEnemy(int kid1, int kid2)
	{
		return GetKingdom(kid1)?.IsEnemy(kid2) ?? false;
	}

	public new bool IsAuthority()
	{
		if (isLoadingSaveGame)
		{
			return false;
		}
		if (multiplayer == null || !multiplayer.IsOnline())
		{
			return true;
		}
		return multiplayer.type != Multiplayer.Type.Client;
	}

	public static KingdomAndKingdomRelation rel(Kingdom k1, Kingdom k2)
	{
		return KingdomAndKingdomRelation.Get(k1, k2);
	}

	public Kingdom GetIndependenceKingdom(List<Realm> realms, Religion religion = null, List<Kingdom> excludedKingdoms = null)
	{
		if (realms == null)
		{
			return null;
		}
		Kingdom kingdom = GetIndependenceNewKindom(realms);
		if (kingdom == null)
		{
			kingdom = GetIndependenceExistingKindom(realms, religion, excludedKingdoms);
		}
		return kingdom;
	}

	public Kingdom GetIndependenceNewKindom(List<Realm> realms)
	{
		if (realms == null)
		{
			return null;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			Kingdom independenceNewKingdom = realms[i].GetIndependenceNewKingdom();
			if (independenceNewKingdom != null)
			{
				return independenceNewKingdom;
			}
		}
		return null;
	}

	public Kingdom GetIndependenceExistingKindom(List<Realm> realms, Religion religion = null, List<Kingdom> excludedKingdoms = null)
	{
		if (realms == null)
		{
			return null;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			Kingdom independenceExistingKingdom = realms[i].GetIndependenceExistingKingdom(religion, excludedKingdoms);
			if (independenceExistingKingdom != null)
			{
				return independenceExistingKingdom;
			}
		}
		return null;
	}

	public Kingdom TryDeclareIndependence(List<Realm> realms, Character king = null, List<Character> courtChars = null, List<Army> armies = null, Religion religion = null)
	{
		if (realms == null)
		{
			return null;
		}
		Kingdom independenceKingdom = GetIndependenceKingdom(realms, religion);
		if (independenceKingdom != null)
		{
			independenceKingdom.DeclareIndependenceOrJoin(realms, king, courtChars, armies, religion);
			return independenceKingdom;
		}
		return independenceKingdom;
	}

	public float GetSpeed()
	{
		return speed;
	}

	public void SetSpeed(float speed, int sender_pid = -1)
	{
		if (multiplayer == null)
		{
			return;
		}
		if (!IsAuthority() && sender_pid < 0)
		{
			SendEvent(new GameSpeedEvent(speed));
			return;
		}
		if (sender_pid < 0)
		{
			sender_pid = multiplayer.playerData.pid;
		}
		if (IsPaused())
		{
			pause.DelRequest("ManualPause", sender_pid);
			if (IsPaused())
			{
				return;
			}
		}
		if (IsAuthority())
		{
			if (speed < rules.min_speed)
			{
				speed = rules.min_speed;
			}
			if (speed > rules.max_speed)
			{
				speed = rules.max_speed;
			}
		}
		this.speed = speed;
		last_speed_control_pid = sender_pid;
		SendState<GameSpeedState>();
		NotifyListeners("game_speed_changed", speed);
	}

	public bool IsPaused()
	{
		if (pause != null)
		{
			return pause.is_paused;
		}
		return false;
	}

	public void BroadcastRadioEvent(string def_id, Vars vars)
	{
		DT.Field field = dt.Find(def_id);
		Event obj = new Event(this, def_id, vars, null, notify_listeners: false);
		Kingdom k = Vars.Get<Kingdom>(vars, "kingdom_a");
		Kingdom k2 = Vars.Get<Kingdom>(vars, "kingdom_b");
		Kingdom k3 = Vars.Get<Kingdom>(vars, "kingdom_c");
		int num = field.GetInt("important");
		if (num == 0)
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (int uniqueKingdomID in Multiplayer.CurrentPlayers.GetUniqueKingdomIDs())
		{
			Kingdom kingdom = GetKingdom(uniqueKingdomID);
			switch (num)
			{
			case 1:
				if (kingdom.CaresAbout(k) || kingdom.CaresAbout(k2) || kingdom.CaresAbout(k3))
				{
					list.Add(uniqueKingdomID);
				}
				break;
			case 2:
				list.Add(uniqueKingdomID);
				break;
			}
		}
		if (list.Count > 0)
		{
			obj.send_to_kingdoms = list;
			FireEvent(obj);
		}
	}

	public DevSettings.Def GetDevSettingsDef()
	{
		if (dev_settings == null)
		{
			dev_settings = game.defs.GetBase<DevSettings.Def>();
		}
		return dev_settings;
	}

	public float GetPerDifficultyFloat(DT.Field f, string key, float def_val = 1f)
	{
		if (f == null)
		{
			return def_val;
		}
		int num = f.NumValues();
		if (num <= 0)
		{
			return def_val;
		}
		int num2 = ((rules == null) ? 1 : rules.ai_difficulty);
		int idx = ((num2 < num) ? num2 : (num - 1));
		return f.Float(idx, null, 1f);
	}

	public int GetPerDifficultyInt(DT.Field f, string key, int def_val = 0)
	{
		if (f == null)
		{
			return def_val;
		}
		int num = f.NumValues();
		if (num <= 0)
		{
			return def_val;
		}
		int num2 = ((rules == null) ? 1 : rules.ai_difficulty);
		int idx = ((num2 < num) ? num2 : (num - 1));
		return f.Int(idx, null, def_val);
	}

	public bool GetPerDifficultyBool(DT.Field f, string key, bool def_val = false)
	{
		if (f == null)
		{
			return def_val;
		}
		int num = f.NumValues();
		if (num <= 0)
		{
			return def_val;
		}
		int num2 = ((rules == null) ? 1 : rules.ai_difficulty);
		int idx = ((num2 < num) ? num2 : (num - 1));
		return f.Bool(idx, null, def_val);
	}

	public float GetDevSettingsFloat(string key, float def_val = 0f)
	{
		DevSettings.Def devSettingsDef = GetDevSettingsDef();
		if (devSettingsDef?.field == null)
		{
			return def_val;
		}
		return GetPerDifficultyFloat(devSettingsDef.field, key, def_val);
	}

	public float GetAIResourcesBoost(string key)
	{
		DevSettings.Def devSettingsDef = GetDevSettingsDef();
		if (devSettingsDef?.ai_resource_boost_field == null)
		{
			return 1f;
		}
		DT.Field field = devSettingsDef.ai_resource_boost_field.FindChild(key);
		if (field == null)
		{
			field = devSettingsDef.ai_resource_boost_field;
		}
		return GetPerDifficultyFloat(field, key);
	}

	public float GetMinRebelPopTime()
	{
		DevSettings.Def devSettingsDef = GetDevSettingsDef();
		if (devSettingsDef?.min_rebel_pop_time_field == null)
		{
			return 1f;
		}
		return GetPerDifficultyFloat(devSettingsDef.min_rebel_pop_time_field, null);
	}

	public int GetMaxPlayerInitialWars()
	{
		return GetPerDifficultyInt(GetDevSettingsDef()?.max_player_initial_wars, null);
	}

	public bool ProvincesAlwaysInitiallyConverted()
	{
		return GetPerDifficultyBool(GetDevSettingsDef()?.provinces_always_initially_converted, null, def_val: true);
	}

	public void SetCheatLevel(CheatLevel level)
	{
		cheat_level = level;
		SendState<CheatLevelState>();
	}

	public static bool CheckCheatLevel(CheatLevel level, string cheat, bool warning = true)
	{
		if (cheat_level < level)
		{
			if (warning)
			{
				Log($"{cheat}: Command ignored! Required cheat level is {level}, but current is {cheat_level}.", LogType.Warning);
			}
			return false;
		}
		return true;
	}

	public static bool CheckCheatLevelNoWarning(CheatLevel level)
	{
		if (cheat_level < level)
		{
			return false;
		}
		return true;
	}

	public static string BranchName()
	{
		if (branch_name != null)
		{
			return branch_name;
		}
		THQNORequest.Connect();
		StringBuilder stringBuilder = new StringBuilder(128);
		THQNORequest.GetSteamBetaName(stringBuilder, 128u);
		branch_name = stringBuilder.ToString();
		if (string.IsNullOrEmpty(branch_name))
		{
			branch_name = "Local";
		}
		return branch_name;
	}

	public static bool IsInternalBranch()
	{
		switch (BranchName())
		{
		case "Editor":
			return true;
		case "internal":
		case "internal2":
			return true;
		case "internal_loca":
			return true;
		default:
			return false;
		}
	}

	public Game(Multiplayer multiplayer)
		: base(multiplayer)
	{
		Error("created from multiplayer");
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Game(multiplayer);
	}

	public static void CopyToClipboard(string text)
	{
		GUIUtility.systemCopyBuffer = text;
	}

	public static void Log(string msg, LogType type)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		switch (type)
		{
		case LogType.Error:
			UnityEngine.Debug.LogError(msg);
			break;
		case LogType.Warning:
			UnityEngine.Debug.LogWarning(msg);
			break;
		default:
			UnityEngine.Debug.Log(msg);
			break;
		}
	}

	public static void LogWithoutStackTrace(string msg, LogType type)
	{
		UnityEngine.LogType logType = GetUnityLogType(type);
		StackTraceLogType stackTraceLogType = Application.GetStackTraceLogType(logType);
		Application.SetStackTraceLogType(logType, StackTraceLogType.None);
		Log(msg, type);
		Application.SetStackTraceLogType(logType, stackTraceLogType);
		static UnityEngine.LogType GetUnityLogType(LogType lt)
		{
			switch (lt)
			{
			case LogType.Error:
				return UnityEngine.LogType.Error;
			case LogType.Warning:
				return UnityEngine.LogType.Warning;
			case LogType.Message:
				return UnityEngine.LogType.Log;
			default:
				Log($"Unmapped log type: {lt}", LogType.Error);
				return UnityEngine.LogType.Log;
			}
		}
	}
}
