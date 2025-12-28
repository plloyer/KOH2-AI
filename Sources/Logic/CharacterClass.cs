using System;
using System.Collections.Generic;

namespace Logic;

public class CharacterClass
{
	public class Def : Logic.Def
	{
		public string name;

		public Def base_def;

		public Opportunity.ClassDef opportunities;

		public List<(Action.Def, DT.Field)> actions;

		public SkillsTable.RowDef skills_row_def;

		public List<string> skill_slots;

		public string class_level_king_ability;

		public KingdomAI.Expense.Category ai_category;

		public DT.Field ai_min_court_count;

		public DT.Field ai_max_court_count;

		public DT.Field ai_max_characters_limit_per_realm;

		public Dictionary<string, string> voice_lines;

		public Dictionary<string, string> sound_effects;

		public bool can_be_picked_randomly_at_start;

		public bool HasAction(Action.Def def)
		{
			if (actions == null)
			{
				return false;
			}
			for (int i = 0; i < actions.Count; i++)
			{
				if (actions[i].Item1 == def)
				{
					return true;
				}
			}
			return false;
		}

		public void AddAction(Action.Def def, DT.Field requirement)
		{
			if (def.available_in_class_defs == null)
			{
				def.available_in_class_defs = new List<Def>();
			}
			def.available_in_class_defs.Add(this);
			actions.Add((def, requirement));
		}

		public int GetAIMinCourtCount(Kingdom k)
		{
			if (k == null)
			{
				return 0;
			}
			if (ai_min_court_count == null)
			{
				return 0;
			}
			return ai_min_court_count.Int(k);
		}

		public int GetAIMaxCourtCount(Kingdom k)
		{
			if (k == null)
			{
				return 0;
			}
			if (ai_max_court_count == null)
			{
				if (k.court != null)
				{
					return k.court.Count;
				}
				return 0;
			}
			return ai_max_court_count.Int(k);
		}

		public int GetAIMaxTotalCharactersPerRealm(Kingdom k)
		{
			if (k == null)
			{
				return 1;
			}
			if (ai_max_characters_limit_per_realm == null)
			{
				return 1;
			}
			return ai_max_characters_limit_per_realm.Int(k);
		}

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			name = dt_def.path;
			LoadSkillSlots();
			class_level_king_ability = field.GetString("class_level_king_ability");
			Enum.TryParse<KingdomAI.Expense.Category>(field.GetString("ai_category"), out ai_category);
			ai_min_court_count = field.FindChild("ai_min_court_count");
			ai_max_court_count = field.FindChild("ai_max_court_count");
			ai_max_characters_limit_per_realm = field.FindChild("ai_max_characters_limit_per_realm");
			if (field.based_on != null)
			{
				base_def = game.defs.Get<Def>(field.based_on.Path());
			}
			DT.Field f = field.FindChild("voice_lines");
			DT.Field f2 = field.FindChild("sound_effects");
			LoadVoiceLines(f, base.field);
			LoadSoundEffects(f2, base.field);
			opportunities = Opportunity.ClassDef.Load(field.FindChild("opportunities"), game);
			LoadActions(game);
			can_be_picked_randomly_at_start = field.GetBool("can_be_picked_randomly_at_start", null, can_be_picked_randomly_at_start);
			return true;
		}

		private void LoadSkillSlots()
		{
			skill_slots = new List<string>();
			DT.Field field = base.field.FindChild("skill_slots");
			if (field == null)
			{
				return;
			}
			int num = field.NumValues();
			for (int i = 0; i < num; i++)
			{
				string text = field.String(i);
				if (!string.IsNullOrEmpty(text))
				{
					skill_slots.Add(text);
				}
			}
		}

		private void LoadVoiceLines(DT.Field f, DT.Field parent)
		{
			if (f == null)
			{
				return;
			}
			if (voice_lines == null)
			{
				voice_lines = new Dictionary<string, string>();
			}
			if (f.children != null)
			{
				for (int i = 0; i < f.children.Count; i++)
				{
					string key = f.children[i].key;
					if (!string.IsNullOrEmpty(key) && !voice_lines.ContainsKey(key))
					{
						voice_lines[key] = f.children[i].value.String();
					}
				}
			}
			if (parent.based_on != null)
			{
				LoadVoiceLines(parent.based_on.FindChild("voice_lines"), parent.based_on);
			}
		}

		private void LoadSoundEffects(DT.Field f, DT.Field parent)
		{
			if (f == null)
			{
				return;
			}
			if (sound_effects == null)
			{
				sound_effects = new Dictionary<string, string>();
			}
			if (f.children != null)
			{
				for (int i = 0; i < f.children.Count; i++)
				{
					string key = f.children[i].key;
					if (!string.IsNullOrEmpty(key) && !sound_effects.ContainsKey(key))
					{
						sound_effects[key] = f.children[i].value.String();
					}
				}
			}
			if (parent.based_on != null)
			{
				LoadSoundEffects(parent.based_on.FindChild("sound_effects"), parent.based_on);
			}
		}

		private void LoadActions(Game game)
		{
			DT.Field field = base.field.FindChild("actions", null, allow_base: false, allow_extensions: false, allow_switches: false);
			if (field == null || field.children == null)
			{
				return;
			}
			actions = new List<(Action.Def, DT.Field)>(field.children.Count);
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				string key = field2.key;
				if (!(key == ""))
				{
					Action.Def def = game.defs.Get<Action.Def>(key);
					DT.Field requirement = field2.FindChild("requirement");
					AddAction(def, requirement);
				}
			}
		}
	}
}
