using System;
using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.War)]
public class War : Object
{
	[Serialization.State(11)]
	public class InitState : Serialization.ObjectState
	{
		public int side1Kingdom_id;

		public int side2Kingdom_id;

		public InvolvementReason InvolvementReason;

		public static InitState Create()
		{
			return new InitState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			War war = obj as War;
			side1Kingdom_id = war.attacker.id;
			side2Kingdom_id = war.defender.id;
			InvolvementReason = war.involvementReason;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(side1Kingdom_id, "side1Kingdom_id");
			ser.Write7BitUInt(side2Kingdom_id, "side2Kingdom_id");
			if (Serialization.cur_version >= 2)
			{
				ser.Write7BitUInt((int)InvolvementReason, "involvement_reason");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			side1Kingdom_id = ser.Read7BitUInt("side1Kingdom_id");
			side2Kingdom_id = ser.Read7BitUInt("side2Kingdom_id");
			if (Serialization.cur_version >= 2)
			{
				InvolvementReason = (InvolvementReason)ser.Read7BitUInt("involvement_reason");
			}
		}

		public override void ApplyTo(Object obj)
		{
			War war = obj as War;
			if (Serialization.cur_version >= 2)
			{
				war.involvementReason = InvolvementReason;
			}
			war.Init(war.game.GetKingdom(side1Kingdom_id), war.game.GetKingdom(side2Kingdom_id));
		}
	}

	[Serialization.State(12)]
	public class HistoryState : Serialization.ObjectState
	{
		public struct HistoryEntryData
		{
			public string def_id;

			public Data vars;

			public HistoryEntryData(string actionType_def_id, Data vars)
			{
				def_id = actionType_def_id;
				this.vars = vars;
			}
		}

		public List<HistoryEntryData> history = new List<HistoryEntryData>();

		public static HistoryState Create()
		{
			return new HistoryState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is War war))
			{
				return false;
			}
			return war.history.Count > 0;
		}

