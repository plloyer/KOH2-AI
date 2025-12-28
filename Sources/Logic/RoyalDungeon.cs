using System;
using System.Collections.Generic;

namespace Logic;

public class RoyalDungeon : Component, IVars
{
	public class Def : Logic.Def
	{
		public float event_tick_min = 30f;

		public float event_tick_max = 90f;

		public OutcomeDef event_outcomes;

		public float ai_min_character_prison_time = 30f;

		public float ai_actions_subsequent_tick_min = 450f;

		public float ai_actions_subsequent_tick_max = 600f;

		public float ai_war_execute_marshal_spy_dangerous = 25f;

		public float ai_war_execute_other = 10f;

		public float ai_war_ransom_marshal_spy;

		public float ai_war_ransom_other = 25f;

		public float ai_neutral_ransom = 40f;

		public float ai_alliance_ransom = 50f;

		public float ai_alliance_release = 50f;

		public float ai_offer_ransom = 25f;

		public float ai_offer_royal_ransom = 75f;

		public float base_prisoner_value = 15f;

		public float prisoner_position_knight = 1f;

		public float prisoner_position_prince = 2f;

		public float prisoner_position_king = 3f;

		public float prisoner_position_pope = 2f;

		public float prisoner_position_cardinal = 2f;

		public float prisoner_position_ecumenical_patriarch = 2f;

		public float prisoner_position_patriarch = 2f;

		public float prisoner_position_caliph = 2f;

		public float prisoner_age_old = 0.75f;

		public float prisoner_age_venerable = 0.5f;

		public float relationship_execute_mod_war = 0.25f;

		public float relationship_execute_mod_prince_or_cardinal = 2f;

		public float relationship_execute_mod_king_or_patriarch = 3f;

		public float relationship_release_mod = 0.5f;

		public float relationship_release_mod_war = 0.75f;

		public float relationship_release_mod_prince_or_cardinal = 2f;

		public float relationship_release_mod_king_or_patriarch = 3f;

		public float renounce_after_min = 900f;

		public float renounce_after_max = 1800f;

		public float relationship_ransom_mod = 0.0002f;

		public float base_ransom_price = 100f;

		public float ransom_war_royal = 100f;

		public float ransom_war_not_royal = 50f;

		public float ransom_no_war_not_royal = 75f;

		public float ransom_no_war_royal = 100f;

		public float successful_ransom_relationship_bonus = 10f;

		public float unsuccessful_ransom_relationship_penalty = 10f;

		public float ransom_cooldown_time = 300f;

		public float ransom_spy_or_war_and_royal_or_marshal = 25f;

		public float ransom_war_none = 50f;

		public float ransom_no_war_no_spy = 75f;

		public float ransom_no_war_spy = 100f;

		public float inspire_riot_death_chance = 25f;

		public float inspire_riot_conspiracy_bonus = 10f;

		public int riot_max_escaped = 2;

		public int riot_max_died = 2;

		public int inspire_riot_unit_count_min = 1;

		public int inspire_riot_unit_count_max = 3;

		public float plot_escape_together_chance = 50f;

		public float mass_escape_success = 50f;

		public float mass_escape_death = 25f;

		public float mass_escape_recaptured = 25f;

		public int min_prisoner_escape = 5;

		public int min_prisoner_riot = 5;

		public int excess_prisoner_escape = 12;

		public int excess_prisoner_riot = 12;

