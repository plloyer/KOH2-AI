using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Logic;

public class AIDirector
{
	public bool traceToFile;

	public List<Kingdom> HighlyImprovedAIList = new List<Kingdom>();

	public List<Kingdom> ImprovedAIList = new List<Kingdom>();

	public List<Kingdom> HinderedAIList = new List<Kingdom>();

	private string path = "";

	private bool init;

	private float last_trace = -100f;

	private bool uq_once;

	public IEnumerator ThinkBalance(Game game)
	{
		int num = 0;
		int num2 = 0;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && !kingdom.IsDefeated())
			{
				num++;
				num2 += kingdom.realms.Count;
				num3 += kingdom.income[ResourceType.Gold];
				num4 += kingdom.income[ResourceType.Books];
				num5 += kingdom.CalcArmyStrength();
				num6 += (float)kingdom.CalcRankingCategoriesScore();
				kingdom.last_calculated_power = 0.5f * kingdom.income[ResourceType.Gold] + 2f * kingdom.income[ResourceType.Books] + 0.0025f * kingdom.CalcArmyStrength() + 0.1f * (float)kingdom.CalcRankingCategoriesScore();
				num7 += kingdom.last_calculated_power;
			}
		}
		float averagePower = num7 / (float)num;
		GiveBonusesToPlayers(game, averagePower);
		if (game.session_time.minutes > (float)(20 * (ImprovedAIList.Count + 2 * HighlyImprovedAIList.Count)))
		{
			GiveBonusesToAI(game, averagePower);
		}
		if (traceToFile)
		{
			TraceToFile(game);
		}
		yield return null;
	}

	private void GiveBonusesToAI(Game game, float averagePower)
	{
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && !kingdom.IsDefeated() && !kingdom.is_player && game.ai.def.IsKingdomEligibleForBigBonus(kingdom) && !ImprovedAIList.Contains(kingdom) && !HighlyImprovedAIList.Contains(kingdom))
			{
				if (kingdom.last_calculated_power < 0.5f * averagePower)
				{
					kingdom.balance_factor_luck = game.ai.def.big_bonus_power;
					kingdom.balance_factor_income = 0.5f * game.ai.def.big_bonus_power + 0.5f;
				}
				else if (kingdom.last_calculated_power < averagePower)
				{
					kingdom.balance_factor_luck = game.ai.def.bonus_power;
					kingdom.balance_factor_income = 0.5f * game.ai.def.bonus_power + 0.5f;
				}
				else
				{
					kingdom.balance_factor_luck = 1f;
					kingdom.balance_factor_income = 1f;
				}
			}
		}
		if (ImprovedAIList.Count > 3 * HighlyImprovedAIList.Count + 2 && HighlyImprovedAIList.Count < 3)
		{
			List<Kingdom> list = new List<Kingdom>();
			for (int j = 0; j < game.kingdoms.Count; j++)
			{
				Kingdom kingdom2 = game.kingdoms[j];
				if (kingdom2 != null && !kingdom2.IsDefeated() && !kingdom2.is_player && game.ai.def.IsKingdomEligibleForBigBonus(kingdom2) && !ImprovedAIList.Contains(kingdom2) && !HighlyImprovedAIList.Contains(kingdom2) && kingdom2.last_calculated_power > averagePower)
				{
					list.Add(kingdom2);
				}
			}
			if (list.Count > 0)
			{
				Kingdom kingdom3 = list[game.Random(0, list.Count)];
				kingdom3.balance_factor_luck = game.ai.def.big_bonus_power;
				kingdom3.balance_factor_income = 0.5f * game.ai.def.big_bonus_power + 0.5f;
				HighlyImprovedAIList.Add(kingdom3);
			}
		}
		else
		{
			if (ImprovedAIList.Count >= 9)
			{
				return;
			}
			List<Kingdom> list2 = new List<Kingdom>();
			for (int k = 0; k < game.kingdoms.Count; k++)
			{
				Kingdom kingdom4 = game.kingdoms[k];
				if (kingdom4 != null && !kingdom4.IsDefeated() && !kingdom4.is_player && game.ai.def.IsKingdomEligibleForBonus(kingdom4) && !ImprovedAIList.Contains(kingdom4) && !HighlyImprovedAIList.Contains(kingdom4) && kingdom4.last_calculated_power > averagePower)
				{
					list2.Add(kingdom4);
				}
			}
			if (list2.Count > 0)
			{
				Kingdom kingdom5 = list2[game.Random(0, list2.Count)];
				kingdom5.balance_factor_luck = game.ai.def.bonus_power;
				kingdom5.balance_factor_income = 0.5f * game.ai.def.bonus_power + 0.5f;
				ImprovedAIList.Add(kingdom5);
			}
		}
	}

	private void GiveBonusesToPlayers(Game game, float averagePower)
	{
		float num = 0f;
		if (game.rules.ai_difficulty == 0)
		{
			num = 2f;
		}
		else if (game.rules.ai_difficulty == 1)
		{
			num = 1f;
		}
		else if (game.rules.ai_difficulty == 2)
		{
			num = 0.5f;
		}
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && !kingdom.IsDefeated() && kingdom.is_player)
			{
				if (kingdom.last_calculated_power < 0.25f * averagePower)
				{
					kingdom.balance_factor_luck = 1f + 2f * num;
				}
				else if (kingdom.last_calculated_power < 0.5f * averagePower)
				{
					kingdom.balance_factor_luck = 1f + num;
				}
				else
				{
					kingdom.balance_factor_luck = 1f;
				}
			}
		}
	}

	private void TraceToFile(Game game)
	{
		if (game.session_time.minutes < 20f + last_trace)
		{
			return;
		}
		last_trace = game.session_time.minutes % 20f * 20f;
		if (!init)
		{
			int num = 0;
			do
			{
				path = System.IO.Path.GetFullPath("../AI_Director_log_" + num + ".txt");
				num++;
			}
			while (File.Exists(path));
			File.WriteAllText(path, "");
			init = true;
		}
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		float num8 = 0f;
		float num9 = 0f;
		float num10 = 0f;
		File.AppendAllText(path, " --- Tick Bonus AI -- " + game.session_time.minutes.ToString().PadLeft(3) + " ---  Difficulty: " + game.ai.difficulty + " ---\n");
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && !kingdom.IsDefeated() && (game.ai.def.IsKingdomEligibleForBigBonus(kingdom) || game.ai.def.IsKingdomEligibleForBonus(kingdom) || kingdom.is_player || kingdom.IsGreatPower()))
			{
				num2 += 1f;
				float balance_factor_luck = kingdom.balance_factor_luck;
				int num11 = 0;
				if (kingdom.ai != null)
				{
					num11 = kingdom.ai.general_thinks;
				}
				int num12 = 0;
				if (kingdom.ai != null)
				{
					num12 = kingdom.ai.build_thinks;
				}
				int num13 = 0;
				if (kingdom.ai != null)
				{
					num13 = kingdom.ai.diplomacy_thinks;
				}
				int num14 = 0;
				if (kingdom.ai != null)
				{
					num14 = kingdom.ai.military_thinks;
				}
				int num15 = 0;
				if (kingdom.ai != null)
				{
					num15 = kingdom.ai.governor_thinks;
				}
				int count = kingdom.realms.Count;
				float num16 = kingdom.resources[ResourceType.Gold];
				float num17 = kingdom.income[ResourceType.Gold];
				float num18 = kingdom.income[ResourceType.Books];
				float value = kingdom.stability.value;
				int knightsOnWageCount = kingdom.GetKnightsOnWageCount("Marshal");
				float num19 = kingdom.CalcArmyStrength();
				float num20 = kingdom.CalcRankingCategoriesScore();
				string contents = kingdom.ActiveName.PadRight(24) + ": " + num11.ToString().PadLeft(3) + "G|" + num12.ToString().PadLeft(3) + "B|" + num13.ToString().PadLeft(3) + "D|" + num14.ToString().PadLeft(3) + "M|" + num15.ToString().PadLeft(3) + "V AI steps " + balance_factor_luck.ToString("F2").PadLeft(4) + " bonus " + count.ToString().PadLeft(4) + " provinces " + num16.ToString("F0").PadLeft(6) + " +" + num17.ToString("F0").PadLeft(4) + " gold " + num18.ToString("F0").PadLeft(4) + " books " + value.ToString("F1").PadLeft(4) + " stability " + knightsOnWageCount.ToString().PadLeft(4) + " armies " + num19.ToString("F0").PadLeft(4) + " power " + num20.ToString("F0").PadLeft(6) + " score \n";
				File.AppendAllText(path, contents);
				num3 += (float)count;
				num4 += num16;
				num5 += num17;
				num6 += num18;
				num7 += value;
				num8 += (float)knightsOnWageCount;
				num9 += num19;
				num10 += num20;
			}
		}
		num3 /= num2;
		num4 /= num2;
		num5 /= num2;
		num6 /= num2;
		num7 /= num2;
		num8 /= num2;
		num9 /= num2;
		num10 /= num2;
		string contents2 = "Average".PadRight(24) + ": " + num3.ToString("F1").PadLeft(24) + " provinces " + num4.ToString("F0").PadLeft(6) + " +" + num5.ToString("F0").PadLeft(4) + " gold " + num6.ToString("F0").PadLeft(4) + " books " + num7.ToString("F1").PadLeft(4) + " stability " + num8.ToString("F1").PadLeft(4) + " armies " + num9.ToString("F0").PadLeft(4) + " power " + num10.ToString("F0").PadLeft(6) + " score \n";
		File.AppendAllText(path, contents2);
	}

	private void TraceToFileGovRealms(Game game)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom == null || kingdom.IsDefeated() || kingdom.realms == null)
			{
				continue;
			}
			num2 += Math.Max(0, kingdom.realms.Count - 9);
			for (int j = 0; j < kingdom.realms.Count; j++)
			{
				if (kingdom.realms[j].castle.governor == null)
				{
					num++;
				}
			}
		}
		if (path.Length > 2)
		{
			File.AppendAllText(path, " ---  Ungoverned realms " + num + " - min " + num2 + " ---\n");
		}
	}

	private void TraceToFileUnionQuests(Game game)
	{
		if (game.time.minutes < 2f && uq_once)
		{
			return;
		}
		string fullPath = System.IO.Path.GetFullPath("../Union_Quests_log.txt");
		File.WriteAllText(fullPath, "");
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom == null || kingdom.IsDefeated() || kingdom.realms == null)
			{
				continue;
			}
			if (kingdom.quests == null || kingdom.quests.Count == 0)
			{
				File.AppendAllText(fullPath, kingdom.Name + ": NO QUESTS\n");
			}
			for (int j = 0; j < kingdom.quests.Count; j++)
			{
				if (kingdom.quests[j] is UnionQuest unionQuest)
				{
					if (kingdom.quests.Count > 1)
					{
						File.AppendAllText(fullPath, kingdom.Name + ": " + (j + 1) + "/" + kingdom.quests.Count + " --- " + unionQuest.def.GetVar("name").ToString() + "\n");
					}
					else
					{
						File.AppendAllText(fullPath, kingdom.Name + ": " + unionQuest.def.GetVar("name").ToString() + "\n");
					}
				}
				else
				{
					File.AppendAllText(fullPath, kingdom.Name + ": " + (j + 1) + "/" + kingdom.quests.Count + " --- UNKNOWN QUEST\n");
				}
			}
		}
		uq_once = true;
	}
}
