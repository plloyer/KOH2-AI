using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Logic;

public class AI : IVars
{
	public enum ProvinceSpecialization
	{
		General,
		Military,
		Gold,
		Food,
		Religion,
		GeneralSpec,
		MilitarySpec,
		TradeSpec,
		FoodSpec,
		ReligionSpec,
		COUNT
	}

	public struct ProductionEvaluationFutureWeights
	{
		public float immediate;

		public float near_future;

		public float far_future;

		public static ProductionEvaluationFutureWeights Immediate = new ProductionEvaluationFutureWeights
		{
			immediate = 1f,
			near_future = 0f,
			far_future = 0f
		};

		public static ProductionEvaluationFutureWeights Default = new ProductionEvaluationFutureWeights
		{
			immediate = 1f,
			near_future = 0.5f,
			far_future = 0.1f
		};

		public static ProductionEvaluationFutureWeights Full = new ProductionEvaluationFutureWeights
		{
			immediate = 1f,
			near_future = 1f,
			far_future = 1f
		};

		public override string ToString()
		{
			return $"{immediate}/{near_future}/{far_future}";
		}
	}

	public class Def : Logic.Def
	{
		public Resource[] province_specialization_resource_weights = new Resource[10];

		public float[] unique_PF_goods_eval_multipliers;

		public ProductionEvaluationFutureWeights[] production_evaluation_future_weights;

		public DT.Field[] budget;

		public DT.Field[] upkeeps_budget;

		public float non_governed_production_eval_mul = 1f;

		public float food_reserve = 10f;

		public float critical_food_weight = 1000f;

		public float low_food_weight = 500f;

		public float big_bonus_power = 2f;

		public float bonus_power = 1.5f;

		public float malus_power = 0.65f;

		private DT.Field big_bonus_eligible_kingdoms;

		private DT.Field bonus_eligible_kingdoms;

		public override bool Load(Game game)
		{
			LoadProvinceSpecializationResourceWeights(game);
			LoadBudgets(game);
			non_governed_production_eval_mul = base.field.GetFloat("non_governed_production_eval_mul", null, non_governed_production_eval_mul);
			LoadUniquePFGoodsEvalMultiplier(game);
			LoadFoodReserve(game);
			LoadBonuses(game);
			return true;
		}

		public override bool Validate(Game game)
		{
			LoadProductionEvaluationFutureWeights(game);
			return true;
		}

		private void LoadBudgets(Game game)
		{
			budget = new DT.Field[7];
			DT.Field field = base.field.FindChild("budget");
			for (KingdomAI.Expense.Category category = KingdomAI.Expense.Category.None; category < KingdomAI.Expense.Category.COUNT; category++)
			{
				DT.Field field2 = field?.FindChild(category.ToString());
				budget[(int)category] = field2;
			}
			upkeeps_budget = new DT.Field[7];
			DT.Field field3 = base.field.FindChild("upkeeps_budget");
			for (KingdomAI.Expense.Category category2 = KingdomAI.Expense.Category.None; category2 < KingdomAI.Expense.Category.COUNT; category2++)
			{
				DT.Field field4 = field3?.FindChild(category2.ToString());
				upkeeps_budget[(int)category2] = field4;
			}
		}

		private void LoadFoodReserve(Game game)
		{
			DT.Field field = base.field.FindChild("food_reserve");
			if (field != null)
			{
				food_reserve = field.Float(null, food_reserve);
				critical_food_weight = field.GetFloat("critical_weight", null, critical_food_weight);
				low_food_weight = field.GetFloat("low_weight", null, low_food_weight);
			}
		}

		private void LoadBonuses(Game game)
		{
			DT.Field field = base.field.FindChild("BigBonusPower");
			big_bonus_power = field.Float(null, big_bonus_power);
			field = base.field.FindChild("BonusPower");
			bonus_power = field.Float(null, bonus_power);
			field = base.field.FindChild("MalusPower");
			malus_power = field.Float(null, malus_power);
			big_bonus_eligible_kingdoms = base.field.FindChild("big_bonus_eligible_kingdoms");
			bonus_eligible_kingdoms = base.field.FindChild("bonus_eligible_kingdoms");
		}

