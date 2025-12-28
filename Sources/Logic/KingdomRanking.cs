using System;
using System.Collections.Generic;

namespace Logic;

public class KingdomRanking : BaseObject, IVars
{
	public class Def : Logic.Def
	{
		public string name;

		public DT.Field score;

		public DT.Field tiebreakerScores;

		public bool ascending;

		public int max_fame;

		public float next_place_fame_penalty = 80f;

		public float fame_min_score_treshold;

		public bool enable_notifications = true;

		public float min_time_between_any_notifications = 300f;

		public float min_time_leading_notification = 600f;

		public float max_time_leading_notification = 600f;

		public float in_top_perc_X = 20f;

		public float in_bottom_perc_X = 20f;

		public override bool Load(Game game)
		{
			if (IsBase())
			{
				return true;
			}
			DT.Field field = base.field;
			name = field.key;
			score = field.FindChild("score");
			tiebreakerScores = field.FindChild("tiebreakers");
			max_fame = field.GetInt("max_fame", null, max_fame);
			next_place_fame_penalty = field.GetFloat("next_place_fame_penalty", null, next_place_fame_penalty);
			fame_min_score_treshold = field.GetFloat("fame_min_score_treshold", null, fame_min_score_treshold);
			if (score == null || !(score.value.obj_val is Expression))
			{
				Game.Log(field.Path(include_file: true) + ": invalid kingdom ranking score (must be expression)", Game.LogType.Error);
			}
			if (tiebreakerScores != null)
			{
				if (tiebreakerScores.NumValues() == 1)
				{
					if (!(tiebreakerScores.value.obj_val is Expression))
					{
						Game.Log(field.Path(include_file: true) + ": invalid kingdom ranking tiebreaker (must be expression)", Game.LogType.Error);
					}
				}
				else
				{
					for (int i = 0; i < tiebreakerScores.NumValues(); i++)
					{
						if (!((tiebreakerScores.value.obj_val as List<DT.SubValue>)[i].value.obj_val is Expression))
						{
							Game.Log(field.Path(include_file: true) + ": invalid kingdom ranking tiebreaker (must be expression)", Game.LogType.Error);
						}
					}
				}
			}
			ascending = field.GetBool("ascending");
			enable_notifications = field.GetBool("enable_notifications", null, enable_notifications);
			min_time_between_any_notifications = field.GetFloat("min_time_between_any_notifications", null, min_time_between_any_notifications);
			min_time_leading_notification = field.GetFloat("min_time_leading_notification", null, min_time_leading_notification);
			max_time_leading_notification = field.GetFloat("max_time_leading_notification", null, max_time_leading_notification);
			in_top_perc_X = field.GetFloat("in_top_perc_X", null, in_top_perc_X);
			in_bottom_perc_X = field.GetFloat("in_bottom_perc_X", null, in_bottom_perc_X);
			return true;
		}
	}

	public struct Row
	{
		public Kingdom kingdom;

		public float score;

		public float[] tieBreakerScores;

		public int rank;

		public int rank_group;

		public int fame;

		public override string ToString()
		{
			string text = "null";
			if (tieBreakerScores != null)
			{
				text = "[";
				for (int i = 0; i < tieBreakerScores.Length; i++)
				{
					text = text + tieBreakerScores[i] + ", ";
				}
				text += "]";
			}
			return rank + ": " + kingdom.Name + " (" + score + ") - " + fame + " - " + text;
		}
	}

	public Def def;

	public KingdomRankings rankings;

	public List<Row> rows = new List<Row>(100);

	public int last_rank;

	public readonly int first_rank = 1;

	public List<Row> cur_rows = new List<Row>(100);

	public List<KingdomRankingCategory> categories = new List<KingdomRankingCategory>();

	public Time nextNotification = Time.Zero;

	public Dictionary<int, int> lastRanks = new Dictionary<int, int>();

	public int next_kid;

	public KingdomRanking(Def def, KingdomRankings rankings)
	{
		this.def = def;
		this.rankings = rankings;
		InitNotificationTimer();
	}

	public void InitNotificationTimer()
	{
		if (rankings?.game != null && def != null)
		{
			nextNotification = rankings.game.time + rankings.game.Random(def.min_time_leading_notification, def.max_time_leading_notification);
		}
	}

	public override string ToString()
	{
		return "KingdomRanking(" + def.name + ")";
	}

	private float CalcScore(Kingdom k)
	{
		return def.score.Float(k);
	}

	private float CalcTieBreakerScore(Kingdom k, int tiebreakerIndex)
	{
		if (def.tiebreakerScores == null || tiebreakerIndex < 0 || tiebreakerIndex >= def.tiebreakerScores.NumValues())
		{
			return 0f;
		}
		return def.tiebreakerScores.Value(tiebreakerIndex, k);
	}

