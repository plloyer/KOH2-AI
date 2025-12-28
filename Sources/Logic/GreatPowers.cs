using System;
using System.Collections.Generic;

namespace Logic;

public class GreatPowers : Component
{
	public KingdomRanking fame_ranking;

	public const int max_count_default = 9;

	private int max_count = 9;

	private List<Kingdom> top_kingdoms = new List<Kingdom>();

	private int recalc_time = 900;

	public List<KingdomRanking.Row> previous_last_rankings;

	public List<KingdomRanking.Row> last_rankings;

	private float min_fame_perc = 25f;

	private float min_fame_perc_fade_time = 90f;

	public GreatPowers(Game game)
		: base(game)
	{
	}

	public void Init(bool new_game)
	{
		TopKingdoms(recalc: true);
	}

	public void Shutdown()
	{
		StopUpdating();
		fame_ranking = null;
		top_kingdoms.Clear();
		previous_last_rankings = null;
		last_rankings = null;
	}

	public override void OnUpdate()
	{
		TopKingdoms(recalc: true);
	}

	private void FindFameRanking()
	{
		if (fame_ranking != null)
		{
			return;
		}
		KingdomRankings component = base.game.GetComponent<KingdomRankings>();
		if (component == null)
		{
			return;
		}
		List<KingdomRanking> rankings = component.rankings;
		if (rankings == null)
		{
			return;
		}
		_ = rankings.Count;
		if (false)
		{
			return;
		}
		for (int i = 0; i < component.rankings.Count; i++)
		{
			KingdomRanking kingdomRanking = component.rankings[i];
			if (kingdomRanking.def.name == "FameRanking")
			{
				fame_ranking = kingdomRanking;
				recalc_time = kingdomRanking.def.field.GetInt("recalc_time", null, recalc_time);
				min_fame_perc = kingdomRanking.def.field.GetFloat("min_fame_perc", null, min_fame_perc);
				min_fame_perc_fade_time = kingdomRanking.def.field.GetFloat("min_fame_perc_fade_time", null, min_fame_perc_fade_time);
				break;
			}
		}
	}

	public void SetTopKingdoms(List<Kingdom> kingdoms, bool send_state = true)
	{
		FindFameRanking();
		if (last_rankings != null)
		{
			previous_last_rankings = new List<KingdomRanking.Row>();
		}
		last_rankings = new List<KingdomRanking.Row>(fame_ranking.rows);
		List<Kingdom> list = new List<Kingdom>(base.game.great_powers.top_kingdoms);
		List<Kingdom> list2 = base.game.great_powers.top_kingdoms;
		List<Kingdom> list3 = new List<Kingdom>();
		List<Kingdom> list4 = new List<Kingdom>();
		list2.Clear();
		for (int i = 0; i < kingdoms.Count; i++)
		{
			Kingdom item = kingdoms[i];
			list2.Add(item);
			if (!list.Contains(item))
			{
				list3.Add(item);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			Kingdom item2 = list[j];
			if (!list2.Contains(item2))
			{
				list4.Add(item2);
			}
		}
		if (!Game.isLoadingSaveGame)
		{
			for (int k = 0; k < list4.Count; k++)
			{
				list4[k].NotifyListeners("great_powers_changed", false);
			}
			for (int l = 0; l < list3.Count; l++)
			{
				list3[l].NotifyListeners("great_powers_changed", true);
			}
			base.game.NotifyListeners("ranking_updated", this);
		}
		if (send_state)
		{
			base.game.SendState<Game.GreatPowersState>();
		}
	}

	public List<Kingdom> TopKingdoms(bool recalc = false)
	{
		if (recalc)
		{
			float seconds = 5f;
			if (base.game.IsAuthority())
			{
				if (base.game?.kingdoms != null)
				{
					int num = 0;
					for (int i = 0; i < base.game.kingdoms.Count; i++)
					{
						Kingdom kingdom = base.game.kingdoms[i];
						if (kingdom.type == Kingdom.Type.Regular && !kingdom.IsDefeated())
						{
							num++;
						}
					}
					max_count = Math.Min(9, num);
				}
				FindFameRanking();
				if (fame_ranking == null)
				{
					return null;
				}
				if (base.game.kingdoms != null && fame_ranking.rows != null && fame_ranking.rows.Count >= max_count)
				{
					float minutes = base.game.session_time.minutes;
					float num2 = base.game.Map(minutes, 0f, min_fame_perc_fade_time, min_fame_perc, 0f, clamp: true);
					List<Kingdom> list = new List<Kingdom>();
					for (int j = 0; j < max_count; j++)
					{
						Kingdom kingdom2 = fame_ranking.rows[j].kingdom;
						if (!(kingdom2.fame < kingdom2.max_fame * num2 / 100f))
						{
							list.Add(kingdom2);
						}
					}
					SetTopKingdoms(list);
					seconds = recalc_time;
				}
			}
			else
			{
				FindFameRanking();
				if (fame_ranking != null)
				{
					seconds = recalc_time;
				}
			}
			UpdateAfter(seconds);
		}
		return top_kingdoms;
	}

	public int MaxGreatPowers()
	{
		return max_count;
	}
}
