namespace Logic;

public class DevSettings
{
	public class Def : Logic.Def
	{
		public float building_cost_gold_mod = 1f;

		public float building_cost_hammers_mod = 1f;

		public float gold_income_dev_mod = 1f;

		public float piety_income_dev_mod = 1f;

		public float books_income_dev_mod = 1f;

		public float gold_expenses_dev_mod = 1f;

		public float piety_expenses_dev_mod = 1f;

		public float books_expenses_dev_mod = 1f;

		public DT.Field ai_resource_boost_field;

		public DT.Field min_rebel_pop_time_field;

		public float ai_jihad_upkeep_mult;

		public float unit_food_upkeep_mod = 1f;

		public float unit_gold_hire_mod = 1f;

		public float unit_mercenary_gold_hire_mod = 1f;

		public bool track_stats;

		public bool force_endless_game;

		public DT.Field provinces_always_initially_converted;

		public DT.Field max_player_initial_wars;

		public DT.Field max_player_rebellions;

		public DT.Field max_rebels_per_player_kingdom;

		public DT.Field unit_player_resilience_bonus;

		public DT.Field army_player_retreat_penalty;

		public int audio_log_level = 2;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			building_cost_gold_mod = field.GetFloat("building_cost_gold_mod", null, building_cost_gold_mod);
			building_cost_hammers_mod = field.GetFloat("building_cost_hammers_mod", null, building_cost_hammers_mod);
			gold_income_dev_mod = field.GetFloat("gold_income_dev_mod", null, gold_income_dev_mod);
			piety_income_dev_mod = field.GetFloat("piety_income_dev_mod", null, piety_income_dev_mod);
			books_income_dev_mod = field.GetFloat("books_income_dev_mod", null, books_income_dev_mod);
			gold_expenses_dev_mod = field.GetFloat("gold_expenses_dev_mod", null, gold_expenses_dev_mod);
			piety_expenses_dev_mod = field.GetFloat("piety_expenses_dev_mod", null, piety_expenses_dev_mod);
			books_expenses_dev_mod = field.GetFloat("books_expenses_dev_mod", null, books_expenses_dev_mod);
			ai_resource_boost_field = field.FindChild("ai_resource_boost");
			ai_jihad_upkeep_mult = field.GetFloat("ai_jihad_upkeep_mult", null, ai_jihad_upkeep_mult);
			unit_food_upkeep_mod = field.GetFloat("unit_food_upkeep_mod", null, unit_food_upkeep_mod);
			unit_gold_hire_mod = field.GetFloat("unit_gold_hire_mod", null, unit_gold_hire_mod);
			unit_mercenary_gold_hire_mod = field.GetFloat("unit_mercenary_gold_hire_mod", null, unit_mercenary_gold_hire_mod);
			track_stats = field.GetBool("track_stats_on_non_dev_branches", null, track_stats) || Game.IsInternalBranch();
			Action.Tracker.enabled = (ProsAndCons.Tracker.enabled = (DBGOffersData.tracking_enabled = track_stats));
			force_endless_game = field.GetBool("force_endless_game", null, force_endless_game);
			min_rebel_pop_time_field = field.FindChild("min_rebel_pop_time");
			provinces_always_initially_converted = field.FindChild("provinces_always_initially_converted");
			max_player_initial_wars = field.FindChild("max_player_initial_wars");
			max_player_rebellions = field.FindChild("max_player_rebellions");
			max_rebels_per_player_kingdom = field.FindChild("max_rebels_per_player_kingdom");
			unit_player_resilience_bonus = field.FindChild("unit_player_resilience_bonus");
			army_player_retreat_penalty = field.FindChild("army_player_retreat_penalty");
			audio_log_level = field.GetInt("audio_log_level", null, audio_log_level);
			return true;
		}
	}
}