	public bool CalcOne(Game game)
	{
		if (def.score == null)
		{
			return false;
		}
		if (game?.kingdoms == null)
		{
			return false;
		}
		while (true)
		{
			if (next_kid >= game.kingdoms.Count)
			{
				using (Game.Profile("Ranking Finish"))
				{
					Finish(game);
				}
				return false;
			}
			Kingdom kingdom = game.GetKingdom(next_kid);
			if (kingdom == null || kingdom.IsDefeated())
			{
				next_kid++;
				continue;
			}
			if (cur_rows.Count == 0 || cur_rows[cur_rows.Count - 1].kingdom != kingdom)
			{
				float score = CalcScore(kingdom);
				cur_rows.Add(new Row
				{
					kingdom = kingdom,
					score = score
				});
				return true;
			}
			Row value = cur_rows[cur_rows.Count - 1];
			if (value.tieBreakerScores == null && def.tiebreakerScores != null)
			{
				value.tieBreakerScores = new float[def.tiebreakerScores.NumValues()];
				for (int i = 0; i < value.tieBreakerScores.Length; i++)
				{
					value.tieBreakerScores[i] = float.NaN;
				}
			}
			if (value.tieBreakerScores == null || value.tieBreakerScores.Length == 0)
			{
				break;
			}
			for (int j = 0; j < value.tieBreakerScores.Length; j++)
			{
				if (float.IsNaN(value.tieBreakerScores[j]))
				{
					value.tieBreakerScores[j] = CalcTieBreakerScore(kingdom, j);
					cur_rows[cur_rows.Count - 1] = value;
					if (j == value.tieBreakerScores.Length - 1 || float.IsNaN(value.tieBreakerScores[j]))
					{
						next_kid++;
					}
					return true;
				}
			}
		}
		next_kid++;
		return true;
	}

	public void NotifyKingdom(Kingdom k, int rank, string message, object param = null)
	{
		Game game = k.game;
		Vars vars = new Vars();
		vars.Set("kingdom", k);
		vars.Set("ranking", this);
		vars.Set("ranking_def_name", def.field.key);
		vars.Set("rank", rank);
		vars.Set("update_timers", val: true);
		k.NotifyListeners(message, vars);
		if (vars.Get<bool>("update_timers", def_val: false))
		{
			rankings.lastNotification = game.time;
			nextNotification = game.time + game.Random(def.min_time_leading_notification, def.max_time_leading_notification);
		}
	}

	public void NotifyKingdom(Row row)
	{
		Kingdom kingdom = row.kingdom;
		Game game = kingdom.game;
		if (!def.enable_notifications || rankings.lastNotification + def.min_time_between_any_notifications >= game.time)
		{
			return;
		}
		int value = 0;
		lastRanks.TryGetValue(kingdom.id, out value);
		if (row.rank == first_rank)
		{
			if (value == first_rank)
			{
				if (nextNotification <= game.time)
				{
					NotifyKingdom(kingdom, row.rank, "rankings_leading_ruler", this);
				}
			}
			else
			{
				NotifyKingdom(kingdom, row.rank, "rankings_became_leading_ruler", this);
			}
		}
		else if (value == first_rank)
		{
			NotifyKingdom(kingdom, row.rank, "rankings_lost_lead", this);
		}
	}

	public void NotifyKingdomsAndUpdateLastRanks(Game game)
	{
		if (cur_rows == null || cur_rows.Count == 0)
		{
			return;
		}
		int num = game.Random(0, cur_rows.Count);
		for (int i = 0; i < cur_rows.Count; i++)
		{
			Row row = cur_rows[(i + num) % cur_rows.Count];
			if (last_rank != first_rank)
			{
				NotifyKingdom(row);
			}
			lastRanks[row.kingdom.id] = row.rank;
		}
	}