		public override bool InitFrom(Object obj)
		{
			War war = obj as War;
			for (int i = 0; i < war.history.Count; i++)
			{
				HistoryEntry historyEntry = war.history[i];
				history.Add(new HistoryEntryData(historyEntry.GetDefField().key, historyEntry.vars.CreateFullData()));
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(history.Count, "count");
			for (int i = 0; i < history.Count; i++)
			{
				ser.WriteStr(history[i].def_id, "def_id_", i);
				ser.WriteData(history[i].vars, "vars_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				string actionType_def_id = ser.ReadStr("def_id_", i);
				Data vars = ser.ReadData("vars_", i);
				history.Add(new HistoryEntryData(actionType_def_id, vars));
			}
		}

		public override void ApplyTo(Object obj)
		{
			War war = obj as War;
			Game game = war.game;
			war.history.Clear();
			for (int i = 0; i < history.Count; i++)
			{
				string def_id = history[i].def_id;
				war.history.Add(new HistoryEntry(game.dt.Find("WarStats." + def_id), Data.RestoreObject<Vars>(history[i].vars, game)));
			}
		}
	}

	[Serialization.State(13)]
	public class SupportersState : Serialization.ObjectState
	{
		public List<int> attacker_supporters;

		public List<int> defender_supporters;

		public List<int> refused_to_participate;

		public static SupportersState Create()
		{
			return new SupportersState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is War war))
			{
				return false;
			}
			if (war.attackers.Count > 1)
			{
				return true;
			}
			if (war.defenders.Count > 1)
			{
				return true;
			}
			if (war.refusedToParticipate.Count > 0)
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is War war))
			{
				return false;
			}
			if (war.attackers.Count > 1)
			{
				attacker_supporters = new List<int>(war.attackers.Count - 1);
				for (int i = 1; i < war.attackers.Count; i++)
				{
					Kingdom kingdom = war.attackers[i];
					attacker_supporters.Add(kingdom.id);
				}
			}
			if (war.defenders.Count > 1)
			{
				defender_supporters = new List<int>(war.defenders.Count - 1);
				for (int j = 1; j < war.defenders.Count; j++)
				{
					Kingdom kingdom2 = war.defenders[j];
					defender_supporters.Add(kingdom2.id);
				}
			}
			if (war.refusedToParticipate.Count > 0)
			{
				refused_to_participate = new List<int>(war.refusedToParticipate.Count);
				for (int k = 0; k < war.refusedToParticipate.Count; k++)
				{
					Kingdom kingdom3 = war.refusedToParticipate[k];
					refused_to_participate.Add(kingdom3.id);
				}
			}
			if (attacker_supporters == null && defender_supporters == null)
			{
				return refused_to_participate != null;
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((attacker_supporters != null) ? attacker_supporters.Count : 0);
			ser.Write7BitUInt(num, "attackers");
			for (int i = 0; i < num; i++)
			{
				int val = attacker_supporters[i];
				ser.Write7BitUInt(val, "attacker", i);
			}
			int num2 = ((defender_supporters != null) ? defender_supporters.Count : 0);
			ser.Write7BitUInt(num2, "defenders");
			for (int j = 0; j < num2; j++)
			{
				int val2 = defender_supporters[j];
				ser.Write7BitUInt(val2, "defender", j);
			}
			if (Serialization.cur_version >= 20)
			{
				int num3 = ((refused_to_participate != null) ? refused_to_participate.Count : 0);
				ser.Write7BitUInt(num3, "refusers");
				for (int k = 0; k < num3; k++)
				{
					int val3 = refused_to_participate[k];
					ser.Write7BitUInt(val3, "refused", k);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("attackers");
			if (num > 0)
			{
				attacker_supporters = new List<int>(num);
				for (int i = 0; i < num; i++)
				{
					int item = ser.Read7BitUInt("attacker", i);
					attacker_supporters.Add(item);
				}
			}
			int num2 = ser.Read7BitUInt("defenders");
			if (num2 > 0)
			{
				defender_supporters = new List<int>(num2);
				for (int j = 0; j < num2; j++)
				{
					int item2 = ser.Read7BitUInt("defender", j);
					defender_supporters.Add(item2);
				}
			}
			if (Serialization.cur_version < 20)
			{
				return;
			}
			int num3 = ser.Read7BitUInt("refusers");
			if (num3 > 0)
			{
				refused_to_participate = new List<int>(num3);
				for (int k = 0; k < num3; k++)
				{
					int item3 = ser.Read7BitUInt("refused", k);
					refused_to_participate.Add(item3);
				}
			}
		}

		private void SetSupporters(War w, int side, List<int> kids)
		{
			List<Kingdom> kingdoms = w.GetKingdoms(side);
			for (int num = kingdoms.Count - 1; num >= 1; num--)
			{
				Kingdom kingdom = kingdoms[num];
				if (kids == null || !kids.Contains(kingdom.id))
				{
					w.Leave(kingdom, null, silent: true);
				}
			}
			if (kids == null)
			{
				return;
			}
			for (int i = 0; i < kids.Count; i++)
			{
				int kid = kids[i];
				Kingdom kingdom2 = w.game.GetKingdom(kid);
				if (!kingdoms.Contains(kingdom2))
				{
					w.Join(kingdom2, side);
				}
			}
		}

		private void SetRefusers(War w, List<int> kids)
		{
			if (kids == null)
			{
				return;
			}
			for (int i = 0; i < kids.Count; i++)
			{
				int kid = kids[i];
				Kingdom kingdom = w.game.GetKingdom(kid);
				if (!w.refusedToParticipate.Contains(kingdom))
				{
					w.refusedToParticipate.Add(kingdom);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is War w)
			{
				SetSupporters(w, 0, attacker_supporters);
				SetSupporters(w, 1, defender_supporters);
				SetRefusers(w, refused_to_participate);
			}
		}
	}

	[Serialization.State(14)]
	public class ConcludeState : Serialization.ObjectState
	{
		public int victor_side;

		public static ConcludeState Create()
		{
			return new ConcludeState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			War war = obj as War;
			victor_side = war.victor_side;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(victor_side + 1, "winner");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			victor_side = ser.Read7BitUInt("winner") - 1;
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is War war)
			{
				war.Conclude(victor_side);
			}
		}
	}

	[Serialization.State(15)]
	public class ScoreState : Serialization.ObjectState
	{
		private struct IndividualScore
		{
			public int kingdom_from_id;

			public int kingdom_to_id;

			public float score;

			public IndividualScore(int kingdom_from_id, int kingdom_to_id, float score)
			{
				this.kingdom_from_id = kingdom_from_id;
				this.kingdom_to_id = kingdom_to_id;
				this.score = score;
			}
		}

		public float scoreAttackers;

		public float scoreDefenders;

		private List<IndividualScore> scoresIndividual = new List<IndividualScore>();

		public static ScoreState Create()
		{
			return new ScoreState();
		}

		public static bool IsNeeded(Object obj)
		{
			War war = obj as War;
			if (war.scoreAttackers == 0f && war.scoreDefenders == 0f)
			{
				return war.scoresIndividual.Count > 0;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			War war = obj as War;
			scoreAttackers = war.scoreAttackers;
			scoreDefenders = war.scoreDefenders;
			foreach (KeyValuePair<Kingdom, Dictionary<Kingdom, float>> item in war.scoresIndividual)
			{
				foreach (KeyValuePair<Kingdom, float> item2 in item.Value)
				{
					scoresIndividual.Add(new IndividualScore(item.Key.id, item2.Key.id, item2.Value));
				}
			}
			scoreAttackers = war.scoreAttackers;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(scoreAttackers, "scoreAttackers");
			ser.WriteFloat(scoreDefenders, "scoreDefenders");
			ser.Write7BitUInt(scoresIndividual.Count, "count");
			for (int i = 0; i < scoresIndividual.Count; i++)
			{
				ser.Write7BitUInt(scoresIndividual[i].kingdom_from_id, "kingdom_from_id_", i);
				ser.Write7BitUInt(scoresIndividual[i].kingdom_to_id, "kingdom_to_id_", i);
				ser.WriteFloat(scoresIndividual[i].score, "score_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			scoreAttackers = ser.ReadFloat("scoreAttackers");
			scoreDefenders = ser.ReadFloat("scoreDefenders");
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				int kingdom_from_id = ser.Read7BitUInt("kingdom_from_id_", i);
				int kingdom_to_id = ser.Read7BitUInt("kingdom_to_id_", i);
				float score = ser.ReadFloat("score_", i);
				scoresIndividual.Add(new IndividualScore(kingdom_from_id, kingdom_to_id, score));
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is War war))
			{
				return;
			}
			war.scoresIndividual.Clear();
			war.scoreAttackers = scoreAttackers;
			war.scoreDefenders = scoreDefenders;
			for (int i = 0; i < scoresIndividual.Count; i++)
			{
				Kingdom kingdom = war.game.GetKingdom(scoresIndividual[i].kingdom_from_id);
				Kingdom kingdom2 = war.game.GetKingdom(scoresIndividual[i].kingdom_to_id);
				float score = scoresIndividual[i].score;
				if (war.scoresIndividual.ContainsKey(kingdom))
				{
					war.scoresIndividual[kingdom].Add(kingdom2, score);
					continue;
				}
				war.scoresIndividual.Add(kingdom, new Dictionary<Kingdom, float>(10) { { kingdom2, score } });
			}
		}
	}

	[Serialization.State(16)]
	public class DefState : Serialization.ObjectState
	{
		private string def_id = "";

		public static DefState Create()
		{
			return new DefState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			War war = obj as War;
			def_id = war.def.id;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def_id, "def_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def_id = ser.ReadStr("def_id");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as War).SetType(def_id, send_state: false);
		}
	}

	[Serialization.State(17)]
	public class BonusesState : Serialization.ObjectState
	{
		public struct BonusData
		{
			public string fieldPath;

			public float value;

			public BonusData(string fieldPath, float value)
			{
				this.fieldPath = fieldPath;
				this.value = value;
			}
		}

		public List<BonusData> attackersBonuses = new List<BonusData>();

		public List<BonusData> defendersBonuses = new List<BonusData>();

		public static BonusesState Create()
		{
			return new BonusesState();
		}

		public static bool IsNeeded(Object obj)
		{
			War war = obj as War;
			if (war.attackersBonuses == null)
			{
				return war.defendersBonuses != null;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			War war = obj as War;
			if (war.attackersBonuses != null)
			{
				foreach (KeyValuePair<string, Bonus> attackersBonuse in war.attackersBonuses)
				{
					attackersBonuses.Add(new BonusData(attackersBonuse.Value.field.Path(), attackersBonuse.Value.value));
				}
			}
			if (war.defendersBonuses != null)
			{
				foreach (KeyValuePair<string, Bonus> defendersBonuse in war.defendersBonuses)
				{
					defendersBonuses.Add(new BonusData(defendersBonuse.Value.field.Path(), defendersBonuse.Value.value));
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(attackersBonuses.Count, "count_attackers");
			for (int i = 0; i < attackersBonuses.Count; i++)
			{
				ser.WriteStr(attackersBonuses[i].fieldPath, "attackers_path", i);
				ser.WriteFloat(attackersBonuses[i].value, "attackers_value", i);
			}
			ser.Write7BitUInt(defendersBonuses.Count, "count_defenders");
			for (int j = 0; j < defendersBonuses.Count; j++)
			{
				ser.WriteStr(defendersBonuses[j].fieldPath, "defenders_path", j);
				ser.WriteFloat(defendersBonuses[j].value, "defenders_value", j);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count_attackers");
			for (int i = 0; i < num; i++)
			{
				attackersBonuses.Add(new BonusData(ser.ReadStr("attackers_path", i), ser.ReadFloat("attackers_value", i)));
			}
			num = ser.Read7BitUInt("count_defenders");
			for (int j = 0; j < num; j++)
			{
				defendersBonuses.Add(new BonusData(ser.ReadStr("defenders_path", j), ser.ReadFloat("defenders_value", j)));
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is War war))
			{
				return;
			}
			war.attackersBonuses?.Clear();
			war.defendersBonuses?.Clear();
			if (attackersBonuses.Count != 0 && war.attackersBonuses == null)
			{
				war.attackersBonuses = new Dictionary<string, Bonus>();
			}
			if (defendersBonuses.Count != 0 && war.defendersBonuses == null)
			{
				war.defendersBonuses = new Dictionary<string, Bonus>();
			}
			for (int i = 0; i < attackersBonuses.Count; i++)
			{
				DT.Field field = obj.game.dt.Find(attackersBonuses[i].fieldPath);
				float value = attackersBonuses[i].value;
				if (field != null)
				{
					war.attackersBonuses.Add(field.key, new Bonus(value, field, field.FindChild("condition")));
				}
			}
			for (int j = 0; j < defendersBonuses.Count; j++)
			{
				DT.Field field2 = obj.game.dt.Find(defendersBonuses[j].fieldPath);
				float value2 = defendersBonuses[j].value;
				if (field2 != null)
				{
					war.defendersBonuses.Add(field2.key, new Bonus(value2, field2, field2.FindChild("condition")));
				}
			}
		}
	}

	public enum State
	{
		ongoing,
		concluded
	}

	public class Def : Logic.Def
	{
		public int score_ratio_norm = 100;

		public int strength_ratio_norm = 100;

		public float strength_confidence_factor = 0.5f;

		public float confidence_fade_time = 300f;

		public float confidence_fade_power = 0.5f;

		public int province_taken_base_score = 10;

		public int province_taken_building_score = 2;

		public float core_province_mult = 2f;

		public int squad_destroyed_base_score = 1;

		public int squad_destroyed_level_score = 1;

		public int neutralized_knight_base_score = 1;

		public int neutralized_knight_level_score = 1;

		public int royalty_mod_prince = 2;

		public int royalty_mod_king = 3;

		private float chance_imprison_merchants_perc = 50f;

		private float chance_imprison_diplomats_perc = 20f;

		private float chance_imprison_clerics_perc = 20f;

		public DT.Field upkeep_field;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			score_ratio_norm = field.GetInt("score_ratio_norm", null, score_ratio_norm);
			strength_ratio_norm = field.GetInt("strength_ratio_norm", null, strength_ratio_norm);
			strength_confidence_factor = field.GetFloat("strength_confidence_factor", null, strength_confidence_factor);
			confidence_fade_time = field.GetFloat("confidence_fade_time", null, confidence_fade_time);
			confidence_fade_power = field.GetFloat("confidence_fade_power", null, confidence_fade_power);
			province_taken_base_score = field.GetInt("province_taken_base_score", null, province_taken_base_score);
			province_taken_building_score = field.GetInt("province_taken_building_score", null, province_taken_building_score);
			core_province_mult = field.GetFloat("core_province_mult", null, core_province_mult);
			squad_destroyed_base_score = field.GetInt("squad_destroyed_base_score", null, squad_destroyed_base_score);
			squad_destroyed_level_score = field.GetInt("squad_destroyed_level_score", null, squad_destroyed_level_score);
			neutralized_knight_base_score = field.GetInt("neutralized_knight_base_score", null, neutralized_knight_base_score);
			neutralized_knight_level_score = field.GetInt("neutralized_knight_level_score", null, neutralized_knight_level_score);
			royalty_mod_prince = field.GetInt("royalty_mod_prince", null, royalty_mod_prince);
			royalty_mod_king = field.GetInt("royalty_mod_king", null, royalty_mod_king);
			chance_imprison_merchants_perc = field.GetFloat("chance_imprison_merchants_perc", null, chance_imprison_merchants_perc);
			chance_imprison_diplomats_perc = field.GetFloat("chance_imprison_diplomats_perc", null, chance_imprison_diplomats_perc);
			chance_imprison_clerics_perc = field.GetFloat("chance_imprison_clerics_perc", null, chance_imprison_clerics_perc);
			upkeep_field = field.FindChild("upkeep");
			return base.Load(game);
		}

		public float GetCharacterImprisonmentChance(Character c)
		{
			if (c == null)
			{
				return 0f;
			}
			if (c.IsMerchant())
			{
				return chance_imprison_merchants_perc;
			}
			if (c.IsDiplomat())
			{
				return chance_imprison_diplomats_perc;
			}
			if (c.IsCleric())
			{
				return chance_imprison_clerics_perc;
			}
			return 0f;
		}

		public Dictionary<string, Bonus> CalcBonuses(Kingdom k)
		{
			if (base.field.key == "Jihad")
			{
				return CalcJihadBonuses(k);
			}
			return null;
		}

		public Dictionary<string, Bonus> CalcJihadBonuses(Kingdom k)
		{
			if (!k.IsCaliphate())
			{
				return null;
			}
			Game game = k.game;
			Dictionary<string, Bonus> dictionary = new Dictionary<string, Bonus>();
			DT.Field field = base.field.FindChild("jihad_bonuses");
			DT.Field field2 = field.FindChild("normal_bonuses");
			for (int i = 0; i < field2.children.Count; i++)
			{
				DT.Field field3 = field2.children[i];
				dictionary.Add(field3.key, new Bonus(field3.Value(), field3, field3.FindChild("condition")));
			}
			DT.Field field4 = field.FindChild("random_bonuses");
			int num = field4.Value(0);
			int num2 = num;
			if (field4.NumValues() == 2)
			{
				num2 = field4.Value(1);
			}
			int num3 = game.Random(num, num2 + 1);
			int count = field4.children.Count;
			List<int> randomIndexes = game.GetRandomIndexes(count);
			for (int j = 0; j < num3 && j < count; j++)
			{
				DT.Field field5 = field4.children[randomIndexes[j]];
				dictionary.Add(field5.key, new Bonus(field5.Value(game.Random(0, field5.NumValues())), field5, field5.FindChild("condition")));
			}
			return dictionary;
		}
	}

	public struct Bonus
	{
		public float value;

		public DT.Field field;

		public DT.Field condition;

		public Bonus(float value, DT.Field field, DT.Field condition)
		{
			this.value = value;
			this.condition = condition;
			this.field = field;
		}

		public override string ToString()
		{
			return field?.key + ": " + value;
		}
	}

	public enum InvolvementReason
	{
		DiplomatProvocation,
		SpyProvocation,
		CrownHandedOverByPuppet,
		PrisonRevolt,
		VassalIndependenceClaim,
		VassalIndependenceRefuseReconsider,
		FormalDeclaration,
		DemandAttackKingdom,
		VassalSummoned,
		InheritanceClaimDeclined,
		RejectAIEmperorOfTheWorld,
		RejectPretenderEmperorOfTheWorld,
		SplitKingdom,
		DeclarationOfIndependence,
		GameStartRelations,
		Jihad,
		OffensivePactActivated,
		DefensivePactActivated,
		OfferedSupport,
		InternalPurposes
	}

	public delegate War CreateWar(Kingdom combatant1, Kingdom combatant2, InvolvementReason reason, bool apply_consequences = true);

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public Def def;

	public float war_modifier = 1f;

	public Stats stats;

	public List<HistoryEntry> history = new List<HistoryEntry>();

	public State state;

	public int victor_side = -1;

	public List<Kingdom> attackers = new List<Kingdom>();

	public List<Kingdom> defenders = new List<Kingdom>();

	public List<Kingdom> refusedToParticipate = new List<Kingdom>();

	public Dictionary<Kingdom, Time> lastActivities = new Dictionary<Kingdom, Time>();

	public Dictionary<Kingdom, Dictionary<Kingdom, float>> scoresIndividual = new Dictionary<Kingdom, Dictionary<Kingdom, float>>();

	public float scoreAttackers;

	public float scoreDefenders;

	public Dictionary<string, Bonus> attackersBonuses;

	public Dictionary<string, Bonus> defendersBonuses;

	public InvolvementReason involvementReason;

	private List<(int, bool)> battles_after_war = new List<(int, bool)>();

	public Kingdom attacker
	{
		get
		{
			if (attackers.Count >= 1)
			{
				return attackers[0];
			}
			return null;
		}
	}

	public Kingdom defender
	{
		get
		{
			if (defenders.Count >= 1)
			{
				return defenders[0];
			}
			return null;
		}
	}

	private Kingdom plr_kingdom => game.GetKingdom(game.multiplayer.playerData.kingdomId);

	public War(Multiplayer multiplayer)
		: base(multiplayer)
	{
		def = multiplayer.game.defs.GetBase<Def>();
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new War(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}

	public War(Kingdom k1, Kingdom k2, InvolvementReason reason = InvolvementReason.InternalPurposes, bool apply_consequences = true, Def def = null)
		: base(k1.game)
	{
		if (def == null)
		{
			def = k1.game.defs.GetBase<Def>();
		}
		this.def = def;
		involvementReason = reason;
		RelationUtils.WarFact value = new RelationUtils.WarFact(this, k1, 0, RelationUtils.WarFact.Action.Attack);
		RelationUtils.validators.AddLast(value);
		Init(k1, k2, apply_consequences);
		RelationUtils.validators.Remove(value);
		OnWarStartedAnalytics(reason, k1);
		OnWarStartedAnalytics(reason, k2);
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return def.field.key + ".name";
	}

	protected void Init(Kingdom k1, Kingdom k2, bool apply_consequences = true)
	{
		k1.NotifyListeners("declared_war", k2);
		lastActivities[k1] = game.time;
		lastActivities[k2] = game.time;
		attackers.Add(k1);
		defenders.Add(k2);
		k1.AddWar(this);
		k2.AddWar(this);
		if (IsAuthority())
		{
			SetType(def);
			BreakRelationsWithEnemies(k1, leave_pacts: false, apply_consequences);
		}
		k1.NotifyListeners("war_started", this);
		k2.NotifyListeners("war_started", this);
	}

	public void StopBattlesAtEndOfWar(Kingdom k1, Kingdom k2)
	{
		bool flag = false;
		for (int i = 0; i < k1.realms.Count; i++)
		{
			Realm realm = k1.realms[i];
			for (int j = 0; j < realm.settlements.Count; j++)
			{
				Battle battle = realm.settlements[j].battle;
				if (battle == null || battle.IsFinishing())
				{
					continue;
				}
				for (int l = 0; l < 2; l++)
				{
					List<Army> armies = battle.GetArmies(l);
					for (int m = 0; m < armies.Count; m++)
					{
						Army army = armies[m];
						if (!army.IsEnemy(battle.GetSideKingdom(1 - l)))
						{
							battle.Leave(army, check_victory: true);
						}
					}
				}
			}
		}
		for (int n = 0; n < k1.realms.Count; n++)
		{
			Realm realm2 = k1.realms[n];
			for (int num = 0; num < realm2.settlements.Count; num++)
			{
				Battle battle2 = realm2.settlements[num].battle;
				if (battle2 == null || battle2.IsFinishing())
				{
					continue;
				}
				for (int num2 = 0; num2 < 2; num2++)
				{
					List<Army> armies2 = battle2.GetArmies(num2);
					for (int num3 = 0; num3 < armies2.Count; num3++)
					{
						Army army2 = armies2[num3];
						if (!army2.IsEnemy(battle2.GetSideKingdom(1 - num2)))
						{
							battle2.Leave(army2, check_victory: true);
						}
					}
				}
			}
		}
		for (int num4 = 0; num4 < k1.armies.Count; num4++)
		{
			Army army3 = k1.armies[num4];
			if (army3.battle == null || army3.battle.IsFinishing())
			{
				continue;
			}
			if (army3.battle.attacker_kingdom == k2 || army3.battle.defender_kingdom == k2)
			{
				bool flag2 = army3.battle.defender_kingdom.IsDefeated();
				if (army3.battle.defender_kingdom == k1 || army3.battle.attacker_kingdom == k1)
				{
					army3.battle.Cancel(Battle.VictoryReason.WarOver);
				}
				else if (army3.battle_side == 0)
				{
					army3.battle.RetreatSupporters(0);
				}
				else
				{
					if (army3.battle_side != 1)
					{
						continue;
					}
					army3.battle.RetreatSupporters(1);
				}
				flag = flag || !flag2;
			}
			else
			{
				Army army4 = army3.battle.GetArmy(1 - army3.battle_side);
				if (army4 != null && !army3.IsEnemy(army4))
				{
					army3.battle.Leave(army4, check_victory: true);
				}
			}
		}
		for (int num5 = 0; num5 < k2.armies.Count; num5++)
		{
			Army army5 = k2.armies[num5];
			if (army5.battle == null || army5.battle.IsFinishing())
			{
				continue;
			}
			if (army5.battle.attacker_kingdom == k1 || army5.battle.defender_kingdom == k1)
			{
				bool flag3 = army5.battle.defender_kingdom.IsDefeated();
				if (army5.battle.defender_kingdom == k2 || army5.battle.attacker_kingdom == k2)
				{
					army5.battle.Cancel(Battle.VictoryReason.WarOver);
				}
				else if (army5.battle_side == 0)
				{
					army5.battle.RetreatSupporters(0);
				}
				else
				{
					if (army5.battle_side != 1)
					{
						continue;
					}
					army5.battle.RetreatSupporters(1);
				}
				flag = flag || !flag3;
			}
			else
			{
				Army army6 = army5.battle.GetArmy(1 - army5.battle_side);
				if (army6 != null && !army5.IsEnemy(army6))
				{
					army5.battle.Leave(army6, check_victory: true);
				}
			}
		}
		bool flag4 = false;
		bool flag5 = false;
		for (int num6 = 0; num6 < battles_after_war.Count; num6++)
		{
			int item = battles_after_war[num6].Item1;
			if (item == k2.id)
			{
				flag5 = true;
			}
			else if (item == k1.id)
			{
				flag4 = true;
			}
		}
		if (!flag4)
		{
			battles_after_war.Add((k1.id, flag));
		}
		if (!flag5)
		{
			battles_after_war.Add((k2.id, flag));
		}
	}

	private void BreakRelationsWithEnemies(Kingdom k, bool leave_pacts = true, bool apply_consequences = true)
	{
		if (!AssertAuthority())
		{
			return;
		}
		List<Kingdom> enemies = GetEnemies(k);
		using (new RelationUtils.SpreadWarData(this))
		{
			for (int i = 0; i < enemies.Count; i++)
			{
				Kingdom kingdom = enemies[i];
				RecallImprisonCourtMembers(k);
				RecallImprisonCourtMembers(kingdom, k);
				k.CloseTradeRoute(kingdom, isManual: true);
				kingdom.CloseTradeRoute(k, isManual: true);
				k.SetStance(kingdom, RelationUtils.Stance.War, this, apply_consequences);
				k.loans.Remove(kingdom.id);
				kingdom.loans.Remove(k.id);
				k.ClearSupport(kingdom);
				kingdom.ClearSupport(k);
				if (leave_pacts)
				{
					LeavePactsByWar(k, kingdom);
				}
			}
		}
	}

	public void SetType(string def_name, bool send_state = true)
	{
		SetType(game.defs.Get<Def>(def_name), send_state);
	}

	public void SetType(Def def, bool send_state = true)
	{
		if (def != null)
		{
			this.def = def;
			if (IsJihad())
			{
				AddListener(game.religions);
			}
			else
			{
				DelListener(game.religions);
			}
			if (send_state)
			{
				SendState<DefState>();
			}
			attackersBonuses = def.CalcBonuses(attacker);
			defendersBonuses = def.CalcBonuses(defender);
			NotifyListeners("type_changed");
			if (send_state)
			{
				SendState<BonusesState>();
			}
		}
	}

	public bool IsJihad()
	{
		return def.field.key == "Jihad";
	}

	public int EnemySide(int side)
	{
		return side switch
		{
			0 => 1, 
			1 => 0, 
			_ => -1, 
		};
	}

	public int GetSide(Kingdom k)
	{
		if (attackers.Contains(k))
		{
			return 0;
		}
		if (defenders.Contains(k))
		{
			return 1;
		}
		return -1;
	}

	public int EnemySide(Kingdom k)
	{
		if (attackers.Contains(k))
		{
			return 1;
		}
		if (defenders.Contains(k))
		{
			return 0;
		}
		return -1;
	}

	public bool Involves(Kingdom k)
	{
		return GetSide(k) >= 0;
	}

	public bool IsLeader(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		if (k != attacker)
		{
			return k == defender;
		}
		return true;
	}

	public Kingdom GetLeader(int side)
	{
		return side switch
		{
			0 => attacker, 
			1 => defender, 
			_ => null, 
		};
	}

	public Kingdom GetLeader(Kingdom k)
	{
		int side = GetSide(k);
		return GetLeader(side);
	}

	public Kingdom GetEnemyLeader(int side)
	{
		int side2 = EnemySide(side);
		return GetLeader(side2);
	}

	public Kingdom GetEnemyLeader(Kingdom k)
	{
		int side = GetSide(k);
		int side2 = EnemySide(side);
		return GetLeader(side2);
	}

	public List<Kingdom> GetKingdoms(int side)
	{
		return side switch
		{
			0 => attackers, 
			1 => defenders, 
			_ => null, 
		};
	}

	public List<Kingdom> GetEnemies(Kingdom k)
	{
		int side = GetSide(k);
		int side2 = EnemySide(side);
		return GetKingdoms(side2);
	}

	public List<Kingdom> GetAllies(Kingdom k)
	{
		int side = GetSide(k);
		return GetKingdoms(side);
	}

	public List<Kingdom> GetAlliesExcludeSelf(Kingdom k)
	{
		List<Kingdom> allies = GetAllies(k);
		if (allies == null)
		{
			return null;
		}
		List<Kingdom> list = new List<Kingdom>(allies.Count - 1);
		for (int i = 1; i < allies.Count; i++)
		{
			Kingdom item = allies[i];
			list.Add(item);
		}
		return list;
	}

	public List<Kingdom> GetSupporters(int side)
	{
		List<Kingdom> kingdoms = GetKingdoms(side);
		if (kingdoms == null)
		{
			return null;
		}
		List<Kingdom> list = new List<Kingdom>(kingdoms.Count - 1);
		for (int i = 1; i < kingdoms.Count; i++)
		{
			Kingdom item = kingdoms[i];
			list.Add(item);
		}
		return list;
	}

	public bool IsEnemy(Kingdom k, int side)
	{
		List<Kingdom> kingdoms = GetKingdoms(EnemySide(side));
		if (kingdoms == null)
		{
			return false;
		}
		if (!kingdoms.Contains(k))
		{
			return false;
		}
		return true;
	}

	public bool IsEnemy(Kingdom k1, Kingdom k2)
	{
		int side = GetSide(k1);
		if (side < 0)
		{
			return false;
		}
		int side2 = GetSide(k2);
		if (side2 < 0)
		{
			return false;
		}
		return side != side2;
	}

	public bool IsAlly(Kingdom k, int side)
	{
		List<Kingdom> kingdoms = GetKingdoms(side);
		if (kingdoms == null)
		{
			return false;
		}
		if (!kingdoms.Contains(k))
		{
			return false;
		}
		return true;
	}

	public bool IsAlly(Kingdom k1, Kingdom k2)
	{
		int side = GetSide(k1);
		if (side < 0)
		{
			return false;
		}
		int side2 = GetSide(k2);
		if (side2 < 0)
		{
			return false;
		}
		return side == side2;
	}

	public bool IsMember(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		if (attackers.Contains(k))
		{
			return true;
		}
		if (defenders.Contains(k))
		{
			return true;
		}
		return false;
	}

	public float CalcAllyScore(Kingdom k)
	{
		int side = GetSide(k);
		return GetSideScore(side);
	}

	public float CalcEnemyScore(Kingdom k)
	{
		int side = EnemySide(k);
		return GetSideScore(side);
	}

	public float CalcScoreRatio(Kingdom k)
	{
		int side = GetSide(k);
		if (side < 0)
		{
			return 0f;
		}
		int side2 = EnemySide(side);
		float num = (float)def.score_ratio_norm + GetSideScore(side);
		float num2 = (float)def.score_ratio_norm + GetSideScore(side2);
		float num3 = num + num2;
		if (num3 == 0f)
		{
			return 0.5f;
		}
		return num / num3;
	}

	public float CalcArmyStrength(int side)
	{
		return CalcArmyStrength(GetKingdoms(side));
	}

	public static float CalcArmyStrength(List<Kingdom> kingdoms)
	{
		if (kingdoms == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < kingdoms.Count; i++)
		{
			float num2 = kingdoms[i].CalcArmyStrength();
			num += num2;
		}
		return (float)Math.Floor(num);
	}

	public float CalcAlliesStrength(Kingdom k)
	{
		int side = GetSide(k);
		return CalcArmyStrength(side);
	}

	public float CalcEnemiesStrength(Kingdom k)
	{
		int side = EnemySide(k);
		return CalcArmyStrength(side);
	}

	public float CalcStrengthsRatio(Kingdom k)
	{
		int side = GetSide(k);
		if (side < 0)
		{
			return 0f;
		}
		float num = def.strength_ratio_norm;
		int side2 = EnemySide(side);
		float num2 = num + CalcArmyStrength(side);
		float num3 = num + CalcArmyStrength(side2);
		float num4 = num2 + num3;
		if (num4 == 0f)
		{
			return 0.5f;
		}
		return num2 / num4;
	}

	public bool Join(Kingdom k, int side, InvolvementReason reason = InvolvementReason.InternalPurposes, bool apply_consequences = true)
	{
		List<Kingdom> kingdoms = GetKingdoms(side);
		if (kingdoms == null)
		{
			return false;
		}
		if (base.started && IsAuthority())
		{
			SendSupporterJoinedMessages(k, side);
			Vars vars = new Vars(this);
			vars.Set("kingdom_a", k);
			vars.Set("kingdom_b", GetLeader(side));
			vars.Set("kingdom_c", GetEnemyLeader(side));
			if (IsJihad() && ((attacker.IsCaliphate() && side == 0) || (defender.IsCaliphate() && side == 1)))
			{
				game.BroadcastRadioEvent("SupporterJoinedJihadMessage", vars);
			}
			else
			{
				game.BroadcastRadioEvent("SupporterJoinedWarMessage", vars);
			}
		}
		kingdoms.Add(k);
		RelationUtils.WarFact value = new RelationUtils.WarFact(this, k, side, RelationUtils.WarFact.Action.Join);
		RelationUtils.validators.AddLast(value);
		k.AddWar(this);
		if (IsAuthority())
		{
			using (new RelationUtils.SpreadWarData(this))
			{
				for (int i = 0; i < kingdoms.Count; i++)
				{
					Kingdom kingdom = kingdoms[i];
					if (kingdom != k)
					{
						k.SetStance(kingdom, RelationUtils.Stance.Alliance, this, apply_consequences);
					}
				}
				BreakRelationsWithEnemies(k, leave_pacts: true, apply_consequences);
				BreakInheritance(k);
				k.AddRelationModifier(kingdoms[0], "rel_they_agreed_to_support_us_in_war", this);
			}
			OnWarStartedAnalytics(reason, k);
		}
		if (base.started)
		{
			SendState<SupportersState>();
		}
		k.NotifyListeners("joined_war", this);
		NotifyListeners("supporter_joined", k);
		GetLeader(side)?.NotifyListeners("war_supporter_joined", this);
		if (IsJihad())
		{
			game.religions.FireEvent("jihad_changed", null);
		}
		RelationUtils.validators.Remove(value);
		return true;
	}

	public bool Join(Kingdom k, Kingdom supported, InvolvementReason reason = InvolvementReason.InternalPurposes)
	{
		int side = GetSide(supported);
		return Join(k, side, reason);
	}

	public void ResetBattlesAfterWarList()
	{
		battles_after_war.Clear();
	}

	public bool Leave(Kingdom k, Kingdom victor, bool silent, bool apply_consequences = true, string reason = "")
	{
		int side = GetSide(k);
		List<Kingdom> kingdoms = GetKingdoms(side);
		Kingdom leader = GetLeader(side);
		if (kingdoms == null)
		{
			return false;
		}
		int num = EnemySide(side);
		Kingdom enemyLeader = GetEnemyLeader(victor);
		int side2 = GetSide(victor);
		if (!silent && IsAuthority())
		{
			SendSupporterLeftMessages(k, side);
			Vars vars = new Vars(this);
			vars.Set("kingdom_a", k);
			vars.Set("kingdom_b", GetLeader(num));
			vars.Set("kingdom_c", GetLeader(side));
			string def_id = ((side2 == num) ? "SupporterLostWarMessage" : ((side2 != side) ? "SupporterPeaceSignedMessage" : "SupporterWonWarMessage"));
			game.BroadcastRadioEvent(def_id, vars);
		}
		RelationUtils.WarFact value = new RelationUtils.WarFact(this, k, side, RelationUtils.WarFact.Action.Leave);
		RelationUtils.validators.AddLast(value);
		NotifyListeners("supporter_leaving", k);
		OnWarFinishedAnalytics(k, reason, send_to_all: false);
		kingdoms.Remove(k);
		k.RemoveWar(this);
		if (IsAuthority())
		{
			if (apply_consequences)
			{
				k.GetCrownAuthority().AddModifier("breakAlliance");
			}
			for (int i = 0; i < kingdoms.Count; i++)
			{
				Kingdom kingdom = kingdoms[i];
				if (kingdom == k)
				{
					continue;
				}
				RelationUtils.Stance warStance = k.GetWarStance(kingdom);
				RelationUtils.Stance stance = CalcStance(k, kingdom);
				if (stance == warStance)
				{
					continue;
				}
				using (new RelationUtils.SpreadWarData(this, side))
				{
					k.SetStance(kingdom, stance, this, apply_consequences);
					if (apply_consequences)
					{
						if (kingdom == leader)
						{
							k.AddRelationModifier(kingdom, "rel_left_alliance", this);
						}
						else
						{
							k.AddRelationModifier(kingdom, "rel_they_made_separate_peace", this);
						}
					}
				}
			}
			List<Kingdom> kingdoms2 = GetKingdoms(num);
			if (kingdoms2 != null)
			{
				for (int j = 0; j < kingdoms2.Count; j++)
				{
					Kingdom kingdom2 = kingdoms2[j];
					Cleanup(k, kingdom2, apply_consequences);
					if (RelationUtils.Def.truce_for_kingdoms == "none")
					{
						KingdomAndKingdomRelation.ClearTruceTime(k, kingdom2, game);
					}
					else if (RelationUtils.Def.truce_for_kingdoms != "all" && !IsLeader(kingdom2) && side2 != GetSide(kingdom2))
					{
						KingdomAndKingdomRelation.ClearTruceTime(k, kingdom2, game);
					}
				}
			}
			if (side2 == side)
			{
				ApplyVictoryEffects(k, enemyLeader);
			}
			else if (side2 == num)
			{
				ApplyDefeatEffects(k, victor);
			}
			else
			{
				ApplyDrawEffects(k);
			}
			scoresIndividual.TryGetValue(k, out var value2);
			if (value2 != null)
			{
				foreach (KeyValuePair<Kingdom, float> item in value2)
				{
					switch (side)
					{
					case 0:
						scoreAttackers -= item.Value;
						break;
					case 1:
						scoreDefenders -= item.Value;
						break;
					}
				}
			}
			scoresIndividual.Remove(k);
			if (kingdoms2 != null)
			{
				for (int l = 0; l < kingdoms2.Count; l++)
				{
					if (scoresIndividual.TryGetValue(kingdoms2[l], out value2) && value2.TryGetValue(k, out var value3))
					{
						switch (num)
						{
						case 0:
							scoreAttackers -= value3;
							break;
						case 1:
							scoreDefenders -= value3;
							break;
						}
						scoresIndividual[kingdoms2[l]].Remove(k);
					}
				}
			}
			SendState<ScoreState>();
			if (reason != "non_muslim_in_jihad")
			{
				SendBattleMessage(k, separative_peace: true);
			}
		}
		SendState<SupportersState>();
		Vars vars2 = new Vars();
		vars2.Set("war", this);
		vars2.Set("leader", GetEnemyLeader(num));
		vars2.Set("enemy_leader", GetLeader(num));
		vars2.Set("apply_consequences", apply_consequences);
		k.NotifyListeners("left_war", vars2);
		NotifyListeners("supporter_left", k);
		RelationUtils.validators.Remove(value);
		return true;
	}

	private void SendBattleMessage(Kingdom k, bool had_battles, bool separative_peace = false)
	{
		if (k == null)
		{
			Game.Log($"{this}: Trying to send a war_ended message to null kingdom", Game.LogType.Warning);
		}
		else if (victor_side == -1)
		{
			if (had_battles)
			{
				k.FireEvent("war_drawn_battles", null, k.id);
			}
			else if (GetLeader(GetSide(k)) == k || separative_peace)
			{
				k.FireEvent("war_drawn_no_battles", null, k.id);
			}
			else
			{
				k.FireEvent("war_drawn_no_battles_supporter", null, k.id);
			}
		}
		else if (GetSide(k) == victor_side)
		{
			if (had_battles)
			{
				k.FireEvent("war_won_battles", null, k.id);
			}
			else
			{
				k.FireEvent("war_won_no_battles", null, k.id);
			}
		}
		else if (had_battles)
		{
			k.FireEvent("war_lost_battles", null, k.id);
		}
		else
		{
			k.FireEvent("war_lost_no_battles", null, k.id);
		}
	}

	private void SendBattleMessage(Kingdom specific_kingdom = null, bool separative_peace = false)
	{
		for (int i = 0; i < battles_after_war.Count; i++)
		{
			(int, bool) tuple = battles_after_war[i];
			Kingdom kingdom = game.GetKingdom(tuple.Item1);
			bool item = tuple.Item2;
			if (specific_kingdom != null)
			{
				if (specific_kingdom == kingdom)
				{
					SendBattleMessage(kingdom, item, separative_peace);
					break;
				}
			}
			else
			{
				SendBattleMessage(kingdom, item, separative_peace);
			}
		}
	}

	private void SendMessageEvent(Kingdom k, string message_type, string player_role, Kingdom kingdom = null)
	{
		if (!k.is_player || (kingdom != null && kingdom.IsDefeated()))
		{
			return;
		}
		string text = message_type + "PlayerIs" + player_role + "Message";
		if (game.dt.Find(text) != null)
		{
			object param;
			if (kingdom == null)
			{
				param = this;
			}
			else
			{
				Vars vars = new Vars(this);
				vars.Set("kingdom", kingdom);
				param = vars;
			}
			Event obj = new Event(k, text, param);
			obj.send_to_kingdoms = new List<int> { k.id };
			k.FireEvent(obj);
		}
	}

	private void SendDefensivePactActivatedMessages()
	{
		SendMessageEvent(defender, "DefensivePactActivated", "TheKingdom");
		SendMessageEvent(attacker, "DefensivePactActivated", "Target");
		for (int i = 1; i < defenders.Count; i++)
		{
			Kingdom k = defenders[i];
			SendMessageEvent(k, "DefensivePactActivated", "Supporter");
		}
	}

	private void SendOffensivePactActivatedMessages()
	{
		SendMessageEvent(attacker, "OffensivePactActivated", "Leader");
		SendMessageEvent(defender, "OffensivePactActivated", "Target");
		for (int i = 1; i < attackers.Count; i++)
		{
			Kingdom k = attackers[i];
			SendMessageEvent(k, "OffensivePactActivated", "Supporter");
		}
	}

	private void SendLeadersSignedPeaceMessages()
	{
		for (int i = 0; i <= 1; i++)
		{
			List<Kingdom> kingdoms = GetKingdoms(i);
			for (int j = 1; j < kingdoms.Count; j++)
			{
				Kingdom k = kingdoms[j];
				SendMessageEvent(k, "LeadersSignedPeace", "Supporter");
			}
		}
	}

	private void SendSupporterJoinedMessages(Kingdom k, int side)
	{
		List<Kingdom> kingdoms = GetKingdoms(side);
		List<Kingdom> kingdoms2 = GetKingdoms(EnemySide(side));
		for (int i = 1; i < kingdoms.Count; i++)
		{
			Kingdom kingdom = kingdoms[i];
			if (kingdom != k)
			{
				SendMessageEvent(kingdom, "SupporterJoinedUsInWar", "Supporter", k);
			}
		}
		SendMessageEvent(kingdoms2[0], "SupporterJoinedEnemyInWar", "Leader", k);
		for (int j = 1; j < kingdoms2.Count; j++)
		{
			Kingdom k2 = kingdoms2[j];
			SendMessageEvent(k2, "SupporterJoinedEnemyInWar", "Supporter", k);
		}
	}

	private void SendSupporterLeftMessages(Kingdom k, int side)
	{
		List<Kingdom> kingdoms = GetKingdoms(side);
		List<Kingdom> kingdoms2 = GetKingdoms(EnemySide(side));
		SendMessageEvent(kingdoms[0], "SupporterLeftUsInWar", "Leader", k);
		for (int i = 1; i < kingdoms.Count; i++)
		{
			Kingdom kingdom = kingdoms[i];
			if (kingdom != k)
			{
				SendMessageEvent(kingdom, "SupporterLeftUsInWar", "Supporter", k);
			}
		}
		for (int j = 1; j < kingdoms2.Count; j++)
		{
			Kingdom k2 = kingdoms2[j];
			SendMessageEvent(k2, "SupporterLeftEnemyInWar", "Supporter", k);
		}
	}

	public static RelationUtils.Stance CalcStance(Kingdom k1, Kingdom k2)
	{
		for (int i = 0; i < k1.wars.Count; i++)
		{
			War war = k1.wars[i];
			int side = war.GetSide(k2);
			if (side >= 0)
			{
				if (war.GetSide(k1) == side)
				{
					return RelationUtils.Stance.Alliance;
				}
				return RelationUtils.Stance.War;
			}
		}
		return RelationUtils.Stance.Peace;
	}

	public static bool CanStart(Kingdom k1, Kingdom k2)
	{
		return ValidateWarAllowed(k1, k2) == "ok";
	}

	public static string ValidateWarAllowed(Kingdom k1, Kingdom k2)
	{
		if (k1 == null || k2 == null || k1.IsDefeated() || k2.IsDefeated() || k1 == k2)
		{
			return "invalid_kingdoms";
		}
		if (k1.IsEnemy(k2))
		{
			return "already_enemies";
		}
		if (k1.FindWarWith(k2) != null)
		{
			return "already_enemies";
		}
		if (k1.IsAllyInWar(k2))
		{
			return "_allies_in_war";
		}
		string text = k1.game?.rules?.ValidateWarAllowed(k1, k2);
		if (text != null && text != "ok")
		{
			return text;
		}
		return "ok";
	}

	public static bool CanStop(Kingdom k1, Kingdom k2)
	{
		War war = k1?.FindWarWith(k2);
		if (war == null)
		{
			return false;
		}
		if (war.IsLeader(k1))
		{
			return true;
		}
		if (war.IsLeader(k2))
		{
			return true;
		}
		return false;
	}

	public static bool CanJoin(Kingdom k, int side, List<Kingdom> attackers, List<Kingdom> defenders)
	{
		if (k == null || k.IsDefeated() || side < 0 || attackers == null || defenders == null)
		{
			return false;
		}
		List<Kingdom> list = ((side == 0) ? attackers : defenders);
		for (int i = 0; i < list.Count; i++)
		{
			Kingdom kingdom = list[i];
			if (kingdom == k)
			{
				return false;
			}
			if (k.FindWarWith(kingdom) != null)
			{
				return false;
			}
		}
		List<Kingdom> list2 = ((side == 0) ? defenders : attackers);
		for (int j = 0; j < list2.Count; j++)
		{
			Kingdom kingdom2 = list2[j];
			if (kingdom2 == k)
			{
				return false;
			}
			if (k.FindWarWith(kingdom2) != null)
			{
				return false;
			}
			if (k.IsAllyInWar(kingdom2))
			{
				return false;
			}
			string text = k.game?.rules?.ValidateWarAllowed(k, kingdom2);
			if (text != null && text != "ok")
			{
				return false;
			}
		}
		return true;
	}

	public bool InitialWarContainsPlayersOfTeamOnSide(int team, int side)
	{
		List<Kingdom> kingdoms = GetKingdoms(side);
		for (int i = 0; i < kingdoms.Count; i++)
		{
			Kingdom kingdom = kingdoms[i];
			int playerIndex = game.campaign.GetPlayerIndex(kingdom);
			if (playerIndex != -1 && team == CampaignUtils.GetTeam(game.campaign, playerIndex))
			{
				return true;
			}
		}
		return false;
	}

	public static bool PredictStartMembers(Kingdom attacker, Kingdom defender, out Pact defensive_pact, out Pact offensive_pact, List<Kingdom> attackers, List<Kingdom> defenders)
	{
		bool result = CanStart(attacker, defender);
		defenders.Clear();
		defenders.Add(defender);
		attackers.Clear();
		attackers.Add(attacker);
		defensive_pact = Pact.Find(Pact.Type.Defensive, defender, attacker);
		if (defensive_pact != null)
		{
			for (int i = 0; i < defensive_pact.members.Count; i++)
			{
				Kingdom kingdom = defensive_pact.members[i];
				if (CanJoin(kingdom, 1, attackers, defenders))
				{
					defenders.Add(kingdom);
				}
			}
		}
		offensive_pact = Pact.Find(Pact.Type.Offensive, attacker, defender);
		if (offensive_pact != null && attacker != offensive_pact.leader)
		{
			offensive_pact = null;
		}
		if (offensive_pact != null)
		{
			for (int j = 0; j < offensive_pact.members.Count; j++)
			{
				Kingdom kingdom2 = offensive_pact.members[j];
				if (CanJoin(kingdom2, 0, attackers, defenders))
				{
					attackers.Add(kingdom2);
				}
			}
		}
		return result;
	}

	public bool CanJoin(Kingdom k, int side)
	{
		return CanJoin(k, side, attackers, defenders);
	}

	private void LeavePactsByWar(Kingdom attacker, Kingdom defender)
	{
		for (int num = attacker.pacts.Count - 1; num >= 0; num--)
		{
			Pact pact = attacker.pacts[num];
			if (pact.type == Pact.Type.Defensive && pact.target == defender)
			{
				pact.Leave(attacker, "ByWar");
			}
			if (pact.type == Pact.Type.Offensive && pact.target == defender && pact.leader != attacker)
			{
				pact.Leave(attacker, "ByWar");
			}
			if (pact.members.Contains(defender))
			{
				if (pact.leader == attacker)
				{
					pact.Leave(defender, "ByWar");
				}
				else
				{
					pact.Leave(attacker, "ByWar");
				}
			}
		}
		for (int num2 = defender.pacts.Count - 1; num2 >= 0; num2--)
		{
			Pact pact2 = defender.pacts[num2];
			if (pact2.type == Pact.Type.Offensive && pact2.target == attacker)
			{
				pact2.Leave(defender, "ByWar");
			}
		}
	}

	private void BreakInheritance(Kingdom k)
	{
		List<Kingdom> enemies = GetEnemies(k);
		Inheritance component = k.GetComponent<Inheritance>();
		for (int i = 0; i < enemies.Count; i++)
		{
			Kingdom kingdom = enemies[i];
			Inheritance component2 = kingdom.GetComponent<Inheritance>();
			if (component.currentPrincess != null && component.currentPrincess.kingdom_id == kingdom.id)
			{
				component.HandleNextPrincess();
			}
			if (component2.currentPrincess != null && component2.currentPrincess.kingdom_id == k.id)
			{
				component2.HandleNextPrincess();
			}
		}
	}

	private void UpdatePacts()
	{
		Kingdom kingdom = attacker;
		Kingdom kingdom2 = defender;
		bool flag = false;
		using (new RelationUtils.SpreadWarData(this))
		{
			LeavePactsByWar(kingdom, kingdom2);
			Pact pact = Pact.Find(Pact.Type.Defensive, kingdom2, kingdom);
			if (pact != null)
			{
				pact.Activate(this);
				flag = true;
			}
			pact = Pact.Find(Pact.Type.Offensive, kingdom, kingdom2);
			if (pact != null)
			{
				if (pact.leader == kingdom)
				{
					flag = true;
					pact.Activate(this);
				}
				else
				{
					pact.Leave(kingdom);
				}
			}
			if (flag)
			{
				if (defenders.Count > 1)
				{
					SendDefensivePactActivatedMessages();
				}
				if (attackers.Count > 1)
				{
					SendOffensivePactActivatedMessages();
				}
			}
		}
	}

	private void UpdateSovereign()
	{
		Kingdom target = attacker;
		Kingdom kingdom = defender;
		if (kingdom.vassalage == null)
		{
			return;
		}
		int side = 1;
		if (kingdom.vassalage.def.type == Vassalage.Type.Scuttage)
		{
			if (CanJoin(kingdom.sovereignState, side))
			{
				Join(kingdom.sovereignState, side, InvolvementReason.OfferedSupport, apply_consequences: false);
			}
			for (int i = 0; i < kingdom.sovereignState.vassalStates.Count; i++)
			{
				Kingdom kingdom2 = kingdom.sovereignState.vassalStates[i];
				if (kingdom2 != kingdom && kingdom2.vassalage != null && kingdom2.vassalage.def.type == Vassalage.Type.March && CanJoin(kingdom2, side))
				{
					Join(kingdom2, side, InvolvementReason.VassalSummoned, apply_consequences: false);
				}
			}
		}
		else if (CanJoin(kingdom.sovereignState, side) && !refusedToParticipate.Contains(kingdom.sovereignState) && DemandSovereignDefendOffer.Get(game, kingdom, kingdom.sovereignState, target, this) == null)
		{
			new DemandSovereignDefendOffer(kingdom, kingdom.sovereignState, target, this).Send();
		}
	}

	private void UpdateVassals()
	{
		Kingdom kingdom = defender;
		for (int i = 0; i < kingdom.vassalStates.Count; i++)
		{
			int side = 1;
			Kingdom kingdom2 = kingdom.vassalStates[i];
			if (kingdom2 != kingdom && kingdom2.vassalage != null && kingdom2.vassalage.def.type == Vassalage.Type.March && CanJoin(kingdom2, side))
			{
				Join(kingdom2, side, InvolvementReason.VassalSummoned, apply_consequences: false);
			}
		}
	}

	protected void CancelOffers()
	{
		for (int i = 0; i < attackers.Count; i++)
		{
			for (int j = 0; j < defenders.Count; j++)
			{
				attackers[i].GetOngoingOfferWith(defenders[j])?.Cancel();
			}
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (!IsAuthority())
		{
			return;
		}
		UpdatePacts();
		UpdateSovereign();
		UpdateVassals();
		CancelOffers();
		BreakInheritance(attacker);
		InitiateHistory(attacker, defender);
		for (int i = 0; i < 2; i++)
		{
			List<Kingdom> kingdoms = GetKingdoms(i);
			for (int j = 0; j < kingdoms.Count; j++)
			{
				Kingdom kingdom = kingdoms[j];
				Stat stat = kingdom?.stats?.Find(Stats.ks_war_exhaustion);
				if (stat.all_mods == null)
				{
					continue;
				}
				for (int k = 0; k < stat.all_mods.Count; k++)
				{
					Stat.Modifier modifier = stat.all_mods[k];
					if (modifier.GetField().key == "WarExhaustionModifier")
					{
						modifier.value = modifier.CalcValue(kingdom.stats, stat);
						break;
					}
				}
			}
		}
	}

	public float GetUpkeep(Kingdom k)
	{
		return CalcJihadUpkeep(def, k);
	}

	public static float GetJihadUpkeep(Kingdom k)
	{
		if (k == null)
		{
			return 0f;
		}
		Def def = k.game.defs.Find<Def>("Jihad");
		if (def == null)
		{
			return 0f;
		}
		return CalcJihadUpkeep(def, k);
	}

	private static float CalcJihadUpkeep(Def def, Kingdom k)
	{
		if (k == null || def?.upkeep_field == null)
		{
			return 0f;
		}
		Vars vars = new Vars();
		if (k.IsCaliphate())
		{
			vars.Set("caliphate", k);
		}
		float num = def.upkeep_field.Float(vars);
		if (!k.is_player)
		{
			num *= k.game.GetDevSettingsFloat("ai_jihad_upkeep_mult", 1f);
		}
		return num;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "upkeep":
		{
			if (vars == null)
			{
				return 0;
			}
			Kingdom k = vars.GetVar("kingdom").Get<Kingdom>();
			return GetUpkeep(k);
		}
		case "type":
			return def.field.key + ".type";
		case "state":
			return state.ToString();
		case "stats":
			return new Value(stats);
		case "war_score_attackers":
			return GetSideScore(0);
		case "war_score_defeneders":
			return GetSideScore(1);
		case "strength_attackers":
			return CalcArmyStrength(0);
		case "strength_defenders":
			return CalcArmyStrength(1);
		case "attacker":
			return attacker;
		case "defender":
			return defender;
		case "attackers":
			return new Value(attackers);
		case "defenders":
			return new Value(defenders);
		case "attacker_supporters":
			return new Value(GetSupporters(0));
		case "defender_supporters":
			return new Value(GetSupporters(1));
		case "winner":
			return GetVictor();
		case "loser":
			return GetLoser();
		case "is_jihad":
			return IsJihad();
		case "jihad_owner":
			if (IsJihad())
			{
				return attacker.IsCaliphate() ? attacker : defender;
			}
			return Value.Unknown;
		case "jihad_target":
			if (IsJihad())
			{
				return attacker.IsCaliphate() ? defender : attacker;
			}
			return Value.Unknown;
		case "plr_leader":
			return GetLeader(plr_kingdom);
		case "plr_supporters":
			return new Value(GetSupporters(GetSide(plr_kingdom)));
		case "plr_allies":
			return new Value(GetAlliesExcludeSelf(plr_kingdom));
		case "plr_enemy":
			return GetEnemyLeader(plr_kingdom);
		case "plr_enemies":
			return new Value(GetEnemies(plr_kingdom));
		case "plr_enemy_supporters":
			return new Value(GetSupporters(EnemySide(plr_kingdom)));
		case "plr_side_score":
			return GetSideScore(GetSide(plr_kingdom));
		case "plr_enemies_score":
			return GetSideScore(EnemySide(plr_kingdom));
		case "plr_side_strength":
			return CalcArmyStrength(GetSide(plr_kingdom));
		case "plr_enemies_strength":
			return CalcArmyStrength(EnemySide(plr_kingdom));
		case "involvement_reason":
			return involvementReason.ToString();
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public void RecallImprisonCourtMembers(Kingdom k, Kingdom enemy)
	{
		List<Character> list = null;
		List<Character> list2 = null;
		for (int i = 0; i < k.court.Count; i++)
		{
			Character character = k.court[i];
			if (character == null || (!character.IsMerchant() && !character.IsDiplomat() && !character.IsCleric()) || (character.mission_realm == null && character.mission_kingdom == null) || (character.mission_realm?.GetKingdom() != enemy && character.mission_kingdom != enemy))
			{
				continue;
			}
			if ((float)game.Random(0, 100) < def.GetCharacterImprisonmentChance(character))
			{
				if (list == null)
				{
					list = new List<Character>();
				}
				character.Imprison(enemy);
				list.Add(character);
			}
			else
			{
				if (list2 == null)
				{
					list2 = new List<Character>();
				}
				character.Recall();
				list2.Add(character);
			}
		}
		if (list2 != null || list != null)
		{
			Vars vars = new Vars();
			if (list != null)
			{
				vars.Set("imprisoned", list);
			}
			if (list2 != null)
			{
				vars.Set("fled", list2);
			}
			vars.Set("kingdom", enemy);
			k.FireEvent("war_start_mission_knights_own", vars, k.id);
			if (list != null)
			{
				vars.Set("kingdom", k);
				enemy.FireEvent("war_start_mission_knights_oposition", vars, enemy.id);
			}
		}
	}

	public void RecallImprisonCourtMembers(Kingdom k)
	{
		List<Kingdom> enemies = GetEnemies(k);
		if (enemies == null)
		{
			return;
		}
		foreach (Kingdom item in enemies)
		{
			RecallImprisonCourtMembers(k, item);
		}
	}

	public override string ToString()
	{
		string text = "War: ";
		for (int i = 0; i < attackers.Count; i++)
		{
			Kingdom kingdom = attackers[i];
			if (i > 0)
			{
				text += ", ";
			}
			text += kingdom.Name;
		}
		text += " vs ";
		for (int j = 0; j < defenders.Count; j++)
		{
			Kingdom kingdom2 = defenders[j];
			if (j > 0)
			{
				text += ", ";
			}
			text += kingdom2.Name;
		}
		return text;
	}

	private void InitiateHistory(Kingdom k1, Kingdom k2)
	{
		AddActivity("WarStart", k1, k2);
	}

	public static War Create(Kingdom combatant1, Kingdom combatant2, InvolvementReason reason, bool apply_consequences = true)
	{
		return new War(combatant1, combatant2, reason, apply_consequences);
	}

	public Kingdom GetAttacker()
	{
		return attacker;
	}

	public Kingdom GetDefender()
	{
		return defender;
	}

	public List<Kingdom> GetDirectSupporters(Kingdom k)
	{
		List<Kingdom> list = ((!attackers.Contains(k)) ? new List<Kingdom>(defenders) : new List<Kingdom>(attackers));
		list.RemoveAll((Kingdom kingdom) => kingdom.id == k.id);
		return list;
	}

	public List<Kingdom> GetSupporters(Kingdom k)
	{
		if (GetAttacker().id == k.id)
		{
			return GetSide1Supporters();
		}
		return GetSide2Supporters();
	}

	public List<Kingdom> GetSide1Supporters()
	{
		return attacker.GetAllSupportersAgainst(defender);
	}

	public List<Kingdom> GetSide2Supporters()
	{
		return defender.GetAllSupportersAgainst(attacker);
	}

	public void AddActivity(string modName, Kingdom fromKingdom, Kingdom againstKingdom, Realm realm = null, float valueMultiplier = 1f)
	{
		if (IsAuthority() && fromKingdom != null && !string.IsNullOrEmpty(modName))
		{
			Vars vars = new Vars();
			vars.Set("kingdom", fromKingdom);
			vars.Set("kingdom2", againstKingdom);
			if (realm != null)
			{
				vars.Set("realm", realm);
			}
			AddScoreModifier(modName, fromKingdom, againstKingdom, vars, valueMultiplier);
			lastActivities[fromKingdom] = game.time;
		}
	}

	private void AddScoreModifier(string modName, Kingdom kingdom, Kingdom kingdom2, Vars vars = null, float valueMultiplier = 1f)
	{
		if (!IsAuthority())
		{
			return;
		}
		DT.Field field = game.dt.Find("WarStats." + modName);
		float num = field.GetFloat("value") * valueMultiplier * war_modifier;
		if (num != 0f)
		{
			if (scoresIndividual.ContainsKey(kingdom))
			{
				if (scoresIndividual[kingdom].ContainsKey(kingdom2))
				{
					scoresIndividual[kingdom][kingdom2] = num;
				}
				else
				{
					scoresIndividual[kingdom].Add(kingdom2, num);
				}
			}
			else
			{
				scoresIndividual.Add(kingdom, new Dictionary<Kingdom, float>(10) { { kingdom2, num } });
			}
			switch (GetSide(kingdom))
			{
			case 0:
				scoreAttackers += num;
				break;
			case 1:
				scoreDefenders += num;
				break;
			}
			SendState<ScoreState>();
		}
		if (field.FindChild("historyText") != null)
		{
			HistoryEntry item = new HistoryEntry(field, vars);
			history.Add(item);
			SendState<HistoryState>();
		}
	}

	public bool IsConcluded()
	{
		return state == State.concluded;
	}

	private void Cleanup(Kingdom k1, Kingdom k2, bool apply_consequences = true)
	{
		AssertAuthority();
		StopBattlesAtEndOfWar(k1, k2);
		Vars vars = new Vars();
		vars.Set("leader_signed_peace", !IsLeader(k1) || !IsLeader(k2));
		using (new RelationUtils.SpreadWarData(this))
		{
			k1.SetStance(k2, RelationUtils.Stance.Peace, vars, apply_consequences && !k1.IsDefeated() && !k2.IsDefeated());
		}
		k1.FreeAllRealmsOf(k2);
		k2.FreeAllRealmsOf(k1);
		k1.FreeAllKeeps(k2);
		k2.FreeAllKeeps(k1);
	}

	private void RecalcStances(List<Kingdom> kingdoms)
	{
		AssertAuthority();
		using RelationUtils.SpreadWarData spreadWarData = new RelationUtils.SpreadWarData(this);
		for (int i = 0; i < kingdoms.Count - 1; i++)
		{
			Kingdom kingdom = kingdoms[i];
			for (int j = i + 1; j < kingdoms.Count; j++)
			{
				Kingdom kingdom2 = kingdoms[j];
				RelationUtils.Stance warStance = kingdom.GetWarStance(kingdom2);
				RelationUtils.Stance stance = CalcStance(kingdom, kingdom2);
				if (stance != warStance)
				{
					spreadWarData.SetWarSide(GetSide(kingdom));
					kingdom.SetStance(kingdom2, stance, this);
				}
			}
		}
	}

	private void ApplyVictoryEffects(Kingdom victor, Kingdom loser)
	{
		victor.wars_won++;
		victor.SendState<Kingdom.PastWarsState>();
		victor.GetCrownAuthority().AddModifier("warWon");
		Vars vars = new Vars();
		vars.Set("war", this);
		vars.Set("winner", victor);
		vars.Set("loser", loser);
		victor.NotifyListeners("war_won", vars);
	}

	private void ApplyDefeatEffects(Kingdom loser, Kingdom victor)
	{
		loser.GetCrownAuthority().AddModifier("warLost");
		Vars vars = new Vars();
		vars.Set("war", this);
		vars.Set("winner", victor);
		vars.Set("loser", loser);
		loser.NotifyListeners("war_lost", vars);
		loser.wars_lost++;
		loser.SendState<Kingdom.PastWarsState>();
		for (int i = 0; i < loser.armies.Count; i++)
		{
			Army army = loser.armies[i];
			army.morale.AddTemporaryMorale(army.morale.def.morale_on_war_lost);
		}
	}

	private void ApplyDrawEffects(Kingdom k)
	{
		k.NotifyListeners("war_draw", this);
	}

	private void OnWarStartedAnalytics(InvolvementReason reason, Kingdom send_to)
	{
		if (IsAuthority() && reason != InvolvementReason.InternalPurposes && IsValid() && game.IsRunning())
		{
			SendTo(send_to);
		}
		Vars CreateVars(Kingdom k)
		{
			if (k == null)
			{
				Game.Log("Null kingdom", Game.LogType.Warning);
				return null;
			}
			Vars vars = new Vars();
			if (def?.field?.key == null)
			{
				Game.Log("Null def for war", Game.LogType.Warning);
				return vars;
			}
			vars.Set("warType", def.id);
			int side = GetSide(k);
			if (side == -1)
			{
				Game.Log($"{k} has no side in war", Game.LogType.Warning);
				return vars;
			}
			Kingdom leader = GetLeader(0);
			if (leader == null || leader.Name == null)
			{
				Game.Log("No attackers leader in war!", Game.LogType.Warning);
				return vars;
			}
			vars.Set("originatingKingdom", leader);
			Kingdom enemyLeader = GetEnemyLeader(side);
			if (enemyLeader == null || enemyLeader.Name == null)
			{
				Game.Log("No defenders leader in war!", Game.LogType.Warning);
				return vars;
			}
			vars.Set("targetKingdom", enemyLeader.Name);
			vars.Set("warCause", reason.ToString());
			List<Kingdom> kingdoms = GetKingdoms(side);
			if (kingdoms == null)
			{
				Game.Log($"War contains no kingdoms on side {side}", Game.LogType.Warning);
				return vars;
			}
			vars.Set("totalAlliedKingdoms", kingdoms.Count);
			List<Kingdom> kingdoms2 = GetKingdoms(GetSide(enemyLeader));
			if (kingdoms2 == null)
			{
				Game.Log($"War contains no kingdoms on side {GetSide(enemyLeader)}", Game.LogType.Warning);
				return vars;
			}
			vars.Set("totalOpposingKingdoms", kingdoms2.Count);
			return vars;
		}
		void SendTo(Kingdom k)
		{
			if (k == null)
			{
				Game.Log("Trying to send OnWarFinishedAnalytics event to null kingdom!", Game.LogType.Warning);
			}
			if (k.is_player)
			{
				Vars param = CreateVars(k);
				k.FireEvent("analytics_war_started", param, k.id);
			}
		}
	}

	private void OnWarFinishedAnalytics(Kingdom concluded_by, string reason, bool send_to_all = true)
	{
		if (IsAuthority() && concluded_by?.Name != null && reason != null && IsValid() && game.IsRunning())
		{
			if (send_to_all)
			{
				SendToAll(attackers);
				SendToAll(defenders);
			}
			else
			{
				SendTo(concluded_by);
			}
		}
		Vars CreateVars(Kingdom k)
		{
			if (k == null)
			{
				Game.Log("Null kingdom", Game.LogType.Warning);
				return null;
			}
			Vars vars = new Vars();
			if (def?.field?.key == null)
			{
				Game.Log("Null def for war", Game.LogType.Warning);
				return vars;
			}
			vars.Set("warType", def.id);
			int side = GetSide(k);
			if (side == -1)
			{
				Game.Log($"{k} has no side in war", Game.LogType.Warning);
				return vars;
			}
			Kingdom enemyLeader = GetEnemyLeader(side);
			if (enemyLeader == null || enemyLeader.Name == null)
			{
				Game.Log("No enemy leader in war!", Game.LogType.Warning);
				return vars;
			}
			vars.Set("targetKingdom", enemyLeader.Name);
			vars.Set("peaceType", reason);
			vars.Set("peaceRequester", concluded_by.Name);
			int val = (int)GetScoreOf(k);
			vars.Set("playerWarScore", val);
			int val2 = (int)GetSideScore(side);
			vars.Set("totalAlliedWarScore", val2);
			int val3 = (int)GetSideScore(enemyLeader);
			vars.Set("totalOpposingWarScore", val3);
			List<Kingdom> kingdoms = GetKingdoms(side);
			if (kingdoms == null)
			{
				Game.Log($"War contains no kingdoms on side {side}", Game.LogType.Warning);
				return vars;
			}
			vars.Set("totalAlliedKingdoms", kingdoms.Count);
			List<Kingdom> kingdoms2 = GetKingdoms(GetSide(enemyLeader));
			if (kingdoms2 == null)
			{
				Game.Log($"War contains no kingdoms on side {GetSide(enemyLeader)}", Game.LogType.Warning);
				return vars;
			}
			vars.Set("totalOpposingKingdoms", kingdoms2.Count);
			return vars;
		}
		void SendTo(Kingdom k)
		{
			if (k == null)
			{
				Game.Log("Trying to send OnWarFinishedAnalytics event to null kingdom!", Game.LogType.Warning);
			}
			if (k.is_player)
			{
				Vars param = CreateVars(k);
				k.FireEvent("analytics_war_ended", param, k.id);
			}
		}
		void SendToAll(List<Kingdom> kingdoms)
		{
			if (kingdoms == null)
			{
				Game.Log("Trying to send OnWarFinishedAnalytics event to null kingdoms!", Game.LogType.Warning);
			}
			for (int i = 0; i < kingdoms.Count; i++)
			{
				SendTo(kingdoms[i]);
			}
		}
	}

	public void Conclude(int victor_side, string reason = null, bool silent = false, Kingdom concluded_by = null)
	{
		ResetBattlesAfterWarList();
		RelationUtils.WarFact value = new RelationUtils.WarFact(this, concluded_by, victor_side, RelationUtils.WarFact.Action.Conclude);
		RelationUtils.validators.AddLast(value);
		state = State.concluded;
		this.victor_side = victor_side;
		if (!silent && IsAuthority())
		{
			Vars vars = new Vars(this);
			string def_id;
			if (victor_side < 0)
			{
				SendLeadersSignedPeaceMessages();
				def_id = "PeaceSignedMessage";
				vars.Set("kingdom_a", attacker);
				vars.Set("kingdom_b", defender);
			}
			else
			{
				Kingdom leader = GetLeader(victor_side);
				Kingdom enemyLeader = GetEnemyLeader(victor_side);
				def_id = "LostWarMessage";
				vars.Set("kingdom_a", enemyLeader);
				vars.Set("kingdom_b", leader);
			}
			game.BroadcastRadioEvent(def_id, vars);
		}
		RemoveLogicConnections();
		if (IsAuthority())
		{
			OnWarFinishedAnalytics(concluded_by, reason);
			RecalcStances(attackers);
			RecalcStances(defenders);
			for (int i = 0; i < attackers.Count; i++)
			{
				Kingdom kingdom = attackers[i];
				for (int j = 0; j < defenders.Count; j++)
				{
					Kingdom kingdom2 = defenders[j];
					Cleanup(kingdom, kingdom2);
					if (RelationUtils.Def.truce_for_kingdoms == "none")
					{
						KingdomAndKingdomRelation.ClearTruceTime(kingdom, kingdom2, game);
					}
					else if (RelationUtils.Def.truce_for_kingdoms != "all" && ((!IsLeader(kingdom) && victor_side != GetSide(kingdom)) || (!IsLeader(kingdom2) && victor_side != GetSide(kingdom2))))
					{
						KingdomAndKingdomRelation.ClearTruceTime(kingdom, kingdom2, game);
					}
				}
			}
			for (int k = 0; k < 2; k++)
			{
				List<Kingdom> kingdoms = GetKingdoms(k);
				for (int l = 0; l < kingdoms.Count; l++)
				{
					Kingdom kingdom3 = kingdoms[l];
					for (int m = 0; m < kingdom3.mercenaries.Count; m++)
					{
						Army army = kingdom3.mercenaries[m];
						if (army.mercenary.mission_def.can_attack_war_kingdoms)
						{
							army.mercenary.Think();
						}
					}
				}
			}
			int side = EnemySide(victor_side);
			List<Kingdom> kingdoms2 = GetKingdoms(victor_side);
			List<Kingdom> kingdoms3 = GetKingdoms(side);
			SendState<ConcludeState>();
			if (kingdoms2 != null || kingdoms3 != null)
			{
				if (kingdoms2 != null)
				{
					for (int n = 0; n < kingdoms2.Count; n++)
					{
						Kingdom victor = kingdoms2[n];
						ApplyVictoryEffects(victor, GetLeader(side));
					}
				}
				if (kingdoms3 != null)
				{
					for (int num = 0; num < kingdoms3.Count; num++)
					{
						Kingdom loser = kingdoms3[num];
						ApplyDefeatEffects(loser, GetLeader(victor_side));
					}
				}
			}
			else
			{
				for (int num2 = 0; num2 < attackers.Count; num2++)
				{
					Kingdom k2 = attackers[num2];
					ApplyDrawEffects(k2);
				}
				for (int num3 = 0; num3 < defenders.Count; num3++)
				{
					Kingdom k3 = defenders[num3];
					ApplyDrawEffects(k3);
				}
			}
			SendBattleMessage();
		}
		for (int num4 = 0; num4 < attackers.Count; num4++)
		{
			attackers[num4].NotifyListeners("war_ended", this);
		}
		for (int num5 = 0; num5 < defenders.Count; num5++)
		{
			defenders[num5].NotifyListeners("war_ended", this);
		}
		NotifyListeners("war_concluded", reason);
		RelationUtils.validators.Remove(value);
		if (IsAuthority())
		{
			Destroy();
		}
	}

	public Kingdom GetVictor()
	{
		return GetLeader(victor_side);
	}

	public Kingdom GetLoser()
	{
		return GetLeader(EnemySide(victor_side));
	}

	public List<Realm> GetLostRealms(Kingdom k)
	{
		return null;
	}

	public Dictionary<string, Bonus> GetBonuses(Kingdom k)
	{
		if (attackers.Contains(k))
		{
			return attackersBonuses;
		}
		if (defenders.Contains(k))
		{
			return defendersBonuses;
		}
		return null;
	}

	public void RemoveLogicConnections()
	{
		for (int i = 0; i < attackers.Count; i++)
		{
			attackers[i].RemoveWar(this);
		}
		for (int j = 0; j < defenders.Count; j++)
		{
			defenders[j].RemoveWar(this);
		}
	}

	public int GetHistoryLineCount()
	{
		return history.Count;
	}

	public List<HistoryEntry> GetHistory()
	{
		return history;
	}

	public float GetSideScore(Kingdom k)
	{
		return GetSideScore(GetSide(k));
	}

	public float GetSideScore(int side)
	{
		if (side == 0)
		{
			return scoreAttackers;
		}
		return scoreDefenders;
	}

	public float GetScore(Kingdom from, Kingdom to)
	{
		if (scoresIndividual.TryGetValue(from, out var value) && value.TryGetValue(to, out var value2))
		{
			return value2;
		}
		return 0f;
	}

	public float GetScoreOf(Kingdom kingdom)
	{
		scoresIndividual.TryGetValue(kingdom, out var value);
		if (value == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (KeyValuePair<Kingdom, float> item in value)
		{
			num += item.Value;
		}
		return num;
	}

	public float GetScoreAgainst(Kingdom kingdom)
	{
		float num = 0f;
		foreach (KeyValuePair<Kingdom, Dictionary<Kingdom, float>> item in scoresIndividual)
		{
			if (item.Value.TryGetValue(kingdom, out var value))
			{
				num += value;
			}
		}
		return num;
	}

	public float CalcWarConfidence(Kingdom k)
	{
		float num = CalcScoreRatio(k);
		float num2 = CalcStrengthsRatio(k);
		num = num * 2f - 1f;
		num2 = num2 * 2f - 1f;
		float strength_confidence_factor = def.strength_confidence_factor;
		float confidence_fade_time = def.confidence_fade_time;
		float confidence_fade_power = def.confidence_fade_power;
		Kingdom enemyLeader = GetEnemyLeader(k);
		if (enemyLeader == null)
		{
			return 0f;
		}
		float num3 = 0f;
		if (Math.Sign(num) == Math.Sign(num2))
		{
			num3 = num + num2 * strength_confidence_factor;
			Game.clamp(num3, -1f, 1f);
		}
		else
		{
			num3 = strength_confidence_factor * num2 + (1f - strength_confidence_factor) * num;
		}
		Time time = game.time;
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(k, enemyLeader);
		float num4 = Game.map_clamp(time - kingdomAndKingdomRelation.war_time, 0f, confidence_fade_time, 0f, 1f);
		num4 = (float)Math.Pow(num4, confidence_fade_power);
		return num3 * num4;
	}

	public float CalcVictoryExpectation(Kingdom k)
	{
		float num = CalcWarConfidence(k);
		if (num > 0f)
		{
			return num;
		}
		return 0f;
	}

	public float CalcDefeatExpectation(Kingdom k)
	{
		float num = CalcWarConfidence(k);
		if (num < 0f)
		{
			return -1f * num;
		}
		return 0f;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		if (attackers != null && attackers.Count > 0)
		{
			dump.OpenSection("attackers");
			for (int i = 0; i < attackers.Count; i++)
			{
				dump.Append(attackers[i]?.Name);
			}
			dump.CloseSection("attackers");
		}
		if (defenders != null && defenders.Count > 0)
		{
			dump.OpenSection("defenders");
			for (int j = 0; j < defenders.Count; j++)
			{
				dump.Append(defenders[j]?.Name);
			}
			dump.CloseSection("defenders");
		}
		dump.Append("score_attakcers", scoreAttackers);
		dump.Append("score_defenders", scoreDefenders);
		if (scoresIndividual != null && scoresIndividual.Count > 0)
		{
			dump.OpenSection("scores_individual");
			foreach (KeyValuePair<Kingdom, Dictionary<Kingdom, float>> item in scoresIndividual)
			{
				if (item.Key == null || item.Value == null)
				{
					continue;
				}
				dump.OpenSection(item.Key.Name);
				foreach (KeyValuePair<Kingdom, float> item2 in item.Value)
				{
					dump.Append("vs_" + item2.Key?.Name, item2.Value);
				}
				dump.CloseSection(item.Key.Name);
			}
			dump.CloseSection("scores_individual");
		}
		if (attackersBonuses != null && attackersBonuses.Count > 0)
		{
			dump.OpenSection("attackers_bonuses");
			foreach (KeyValuePair<string, Bonus> attackersBonuse in attackersBonuses)
			{
				dump.Append(attackersBonuse.Key, attackersBonuse.Value.ToString());
			}
			dump.CloseSection("attackers_bonuses");
		}
		if (defendersBonuses == null || defendersBonuses.Count <= 0)
		{
			return;
		}
		dump.OpenSection("defenders_bonuses");
		foreach (KeyValuePair<string, Bonus> defendersBonuse in defendersBonuses)
		{
			dump.Append(defendersBonuse.Key, defendersBonuse.Value.ToString());
		}
		dump.CloseSection("defenders_bonuses");
	}

	public Vars CreateWarBonusConditionVars(Kingdom k)
	{
		Vars vars = new Vars(k);
		vars.Set("war", this);
		return vars;
	}

	public static float GetBonus(Kingdom ksrc, Kingdom ktgt, string bonus_name)
	{
		War war = ksrc?.FindWarWith(ktgt);
		if (war == null)
		{
			return 0f;
		}
		Dictionary<string, Bonus> bonuses = war.GetBonuses(ksrc);
		if (bonuses == null)
		{
			return 0f;
		}
		if (!bonuses.TryGetValue(bonus_name, out var value))
		{
			return 0f;
		}
		Vars vars = war.CreateWarBonusConditionVars(ksrc);
		if (!value.condition.Value(vars))
		{
			return 0f;
		}
		return value.value;
	}

	public static bool GetBonusField(Kingdom ksrc, Kingdom ktgt, string bonus_name, out Bonus bonus)
	{
		bonus = default(Bonus);
		War war = ksrc?.FindWarWith(ktgt);
		if (war == null)
		{
			return false;
		}
		Dictionary<string, Bonus> bonuses = war.GetBonuses(ksrc);
		if (bonuses == null)
		{
			return false;
		}
		if (!bonuses.TryGetValue(bonus_name, out bonus))
		{
			return false;
		}
		Vars vars = war.CreateWarBonusConditionVars(ksrc);
		if (!bonus.condition.Value(vars))
		{
			return false;
		}
		return true;
	}

	public static void StartJihad(Kingdom caliphate, Kingdom targetK)
	{
		if (!caliphate.IsCaliphate() || targetK.is_muslim || caliphate.game.religions.jihad_kingdoms.Count > 0 || !caliphate.IsEnemy(targetK))
		{
			return;
		}
		Game game = caliphate.game;
		War war = caliphate.FindWarWith(targetK);
		if (war == null || !war.IsLeader(caliphate) || !war.IsLeader(targetK))
		{
			return;
		}
		war.SetType("Jihad");
		List<Kingdom> alliesExcludeSelf = war.GetAlliesExcludeSelf(caliphate);
		Kingdom enemyLeader = war.GetEnemyLeader(caliphate);
		war.ResetBattlesAfterWarList();
		using (new RelationUtils.SpreadWarData(war))
		{
			for (int num = alliesExcludeSelf.Count - 1; num >= 0; num--)
			{
				Kingdom kingdom = alliesExcludeSelf[num];
				if (!kingdom.is_muslim)
				{
					Vars vars = new Vars();
					vars.Set("left_kingdom", kingdom);
					vars.Set("jihad_target_kingdom", targetK);
					vars.Set("caliphate", caliphate);
					caliphate.FireEvent("new_jihad_lost_ally", vars, caliphate.id);
					kingdom.FireEvent("new_jihad_left_war", vars, kingdom.id);
					war.Leave(kingdom, null, silent: true, apply_consequences: false, "non_muslim_in_jihad");
					kingdom.StartWarWith(enemyLeader, InvolvementReason.Jihad, null, null, apply_consequences: false);
				}
			}
			caliphate.AddRelationModifier(targetK, "rel_new_jihad_with_target", null);
			for (int i = 0; i < game.kingdoms.Count; i++)
			{
				Kingdom kingdom2 = game.kingdoms[i];
				if (kingdom2.IsDefeated() || kingdom2 == caliphate || kingdom2 == targetK)
				{
					continue;
				}
				if (kingdom2.IsCaliphate() && kingdom2.jihad_target != null)
				{
					caliphate.AddRelationModifier(kingdom2, "rel_new_jihad_with_other_jihad_leaders", null, caliphate.game.religions.jihad_kingdoms.Count);
				}
				else if (kingdom2.is_muslim)
				{
					if (kingdom2.IsEnemy(targetK))
					{
						caliphate.AddRelationModifier(kingdom2, "rel_new_jihad_with_muslim_target_enemies", null);
					}
					else
					{
						caliphate.AddRelationModifier(kingdom2, "rel_new_jihad_with_other_muslim", null);
					}
				}
				else if (targetK.is_christian && kingdom2.is_christian)
				{
					caliphate.AddRelationModifier(kingdom2, "rel_new_jihad_with_other_christian", null);
				}
			}
		}
		caliphate.jihad_target = targetK;
		caliphate.jihad = war;
		caliphate.SendState<Kingdom.ReligionState>();
		targetK.jihad_attacker = caliphate;
		targetK.jihad = war;
		targetK.SendState<Kingdom.ReligionState>();
		caliphate.game.religions.jihad_kingdoms.Add(caliphate);
		caliphate.game.religions.jihad_targets.Add(targetK);
		caliphate.game.religions.SendState<Religions.JihadsState>();
		caliphate.FireEvent("new_jihad", targetK);
		game.religions.FireEvent("new_jihad", null);
	}

	public static void EndJihad(Kingdom caliphate, string reason, bool end_war = true)
	{
		Kingdom jihad_target = caliphate.jihad_target;
		War war = caliphate?.jihad;
		Game game = caliphate?.game;
		if (caliphate == null || !caliphate.IsCaliphate() || war == null || !war.IsJihad())
		{
			return;
		}
		if (end_war)
		{
			caliphate.EndWarWith(jihad_target, jihad_target, reason);
			return;
		}
		war.SetType(game.defs.GetBase<Def>());
		caliphate.jihad_target = null;
		caliphate.jihad = null;
		caliphate.SendState<Kingdom.ReligionState>();
		jihad_target.jihad_attacker = null;
		jihad_target.jihad = null;
		jihad_target.SendState<Kingdom.ReligionState>();
		caliphate.game.religions.jihad_kingdoms.Remove(caliphate);
		caliphate.game.religions.jihad_targets.Remove(jihad_target);
		caliphate.game.religions.SendState<Religions.JihadsState>();
		using (new RelationUtils.SpreadWarData(war))
		{
			if (reason != "caliph_dead" && war.victor_side != war.GetSide(caliphate))
			{
				caliphate.GetCrownAuthority().AddModifier("end_jihad");
				for (int i = 0; i < jihad_target.wars.Count; i++)
				{
					List<Kingdom> enemies = jihad_target.wars[i].GetEnemies(jihad_target);
					for (int j = 0; j < enemies.Count; j++)
					{
						Kingdom kingdom = enemies[j];
						if (kingdom.is_muslim)
						{
							caliphate.AddRelationModifier(kingdom, "rel_with_muslim_end_jihad", null);
						}
					}
				}
			}
		}
		Vars vars = new Vars();
		vars.Set("reason", reason);
		vars.Set("ktgt", jihad_target);
		caliphate.FireEvent("end_jihad", vars);
		game.religions.FireEvent("end_jihad", null);
	}
}