		private void LoadProvinceSpecializationResourceWeights(Game game)
		{
			DT.Field field = base.field.FindChild("province_specialization_resource_weights");
			for (ProvinceSpecialization provinceSpecialization = ProvinceSpecialization.General; provinceSpecialization < ProvinceSpecialization.COUNT; provinceSpecialization++)
			{
				Resource resource = Resource.Parse(field?.FindChild(provinceSpecialization.ToString()));
				province_specialization_resource_weights[(int)provinceSpecialization] = resource;
			}
		}

		private void LoadProductionEvaluationFutureWeights(Game game)
		{
			DT.Field field = base.field.FindChild("production_evaluation_future_weights");
			Building.Def def = game.defs.GetBase<Building.Def>();
			int num = def.slots_base + def.slots_per_tier * game.rules.max_unlockable_tiers;
			production_evaluation_future_weights = new ProductionEvaluationFutureWeights[num + 1];
			ProductionEvaluationFutureWeights productionEvaluationFutureWeights = ProductionEvaluationFutureWeights.Default;
			for (int i = 0; i <= num; i++)
			{
				DT.Field field2 = field?.FindChild(i.ToString());
				if (field2 == null)
				{
					production_evaluation_future_weights[i] = productionEvaluationFutureWeights;
					continue;
				}
				ProductionEvaluationFutureWeights productionEvaluationFutureWeights2 = default(ProductionEvaluationFutureWeights);
				productionEvaluationFutureWeights2.immediate = field2.Float(0, null, 1f);
				productionEvaluationFutureWeights2.near_future = field2.Float(1, null, productionEvaluationFutureWeights2.immediate);
				productionEvaluationFutureWeights2.far_future = field2.Float(2, null, productionEvaluationFutureWeights2.near_future);
				production_evaluation_future_weights[i] = productionEvaluationFutureWeights2;
				productionEvaluationFutureWeights = productionEvaluationFutureWeights2;
			}
		}

		private void LoadUniquePFGoodsEvalMultiplier(Game game)
		{
			DT.Field field = base.field.FindChild("unique_PF_goods_eval_multiplier");
			Building.Def def = game.defs.GetBase<Building.Def>();
			int num = def.slots_base + def.slots_per_tier * game.rules.max_unlockable_tiers;
			unique_PF_goods_eval_multipliers = new float[num + 1];
			float num2 = 1f;
			for (int i = 0; i <= num; i++)
			{
				DT.Field field2 = field?.FindChild(i.ToString());
				if (field2 == null)
				{
					unique_PF_goods_eval_multipliers[i] = num2;
					continue;
				}
				float num3 = field2.Float(null, 1f);
				unique_PF_goods_eval_multipliers[i] = num3;
				num2 = num3;
			}
		}