		public override bool Load(Game game)
		{
			DT.Field field = dt_def.field;
			event_tick_min = field.GetFloat("event_tick_min", null, event_tick_min);
			event_tick_max = field.GetFloat("event_tick_max", null, event_tick_max);
			DT.Field field2 = field.FindChild("event_outcomes");
			if (field2 != null)
			{
				DT.Field defaults = null;
				event_outcomes = new OutcomeDef(game, field2, defaults);
			}
			ai_min_character_prison_time = field.GetFloat("ai_min_character_prison_time", null, ai_min_character_prison_time);
			ai_actions_subsequent_tick_min = field.GetFloat("ai_actions_subsequent_tick_min", null, ai_actions_subsequent_tick_min);
			ai_actions_subsequent_tick_max = field.GetFloat("ai_actions_subsequent_tick_max", null, ai_actions_subsequent_tick_max);
			ai_war_execute_marshal_spy_dangerous = field.GetFloat("ai_war_execute_marshal_spy_dangerous", null, ai_war_execute_marshal_spy_dangerous);
			ai_war_execute_other = field.GetFloat("ai_war_execute_other", null, ai_war_execute_other);
			ai_war_ransom_marshal_spy = field.GetFloat("ai_war_ransom_marshal_spy", null, ai_war_ransom_marshal_spy);
			ai_war_ransom_other = field.GetFloat("ai_war_ransom_other", null, ai_war_ransom_other);
			ai_neutral_ransom = field.GetFloat("ai_neutral_ransom", null, ai_neutral_ransom);
			ai_alliance_ransom = field.GetFloat("ai_alliance_ransom", null, ai_alliance_ransom);
			ai_offer_ransom = field.GetFloat("ai_offer_ransom", null, ai_offer_ransom);
			ai_offer_royal_ransom = field.GetFloat("ai_offer_royal_ransom", null, ai_offer_royal_ransom);
			base_prisoner_value = field.GetFloat("base_prisoner_value", null, base_prisoner_value);
			prisoner_position_knight = field.GetFloat("prisoner_position_knight", null, prisoner_position_knight);
			prisoner_position_prince = field.GetFloat("prisoner_position_prince", null, prisoner_position_prince);
			prisoner_position_king = field.GetFloat("prisoner_position_king", null, prisoner_position_king);
			prisoner_position_pope = field.GetFloat("prisoner_position_pope", null, prisoner_position_pope);
			prisoner_position_cardinal = field.GetFloat("prisoner_position_cardinal", null, prisoner_position_cardinal);
			prisoner_position_ecumenical_patriarch = field.GetFloat("prisoner_position_ecumenical_patriarch", null, prisoner_position_ecumenical_patriarch);
			prisoner_position_patriarch = field.GetFloat("prisoner_position_patriarch", null, prisoner_position_patriarch);
			prisoner_position_caliph = field.GetFloat("prisoner_position_caliph", null, prisoner_position_caliph);
			prisoner_age_old = field.GetFloat("prisoner_age_old", null, prisoner_age_old);
			prisoner_age_venerable = field.GetFloat("prisoner_age_venerable", null, prisoner_age_venerable);
			relationship_execute_mod_war = field.GetFloat("relationship_execute_mod_war", null, relationship_execute_mod_war);
			relationship_execute_mod_prince_or_cardinal = field.GetFloat("relationship_execute_mod_prince_or_cardinal", null, relationship_execute_mod_prince_or_cardinal);
			relationship_execute_mod_king_or_patriarch = field.GetFloat("relationship_execute_mod_king_or_patriarch", null, relationship_execute_mod_king_or_patriarch);
			relationship_release_mod = field.GetFloat("relationship_release_mod", null, relationship_release_mod);
			relationship_release_mod_war = field.GetFloat("relationship_release_mod_war", null, relationship_release_mod_war);
			relationship_release_mod_prince_or_cardinal = field.GetFloat("relationship_release_mod_prince_or_cardinal", null, relationship_release_mod_prince_or_cardinal);
			relationship_release_mod_king_or_patriarch = field.GetFloat("relationship_release_mod_king_or_patriarch", null, relationship_release_mod_king_or_patriarch);
			DT.Field field3 = field.FindChild("renounce_after");
			if (field3 != null)
			{
				renounce_after_min = field3.Float(0, null, renounce_after_min);
				renounce_after_max = field3.Float(1, null, renounce_after_min);
			}
			relationship_ransom_mod = field.GetFloat("relationship_ransom_mod", null, relationship_ransom_mod);
			base_ransom_price = field.GetFloat("base_ransom_price", null, base_ransom_price);
			ransom_war_royal = field.GetFloat("ransom_war_royal", null, ransom_war_royal);
			ransom_war_not_royal = field.GetFloat("ransom_war_not_royal", null, ransom_war_not_royal);
			ransom_no_war_not_royal = field.GetFloat("ransom_no_war_not_royal", null, ransom_no_war_not_royal);
			ransom_no_war_royal = field.GetFloat("ransom_no_war_royal", null, ransom_no_war_royal);
			successful_ransom_relationship_bonus = field.GetFloat("successful_ransom_relationship_bonus", null, successful_ransom_relationship_bonus);
			unsuccessful_ransom_relationship_penalty = field.GetFloat("unsuccessful_ransom_relationship_penalty", null, unsuccessful_ransom_relationship_penalty);
			ransom_cooldown_time = field.GetFloat("ransom_cooldown_time", null, ransom_cooldown_time);
			ransom_spy_or_war_and_royal_or_marshal = field.GetFloat("ransom_spy_or_war_and_royal_or_marshal", null, ransom_spy_or_war_and_royal_or_marshal);
			ransom_war_none = field.GetFloat("ransom_war_none", null, ransom_war_none);
			ransom_no_war_no_spy = field.GetFloat("ransom_no_war_no_spy", null, ransom_no_war_no_spy);
			ransom_no_war_spy = field.GetFloat("ransom_no_war_spy", null, ransom_no_war_spy);
			riot_max_escaped = field.GetInt("riot_max_escaped", null, riot_max_escaped);
			riot_max_died = field.GetInt("riot_max_died", null, riot_max_died);
			inspire_riot_unit_count_min = field.GetInt("inspire_riot_unit_count_min", null, inspire_riot_unit_count_min);
			inspire_riot_unit_count_max = field.GetInt("inspire_riot_unit_count_max", null, inspire_riot_unit_count_max);
			inspire_riot_death_chance = field.GetFloat("inspire_riot_death_chance", null, inspire_riot_death_chance);
			inspire_riot_conspiracy_bonus = field.GetFloat("inspire_riot_conspiracy_bonus", null, inspire_riot_conspiracy_bonus);
			plot_escape_together_chance = field.GetFloat("plot_escape_together_chance", null, plot_escape_together_chance);
			mass_escape_success = field.GetFloat("mass_escape_success", null, mass_escape_success);
			mass_escape_death = field.GetFloat("mass_escape_death", null, mass_escape_death);
			min_prisoner_escape = field.GetInt("min_prisoner_escape", null, min_prisoner_escape);
			min_prisoner_riot = field.GetInt("min_prisoner_riot", null, min_prisoner_riot);
			excess_prisoner_riot = field.GetInt("excess_prisoner_riot", null, excess_prisoner_riot);
			excess_prisoner_escape = field.GetInt("excess_prisoner_escape", null, excess_prisoner_escape);
			return true;
		}
	}