	public void Finish(Game game)
	{
		using (Game.Profile("Ranking Finish Sort"))
		{
			cur_rows.Sort(delegate(Row a, Row b)
			{
				int num9 = (def.ascending ? 1 : (-1));
				int num10 = a.score.CompareTo(b.score);
				if (num10 != 0)
				{
					return num10 * num9;
				}
				if (def.tiebreakerScores != null && a.tieBreakerScores != null && b.tieBreakerScores != null)
				{
					for (int i = 0; i < def.tiebreakerScores.NumValues(); i++)
					{
						using (Game.Profile("Ranking Finish Sort Tiebreakers " + i))
						{
							num10 = a.tieBreakerScores[i].CompareTo(b.tieBreakerScores[i]);
							if (num10 != 0)
							{
								return num10 * num9;
							}
						}
					}
				}
				num10 = a.kingdom.realms.Count.CompareTo(b.kingdom.realms.Count);
				if (num10 != 0)
				{
					return num10 * num9;
				}
				num10 = a.kingdom.GetTotalPopulation().CompareTo(b.kingdom.GetTotalPopulation());
				return (num10 != 0) ? (num10 * num9) : (a.kingdom.id.CompareTo(b.kingdom.id) * num9);
			});
		}
		last_rank = 0;
		int num = 0;
		float num2 = float.MaxValue;
		for (int num3 = 0; num3 < cur_rows.Count; num3++)
		{
			Row value = cur_rows[num3];
			if (value.score != num2)
			{
				num++;
				num2 = value.score;
			}
			value.rank = ++last_rank;
			value.rank_group = num;
			cur_rows[num3] = value;
		}
		num = 1;
		float num4 = (float)def.max_fame * def.next_place_fame_penalty / 100f;
		float num5 = def.max_fame;
		for (int num6 = 0; num6 < cur_rows.Count; num6++)
		{
			Row value2 = cur_rows[num6];
			if (last_rank == value2.rank)
			{
				value2.fame = 0;
				cur_rows[num6] = value2;
				continue;
			}
			if (value2.score < def.fame_min_score_treshold)
			{
				value2.fame = 0;
				cur_rows[num6] = value2;
				continue;
			}
			if (value2.rank_group == num)
			{
				value2.fame = (int)num5;
				cur_rows[num6] = value2;
				continue;
			}
			num = value2.rank_group;
			float num7 = (float)Math.Round((float)def.max_fame * (float)(cur_rows.Count - value2.rank) / (float)cur_rows.Count);
			if (num7 > num4)
			{
				num5 = num4;
			}
			else
			{
				num5 = num7;
				num4 = num7;
			}
			num4 *= def.next_place_fame_penalty / 100f;
			if (num4 > 50f)
			{
				num4 -= num4 % 10f;
			}
			else
			{
				num4 -= num4 % 5f;
				if (num4 < 0f)
				{
					num4 = 0f;
				}
			}
			if (num5 < 0f)
			{
				num5 = 0f;
			}
			value2.fame = (int)num5;
			cur_rows[num6] = value2;
		}
		if (cur_rows.Count > 0 && cur_rows[0].fame != 0)
		{
			Row row = cur_rows[0];
			if (row.kingdom.is_player)
			{
				row.kingdom.NotifyListeners("player_leads_ranking", this);
				if (lastRanks.TryGetValue(row.kingdom.id, out var value3) && value3 != first_rank)
				{
					row.kingdom.NotifyListeners("player_became_leader_in_ranking", this);
				}
			}
			NotifyKingdomsAndUpdateLastRanks(game);
		}
		List<Row> list = rows;
		rows = cur_rows;
		cur_rows = list;
		cur_rows.Clear();
		next_kid = 0;
		for (int num8 = 0; num8 < categories.Count; num8++)
		{
			categories[num8].ResetScore();
		}
		game.NotifyListeners("ranking_updated", this);
	}

	public void CalcAll(Game game)
	{
		next_kid = 0;
		cur_rows.Clear();
		while (CalcOne(game))
		{
		}
	}

	public Row Find(Kingdom k)
	{
		for (int i = 0; i < rows.Count; i++)
		{
			Row result = rows[i];
			if (result.kingdom == k)
			{
				return result;
			}
		}
		return default(Row);
	}

	public Row GetRow(Kingdom k)
	{
		for (int i = 0; i < rows.Count; i++)
		{
			Row result = rows[i];
			if (result.kingdom == k)
			{
				return result;
			}
		}
		return default(Row);
	}

	public int GetRank(Kingdom k)
	{
		if (k.force_rank > 0)
		{
			return k.force_rank;
		}
		return Find(k).rank;
	}

	public int GetFame(Kingdom k)
	{
		return Find(k).fame;
	}

	public float GetScore(Kingdom k)
	{
		return Find(k).score;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"def" => def, 
			"fame" => GetFame(GetContextKingdom()), 
			"rank" => GetRank(GetContextKingdom()), 
			"score" => GetScore(GetContextKingdom()), 
			_ => Value.Unknown, 
		};
		Kingdom GetContextKingdom()
		{
			if (vars == null)
			{
				return null;
			}
			if (vars is Kingdom result)
			{
				return result;
			}
			Value var = vars.GetVar("kingdom");
			if (var.is_object)
			{
				return var.Get<Kingdom>();
			}
			return null;
		}
	}
}
