using System.Collections.Generic;

namespace Logic;

public class KingdomRankings : Component
{
	public List<KingdomRanking> rankings;

	public Time lastNotification = Time.Zero;

	public int cur_idx;

	public KingdomRankings(Game game)
		: base(game)
	{
	}

	public void Init(bool new_game)
	{
		Defs.Registry registry = base.game.defs.Get(typeof(KingdomRanking.Def));
		if (registry == null || registry.defs.Count == 0)
		{
			return;
		}
		rankings = new List<KingdomRanking>(registry.defs.Count);
		foreach (KeyValuePair<string, Def> def in registry.defs)
		{
			KingdomRanking item = new KingdomRanking(def.Value as KingdomRanking.Def, this);
			rankings.Add(item);
		}
		if (rankings.Count != 0)
		{
			InitNotificationTimers();
			UpdateNextFrame();
		}
	}

	public void Shutdown()
	{
		StopUpdating();
		rankings = null;
		lastNotification = Time.Zero;
		cur_idx = 0;
	}

	public void InitNotificationTimers()
	{
		lastNotification = base.game.time;
		if (rankings != null)
		{
			for (int i = 0; i < rankings.Count; i++)
			{
				rankings[i].InitNotificationTimer();
			}
		}
	}

	public override void OnUpdate()
	{
		UpdateNextFrame();
		if (base.game.isInVideoMode)
		{
			return;
		}
		for (int i = 0; i < 10; i++)
		{
			using (Game.Profile("Ranking " + rankings[cur_idx].def.name))
			{
				if (!rankings[cur_idx].CalcOne(base.game))
				{
					cur_idx++;
					if (cur_idx >= rankings.Count)
					{
						cur_idx = 0;
					}
				}
			}
		}
	}

	public void FullRecalc()
	{
		for (int i = 0; i < base.game.realms.Count; i++)
		{
			base.game.realms[i].InvalidateIncomes();
		}
		CalcAll("FameRanking");
		for (int j = 0; j < base.game.kingdoms.Count; j++)
		{
			Kingdom kingdom = base.game.kingdoms[j];
			if (!kingdom.IsDefeated() && kingdom.type == Kingdom.Type.Regular)
			{
				kingdom.fameObj.CalcFame();
			}
		}
		CalcOnly("FameRanking");
		for (int k = 0; k < base.game.kingdoms.Count; k++)
		{
			Kingdom kingdom2 = base.game.kingdoms[k];
			if (!kingdom2.IsDefeated())
			{
				kingdom2.GetRankingCategories().CalcScore();
			}
		}
	}

	public void CalcAll(params string[] skipRankings)
	{
		if (skipRankings.Length == 0)
		{
			FullRecalc();
			return;
		}
		for (int i = 0; i < rankings.Count; i++)
		{
			bool flag = false;
			for (int j = 0; j < skipRankings.Length; j++)
			{
				if (skipRankings[j] == rankings[i].def.field.key)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				rankings[i].CalcAll(base.game);
			}
		}
	}

	public void CalcOnly(params string[] onlyRankings)
	{
		for (int i = 0; i < rankings.Count; i++)
		{
			bool flag = false;
			for (int j = 0; j < onlyRankings.Length; j++)
			{
				if (onlyRankings[j] == rankings[i].def.field.key)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				rankings[i].CalcAll(base.game);
			}
		}
	}

	public KingdomRanking Find(string name)
	{
		for (int i = 0; i < rankings.Count; i++)
		{
			KingdomRanking kingdomRanking = rankings[i];
			if (kingdomRanking.def.name == name)
			{
				return kingdomRanking;
			}
		}
		return null;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		FullRecalc();
		for (int i = 0; i < rankings.Count; i++)
		{
			KingdomRanking kingdomRanking = rankings[i];
			dump.OpenSection(kingdomRanking.def.name);
			for (int j = 0; j < kingdomRanking.rows.Count; j++)
			{
				KingdomRanking.Row row = kingdomRanking.rows[j];
				dump.Append(row.rank_group.ToString() + "(" + row.rank + ")", row.kingdom.Name + " Fame-" + row.fame + " Score-" + row.score);
			}
			dump.CloseSection(kingdomRanking.def.name);
		}
		base.DumpInnerState(dump, verbosity);
	}
}