	public Def def;

	public List<Character> prisoners = new List<Character>();

	private List<Character> valid_prisoners = new List<Character>();

	private List<OutcomeDef> event_outcomes;

	private List<OutcomeDef> event_unique_outcomes;

	private List<OutcomeDef> event_forced_outcomes;

	private Vars outcome_vars;

	private List<Character> escaped_characters = new List<Character>();

	private List<Character> died_characters = new List<Character>();

	public RoyalDungeon(Kingdom kingdom)
		: base(kingdom)
	{
		def = base.game.defs.Get<Def>("RoyalDungeon");
	}

	public void StartTimer()
	{
		if (obj.IsAuthority() && Timer.Find(obj, "royal_dungeon_tick") == null)
		{
			Timer.Start(obj, "royal_dungeon_tick", base.game.Random(def.event_tick_min, def.event_tick_max));
		}
	}

	public float GetCapacity()
	{
		return (obj as Kingdom)?.GetStat(Stats.ks_prison_capacity, must_exist: false) ?? 0f;
	}

	public void OnPrisonEvent()
	{
		Timer.Start(obj, "royal_dungeon_tick", base.game.Random(def.event_tick_min, def.event_tick_max), restart: true);
		if (!obj.IsAuthority() || prisoners.Count == 0)
		{
			return;
		}
		Kingdom kingdom = obj as Kingdom;
		valid_prisoners.Clear();
		for (int i = 0; i < prisoners.Count; i++)
		{
			if (prisoners[i] != null)
			{
				prisoners[i].GetKingdom();
				if (prisoners[i].IsValid())
				{
					valid_prisoners.Add(prisoners[i]);
				}
			}
		}
		if (def.event_outcomes != null && valid_prisoners.Count != 0)
		{
			outcome_vars = new Vars(obj);
			outcome_vars.Set("kingdom", obj);
			outcome_vars.Set("prisoner_count", prisoners.Count);
			outcome_vars.Set("kingdom_authority", (obj as Kingdom).GetCrownAuthority().GetValue());
			outcome_vars.Set("min_riot", (float)Math.Max(prisoners.Count - def.min_prisoner_riot, 0));
			outcome_vars.Set("min_escape", (float)Math.Max(prisoners.Count - def.min_prisoner_escape, 0));
			outcome_vars.Set("excess_prisoners", Math.Max((float)prisoners.Count - kingdom.GetStat(Stats.ks_prison_capacity), 0f));
			event_outcomes = def.event_outcomes.DecideOutcomes(base.game, outcome_vars, event_forced_outcomes);
			event_unique_outcomes = OutcomeDef.UniqueOutcomes(event_outcomes);
			OutcomeDef.PrecalculateValues(event_unique_outcomes, base.game, outcome_vars, outcome_vars);
			ApplyEventOutcomes();
			event_forced_outcomes = null;
			event_outcomes = null;
			event_unique_outcomes = null;
		}
	}