		public bool IsKingdomEligibleForBigBonus(Kingdom kingdom)
		{
			if (big_bonus_eligible_kingdoms == null)
			{
				return false;
			}
			List<DT.Field> list = big_bonus_eligible_kingdoms.Children();
			if (list == null)
			{
				return false;
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].key == kingdom.Name)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsKingdomEligibleForBonus(Kingdom kingdom)
		{
			if (bonus_eligible_kingdoms == null)
			{
				return false;
			}
			List<DT.Field> list = bonus_eligible_kingdoms.Children();
			if (list == null)
			{
				return false;
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].key == kingdom.Name)
				{
					return true;
				}
			}
			return false;
		}
	}

	public Game game;

	public Def def;

	public AIDirector director;

	public bool enabled = true;

	public bool profile;

	public CoopThread think_general_thread;

	public CoopThread think_build_thread;

	public CoopThread think_military_thread;

	public CoopThread think_governors_thread;

	public CoopThread think_diplomacy_thread;

	public CoopThread think_director_thread;

	public Army.Def army_def;

	public CharacterClass.Def marshal_def;

	public CharacterClass.Def merchant_def;

	public CharacterClass.Def cleric_def;

	public DT.Def kingdom_gold_upkeep_panel_def;

	public int difficulty
	{
		get
		{
			if (game?.rules != null)
			{
				return game.rules.ai_difficulty;
			}
			return 1;
		}
	}

	public bool Easy => difficulty == 0;

	public bool Hard => difficulty >= 2;

	public bool Profile(int profile)
	{
		return this.profile = profile != 0;
	}

	public void Init(Game game)
	{
		this.game = game;
		def = game.defs.GetBase<Def>();
		director = new AIDirector();
		int trace_verbosity = 0;
		think_general_thread = CoopThread.Start("AI.ThinkGeneral", ThinkGeneral(), trace_verbosity);
		think_build_thread = CoopThread.Start("AI.ThinkBuild", ThinkBuild(), trace_verbosity);
		think_military_thread = CoopThread.Start("AI.ThinkMilitary", ThinkMilitary(), trace_verbosity);
		think_governors_thread = CoopThread.Start("AI.ThinkGovernors", ThinkGovernors(), trace_verbosity);
		think_diplomacy_thread = CoopThread.Start("AI.ThinkDiplomacy", ThinkDiplomacy(), trace_verbosity);
		think_director_thread = CoopThread.Start("AI.ThinkDirector", ThinkDirector(), trace_verbosity);
		army_def = game.defs.GetBase<Army.Def>();
		marshal_def = game.defs.Get<CharacterClass.Def>("Marshal");
		merchant_def = game.defs.Get<CharacterClass.Def>("Merchant");
		cleric_def = game.defs.Get<CharacterClass.Def>("Cleric");
		kingdom_gold_upkeep_panel_def = game.dt.FindDef("KingdomGoldUpkeepPanel");
	}

	public void Shutdown()
	{
		if (CoopThread.IsValid(think_general_thread))
		{
			think_general_thread.Stop();
		}
		think_general_thread = null;
		if (CoopThread.IsValid(think_build_thread))
		{
			think_build_thread.Stop();
		}
		think_build_thread = null;
		if (CoopThread.IsValid(think_military_thread))
		{
			think_military_thread.Stop();
		}
		think_military_thread = null;
		if (CoopThread.IsValid(think_governors_thread))
		{
			think_governors_thread.Stop();
		}
		think_governors_thread = null;
		if (CoopThread.IsValid(think_diplomacy_thread))
		{
			think_diplomacy_thread.Stop();
		}
		think_diplomacy_thread = null;
		director = null;
		game = null;
	}

	public static void Trace(string message, params object[] args)
	{
		if (CoopThread.current_trace_verbosity >= 1)
		{
			if (args.Length != 0)
			{
				message = string.Format(message, args);
			}
			CoopThread.Log(message);
		}
	}

	private bool ShouldUpdate()
	{
		if (!enabled)
		{
			return false;
		}
		if (game?.kingdoms == null)
		{
			return false;
		}
		if (string.IsNullOrEmpty(game.map_name))
		{
			return false;
		}
		return true;
	}

	private IEnumerator ThinkGeneral()
	{
		while (true)
		{
			yield return null;
			if (!ShouldUpdate())
			{
				yield return CoopThread.Yield;
			}
			yield return CoopThread.Call("AI.ThinkGeneralLoop", ThinkGeneralLoop());
		}
	}

	private IEnumerator ThinkGeneralLoop()
	{
		Time loop_start_time = game.time;
		int updated_kingdoms = 0;
		int cur_kingdom = 0;
		while (cur_kingdom < game.kingdoms.Count)
		{
			yield return null;
			Kingdom kingdom = game.GetKingdom(cur_kingdom);
			int num;
			if (kingdom != null && kingdom.started && kingdom.ai != null && !kingdom.IsDefeated() && kingdom.IsAuthority() && kingdom.ai.Enabled(KingdomAI.EnableFlags.All))
			{
				kingdom.ai.general_thinks_tries++;
				if ((kingdom.realms.Count > 1 || kingdom.ai.general_thinks_tries % 2 != 0) && (kingdom.realms.Count <= 1 || kingdom.realms.Count > 3 || kingdom.ai.general_thinks_tries % 3 != 0))
				{
					num = updated_kingdoms + 1;
					updated_kingdoms = num;
					kingdom.ai.general_thinks++;
					yield return CoopThread.Call("KingdomAI.ThinkGeneral", kingdom.ai.ThinkGeneral());
				}
			}
			num = cur_kingdom + 1;
			cur_kingdom = num;
		}
		Trace("Looped through {0} / {1} kingdoms for {2} seconds", updated_kingdoms, game.kingdoms.Count, game.time - loop_start_time);
	}

	private IEnumerator ThinkBuild()
	{
		int cur_kingdom = 0;
		Time last_loop_time = game.time;
		int updated_kingdoms = 0;
		while (true)
		{
			yield return null;
			if (!ShouldUpdate())
			{
				yield return CoopThread.Yield;
			}
			int num = cur_kingdom + 1;
			cur_kingdom = num;
			if (cur_kingdom > game.kingdoms.Count)
			{
				cur_kingdom = 1;
				Trace("Looped through {0} / {1} kingdoms for {2} seconds", updated_kingdoms, game.kingdoms.Count, game.time - last_loop_time);
				last_loop_time = game.time;
				updated_kingdoms = 0;
			}
			Kingdom kingdom = game.GetKingdom(cur_kingdom);
			if (kingdom == null || !kingdom.started || kingdom.ai == null || kingdom.IsDefeated() || !kingdom.IsAuthority() || !kingdom.ai.Enabled(KingdomAI.EnableFlags.Buildings))
			{
				continue;
			}
			kingdom.ai.build_thinks_tries++;
			if ((kingdom.realms.Count <= 1 && kingdom.ai.build_thinks_tries % 2 == 0) || (kingdom.realms.Count > 1 && kingdom.realms.Count <= 3 && kingdom.ai.build_thinks_tries % 3 == 0) || (kingdom.resources[ResourceType.Gold] < 500f + 5f * kingdom.income[ResourceType.Gold] && kingdom.ai.governor_thinks_tries % 10 != 0))
			{
				continue;
			}
			num = updated_kingdoms + 1;
			updated_kingdoms = num;
			kingdom.ai.build_thinks++;
			yield return CoopThread.Call("KingdomAI.ThinkBuild", kingdom.ai.ThinkBuild());
			if (game.speed >= 2f)
			{
				if ((float)cur_kingdom % (game.speed / 2f) == 0f)
				{
					yield return CoopThread.Yield;
				}
			}
			else
			{
				for (int i = 0; i < 2; i++)
				{
					yield return CoopThread.Yield;
				}
			}
		}
	}

	private IEnumerator ThinkMilitary()
	{
		int cur_kingdom = 0;
		Time last_loop_time = game.time;
		int updated_kingdoms = 0;
		while (true)
		{
			yield return null;
			if (!ShouldUpdate())
			{
				yield return CoopThread.Yield;
			}
			int num = cur_kingdom + 1;
			cur_kingdom = num;
			if (cur_kingdom > game.kingdoms.Count)
			{
				cur_kingdom = 1;
				Trace("Looped through {0} / {1} kingdoms for {2} seconds", updated_kingdoms, game.kingdoms.Count, game.time - last_loop_time);
				last_loop_time = game.time;
				updated_kingdoms = 0;
			}
			Kingdom kingdom = game.GetKingdom(cur_kingdom);
			if (kingdom == null || !kingdom.started || kingdom.ai == null || kingdom.IsDefeated() || !kingdom.IsAuthority() || !kingdom.ai.Enabled(KingdomAI.EnableFlags.All))
			{
				continue;
			}
			kingdom.ai.military_thinks_tries++;
			if ((kingdom.realms.Count <= 1 && kingdom.ai.military_thinks_tries % 2 == 0) || (kingdom.realms.Count > 1 && kingdom.realms.Count <= 3 && kingdom.ai.military_thinks_tries % 3 == 0) || (kingdom.wars.Count == 0 && kingdom.ai.military_thinks_tries % 2 == 0))
			{
				continue;
			}
			num = updated_kingdoms + 1;
			updated_kingdoms = num;
			kingdom.ai.military_thinks++;
			yield return CoopThread.Call("KingdomAI.ThinkMilitary", kingdom.ai.ThinkMilitary());
			if (game.speed >= 2f)
			{
				if ((float)cur_kingdom % (game.speed / 2f) == 0f)
				{
					yield return CoopThread.Yield;
				}
			}
			else
			{
				for (int i = 0; i < 2; i++)
				{
					yield return CoopThread.Yield;
				}
			}
		}
	}

	private IEnumerator ThinkGovernors()
	{
		int cur_kingdom = 0;
		Time last_loop_time = game.time;
		int updated_kingdoms = 0;
		while (true)
		{
			yield return null;
			if (!ShouldUpdate())
			{
				yield return CoopThread.Yield;
			}
			int num = cur_kingdom + 1;
			cur_kingdom = num;
			if (cur_kingdom > game.kingdoms.Count)
			{
				cur_kingdom = 1;
				Trace("Looped through {0} / {1} kingdoms for {2} seconds", updated_kingdoms, game.kingdoms.Count, game.time - last_loop_time);
				last_loop_time = game.time;
				updated_kingdoms = 0;
			}
			Kingdom kingdom = game.GetKingdom(cur_kingdom);
			if (kingdom == null || !kingdom.started || kingdom.ai == null || kingdom.IsDefeated() || !kingdom.IsAuthority() || !kingdom.ai.Enabled(KingdomAI.EnableFlags.Characters))
			{
				continue;
			}
			kingdom.ai.governor_thinks_tries++;
			if ((kingdom.realms.Count <= 1 && kingdom.ai.governor_thinks_tries % 2 == 0) || (kingdom.realms.Count > 1 && kingdom.realms.Count <= 3 && kingdom.ai.governor_thinks_tries % 3 == 0))
			{
				continue;
			}
			if (kingdom.ai.governor_thinks_tries % 19 != 0)
			{
				bool flag = false;
				for (int i = 0; i < kingdom.court.Count; i++)
				{
					Character character = kingdom.court[i];
					if (character != null && character.governed_castle == null)
					{
						flag = true;
						break;
					}
				}
				bool flag2 = false;
				for (int j = 0; j < kingdom.realms.Count; j++)
				{
					Castle castle = kingdom.realms[j].castle;
					if (castle != null && castle.governor == null)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag || !flag2)
				{
					continue;
				}
			}
			num = updated_kingdoms + 1;
			updated_kingdoms = num;
			kingdom.ai.governor_thinks++;
			yield return CoopThread.Call("KingdomAI.ThinkGovernors", kingdom.ai.ThinkGovernors());
			if (game.speed >= 2f)
			{
				if ((float)cur_kingdom % (game.speed / 2f) == 0f)
				{
					yield return CoopThread.Yield;
				}
			}
			else
			{
				for (int k = 0; k < 2; k++)
				{
					yield return CoopThread.Yield;
				}
			}
		}
	}

	private IEnumerator ThinkDiplomacy()
	{
		int cur_kingdom = 0;
		Time last_loop_time = game.time;
		int updated_kingdoms = 0;
		while (true)
		{
			yield return null;
			if (!ShouldUpdate())
			{
				yield return CoopThread.Yield;
			}
			int num = cur_kingdom + 1;
			cur_kingdom = num;
			if (cur_kingdom > game.kingdoms.Count)
			{
				cur_kingdom = 1;
				Trace("Looped through {0} / {1} kingdoms for {2} seconds", updated_kingdoms, game.kingdoms.Count, game.time - last_loop_time);
				last_loop_time = game.time;
				updated_kingdoms = 0;
			}
			Kingdom kingdom = game.GetKingdom(cur_kingdom);
			if (kingdom == null || !kingdom.started || kingdom.ai == null || !kingdom.ai.Enabled(KingdomAI.EnableFlags.Diplomacy) || kingdom.IsDefeated() || !kingdom.IsAuthority())
			{
				continue;
			}
			kingdom.ai.diplomacy_thinks_tries++;
			if ((kingdom.realms.Count <= 1 && kingdom.ai.diplomacy_thinks_tries % 2 == 0) || (kingdom.realms.Count > 1 && kingdom.realms.Count <= 3 && kingdom.ai.diplomacy_thinks_tries % 3 == 0))
			{
				continue;
			}
			num = updated_kingdoms + 1;
			updated_kingdoms = num;
			kingdom.ai.diplomacy_thinks++;
			yield return CoopThread.Call("Think Diplomacy", kingdom.ai.ThinkDiplomacy());
			if (game.speed >= 10f)
			{
				if ((float)cur_kingdom % (game.speed / 10f) == 0f)
				{
					yield return CoopThread.Yield;
				}
			}
			else
			{
				for (int i = 0; (float)i < 10f / game.speed; i++)
				{
					yield return CoopThread.Yield;
				}
			}
		}
	}

	private IEnumerator ThinkDirector()
	{
		Time last_loop_time = game.time;
		_ = game.time;
		int interval = 0;
		while (true)
		{
			Time current_time = game.time;
			if ((int)current_time.minutes < (int)last_loop_time.minutes + interval)
			{
				yield return CoopThread.Yield;
				continue;
			}
			if (!ShouldUpdate())
			{
				yield return CoopThread.Yield;
			}
			yield return CoopThread.Call("Think Director", director.ThinkBalance(game));
			last_loop_time = current_time;
			interval = 3;
		}
	}

	private IEnumerator LogThread()
	{
		Time last_loop_time = game.time;
		_ = game.time;
		int interval = 0;
		while (true)
		{
			Time time = game.time;
			if ((int)time.minutes < (int)last_loop_time.minutes + interval)
			{
				yield return null;
				continue;
			}
			string text = "\n--- Time: " + time.ToString() + " ---";
			text = text + "\n" + GetPersonalityStats(interval == 0);
			string fullPath = System.IO.Path.GetFullPath("../AI_Personality_log.txt");
			if (interval == 0)
			{
				File.WriteAllText(fullPath, "AI_Personality_log\n");
				Process.Start(new ProcessStartInfo(fullPath)
				{
					UseShellExecute = true
				});
			}
			File.AppendAllText(fullPath, text);
			last_loop_time = time;
			interval = 15;
		}
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"difficulty" => difficulty, 
			"build_options" => new Value(Castle.last_build_options), 
			"cur_build_options" => new Value(Castle.build_options), 
			"upgrade_options" => new Value(Castle.last_upgrade_options), 
			"cur_upgrade_options" => new Value(Castle.upgrade_options), 
			"threats" => new Value(KingdomAI.threats), 
			_ => Value.Unknown, 
		};
	}

	public string GetPersonalityStats(bool firstTime = false)
	{
		string text = "AI Statistics by personality";
		for (int i = 0; i < 4; i++)
		{
			KingdomAI.AIPersonality aIPersonality = (KingdomAI.AIPersonality)i;
			int num = 0;
			int num2 = 0;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			float num7 = 0f;
			float num8 = 0f;
			float num9 = 0f;
			float num10 = 0f;
			float num11 = 0f;
			float num12 = 0f;
			float num13 = 0f;
			float num14 = 0f;
			for (int j = 0; j < game.kingdoms.Count; j++)
			{
				Kingdom kingdom = game.kingdoms[j];
				if (kingdom != null && !kingdom.is_player && !kingdom.IsDefeated() && kingdom.ai != null && kingdom.ai.personality == aIPersonality && (kingdom.ai.original_realm || firstTime))
				{
					num++;
					num2 += kingdom.realms.Count;
					num3 += kingdom.income[ResourceType.Gold];
					num5 += kingdom.income[ResourceType.Books];
					num7 += (float)kingdom.GetKnightsOnWageCount("Marshal");
					num9 += kingdom.CalcArmyStrength();
					num11 += (float)kingdom.CalcRankingCategoriesScore();
					num13 += kingdom.stability.value;
					if (firstTime)
					{
						kingdom.ai.original_realm = true;
					}
				}
			}
			num4 = num3 / (float)num;
			num6 = num5 / (float)num;
			num8 = num7 / (float)num;
			num10 = num9 / (float)num;
			num12 = num11 / (float)num;
			num14 = num13 / (float)num;
			text = text + "\n " + aIPersonality.ToString().PadRight(16) + ": " + num.ToString().PadLeft(4) + " kingdoms " + num2.ToString().PadLeft(4) + " provinces " + num4.ToString("F3").PadLeft(4) + " gold " + num6.ToString("F3").PadLeft(4) + " books " + num14.ToString("F3").PadLeft(4) + " stability " + num8.ToString("F3").PadLeft(4) + " armies " + num10.ToString("F3").PadLeft(4) + " power " + num12.ToString("F3").PadLeft(6) + " score";
		}
		return text;
	}
}