	private void ApplyEventOutcomes()
	{
		for (int i = 0; i < event_unique_outcomes.Count; i++)
		{
			OutcomeDef outcome = event_unique_outcomes[i];
			ApplyEventOutcome(outcome);
		}
	}

	private bool ApplyEventOutcome(OutcomeDef outcome)
	{
		switch (outcome.key)
		{
		case "escape_success":
		{
			Character character2 = valid_prisoners[base.game.Random(0, valid_prisoners.Count)];
			if (character2 == null)
			{
				return false;
			}
			PlotToEscapeAction obj = character2.actions.Find("PlotToEscapeAction") as PlotToEscapeAction;
			obj.mass_escape_allowed = false;
			obj.Run();
			if (character2.IsValid())
			{
				outcome_vars.Set("target", character2);
			}
			else
			{
				character2.FillDeadVars(outcome_vars);
			}
			outcome_vars.Set("message", "prison_escape_success");
			base.obj.FireEvent("prison_event", outcome_vars);
			obj.mass_escape_allowed = true;
			return true;
		}
		case "mass_escape_success":
		{
			escaped_characters.Clear();
			died_characters.Clear();
			for (int num = valid_prisoners.Count - 1; num >= 0; num--)
			{
				Character character5 = valid_prisoners[num];
				if (character5 != null)
				{
					float num2 = base.game.Random(0, 100);
					if (num2 < def.mass_escape_success)
					{
						escaped_characters.Add(character5);
						character5.Imprison(null);
						Vars vars = new Vars();
						vars.Set("prisoner", character5);
						vars.Set("kingdom", base.obj);
						vars.Set("message", "mass_escape_our_prisoner_escaped");
						if (character5.IsInSpecialCourt())
						{
							character5.game.GetKingdom(character5.GetSpecialCourtKingdomId())?.DelSpecialCourtMember(character5);
						}
						character5.GetKingdom().FireEvent("prison_event", vars);
					}
					else
					{
						num2 -= def.mass_escape_success;
						if (num2 < def.mass_escape_death)
						{
							died_characters.Add(character5);
							character5.Die(new DeadStatus("prison_break", character5));
						}
					}
				}
			}
			outcome_vars.Set("escaped_prisoners", new Value(escaped_characters));
			outcome_vars.Set("died_prisoners", new Value(died_characters));
			if (escaped_characters.Count > 0 || died_characters.Count > 0)
			{
				outcome_vars.Set("message", "mass_escape_success");
			}
			else
			{
				outcome_vars.Set("message", "mass_escape_fail");
			}
			base.obj.FireEvent("prison_event", outcome_vars);
			return true;
		}
		case "inspire_riot_success":
		{
			for (int i = 0; i < valid_prisoners.Count; i++)
			{
				Character character = valid_prisoners[i];
				if (character == null)
				{
					return false;
				}
				if (character.actions.Find("InspireRiotBaseAction") is InspireRiotBaseAction inspireRiotBaseAction && inspireRiotBaseAction.Execute(character))
				{
					outcome_vars.Set("target", character);
					outcome_vars.Set("escaped_prisoners", inspireRiotBaseAction.GetVar("escaped_prisoners"));
					outcome_vars.Set("died_prisoners", inspireRiotBaseAction.GetVar("died_prisoners"));
					outcome_vars.Set("message", "prison_revolt_success");
					base.obj.FireEvent("prison_event", outcome_vars);
					break;
				}
			}
			return true;
		}
		case "die_success":
		{
			Character character4 = valid_prisoners[base.game.Random(0, valid_prisoners.Count)];
			if (character4 == null)
			{
				return false;
			}
			outcome_vars.Set("target", character4.GetNameKey(null, ""));
			outcome_vars.Set("class_title", character4.class_title);
			outcome_vars.Set("title", character4.GetTitle());
			outcome_vars.Set("name", character4.GetName());
			if (character4.name_idx > 0)
			{
				outcome_vars.Set("name_idx", character4.name_idx);
			}
			character4.Die(new DeadStatus("prison_break", character4));
			outcome_vars.Set("message", "prisoner_died");
			base.obj.FireEvent("prison_event", outcome_vars);
			return true;
		}
		case "escape_fail":
		{
			Character character6 = valid_prisoners[base.game.Random(0, valid_prisoners.Count)];
			if (character6 == null)
			{
				return false;
			}
			outcome_vars.Set("target", character6);
			outcome_vars.Set("message", "prison_escape_fail");
			base.obj.FireEvent("prison_event", outcome_vars);
			return true;
		}
		case "mass_escape_fail":
			outcome_vars.Set("message", "mass_escape_fail");
			base.obj.FireEvent("prison_event", outcome_vars);
			return true;
		case "inspire_riot_fail":
		{
			Character character3 = valid_prisoners[base.game.Random(0, valid_prisoners.Count)];
			if (character3 == null)
			{
				return false;
			}
			outcome_vars.Set("target", character3);
			outcome_vars.Set("message", "prison_revolt_fail");
			base.obj.FireEvent("prison_event", outcome_vars);
			return true;
		}
		default:
			if (outcome.Apply(base.game, outcome_vars))
			{
				return true;
			}
			return false;
		}
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Kingdom kingdom = vars.GetVar("own_kingdom").Get<Kingdom>();
		Kingdom kingdom2 = vars.GetVar("target_kingdom").Get<Kingdom>();
		Character character = vars.GetVar("prisoner").Get<Character>();
		if (kingdom == null || kingdom2 == null || character == null)
		{
			return Value.Null;
		}
		switch (key)
		{
		case "relationship_execute_war_mod":
		{
			if (character == null)
			{
				return Value.Null;
			}
			float num4 = 1f;
			if (kingdom2.IsEnemy(character.prison_kingdom))
			{
				num4 = def.relationship_execute_mod_war;
			}
			return num4;
		}
		case "relationship_execute_royalty_mod":
		{
			if (character == null)
			{
				return Value.Null;
			}
			float num3 = 1f;
			if (character.IsPrince() || character.IsCardinal())
			{
				num3 = def.relationship_execute_mod_prince_or_cardinal;
			}
			if (character.IsKing() || character.IsPatriarch())
			{
				num3 = def.relationship_execute_mod_king_or_patriarch;
			}
			return num3;
		}
		case "relationship_execute":
			if (kingdom != kingdom2 && kingdom2.type == Kingdom.Type.Regular)
			{
				return def.field.GetFloat("relationship_execute", vars);
			}
			return Value.Null;
		case "relationship_release_war_mod":
		{
			if (character == null)
			{
				return Value.Null;
			}
			float num2 = 1f;
			if (kingdom2.IsEnemy(character.prison_kingdom))
			{
				num2 = def.relationship_release_mod_war;
			}
			return num2;
		}
		case "relationship_release_royalty_mod":
		{
			if (character == null)
			{
				return Value.Null;
			}
			float num = 1f;
			if (character.IsPrince() || character.IsCardinal())
			{
				num = def.relationship_release_mod_prince_or_cardinal;
			}
			if (character.IsKing() || character.IsPatriarch())
			{
				num = def.relationship_release_mod_king_or_patriarch;
			}
			return num;
		}
		case "relationship_release":
			if (character == null)
			{
				return Value.Null;
			}
			if (kingdom != kingdom2 && kingdom2.type == Kingdom.Type.Regular)
			{
				return def.field.GetFloat("relationship_release", vars);
			}
			return Value.Null;
		default:
			return Value.Null;
		}
	}
}
