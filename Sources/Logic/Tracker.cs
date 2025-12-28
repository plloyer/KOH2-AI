using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Logic.ExtensionMethods;

namespace Logic;

public class ProsAndCons : IVars
{
	public class Factor : IVars
	{
		public class Def
		{
			public string section;

			public DT.Field field;

			public Def based_on;

			public CalcFunc calc_func;

			public ProsAndCons.Def pc_ref;

			public DT.Field our_kingdom_field;

			public DT.Field their_kingdom_field;

			public DT.Field max_value;

			public bool Load(string section, DT.Field field, ProsAndCons.Def root_def)
			{
				this.section = section;
				this.field = field;
				based_on = root_def?.FindFactor(field.key);
				if (based_on != null)
				{
					field.based_on = based_on.field;
					calc_func = based_on.calc_func;
				}
				if (calc_func == null)
				{
					calc_func = FindCalcFunc(field.key);
				}
				max_value = field.FindChild("max");
				return true;
			}

			public bool Validate(Game game)
			{
				pc_ref = game.defs.Find<ProsAndCons.Def>(field.key);
				if (section == "add_pros_and_cons" && pc_ref == null)
				{
					Game.Log(field.Path(include_file: true) + ": Unknown ProsAndCons def", Game.LogType.Error);
					return false;
				}
				our_kingdom_field = field.FindChild("our_kingdom");
				their_kingdom_field = field.FindChild("their_kingdom");
				if (!ValidateValue())
				{
					return false;
				}
				return true;
			}

			public bool ValidateBase()
			{
				if (based_on == null)
				{
					Game.Log(field.Path(include_file: true) + ": Unknown factor (not defined in ProsAndCons.factors)", Game.LogType.Error);
					return false;
				}
				return true;
			}

			public bool ValidateValue()
			{
				if (pc_ref != null && section == "add_pros_and_cons")
				{
					switch (field.NumValues())
					{
					case 0:
						return ValidateRefThresholds();
					default:
						Game.Log(field.Path(include_file: true) + ": Invalid ProsAndCons factor value: must be two expression values", Game.LogType.Error);
						return false;
					case 2:
					{
						for (int i = 0; i <= 1; i++)
						{
							if (!(field.Value(0, null, calc_expression: false).obj_val is Expression))
							{
								Game.Log(field.Path(include_file: true) + ": Invalid ProsAndCons factor value " + i + ": must be expression value", Game.LogType.Error);
								return false;
							}
						}
						return true;
					}
					}
				}
				Value val = field.Value(null, calc_expression: false);
				return ValidateValue(val);
			}

			public bool ValidateRefThresholds()
			{
				DT.Field field = this.field.FindChild("above");
				DT.Field field2 = this.field.FindChild("below");
				if (field == null && field2 == null)
				{
					Game.Log(this.field.Path(include_file: true) + ": No 'above' or 'below' parameters found", Game.LogType.Error);
					return false;
				}
				if (field != null && !ValidateRefThreshold(field))
				{
					return false;
				}
				if (field2 != null && !ValidateRefThreshold(field2))
				{
					return false;
				}
				return true;
			}

			public bool ValidateRefThreshold(DT.Field tf)
			{
				if (tf.NumValues() != 3)
				{
					Game.Log(tf.Path(include_file: true) + ": Must have 3 parameters: [ threshold, PP, CP ]", Game.LogType.Error);
					return false;
				}
				Value value = tf.Value(0, null, calc_expression: false);
				if (value.type == Value.Type.String)
				{
					string text = value.String();
					if (!pc_ref.ResolveThreshold(text, out var _, null).is_valid)
					{
						Game.Log(tf.Path(include_file: true) + ": " + pc_ref.field.key + " has no '" + text + "' threshold", Game.LogType.Error);
						return false;
					}
				}
				else if (value.type != Value.Type.Int && value.type != Value.Type.Float && !(value.obj_val is Expression))
				{
					Game.Log(tf.Path(include_file: true) + ": Value 0 must be a string or number or expression", Game.LogType.Error);
					return false;
				}
				Value value2 = tf.Value(1, null, calc_expression: false);
				if (value2.type != Value.Type.Int && !(value2.obj_val is Expression))
				{
					Game.Log(tf.Path(include_file: true) + ": Value 1 must be int or expression", Game.LogType.Error);
					return false;
				}
				Value value3 = tf.Value(2, null, calc_expression: false);
				if (value3.type != Value.Type.Int && !(value3.obj_val is Expression))
				{
					Game.Log(tf.Path(include_file: true) + ": Value 2 must be int or expression", Game.LogType.Error);
					return false;
				}
				return true;
			}

			public bool ValidateValue(Value val)
			{
				if (val.obj_val is Expression)
				{
					return true;
				}
				if (calc_func == null && pc_ref == null)
				{
					Game.Log(field.Path(include_file: true) + ": Invalid ProsAndCons factor value: not an expression and no C# calculator", Game.LogType.Error);
					return false;
				}
				if (!val.is_valid && pc_ref == null)
				{
					return true;
				}
				if (val.type == Value.Type.Int && val.int_val < 0)
				{
					Game.Log(field.Path(include_file: true) + ": Invalid ProsAndCons factor value: negative numbers are not allowed", Game.LogType.Error);
					return false;
				}
				if (val.type != Value.Type.Int)
				{
					if (pc_ref == null)
					{
						Game.Log(field.Path(include_file: true) + ": Invalid ProsAndCons factor value: must be empty or int or expression", Game.LogType.Error);
					}
					else
					{
						Game.Log(field.Path(include_file: true) + ": Invalid ProsAndCons factor value: must be int or expression", Game.LogType.Error);
					}
					return false;
				}
				return true;
			}
		}

		public Def def;

		public int value;

		public float cs_value;

		public int PP;

		public int CP;

		public Factor(Def def)
		{
			this.def = def;
		}

		public void Calc(ProsAndCons pc)
		{
			ProsAndCons prosAndCons = null;
			this.value = 0;
			PP = 0;
			CP = 0;
			pc.cur_factor = this;
			try
			{
				if (def.calc_func != null)
				{
					cs_value = def.calc_func(pc, this);
				}
				else if (def.pc_ref != null)
				{
					prosAndCons = Get(pc.game, def.pc_ref);
					prosAndCons.CopyParams(pc);
					if (def.our_kingdom_field != null || def.their_kingdom_field != null)
					{
						Kingdom kingdom = null;
						if (def.our_kingdom_field != null)
						{
							kingdom = def.our_kingdom_field.Value(pc).Get<Kingdom>();
						}
						if (kingdom == null)
						{
							kingdom = prosAndCons.our_kingdom;
						}
						Kingdom kingdom2 = null;
						if (def.their_kingdom_field != null)
						{
							kingdom2 = def.their_kingdom_field.Value(pc).Get<Kingdom>();
						}
						if (kingdom2 == null)
						{
							kingdom2 = prosAndCons.their_kingdom;
						}
						prosAndCons.SetKingdoms(kingdom, kingdom2);
					}
					cs_value = prosAndCons.Calc(pc.forced_recalc);
				}
				else
				{
					cs_value = def.field.Float(pc);
				}
				if (prosAndCons != null)
				{
					if (def.section == "add_pros_and_cons")
					{
						if (def.field.NumValues() == 2)
						{
							PP = prosAndCons.PP;
							CP = prosAndCons.CP;
							PP = def.field.Int(0, pc);
							CP = def.field.Int(1, pc);
							PP = ClampValue(pc, PP);
							CP = ClampValue(pc, CP);
						}
						else
						{
							DT.Field tf;
							float threshold = RefThresholdValue("above", pc, 0f, out tf);
							CalcRefPoints(pc, prosAndCons, tf, threshold, above: true, ref PP, ref CP);
							DT.Field tf2;
							float threshold2 = RefThresholdValue("below", pc, 2f, out tf2);
							CalcRefPoints(pc, prosAndCons, tf2, threshold2, above: false, ref PP, ref CP);
							PP = ClampValue(pc, PP);
							CP = ClampValue(pc, CP);
						}
						return;
					}
					Value value = def.field.Value(pc);
					if (value.is_number)
					{
						DT.Field tf3;
						float num = RefThresholdValue("above", pc, 0f, out tf3);
						float num2 = RefThresholdValue("below", pc, 2f, out tf3);
						this.value = ((cs_value >= num && cs_value <= num2) ? value.Int() : 0);
					}
					else
					{
						this.value = def.field.Int(pc);
					}
				}
				else
				{
					Value value2 = def.field.Value(pc);
					if (value2.is_number)
					{
						this.value = ((cs_value > 0f) ? value2.Int() : 0);
					}
					else
					{
						this.value = (int)cs_value;
					}
				}
			}
			catch (Exception arg)
			{
				Game.Log($"Error calculating {this}:\n {arg}", Game.LogType.Error);
			}
			this.value = ClampValue(pc, this.value);
		}

		public int ClampValue(ProsAndCons pc, int value)
		{
			if (value < 0)
			{
				return 0;
			}
			int num = int.MaxValue;
			if (def.max_value != null)
			{
				num = def.max_value.Int(pc, int.MaxValue);
			}
			if (value > num)
			{
				return num;
			}
			return value;
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return key switch
			{
				"value" => value, 
				"cs_value" => cs_value, 
				"PP" => PP, 
				"CP" => CP, 
				_ => Value.Unknown, 
			};
		}

		public override string ToString()
		{
			string text = def.field.key;
			string text2 = def.field.ValueStr();
			if (!string.IsNullOrEmpty(text2))
			{
				text = text + " = " + text2;
			}
			text += " -> ";
			if (def.section == "add_pros_and_cons")
			{
				text = text + PP + "PP, " + CP + "CP (" + cs_value + ")";
			}
			else
			{
				text += value;
				if ((float)value != cs_value)
				{
					text = text + " (" + cs_value + ")";
				}
			}
			return text;
		}

		public float RefThresholdValue(string key, IVars vars, float def_val, out DT.Field tf)
		{
			tf = def.field.FindChild(key);
			if (tf == null)
			{
				return def_val;
			}
			Value value = tf.Value(0, vars, calc_expression: false);
			bool above;
			if (value.type == Value.Type.String)
			{
				return def.pc_ref.GetThreshold(value.String(), out above, vars);
			}
			return tf.Float(0, vars, def_val);
		}

		public void CalcRefPoints(ProsAndCons pc, ProsAndCons pc_ref, DT.Field tf, float threshold, bool above, ref int PP, ref int CP)
		{
			if (tf == null)
			{
				return;
			}
			if (tf.NumValues() == 3)
			{
				if ((!above || !(cs_value < threshold)) && (above || !(cs_value > threshold)))
				{
					int num = tf.Int(1, pc);
					int num2 = tf.Int(2, pc);
					if (num > 0)
					{
						PP += num;
					}
					if (num2 > 0)
					{
						CP += num2;
					}
				}
			}
			else
			{
				Game.Log(tf.Path(include_file: true) + ": invalid threshold value", Game.LogType.Warning);
			}
		}
	}

	public class Def : Logic.Def
	{
		public List<Factor.Def> factors;

		public List<Factor.Def> pros;

		public List<Factor.Def> cons;

		public List<Factor.Def> adds;

		public DT.Field thresholds_field;

		public DT.Field PP_base;

		public DT.Field CP_base;

		public DT.Field base_PP;

		public DT.Field base_CP;

		public DT.Field scale_PP;

		public DT.Field scale_CP;

		public DT.Field pp_rel_scale;

		public DT.Field cp_rel_scale;

		public DT.Field pp_faction_scale;

		public DT.Field cp_faction_scale;

		public DT.Field cost;

		public DT.Field max_consider_treshold_delta;

		public ProsAndCons instance;

		public override bool Load(Game game)
		{
			instance = null;
			Def def = game.defs.GetBase<Def>();
			if (def == this)
			{
				factors = LoadFactors("factors", null);
			}
			else
			{
				factors = def.factors;
				pros = LoadFactors("pros", def);
				cons = LoadFactors("cons", def);
				adds = LoadFactors("add_pros_and_cons", def);
			}
			thresholds_field = base.field.FindChild("thresholds");
			PP_base = base.field.FindChild("pros");
			CP_base = base.field.FindChild("cons");
			base_PP = base.field.FindChild("base.PP");
			base_CP = base.field.FindChild("base.CP");
			scale_PP = base.field.FindChild("scale.PP");
			scale_CP = base.field.FindChild("scale.CP");
			LoadRelScale();
			LoadFactionScale();
			cost = base.field.FindChild("cost");
			max_consider_treshold_delta = base.field.FindChild("max_consider_treshold_delta");
			return true;
		}

		public override bool Validate(Game game)
		{
			bool flag = true;
			if (IsBase())
			{
				flag &= ValidateFactors(game, factors);
			}
			flag &= ValidateFactors(game, pros);
			flag &= ValidateFactors(game, cons);
			flag &= ValidateFactors(game, adds);
			return flag & ValidateThresholds();
		}

		public float GetRelScale(float relationship, DT.Field scale, ProsAndCons pc)
		{
			if (scale == null)
			{
				return 1f;
			}
			float num;
			float num2;
			float num3;
			float num4;
			if (relationship < 0f)
			{
				num = RelationUtils.Def.minRelationship;
				num2 = 0f;
				if (scale.NumValues() == 2)
				{
					num3 = scale.Float(0, pc, 1f);
					num4 = 1f;
				}
				else if (scale.NumValues() == 3)
				{
					num3 = scale.Float(0, pc, 1f);
					num4 = scale.Float(1, pc, 1f);
				}
				else
				{
					num3 = (num4 = 1f);
				}
			}
			else
			{
				num = 0f;
				num2 = RelationUtils.Def.maxRelationship;
				if (scale.NumValues() == 2)
				{
					num3 = 1f;
					num4 = scale.Float(1, pc, 1f);
				}
				else if (scale.NumValues() == 3)
				{
					num3 = scale.Float(1, pc, 1f);
					num4 = scale.Float(2, pc, 1f);
				}
				else
				{
					num3 = (num4 = 1f);
				}
			}
			return num3 + (relationship - num) * (num4 - num3) / (num2 - num);
		}

		public float PPRelScale(float relationship, ProsAndCons pc)
		{
			return GetRelScale(relationship, pp_rel_scale, pc);
		}

		public float CPRelScale(float relationship, ProsAndCons pc)
		{
			return GetRelScale(relationship, cp_rel_scale, pc);
		}

		private void LoadRelScale()
		{
			DT.Field field = base.field.FindChild("relationship_scale");
			if (field != null)
			{
				pp_rel_scale = field.FindChild("PP");
				cp_rel_scale = field.FindChild("CP");
			}
		}

		public float GetFactionScale(bool player_scale, DT.Field scale, ProsAndCons pc)
		{
			if (scale == null)
			{
				return 1f;
			}
			if (player_scale)
			{
				return scale.Float(1, pc, 1f);
			}
			return scale.Float(0, pc, 1f);
		}

		public float PPFactionScale(bool player_scale, ProsAndCons pc)
		{
			return GetFactionScale(player_scale, pp_faction_scale, pc);
		}

		public float CPFactionScale(bool player_scale, ProsAndCons pc)
		{
			return GetFactionScale(player_scale, cp_faction_scale, pc);
		}

		private void LoadFactionScale()
		{
			DT.Field field = base.field.FindChild("faction_scale");
			if (field != null)
			{
				pp_faction_scale = field.FindChild("PP");
				cp_faction_scale = field.FindChild("CP");
			}
		}

		private List<Factor.Def> LoadFactors(string section, Def root_def)
		{
			DT.Field field = base.field.FindChild(section);
			if (field == null)
			{
				return null;
			}
			List<string> list = field.Keys();
			if (list.Count == 0)
			{
				return null;
			}
			List<Factor.Def> list2 = new List<Factor.Def>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				string path = list[i];
				DT.Field field2 = field.FindChild(path);
				Factor.Def def = new Factor.Def();
				if (def.Load(section, field2, root_def))
				{
					list2.Add(def);
				}
			}
			return list2;
		}

		private bool ValidateFactors(Game game, List<Factor.Def> factors)
		{
			if (factors == null)
			{
				return true;
			}
			bool result = true;
			for (int i = 0; i < factors.Count; i++)
			{
				if (!factors[i].Validate(game))
				{
					result = false;
				}
			}
			return result;
		}

		public Factor.Def FindFactor(string key)
		{
			if (factors == null)
			{
				return null;
			}
			for (int i = 0; i < factors.Count; i++)
			{
				Factor.Def def = factors[i];
				if (def.field.key == key)
				{
					return def;
				}
			}
			return null;
		}

		public bool ValidateThresholds()
		{
			if (thresholds_field == null || thresholds_field.children == null)
			{
				return true;
			}
			bool result = true;
			for (int i = 0; i < thresholds_field.children.Count; i++)
			{
				DT.Field field = thresholds_field.children[i];
				if (!string.IsNullOrEmpty(field.key))
				{
					bool above;
					Value value = ResolveThreshold(field.key, out above, null);
					if (!value.is_number && !(value.obj_val is Expression))
					{
						Game.Log(field.Path(include_file: true) + ": invalid threshold", Game.LogType.Error);
						result = false;
					}
				}
			}
			return result;
		}

		public Value ResolveThreshold(string name, out bool above, IVars vars)
		{
			above = true;
			if (thresholds_field == null)
			{
				return Value.Unknown;
			}
			bool flag = true;
			for (int i = 0; i < 100; i++)
			{
				DT.Field field = thresholds_field.FindChild(name);
				if (field == null)
				{
					return Value.Unknown;
				}
				DT.Field field2 = field.FindChild("above");
				if (field2 == null)
				{
					field2 = field.FindChild("below");
					if (field2 != null)
					{
						if (flag)
						{
							above = false;
						}
					}
					else
					{
						field2 = field;
					}
				}
				else
				{
					flag = false;
				}
				Value result = field2.Value(vars, vars != null);
				if (result.type == Value.Type.String)
				{
					name = result.String();
					continue;
				}
				return result;
			}
			return Value.Unknown;
		}

		public float GetThreshold(string name, out bool above, IVars vars)
		{
			Value value = ResolveThreshold(name, out above, vars);
			if (!value.is_number)
			{
				if (!above)
				{
					return float.MinValue;
				}
				return float.MaxValue;
			}
			return value.Float();
		}
	}

	public static class Tracker
	{
		public struct Track_ThresholdStats
		{
			public long count_pass;

			public long count_fail;

			public long count;

			public Dictionary<string, long> pass;

			public Dictionary<string, long> fail;
		}

		public struct Track_OfferStats
		{
			public long count;

			public Dictionary<string, Track_ThresholdStats> thresholds;
		}

		public static int top_n = 3;

		public static bool enabled = true;

		public static Dictionary<string, Track_OfferStats> stats = new Dictionary<string, Track_OfferStats>();

		public static Dictionary<string, Track_OfferStats> stats_player = new Dictionary<string, Track_OfferStats>();

		public static string last_war = "";

		public static string last_war_threshold = "";

		public static List<(string name, long value)> last_factors_pros = null;

		public static List<(string name, long value)> last_factors_cons = null;

		public static int last_cache_count = 5;

		public static void RecordLastPlayerWar(string name, ProsAndCons pc, string threshold)
		{
			List<Factor> list = new List<Factor>();
			List<Factor> list2 = new List<Factor>();
			for (int i = 0; i < pc.pros.Count; i++)
			{
				if (pc.pros[i].value != 0)
				{
					list.Add(pc.pros[i]);
				}
			}
			for (int j = 0; j < pc.cons.Count; j++)
			{
				if (pc.cons[j].value != 0)
				{
					list2.Add(pc.cons[j]);
				}
			}
			if (pc.adds != null)
			{
				for (int k = 0; k < pc.adds.Count; k++)
				{
					Factor factor = pc.adds[k];
					if (factor.PP > 0)
					{
						list.Add(factor);
					}
					if (factor.CP > 0)
					{
						list2.Add(factor);
					}
				}
			}
			list.Sort((Factor a, Factor b) => b.value.CompareTo(a.value));
			list2.Sort((Factor a, Factor b) => b.value.CompareTo(a.value));
			last_war = name;
			last_war_threshold = threshold;
			last_factors_pros = new List<(string, long)>(last_cache_count);
			last_factors_cons = new List<(string, long)>(last_cache_count);
			int num = Math.Min(list.Count, last_cache_count);
			for (int num2 = 0; num2 < num; num2++)
			{
				last_factors_pros.Add((list[num2].def.field.key, list[num2].value));
			}
			num = Math.Min(list2.Count, last_cache_count);
			for (int num3 = 0; num3 < num; num3++)
			{
				last_factors_cons.Add((list2[num3].def.field.key, list2[num3].value));
			}
		}

		public static string DumpLastPlayerWar()
		{
			if (last_factors_pros == null && last_factors_cons == null)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(last_war);
			string text = "    ";
			stringBuilder.AppendLine("Treshold: " + last_war_threshold);
			if (last_factors_pros != null)
			{
				stringBuilder.AppendLine("Pros: ");
				for (int i = 0; i < last_factors_pros.Count; i++)
				{
					stringBuilder.AppendLine(text + last_factors_pros[i].name + ": " + last_factors_pros[i].value);
				}
			}
			if (last_factors_cons != null)
			{
				stringBuilder.AppendLine("Cons: ");
				for (int j = 0; j < last_factors_cons.Count; j++)
				{
					stringBuilder.AppendLine(text + last_factors_cons[j].name + ": " + last_factors_cons[j].value);
				}
			}
			return stringBuilder.ToString();
		}

		public static void Record(Dictionary<string, Track_OfferStats> cur_stats, string offer_name, string threshold, string outcome, List<Factor> factors)
		{
			if (!cur_stats.TryGetValue(offer_name, out var value))
			{
				value.count = 0L;
				value.thresholds = new Dictionary<string, Track_ThresholdStats>();
				cur_stats.Add(offer_name, value);
			}
			if (!value.thresholds.TryGetValue(threshold, out var value2))
			{
				value2.count_pass = 0L;
				value2.count_fail = 0L;
				value2.count = 0L;
				value2.pass = new Dictionary<string, long>();
				value2.fail = new Dictionary<string, long>();
				value.thresholds.Add(threshold, value2);
			}
			Dictionary<string, long> dictionary = ((outcome == "pass") ? value2.pass : value2.fail);
			int num = top_n;
			for (int i = 0; i < factors.Count; i++)
			{
				if (num == 0)
				{
					break;
				}
				string key = factors[i].def.field.key;
				if (dictionary.TryGetValue(key, out var value3))
				{
					dictionary[key] = value3 + num;
				}
				else
				{
					dictionary[key] = num;
				}
				num--;
			}
			value.count++;
			value2.count++;
			if (outcome == "pass")
			{
				value2.count_pass++;
			}
			else
			{
				value2.count_fail++;
			}
			value.thresholds[threshold] = value2;
			cur_stats[offer_name] = value;
		}

		public static void Track(ProsAndCons pc, string threshold)
		{
			if (pc == null || threshold == null || pc.offer == null || pc.offer.parent != null || pc.isCached)
			{
				return;
			}
			bool flag = pc.CheckThreshold(threshold);
			string text = (flag ? "pass" : "fail");
			string offer_name = pc.offer.def.field.key + " (" + pc.def.field.key + ")";
			string key = pc.offer.def.field.key;
			List<Factor> list = new List<Factor>();
			List<Factor> list2 = (flag ? pc.pros : pc.cons);
			for (int i = 0; i < list2.Count; i++)
			{
				if (list2[i].value != 0)
				{
					list.Add(list2[i]);
				}
			}
			if (pc.adds != null)
			{
				for (int j = 0; j < pc.adds.Count; j++)
				{
					Factor factor = pc.adds[j];
					if ((flag && factor.PP > 0) || (!flag && factor.CP > 0))
					{
						list.Add(factor);
					}
				}
			}
			list.Sort((Factor a, Factor b) => b.value.CompareTo(a.value));
			if (!pc.our_kingdom.is_player && pc.their_kingdom.is_player && text == "pass" && (key == "DeclareWar" || key == "DeclareIndependence"))
			{
				RecordLastPlayerWar(key, pc, threshold);
			}
			Record(stats, offer_name, threshold, text, list);
			if ((pc.offer.from as Kingdom).is_player || (pc.offer.to as Kingdom).is_player)
			{
				Record(stats_player, offer_name, threshold, text, list);
			}
		}

		public static string Dump(Game game, Dictionary<string, Track_OfferStats> cur_stats)
		{
			StringBuilder stringBuilder = new StringBuilder();
			List<KeyValuePair<string, Track_OfferStats>> list = cur_stats.ToList();
			list.Sort((KeyValuePair<string, Track_OfferStats> a, KeyValuePair<string, Track_OfferStats> b) => b.Value.count.CompareTo(a.Value.count));
			long num = 0L;
			if (game != null)
			{
				num = game.session_time.milliseconds;
			}
			TimeSpan timeSpan = TimeSpan.FromMilliseconds(num);
			string text = $"{timeSpan.Hours:D2}h {timeSpan.Minutes:D2}m {timeSpan.Seconds:D2}s";
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("PC stats after: " + text);
			stringBuilder.AppendLine();
			string text2 = "    ";
			foreach (KeyValuePair<string, Track_OfferStats> item in list)
			{
				stringBuilder.AppendLine($"{item.Key} : {item.Value.count}");
				foreach (KeyValuePair<string, Track_ThresholdStats> threshold in item.Value.thresholds)
				{
					stringBuilder.AppendLine($"{text2}{threshold.Key}: {threshold.Value.count}");
					stringBuilder.AppendLine($"{text2}{text2}pass: {threshold.Value.count_pass}");
					List<KeyValuePair<string, long>> list2 = threshold.Value.pass.ToList();
					list2.Sort((KeyValuePair<string, long> a, KeyValuePair<string, long> b) => b.Value.CompareTo(a.Value));
					foreach (KeyValuePair<string, long> item2 in list2)
					{
						string text3 = item2.Key;
						if (text3 == "pc_base_pros")
						{
							text3 = "pros (base)";
						}
						else if (text3 == "pc_base_cons")
						{
							text3 = "cons (base)";
						}
						stringBuilder.AppendLine($"{text2}{text2}{text2}{text3}: {item2.Value}");
					}
					List<KeyValuePair<string, long>> list3 = threshold.Value.fail.ToList();
					stringBuilder.AppendLine($"{text2}{text2}fail: {threshold.Value.count_fail}");
					list3.Sort((KeyValuePair<string, long> a, KeyValuePair<string, long> b) => b.Value.CompareTo(a.Value));
					foreach (KeyValuePair<string, long> item3 in list3)
					{
						string text4 = item3.Key;
						if (text4 == "pc_base_pros")
						{
							text4 = "pros (base)";
						}
						else if (text4 == "pc_base_cons")
						{
							text4 = "cons (base)";
						}
						stringBuilder.AppendLine($"{text2}{text2}{text2}{text4}: {item3.Value}");
					}
				}
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}
	}

	public delegate bool ValidateReason(Factor factor);

	public delegate float CalcFunc(ProsAndCons pc, Factor factor);

	public Game game;

	public Def def;

	public bool isCached;

	public long calcs;

	public long cached;

	public static long total_calcs = 0L;

	public static long total_cached = 0L;

	public Kingdom our_kingdom;

	public Kingdom their_kingdom;

	public KingdomAndKingdomRelation rel = KingdomAndKingdomRelation.Default;

	public Offer offer;

	public Action action;

	public Object target;

	public List<Factor> pros;

	public List<Factor> cons;

	public List<Factor> adds;

	public int PP;

	public int CP;

	public float norm_PP;

	public float norm_CP;

	public float ratio;

	public float eval;

	public bool forced_recalc;

	public long calc_frame;

	public Factor cur_factor;

	public static Type[] CalcFuncParams = new Type[2]
	{
		typeof(ProsAndCons),
		typeof(Factor)
	};

	private static List<Kingdom> pc_we_have_common_enemies_list_ours = new List<Kingdom>();

	private static List<Kingdom> pc_we_have_common_enemies_list_theirs = new List<Kingdom>();

	private static List<Kingdom> cache_attackers = new List<Kingdom>(8);

	private static List<Kingdom> cache_defenders = new List<Kingdom>(8);

	protected ProsAndCons(Game game, Def def)
	{
		this.game = game;
		SetDef(def);
	}

	public ProsAndCons Copy()
	{
		ProsAndCons prosAndCons = new ProsAndCons(game, def);
		prosAndCons.CopyParams(this);
		return prosAndCons;
	}

	public static ProsAndCons Get(Game game, Def def)
	{
		if (def == null)
		{
			return null;
		}
		if (def.instance == null)
		{
			def.instance = new ProsAndCons(game, def);
		}
		return def.instance;
	}

	public static ProsAndCons Get(Game game, string def_id, bool reverse_kingdoms = false)
	{
		Def def = game.defs.Get<Def>(def_id);
		return Get(game, def);
	}

	public static ProsAndCons Find(Game game, string def_id)
	{
		Def def = game.defs.Find<Def>(def_id);
		return Get(game, def);
	}

	public static ProsAndCons Get(string def_id, Kingdom our_kingdom, Kingdom their_kingdom)
	{
		ProsAndCons prosAndCons = Get(their_kingdom.game, def_id);
		if (prosAndCons == null)
		{
			return null;
		}
		prosAndCons.SetDef(def_id);
		if (prosAndCons.our_kingdom != our_kingdom || prosAndCons.their_kingdom != their_kingdom)
		{
			prosAndCons.calc_frame = 0L;
		}
		prosAndCons.SetKingdoms(our_kingdom, their_kingdom);
		return prosAndCons;
	}

	public static ProsAndCons Get(Offer offer, string threshold, bool reverse_kingdoms = false)
	{
		using (Game.Profile("ProsAndCond.Get(Offer)"))
		{
			Game game = offer.game;
			Def def;
			if (threshold == "propose")
			{
				def = offer.def.propose_pc_def;
			}
			else
			{
				if (!(threshold == "accept"))
				{
					Game.Log("Invalid offer threshold: '" + threshold + "'", Game.LogType.Error);
					return null;
				}
				def = offer.def.accept_pc_def;
			}
			if (def == null)
			{
				return null;
			}
			ProsAndCons prosAndCons = Get(game, def);
			if (prosAndCons == null)
			{
				return null;
			}
			if (!offer.Equals(prosAndCons.offer))
			{
				prosAndCons.calc_frame = 0L;
			}
			prosAndCons.offer = offer;
			Kingdom kingdom;
			Kingdom kingdom2;
			if (threshold == "propose")
			{
				kingdom = offer.from as Kingdom;
				kingdom2 = offer.to as Kingdom;
			}
			else
			{
				kingdom = offer.to as Kingdom;
				kingdom2 = offer.from as Kingdom;
			}
			if (reverse_kingdoms)
			{
				Kingdom kingdom3 = kingdom;
				kingdom = kingdom2;
				kingdom2 = kingdom3;
			}
			prosAndCons.SetKingdoms(kingdom, kingdom2);
			prosAndCons.Calc();
			prosAndCons.eval = prosAndCons.Eval(threshold);
			if (Tracker.enabled)
			{
				Tracker.Track(prosAndCons, threshold);
			}
			return prosAndCons;
		}
	}

	public static ProsAndCons Get(Action action)
	{
		ProsAndCons prosAndCons = Get(action.game, action.def.use_pc_def);
		if (prosAndCons == null)
		{
			return null;
		}
		prosAndCons.action = action;
		prosAndCons.target = action.target;
		prosAndCons.SetKingdoms(action.own_kingdom, action.CalcTargetKingdom(action.target));
		prosAndCons.Calc();
		prosAndCons.eval = prosAndCons.Eval("use");
		return prosAndCons;
	}

	private bool FactorListMatchesDef(List<Factor> factors, List<Factor.Def> defs)
	{
		if ((factors?.Count ?? (-1)) != (defs?.Count ?? (-1)))
		{
			return false;
		}
		if (factors != null)
		{
			for (int i = 0; i < factors.Count; i++)
			{
				if (factors[i].def != defs[i])
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool FactorListsMatchDef(Def def)
	{
		if (!FactorListMatchesDef(pros, def.pros))
		{
			return false;
		}
		if (!FactorListMatchesDef(cons, def.cons))
		{
			return false;
		}
		if (!FactorListMatchesDef(adds, def.adds))
		{
			return false;
		}
		return true;
	}

	public void SetDef(string id)
	{
		SetDef(game.defs.Find<Def>(id));
	}

	public void SetDef(Def def)
	{
		if (def == null || this.def != def || !FactorListsMatchDef(def))
		{
			calc_frame = 0L;
			this.def = def;
			pros = CreateFactors(def?.pros);
			cons = CreateFactors(def?.cons);
			adds = CreateFactors(def?.adds);
		}
	}

	public bool CopyParams(ProsAndCons pc)
	{
		bool flag = false;
		if (our_kingdom != pc.our_kingdom)
		{
			our_kingdom = pc.our_kingdom;
			flag = true;
		}
		if (their_kingdom != pc.their_kingdom)
		{
			their_kingdom = pc.their_kingdom;
			flag = true;
		}
		rel = pc.rel;
		if (offer != pc.offer)
		{
			offer = pc.offer;
			flag = true;
		}
		if (action != pc.action)
		{
			action = pc.action;
			flag = true;
		}
		if (target != pc.target)
		{
			target = pc.target;
			flag = true;
		}
		if (flag)
		{
			calc_frame = 0L;
		}
		return flag;
	}

	public void SetKingdoms(Kingdom our_kingdom, Kingdom their_kingdom)
	{
		if (this.our_kingdom != our_kingdom || this.their_kingdom != their_kingdom)
		{
			calc_frame = 0L;
		}
		this.our_kingdom = our_kingdom;
		this.their_kingdom = their_kingdom;
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(our_kingdom, their_kingdom);
		if (rel.GetRelationship() != kingdomAndKingdomRelation.GetRelationship())
		{
			calc_frame = 0L;
		}
		rel = kingdomAndKingdomRelation;
	}

	public bool CheckThreshold(string name)
	{
		using (Game.Profile("ProsAndCond.CheckThreshold"))
		{
			bool above;
			float threshold = def.GetThreshold(name, out above, this);
			return above ? (ratio >= threshold) : (ratio <= threshold);
		}
	}

	public float GetRatioAboveThreshold(string name)
	{
		bool above;
		float threshold = def.GetThreshold(name, out above, this);
		return ratio - threshold;
	}

	public float Eval(string threshold_name)
	{
		bool above;
		float threshold = def.GetThreshold(threshold_name, out above, this);
		return (above ? (ratio - threshold) : (threshold - ratio)) * (float)def.cost.Value(this);
	}

	public List<Factor> CreateFactors(List<Factor.Def> defs)
	{
		if (defs == null || defs.Count == 0)
		{
			return null;
		}
		List<Factor> list = new List<Factor>(defs.Count);
		for (int i = 0; i < defs.Count; i++)
		{
			Factor item = new Factor(defs[i]);
			list.Add(item);
		}
		return list;
	}

	public int Calc(List<Factor> factors)
	{
		if (factors == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < factors.Count; i++)
		{
			Factor factor = factors[i];
			factor.Calc(this);
			num += factor.value;
		}
		return num;
	}

	public void CalcAdds()
	{
		if (adds != null)
		{
			for (int i = 0; i < adds.Count; i++)
			{
				Factor factor = adds[i];
				factor.Calc(this);
				PP += factor.PP;
				CP += factor.CP;
			}
		}
	}

	public float Calc(bool force_recalc = false)
	{
		if (calc_frame < 0)
		{
			game.Error(ToString() + ": Recursive ProsAndCons calculation!");
			return 1f;
		}
		if (!force_recalc && calc_frame == game.frame && !game.IsPaused())
		{
			cached++;
			total_cached++;
			isCached = true;
			return ratio;
		}
		calcs++;
		total_calcs++;
		forced_recalc = force_recalc;
		calc_frame = -1L;
		isCached = false;
		PP = Calc(pros);
		CP = Calc(cons);
		CalcAdds();
		norm_PP = (float)def.base_PP.Int(this, 3) + (float)PP * def.scale_PP.Float(this, 1f);
		norm_CP = (float)def.base_CP.Int(this, 3) + (float)CP * def.scale_CP.Float(this, 1f);
		float relationship = rel.GetRelationship();
		norm_PP *= def.PPRelScale(relationship, this);
		norm_CP *= def.CPRelScale(relationship, this);
		norm_PP *= def.PPFactionScale(their_kingdom.is_player, this);
		norm_CP *= def.CPFactionScale(their_kingdom.is_player, this);
		float num = norm_PP + norm_CP;
		if (num <= 0f)
		{
			norm_PP = 0f;
			norm_CP = 0f;
			ratio = 1f;
			return ratio;
		}
		norm_PP *= 2f / num;
		norm_CP *= 2f / num;
		ratio = norm_PP;
		calc_frame = game.frame;
		cur_factor = null;
		return ratio;
	}

	public virtual Factor GetReason(bool pro, ValidateReason validate)
	{
		List<Factor> list = (pro ? pros : cons);
		List<Factor> list2 = null;
		int num = 0;
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				Factor factor = list[i];
				int value = factor.value;
				if (value > 0 && validate(factor))
				{
					if (list2 == null)
					{
						list2 = new List<Factor>();
					}
					list2.Add(factor);
					num += value;
				}
			}
		}
		if (adds != null)
		{
			for (int j = 0; j < adds.Count; j++)
			{
				Factor factor2 = adds[j];
				int num2 = (factor2.value = (pro ? factor2.PP : factor2.CP));
				if (num2 > 0 && validate(factor2))
				{
					if (list2 == null)
					{
						list2 = new List<Factor>();
					}
					list2.Add(factor2);
					num += num2;
				}
			}
		}
		if (num <= 0)
		{
			return null;
		}
		int num3 = game.Random(0, num);
		for (int k = 0; k < list2.Count; k++)
		{
			Factor factor3 = list2[k];
			if (num3 < factor3.value)
			{
				return factor3;
			}
			num3 -= factor3.value;
		}
		return null;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "our_kingdom":
			return our_kingdom;
		case "their_kingdom":
			return their_kingdom;
		case "relationship":
			return rel.GetRelationship();
		case "stance":
			return (int)rel.stance;
		case "trade_agreement":
			return rel.stance.IsTrade();
		case "offer":
			return new Value(offer);
		case "action":
			return new Value(action);
		case "target":
			return target;
		case "pros":
			return new Value(pros);
		case "cons":
			return new Value(cons);
		case "PP":
			return (cur_factor == null) ? PP : cur_factor.PP;
		case "CP":
			return (cur_factor == null) ? CP : cur_factor.CP;
		case "ratio":
			return ratio;
		case "factor":
			return new Value(cur_factor);
		case "value":
			if (cur_factor != null)
			{
				return new Value(cur_factor.cs_value);
			}
			return Value.Null;
		default:
		{
			Factor factor = FindFactor(pros, key);
			if (factor != null)
			{
				factor.Calc(this);
				return new Value(factor);
			}
			factor = FindFactor(cons, key);
			if (factor != null)
			{
				factor.Calc(this);
				return new Value(factor);
			}
			factor = FindFactor(adds, key);
			if (factor != null)
			{
				factor.Calc(this);
				return new Value(factor);
			}
			Factor.Def def = this.def.FindFactor(key);
			if (def != null)
			{
				factor = new Factor(def);
				factor.Calc(this);
				return new Value(factor);
			}
			return Value.Unknown;
		}
		}
	}

	public override string ToString()
	{
		float relationship = rel.GetRelationship();
		return (def?.id ?? "null") + "(" + (our_kingdom?.Name ?? "null") + " -> " + relationship + " -> " + (their_kingdom?.Name ?? "null") + "): +" + PP + "*" + def.PPRelScale(relationship, this) + "*" + def.PPFactionScale(their_kingdom.is_player, this) + " -" + CP + "*" + def.CPRelScale(relationship, this) + "*" + def.CPFactionScale(their_kingdom.is_player, this) + " -> " + ratio;
	}

	public string Dump(string new_line)
	{
		string text = ToString();
		text = text + new_line + "Calcs: " + calcs + ", Cached: " + cached;
		if (pros != null && (pros.Count > 0 || def.PP_base.Int(this) > 0))
		{
			text = text + new_line + "Pros: " + PP;
			for (int i = 0; i < pros.Count; i++)
			{
				Factor factor = pros[i];
				text = text + new_line + "  " + factor.ToString();
			}
		}
		if (cons != null && (cons.Count > 0 || def.CP_base.Int(this, 3) > 0))
		{
			text = text + new_line + "Cons: " + CP;
			for (int j = 0; j < cons.Count; j++)
			{
				Factor factor2 = cons[j];
				text = text + new_line + "  " + factor2.ToString();
			}
		}
		if (adds != null && adds.Count > 0)
		{
			text = text + new_line + "Adds:";
			for (int k = 0; k < adds.Count; k++)
			{
				Factor factor3 = adds[k];
				text = text + new_line + "  " + factor3.ToString();
			}
		}
		if (def.thresholds_field != null)
		{
			List<string> list = def.thresholds_field.Keys();
			if (list.Count > 0)
			{
				text = text + new_line + "Thresholds (ratio = " + ratio + "):";
				for (int l = 0; l < list.Count; l++)
				{
					string text2 = list[l];
					bool above;
					float threshold = def.GetThreshold(text2, out above, this);
					bool flag = CheckThreshold(text2);
					float num = Eval(text2);
					text = text + new_line + "  " + text2 + (above ? " = above " : " = below ") + threshold + (flag ? " -> YES" : " -> NO") + "(" + num + ")";
				}
			}
		}
		return text;
	}

	public string Dump()
	{
		return Dump("\n");
	}

	public string DebugText()
	{
		return "#" + Dump();
	}

	public Factor FindFactor(List<Factor> factors, string name)
	{
		if (factors == null)
		{
			return null;
		}
		for (int i = 0; i < factors.Count; i++)
		{
			Factor factor = factors[i];
			if (factor.def.field.key == name)
			{
				return factor;
			}
		}
		return null;
	}

	public static CalcFunc FindCalcFunc(string name)
	{
		MethodInfo method = typeof(ProsAndCons).GetMethod(name, CalcFuncParams);
		if (method == null || !method.IsStatic || !method.IsPublic || method.ReturnType != typeof(float))
		{
			return null;
		}
		try
		{
			if (Delegate.CreateDelegate(typeof(CalcFunc), method) is CalcFunc result)
			{
				return result;
			}
		}
		catch (Exception ex)
		{
			Game.Log("Error creating ProsAndCons factor calculator delegate for " + name + ": " + ex.ToString(), Game.LogType.Error);
		}
		return null;
	}

	private static ProsAndCons GetTestOfferProCon(string def_id, Kingdom plr_kingdom, Kingdom sel_kingdom)
	{
		string[] array = def_id.Split('_');
		if (array.Length < 2)
		{
			return null;
		}
		string def_id2 = array[1];
		string text = "propose";
		if (array.Length == 3)
		{
			text = array[1].ToLowerInvariant();
			def_id2 = array[2];
		}
		Offer offer = null;
		offer = ((!(text == "accept")) ? Offer.GetCachedOffer(def_id2, plr_kingdom, sel_kingdom) : Offer.GetCachedOffer(def_id2, sel_kingdom, plr_kingdom));
		if (offer == null)
		{
			return null;
		}
		OfferGenerator.instance.FillOfferArgs(text, offer);
		if (offer.Validate() != "ok")
		{
			return null;
		}
		return Get(offer, text);
	}

	public static ProsAndCons GetTestProCon(string def_id, Kingdom plr_kingdom, Kingdom sel_kingdom)
	{
		ProsAndCons prosAndCons = GetTestOfferProCon(def_id, plr_kingdom, sel_kingdom);
		if (prosAndCons == null)
		{
			prosAndCons = Get(def_id, plr_kingdom, sel_kingdom);
		}
		return prosAndCons;
	}

	public static string Test(string def_id, Kingdom plr_kingdom, Kingdom sel_kingdom)
	{
		if (plr_kingdom == null || sel_kingdom == null)
		{
			return "no selected kingdom";
		}
		ProsAndCons testProCon = GetTestProCon(def_id, sel_kingdom, plr_kingdom);
		if (testProCon == null)
		{
			return "unknown def";
		}
		testProCon.Calc();
		return testProCon.Dump();
	}

	public static string Benchmark(Kingdom plr_kingdom, Kingdom sel_kingdom)
	{
		if (plr_kingdom == null || sel_kingdom == null)
		{
			return "no selected kingdom";
		}
		ProsAndCons prosAndCons = Get("PC_War", plr_kingdom, sel_kingdom);
		int num = 10000;
		Game.BeginProfileSection("Benchmark ProsAndCons");
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < num; i++)
		{
			prosAndCons.Calc(force_recalc: true);
		}
		stopwatch.Stop();
		Game.EndProfileSection("Benchmark ProsAndCons");
		return (float)stopwatch.ElapsedMilliseconds / (float)num + "ms";
	}

	public static float pc_base_pros(ProsAndCons pc, Factor factor)
	{
		return pc.def.PP_base.Int(pc);
	}

	public static float pc_base_cons(ProsAndCons pc, Factor factor)
	{
		return pc.def.CP_base.Int(pc);
	}

	public static float pc_we_have_bad_relation(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.rel.GetRelationship() / RelationUtils.Def.maxRelationship;
		if (num >= 0f)
		{
			return 0f;
		}
		float num2 = factor.def.field.GetFloat("max", pc);
		float num3 = factor.def.field.GetFloat("pow", pc);
		return num2 * (float)Math.Pow(num, num3);
	}

	public static float pc_we_have_good_relation(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.rel.GetRelationship() / RelationUtils.Def.maxRelationship;
		if (num < 0f)
		{
			return 0f;
		}
		float num2 = factor.def.field.GetFloat("base", pc);
		float num3 = factor.def.field.GetFloat("max", pc);
		float num4 = factor.def.field.GetFloat("pow", pc);
		return num2 + num3 * (float)Math.Pow(num, num4);
	}

	public static float pc_our_army_is_stronger(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.our_kingdom.CalcArmyStrength();
		float num2 = pc.their_kingdom.CalcArmyStrength();
		return (num / (num + num2) - 0.5f) * 2f * factor.def.field.GetFloat("max", pc);
	}

	public static float pc_our_army_is_weaker(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.our_kingdom.CalcArmyStrength();
		float num2 = pc.their_kingdom.CalcArmyStrength();
		return (num2 / (num + num2) - 0.5f) * 2f * factor.def.field.GetFloat("max", pc);
	}

	public static float pc_they_rule_our_lands(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < pc.their_kingdom.realms.Count; i++)
		{
			Realm realm = pc.their_kingdom.realms[i];
			if (realm.IsHistoricalFor(pc.our_kingdom))
			{
				num += factor.def.field.GetFloat("per_historical_realm", pc);
			}
			if (realm.IsCoreFor(pc.our_kingdom))
			{
				num += factor.def.field.GetFloat("per_core_realm", pc);
			}
		}
		return num;
	}

	public static float pc_we_rule_their_lands(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < pc.our_kingdom.realms.Count; i++)
		{
			Realm realm = pc.their_kingdom.realms[i];
			if (realm.IsHistoricalFor(pc.their_kingdom))
			{
				num += factor.def.field.GetFloat("per_historical_realm", pc);
			}
			if (realm.IsCoreFor(pc.their_kingdom))
			{
				num += factor.def.field.GetFloat("per_core_realm", pc);
			}
		}
		return num;
	}

	public static float pc_they_rule_our_people(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		int num2 = pc.game.cultures.Dist(pc.their_kingdom.culture, pc.our_kingdom.culture);
		float num3 = 1f - pc.their_kingdom.GetStat(Stats.ks_reduce_culture_tension_perc);
		for (int i = 0; i < pc.their_kingdom.realms.Count; i++)
		{
			Realm realm = pc.their_kingdom.realms[i];
			if (realm.pop_majority.kingdom == pc.our_kingdom)
			{
				num += factor.def.field.GetFloat("our_kingdom", pc);
			}
			else
			{
				if (num2 <= 0)
				{
					continue;
				}
				switch (pc.game.cultures.Dist(realm.pop_majority.kingdom?.culture, pc.our_kingdom.culture))
				{
				case 0:
					num += factor.def.field.GetFloat("our_culture", pc) * num3;
					break;
				case 1:
					if (num2 == 2)
					{
						num += factor.def.field.GetFloat("our_culture_family", pc) * num3;
					}
					break;
				}
			}
		}
		return num;
	}

	public static float pc_we_rule_their_people(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		int num2 = pc.game.cultures.Dist(pc.their_kingdom.culture, pc.our_kingdom.culture);
		for (int i = 0; i < pc.our_kingdom.realms.Count; i++)
		{
			Realm realm = pc.our_kingdom.realms[i];
			if (realm.pop_majority.kingdom == pc.their_kingdom)
			{
				num += factor.def.field.GetFloat("their_kingdom", pc);
			}
			else
			{
				if (num2 <= 0)
				{
					continue;
				}
				switch (pc.game.cultures.Dist(realm.pop_majority.kingdom?.culture, pc.their_kingdom.culture))
				{
				case 0:
					num += factor.def.field.GetFloat("their_culture", pc);
					break;
				case 1:
					if (num2 == 2)
					{
						num += factor.def.field.GetFloat("their_culture_family", pc);
					}
					break;
				}
			}
		}
		return num;
	}

	public static float pc_we_are_X_times_larger(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = factor.def.field.GetFloat("added_realms", pc);
		float num3 = factor.def.field.GetFloat("ai_mul", pc, 1f);
		if ((float)pc.our_kingdom.realms.Count + num2 >= factor.def.field.GetFloat("X", pc) * ((float)pc.their_kingdom.realms.Count + num2))
		{
			num = 1f;
		}
		if (!pc.their_kingdom.is_player)
		{
			num *= num3;
		}
		return num;
	}

	public static float pc_they_are_X_times_larger(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = factor.def.field.GetFloat("added_realms");
		float num3 = factor.def.field.GetFloat("ai_mul", pc, 1f);
		if ((float)pc.their_kingdom.realms.Count + num2 >= factor.def.field.GetFloat("X", pc) * ((float)pc.our_kingdom.realms.Count + num2))
		{
			num = 1f;
		}
		if (!pc.their_kingdom.is_player)
		{
			num *= num3;
		}
		return num;
	}

	public static float pc_they_are_excommunicated_and_we_are_catholic(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_catholic)
		{
			return 0f;
		}
		if (!pc.their_kingdom.is_catholic || !pc.their_kingdom.excommunicated)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_papacy(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom == pc.game.religions.catholic.hq_kingdom)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_are_papacy(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom == pc.game.religions.catholic.hq_kingdom)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_control_the_pope(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.HasPope())
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_control_the_pope(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.HasPope())
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_control_the_pope_and_we_are_catholic(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.HasPope() && pc.our_kingdom.is_catholic)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_can_gain_sea_outlet(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.our_kingdom.GetCostalRealmsCount() == 0 && pc.their_kingdom.GetCostalRealmsCount() > 0)
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_we_both_have_under_X_towns(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.our_kingdom.is_player || pc.their_kingdom.is_player)
		{
			return 0f;
		}
		if (pc.our_kingdom.realms.Count <= factor.def.field.GetInt("X", pc) && pc.their_kingdom.realms.Count <= factor.def.field.GetInt("X", pc))
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_there_are_our_loyalists(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		foreach (Army army in pc.their_kingdom.armies)
		{
			if (army.rebel != null && army.rebel.loyal_to == pc.our_kingdom.id)
			{
				result = 1f;
			}
		}
		return result;
	}

	public static float pc_they_have_rebels(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = factor.def.field.GetInt("per_rebel", pc);
		foreach (Army army in pc.their_kingdom.armies)
		{
			if (army.rebel != null && !army.rebel.IsLoyalist())
			{
				num += num2;
			}
		}
		return 0f;
	}

	public static float pc_they_have_different_religion(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.religion != pc.their_kingdom.religion)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_have_different_culture(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.GetStat(Stats.ks_reduce_culture_tension_perc) > 0f || pc.their_kingdom.GetStat(Stats.ks_reduce_culture_tension_perc) > 0f)
		{
			return 0f;
		}
		if (pc.our_kingdom.culture != pc.their_kingdom.culture)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_have_allies(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetInt("per_ally", pc);
		return (float)pc.our_kingdom.GetAllies().Count * num;
	}

	public static float pc_we_have_trade(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.rel.stance.IsTrade())
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_we_have_nap(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.rel.stance.IsNonAgression())
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_we_are_allies(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.rel.stance.IsAlliance())
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_we_are_trading(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.GetMerchantFrom(pc.their_kingdom) != null)
		{
			return 1f;
		}
		if (pc.their_kingdom.GetMerchantFrom(pc.our_kingdom) != null)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_have_more_trade(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.our_kingdom.GetStat(Stats.ks_commerce) < pc.their_kingdom.GetStat(Stats.ks_commerce))
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_we_need_more_trade_agreements(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom?.court == null)
		{
			return 0f;
		}
		int num = factor.def.field.GetInt("per_idle_merchant", pc);
		int num2 = factor.def.field.GetInt("reserve", pc);
		float num3 = factor.def.field.GetFloat("player_mul", pc, 1f);
		int num4 = 0;
		for (int i = 0; i < pc.our_kingdom.court.Count; i++)
		{
			Character character = pc.our_kingdom.court[i];
			if (character != null && character.IsMerchant() && character.IsAlive() && !character.IsPrisoner())
			{
				num4++;
			}
		}
		int count = pc.our_kingdom.tradeAgreementsWith.Count;
		if (count >= num4 + 1)
		{
			return 0f;
		}
		int num5 = num4 - count;
		if (num5 < 0)
		{
			num5 = 0;
		}
		float num6 = num5 * num;
		if (count < num4 + 1)
		{
			num6 += (float)num2;
		}
		if (pc.their_kingdom.is_player)
		{
			num6 *= num3;
		}
		return num6;
	}

	public static float pc_we_are_family(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.rel.stance.IsMarriage())
		{
			return 0f;
		}
		for (int i = 0; i < pc.our_kingdom.marriages.Count; i++)
		{
			if (pc.our_kingdom.marriages[i].husband == pc.our_kingdom.GetKing())
			{
				return factor.def.field.GetInt("our_king", pc);
			}
		}
		return factor.def.field.GetInt("our_prince_or_princess", pc);
	}

	public static float pc_we_have_no_border(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.their_kingdom.HasNeighbor(pc.our_kingdom))
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_are_too_far(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (!pc.our_kingdom.HasNeighbor(pc.their_kingdom) && pc.our_kingdom.DistanceToKingdom(pc.their_kingdom) > factor.def.field.GetInt("threshold", pc))
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_we_share_same_religion(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.our_kingdom.religion == pc.their_kingdom.religion)
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_we_are_catholic_and_they_are_papacy(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_catholic)
		{
			return 0f;
		}
		if (pc.their_kingdom != pc.game.religions.catholic.hq_kingdom)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_have_rebels(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = factor.def.field.GetInt("per_rebel", pc);
		foreach (Rebellion rebellion in pc.our_kingdom.rebellions)
		{
			num += (float)rebellion.rebels.Count * num2;
		}
		return num;
	}

	public static float pc_they_have_allies(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("per_ally", pc);
		return (float)pc.their_kingdom.GetAllies().Count * num;
	}

	public static float pc_they_fight_our_friends(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		List<War> wars = pc.their_kingdom.wars;
		float num2 = factor.def.field.GetFloat("threshold", pc);
		float num3 = factor.def.field.GetFloat("per_kingdom", pc);
		float relationship = pc.our_kingdom.GetRelationship(pc.their_kingdom);
		foreach (War item in wars)
		{
			List<Kingdom> enemies = item.GetEnemies(pc.their_kingdom);
			if (enemies == null)
			{
				continue;
			}
			foreach (Kingdom item2 in enemies)
			{
				if (item2 != null)
				{
					float relationship2 = pc.our_kingdom.GetRelationship(item2);
					if (!(relationship2 < num2) && !(relationship2 < relationship))
					{
						num += num3;
					}
				}
			}
		}
		return num;
	}

	public static float pc_they_have_the_pope(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.their_kingdom == pc.game.religions.catholic.head_kingdom)
		{
			result = 1f;
		}
		return result;
	}

	public static float pc_they_fight_catolics(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = factor.def.field.GetFloat("per_war", pc);
		foreach (War war in pc.their_kingdom.wars)
		{
			List<Kingdom> enemies = war.GetEnemies(pc.their_kingdom);
			if (enemies == null)
			{
				continue;
			}
			foreach (Kingdom item in enemies)
			{
				if (item.is_catholic)
				{
					num += num2;
					break;
				}
			}
		}
		return num;
	}

	public static float pc_they_hold_many_christian_provinces(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.is_christian)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = factor.def.field.GetFloat("per_province", pc);
		float num3 = factor.def.field.GetFloat("constantinople", pc);
		foreach (Realm realm in pc.their_kingdom.realms)
		{
			if (realm.religion.def.christian)
			{
				num = ((realm != pc.game.religions.orthodox.hq_realm) ? (num + num2) : (num + num3));
			}
		}
		return num;
	}

	public static float CalcAverageRelationWithCatholics(Kingdom kingdom)
	{
		List<Kingdom> kingdoms = kingdom.game.kingdoms;
		int num = 0;
		float num2 = 0f;
		foreach (Kingdom item in kingdoms)
		{
			if (!item.IsDefeated() && item.is_catholic && kingdom != item)
			{
				float relationship = item.GetRelationship(kingdom);
				int count = item.realms.Count;
				num2 += relationship * (float)count;
				num += count;
			}
		}
		if (num <= 0)
		{
			return 0f;
		}
		return num2 / (float)num;
	}

	public static float pc_they_are_hated_by_catholics(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = CalcAverageRelationWithCatholics(pc.their_kingdom);
		float num2 = factor.def.field.GetFloat("threshold", pc);
		if (!(num < num2))
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_are_loved_by_catholics(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = CalcAverageRelationWithCatholics(pc.their_kingdom);
		float num2 = factor.def.field.GetFloat("threshold", pc);
		if (!(num > num2))
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_usurp_jerusalem(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.religion.def.christian)
		{
			return 0f;
		}
		if (pc.game.religions.catholic.holy_lands_realm?.GetKingdom() == pc.their_kingdom)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_fight_infidels(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		foreach (War war in pc.their_kingdom.wars)
		{
			List<Kingdom> enemies = war.GetEnemies(pc.their_kingdom);
			if (enemies == null)
			{
				continue;
			}
			foreach (Kingdom item in enemies)
			{
				if (item != null && !item.is_christian)
				{
					result = 1f;
					break;
				}
			}
		}
		return result;
	}

	public static float pc_many_excommunicated_kingdoms(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		int excommunicatedCount = pc.our_kingdom.game.religions.catholic.GetExcommunicatedCount();
		if (excommunicatedCount <= 0)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("per_kingdom", pc) * (float)excommunicatedCount;
	}

	public static float pc_we_want_to_liberate_rome(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_catholic || pc.our_kingdom.excommunicated)
		{
			return 0f;
		}
		Catholic catholic = pc.game.religions.catholic;
		if (pc.their_kingdom == catholic.hq_kingdom)
		{
			return 0f;
		}
		if (catholic.hq_realm?.GetKingdom() != pc.their_kingdom)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_are_at_war_with_papacy(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		foreach (War war in pc.their_kingdom.wars)
		{
			List<Kingdom> enemies = war.GetEnemies(pc.their_kingdom);
			if (enemies == null)
			{
				continue;
			}
			foreach (Kingdom item in enemies)
			{
				if (item.game.religions.catholic.hq_kingdom == item)
				{
					return 1f;
				}
			}
		}
		return 0f;
	}

	public static float pc_we_have_common_allies(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		int num = 0;
		List<Kingdom> allies = pc.our_kingdom.GetAllies();
		List<Kingdom> allies2 = pc.their_kingdom.GetAllies();
		foreach (Kingdom item in allies)
		{
			if (allies2.Contains(item))
			{
				num++;
			}
		}
		if (num <= 0)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("per_ally", pc) * (float)num;
	}

	public static float pc_they_just_refused_to_lead_crusade(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.game.religions.catholic.just_refused_crusade != pc.their_kingdom)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_catholics_and_have_many_heretics(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.their_kingdom.is_catholic)
		{
			return 0f;
		}
		int num = 0;
		int count = pc.their_kingdom.realms.Count;
		if (count == 0)
		{
			return 0f;
		}
		float num2 = factor.def.field.GetFloat("max", pc);
		for (int i = 0; i < count; i++)
		{
			if (pc.their_kingdom.realms[i].is_pagan)
			{
				num++;
			}
		}
		return num2 * (float)num / (float)count;
	}

	public static float pc_we_have_war_exhaustion(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("max", pc);
		float stat = pc.our_kingdom.GetStat(Stats.ks_war_exhaustion);
		return num * stat / 100f;
	}

	public static float pc_they_have_war_exhaustion(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("max", pc);
		float stat = pc.their_kingdom.GetStat(Stats.ks_war_exhaustion);
		return num * stat / 100f;
	}

	public static float pc_our_authority_is_low(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.our_kingdom.GetCrownAuthority().GetValue();
		float num2 = factor.def.field.GetFloat("per_negative_point", pc);
		if (!(num < 0f))
		{
			return 0f;
		}
		return -1f * num * num2;
	}

	public static float pc_their_authority_is_low(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.their_kingdom.GetCrownAuthority().GetValue();
		float num2 = factor.def.field.GetFloat("per_negative_point", pc);
		if (!(num < 0f))
		{
			return 0f;
		}
		return -1f * num * num2;
	}

	public static float pc_our_authority_is_high(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.our_kingdom.GetCrownAuthority().GetValue();
		float num2 = factor.def.field.GetFloat("per_positive_point", pc);
		if (!(num < 0f))
		{
			return 0f;
		}
		return num * num2;
	}

	public static float pc_their_authority_is_high(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.their_kingdom.GetCrownAuthority().GetValue();
		float num2 = factor.def.field.GetFloat("per_positive_point", pc);
		if (!(num < 0f))
		{
			return 0f;
		}
		return num * num2;
	}

	public static float pc_we_didnt_really_fight_yet(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.rel.stance.IsWar())
		{
			return 0f;
		}
		War war = pc.our_kingdom.FindWarWith(pc.their_kingdom);
		if (war == null)
		{
			return 0f;
		}
		int side = war.GetSide(pc.our_kingdom);
		int side2 = war.EnemySide(side);
		float val = war.GetSideScore(side) + war.GetSideScore(side2);
		float num = factor.def.field.GetFloat("time_flat", pc);
		float vmax = factor.def.field.GetFloat("time_fade_out", pc);
		float num2 = pc.game.time - pc.rel.war_time;
		float num3 = factor.def.field.GetFloat("max", pc);
		float vmax2 = factor.def.field.GetFloat("points", pc);
		float num4 = (1f - Game.map_clamp(val, 0f, vmax2, 0f, 1f)) * num3;
		if (num2 < num)
		{
			return num4;
		}
		float val2 = num2 - num;
		return (1f - Game.map_clamp(val2, 0f, vmax, 0f, 1f)) * num4;
	}

	public static float pc_we_need_peace_time(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.wars.Count > 0)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("minutes", pc) * 60f;
		float num2 = pc.our_kingdom.TimeInPeace();
		float num3 = factor.def.field.GetFloat("max", pc);
		float num4 = factor.def.field.GetFloat("pow", pc);
		float num5 = Game.clamp(num2 / num, 0f, 1f);
		float num6 = (1f - (float)Math.Pow(num5, num4)) * num3;
		if (num6 < 0f)
		{
			num6 = 0f;
		}
		return num6;
	}

	public static float pc_we_just_had_peace(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("minutes", pc) * 60f;
		if (Math.Max(pc.game.time - pc.rel.peace_time, 0f) < num)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_have_truce(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.rel.stance.IsWar())
		{
			return 0f;
		}
		if (pc.rel.peace_time == Time.Zero)
		{
			return 0f;
		}
		float num = RelationUtils.Def.truce_time * 60f;
		float num2 = Game.clamp(pc.game.time - pc.rel.peace_time, 0f, num);
		float num3 = factor.def.field.GetFloat("max", pc);
		float num4 = factor.def.field.GetFloat("pow", pc);
		float num5 = (1f - (float)Math.Pow(num2 / num, num4)) * num3;
		if (num5 < 0f)
		{
			num5 = 0f;
		}
		return num5;
	}

	public static float pc_we_have_truce_with_target(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Kingdom kingdom = pc.offer.GetArg<Kingdom>(0);
		if (kingdom == null)
		{
			War arg = pc.offer.GetArg<War>(0);
			if (arg == null)
			{
				return 0f;
			}
			int side = arg.GetSide(pc.their_kingdom);
			kingdom = arg.GetEnemyLeader(side);
			if (kingdom == null)
			{
				return 0f;
			}
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(pc.our_kingdom, kingdom);
		if (kingdomAndKingdomRelation.stance.IsWar())
		{
			return 0f;
		}
		if (kingdomAndKingdomRelation.peace_time == Time.Zero)
		{
			return 0f;
		}
		float num = RelationUtils.Def.truce_time * 60f;
		float num2 = Game.clamp(pc.game.time - kingdomAndKingdomRelation.peace_time, 0f, num);
		float num3 = factor.def.field.GetFloat("max", pc);
		float num4 = factor.def.field.GetFloat("pow", pc, 1f);
		float num5 = (1f - (float)Math.Pow(num2 / num, num4)) * num3;
		if (num5 < 0f)
		{
			num5 = 0f;
		}
		return num5;
	}

	public static float pc_they_have_truce_with_target(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Kingdom kingdom = pc.offer.GetArg<Kingdom>(0);
		if (kingdom == null)
		{
			War arg = pc.offer.GetArg<War>(0);
			if (arg == null)
			{
				return 0f;
			}
			int side = arg.GetSide(pc.our_kingdom);
			kingdom = arg.GetEnemyLeader(side);
			if (kingdom == null)
			{
				return 0f;
			}
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(pc.their_kingdom, kingdom);
		if (kingdomAndKingdomRelation.stance.IsWar())
		{
			return 0f;
		}
		if (kingdomAndKingdomRelation.peace_time == Time.Zero)
		{
			return 0f;
		}
		float num = RelationUtils.Def.truce_time * 60f;
		float num2 = Game.clamp(pc.game.time - kingdomAndKingdomRelation.peace_time, 0f, num);
		float num3 = factor.def.field.GetFloat("max", pc);
		float num4 = factor.def.field.GetFloat("pow", pc, 1f);
		float num5 = (1f - (float)Math.Pow(num2 / num, num4)) * num3;
		if (num5 < 0f)
		{
			num5 = 0f;
		}
		return num5;
	}

	public static float pc_war_just_started(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.rel.stance.IsWar())
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("minutes", pc) * 60f;
		float num2 = pc.game.time - pc.rel.war_time;
		float num3 = factor.def.field.GetFloat("max", pc);
		float num4 = factor.def.field.GetFloat("pow", pc);
		float num5 = (1f - (float)Math.Pow(num2 / num, num4)) * num3;
		if (num5 < 0f)
		{
			num5 = 0f;
		}
		return num5;
	}

	public static float pc_we_have_too_many_wars(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.our_kingdom.wars.Count;
		if (num == 0f)
		{
			return 0f;
		}
		float num2 = factor.def.field.GetFloat("per_additional_war", pc);
		float num3 = factor.def.field.GetFloat("at_war_base", pc);
		float val = factor.def.field.GetFloat("max", pc);
		return Math.Min(num3 + (num - 1f) * num2, val);
	}

	public static float pc_they_have_too_many_wars(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = pc.their_kingdom.wars.Count;
		if (num == 0f)
		{
			return 0f;
		}
		float num2 = factor.def.field.GetFloat("per_additional_war", pc);
		float num3 = factor.def.field.GetFloat("at_war_base", pc);
		float val = factor.def.field.GetFloat("max", pc);
		return Math.Min(num3 + (num - 1f) * num2, val);
	}

	public static float pc_too_many_global_wars(ProsAndCons pc, Factor factor)
	{
		int value = 0;
		pc.game.num_objects_by_type.TryGetValue(typeof(War), out value);
		int num = factor.def.field.GetInt("num_wars", pc);
		float num2 = factor.def.field.GetFloat("pow", pc);
		float num3 = factor.def.field.GetFloat("max", pc);
		float num4 = Game.map_clamp(value, 0f, num, 0f, 1f);
		return num3 * (float)Math.Pow(num4, num2);
	}

	public static float pc_too_many_kingdoms_at_war(ProsAndCons pc, Factor factor)
	{
		int kingdoms_at_war = pc.game.kingdoms_at_war;
		int num = factor.def.field.GetInt("num_kingdoms", pc);
		float num2 = factor.def.field.GetFloat("pow", pc);
		float num3 = factor.def.field.GetFloat("max", pc);
		float num4 = Game.map_clamp(kingdoms_at_war, 0f, num, 0f, 1f);
		return num3 * (float)Math.Pow(num4, num2);
	}

	public static float pc_we_hold_Rome(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.realms.Contains(pc.game.religions.catholic.hq_realm) && pc.our_kingdom != pc.game.religions.catholic.hq_kingdom && pc.their_kingdom.is_catholic && !pc.their_kingdom.excommunicated)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_hold_Rome(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.realms.Contains(pc.game.religions.catholic.hq_realm) && pc.their_kingdom != pc.game.religions.catholic.hq_kingdom && pc.our_kingdom.is_catholic && !pc.our_kingdom.excommunicated)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_hold_Constantinople_and_they_are_Ortodox(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.realms.Contains(pc.game.religions.orthodox.hq_realm) && !pc.our_kingdom.is_orthodox && pc.their_kingdom.is_orthodox)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_hold_Constantinople_and_we_are_Ortodox(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.is_orthodox && pc.their_kingdom.realms.Contains(pc.game.religions.orthodox.hq_realm) && !pc.their_kingdom.is_orthodox)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_have_caliphate(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.caliphate && pc.their_kingdom.is_muslim)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_have_caliphate(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.caliphate && pc.our_kingdom.is_muslim)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_have_common_enemies(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetInt("per_enemy", pc);
		float num2 = factor.def.field.GetInt("max", pc);
		float num3 = 0f;
		pc_we_have_common_enemies_list_ours.Clear();
		pc_we_have_common_enemies_list_theirs.Clear();
		for (int i = 0; i < pc.our_kingdom.wars.Count; i++)
		{
			List<Kingdom> list = pc.our_kingdom.wars[i]?.GetEnemies(pc.our_kingdom);
			if (list == null)
			{
				continue;
			}
			for (int j = 0; j < list.Count; j++)
			{
				Kingdom kingdom = list[j];
				if (kingdom != null && !pc_we_have_common_enemies_list_ours.Contains(kingdom))
				{
					pc_we_have_common_enemies_list_ours.Add(kingdom);
				}
			}
		}
		for (int k = 0; k < pc.their_kingdom.wars.Count; k++)
		{
			List<Kingdom> list2 = pc.their_kingdom.wars[k]?.GetEnemies(pc.their_kingdom);
			if (list2 == null)
			{
				continue;
			}
			for (int l = 0; l < list2.Count; l++)
			{
				Kingdom kingdom2 = list2[l];
				if (kingdom2 != null && !pc_we_have_common_enemies_list_theirs.Contains(kingdom2))
				{
					pc_we_have_common_enemies_list_theirs.Add(kingdom2);
				}
			}
		}
		foreach (Kingdom pc_we_have_common_enemies_list_our in pc_we_have_common_enemies_list_ours)
		{
			if (pc_we_have_common_enemies_list_theirs.Contains(pc_we_have_common_enemies_list_our))
			{
				num3 += num;
				if (num3 >= num2)
				{
					return num2;
				}
			}
		}
		return num3;
	}

	public static float pc_they_are_our_vassal(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.sovereignState == pc.our_kingdom)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_are_their_vassal(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.sovereignState == pc.their_kingdom)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_are_their_vassal_march(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("max", pc);
		if (pc.our_kingdom.sovereignState == pc.their_kingdom && pc.our_kingdom.vassalage != null && pc.our_kingdom.vassalage.def.type == Vassalage.Type.March)
		{
			if (pc.our_kingdom.wars.Count == 0)
			{
				return num;
			}
			if (pc.our_kingdom.wars.Count == 1)
			{
				return num / 4f;
			}
			return num / 10f;
		}
		return 0f;
	}

	public static float pc_they_are_vassal(ProsAndCons pc, Factor factor)
	{
		if (pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.IsVassal())
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_are_supporting_our_liege(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Kingdom kingdom = pc.our_kingdom;
		Kingdom k = pc.their_kingdom;
		Kingdom sovereignState = kingdom.sovereignState;
		if (sovereignState == null)
		{
			return 0f;
		}
		War war = kingdom.FindWarWith(k);
		if (war == null)
		{
			return 0f;
		}
		if (war.GetLeader(kingdom) == sovereignState)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_share_same_liege(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Kingdom sovereignState = pc.our_kingdom.sovereignState;
		Kingdom sovereignState2 = pc.their_kingdom.sovereignState;
		if (sovereignState != null && sovereignState2 != null && sovereignState == sovereignState2)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_liege_attacked_other_vassal(ProsAndCons pc, Factor factor)
	{
		return 0f;
	}

	public static float pc_they_are_new_liege(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.sovereignState != pc.their_kingdom)
		{
			return 0f;
		}
		if (!pc.rel.stance.IsAnyVassalage())
		{
			return 0f;
		}
		Time vassalage_time = pc.rel.vassalage_time;
		float num = factor.def.field.GetFloat("max", pc);
		float num2 = factor.def.field.GetFloat("pow", pc);
		float num3 = factor.def.field.GetFloat("max_time", pc);
		float val = Math.Max(pc.our_kingdom.game.time - vassalage_time, num3);
		val = Game.map_clamp(val, 0f, num3, 0f, 1f);
		return num * (1f - (float)Math.Pow(val, num2));
	}

	public static float pc_our_king_is_venerable(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom?.royalFamily?.Sovereign == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.royalFamily.Sovereign.age == Character.Age.Venerable)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_their_king_is_venerable(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom?.royalFamily?.Sovereign == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.royalFamily.Sovereign.age == Character.Age.Venerable)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_our_king_is_old(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom?.royalFamily?.Sovereign == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.royalFamily.Sovereign.age == Character.Age.Old)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_their_king_is_old(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom?.royalFamily?.Sovereign == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.royalFamily.Sovereign.age == Character.Age.Old)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_have_more_gold_than_ransom_price(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count < 2 || pc.our_kingdom == null)
		{
			return 0f;
		}
		int num = offer.GetArg(1);
		float num2 = pc.our_kingdom.resources.Get(ResourceType.Gold);
		return (1f + (num2 - (float)num) / (float)num) * factor.def.field.GetFloat("multiplier", pc);
	}

	public static float pc_our_dungeon_is_crowded(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.our_kingdom.prisoners.Count >= factor.def.field.GetInt("minimum_prisoners", pc))
		{
			result = pc.our_kingdom.prisoners.Count * factor.def.field.GetInt("per_prisoner", pc);
		}
		return result;
	}

	public static float pc_their_dungeon_is_crowded(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		if (pc.their_kingdom.prisoners.Count >= factor.def.field.GetInt("minimum_prisoners", pc))
		{
			result = pc.their_kingdom.prisoners.Count * factor.def.field.GetInt("per_prisoner", pc);
		}
		return result;
	}

	public static float pc_we_have_too_many_captured_knights(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		int num = 0;
		for (int i = 0; i < pc.our_kingdom.court.Count; i++)
		{
			if (pc.our_kingdom.court[i] != null && pc.our_kingdom.court[i].prison_kingdom != null)
			{
				num++;
			}
		}
		if (num >= factor.def.field.GetInt("minimum_prisoners", pc))
		{
			result = num * factor.def.field.GetInt("per_prisoner", pc);
		}
		return result;
	}

	public static float pc_they_have_too_many_captured_knights(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float result = 0f;
		int num = 0;
		for (int i = 0; i < pc.their_kingdom.court.Count; i++)
		{
			if (pc.their_kingdom.court[i] != null && pc.their_kingdom.court[i].prison_kingdom != null)
			{
				num++;
			}
		}
		if (num >= factor.def.field.GetInt("minimum_prisoners", pc))
		{
			result = num * factor.def.field.GetInt("per_prisoner", pc);
		}
		return result;
	}

	public static float pc_we_are_wining(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		War war = pc.our_kingdom.FindWarWith(pc.their_kingdom);
		if (war == null)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("max", pc) * war.CalcVictoryExpectation(pc.our_kingdom);
	}

	public static float pc_we_are_losing(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		War war = pc.our_kingdom.FindWarWith(pc.their_kingdom);
		if (war == null)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("max", pc) * war.CalcDefeatExpectation(pc.our_kingdom);
	}

	public static float pc_we_have_long_nap(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.rel.stance.IsNonAgression())
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("minimal_minutes", pc) * 60f;
		float num2 = factor.def.field.GetFloat("max_minutes", pc) * 60f;
		float num3 = pc.game.time - pc.rel.nap_time;
		if (num3 < 0f)
		{
			return 0f;
		}
		if (num3 < num)
		{
			return 0f;
		}
		if (num3 > num2)
		{
			return factor.def.field.GetFloat("mapped_amount", pc);
		}
		return (num3 - num) / (num2 - num) * factor.def.field.GetFloat("mapped_amount", pc);
	}

	public static float pc_alliance_just_started(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.rel.stance.IsAlliance())
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("max_minutes", pc) * 60f;
		float num2 = pc.game.time - pc.rel.alliance_time;
		if (num2 < 0f)
		{
			return 0f;
		}
		if (num2 > num)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_are_active_against_our_enemies(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		int num = 0;
		int num2 = 0;
		float num3 = factor.def.field.GetFloat("max_minutes", pc) * 60f;
		for (int i = 0; i < pc.our_kingdom.wars.Count; i++)
		{
			War war = pc.our_kingdom.wars[i];
			if (war == null)
			{
				continue;
			}
			List<Kingdom> enemies = war.GetEnemies(pc.our_kingdom);
			for (int j = 0; j < enemies.Count; j++)
			{
				Kingdom k = enemies[j];
				War war2 = pc.their_kingdom.FindWarWith(k);
				if (war2 != null)
				{
					num++;
					if (pc.game.time - war2.lastActivities[pc.their_kingdom] < num3)
					{
						num2++;
					}
				}
			}
		}
		if (num == 0)
		{
			return 0f;
		}
		if (100f * (float)num2 / (float)num >= factor.def.field.GetFloat("percent_active", pc))
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_are_passive_against_our_enemies(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		int num = 0;
		int num2 = 0;
		float num3 = factor.def.field.GetFloat("max_minutes", pc) * 60f;
		for (int i = 0; i < pc.our_kingdom.wars.Count; i++)
		{
			War war = pc.our_kingdom.wars[i];
			if (war == null)
			{
				continue;
			}
			List<Kingdom> enemies = war.GetEnemies(pc.our_kingdom);
			for (int j = 0; j < enemies.Count; j++)
			{
				Kingdom k = enemies[j];
				War war2 = pc.their_kingdom.FindWarWith(k);
				if (war2 != null)
				{
					num++;
					if (pc.game.time - war2.lastActivities[pc.their_kingdom] < num3)
					{
						num2++;
					}
				}
			}
		}
		if (num == 0)
		{
			return 0f;
		}
		if (100f * (float)(1 - num2 / num) >= factor.def.field.GetFloat("percent_passive", pc))
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_have_many_trade_centers(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("realms_per_trade_center", pc);
		int num2 = 0;
		foreach (Realm realm in pc.our_kingdom.realms)
		{
			if (realm.IsTradeCenter())
			{
				num2++;
			}
		}
		if (((float)pc.our_kingdom.realms.Count + num) / num < (float)num2)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_have_many_trade_centers(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("realms_per_trade_center", pc);
		int num2 = 0;
		foreach (Realm realm in pc.their_kingdom.realms)
		{
			if (realm.IsTradeCenter())
			{
				num2++;
			}
		}
		if (((float)pc.their_kingdom.realms.Count + num) / num < (float)num2)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_vassal_annex_pc_bonus(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		return pc.our_kingdom.GetStat(Stats.ks_vassal_annex_pc_bonus);
	}

	public static float pc_they_hold_our_holy_cities_and_we_are_Sunni(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_sunni)
		{
			return 0f;
		}
		if (pc.their_kingdom.is_sunni)
		{
			return 0f;
		}
		int num = 0;
		foreach (Realm holy_lands_realm in pc.their_kingdom.game.religions.sunni.holy_lands_realms)
		{
			if (pc.their_kingdom.realms.Contains(holy_lands_realm))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return 0f;
		}
		if (pc.their_kingdom.is_shia)
		{
			return factor.def.field.GetFloat("shia_control", pc) * (float)num;
		}
		return factor.def.field.GetFloat("other_religion_control", pc) * (float)num;
	}

	public static float pc_they_hold_Baghdad_and_we_are_Shia(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_shia)
		{
			return 0f;
		}
		if (pc.their_kingdom.is_shia)
		{
			return 0f;
		}
		if (pc.their_kingdom.realms.Contains(pc.their_kingdom.game.religions.shia.holy_lands_realm))
		{
			if (pc.their_kingdom.is_sunni)
			{
				return factor.def.field.GetFloat("sunni_control", pc);
			}
			return factor.def.field.GetFloat("other_religion_control", pc);
		}
		return 0f;
	}

	public static float pc_they_are_player_and_game_just_began(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.is_player)
		{
			return 0f;
		}
		if (!pc.their_kingdom.is_player)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("value", pc);
		float num2 = factor.def.field.GetFloat("timeout_after", pc);
		float num3 = (float)pc.our_kingdom.game.session_time.milliseconds / (num2 * 1000f);
		if (num3 >= 1f)
		{
			return 0f;
		}
		if (factor.def.field.GetBool("linear", pc))
		{
			return num * (1f - num3);
		}
		return num;
	}

	public static float pc_we_have_merchants_in_their_tc(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		int num = 0;
		List<Realm> realms = pc.their_kingdom.realms;
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (!realm.IsTradeCenter())
			{
				continue;
			}
			for (int j = 0; j < realm.merchants.Count; j++)
			{
				if (realm.merchants[j].kingdom_id == pc.our_kingdom.id)
				{
					num++;
				}
			}
		}
		float num2 = 0f;
		if (num > 0)
		{
			num2 += factor.def.field.GetFloat("base", pc) + (float)num * factor.def.field.GetFloat("per_merchant", pc);
		}
		return num2;
	}

	public static float pc_our_influence_in_their_kingdom(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float influenceIn = pc.our_kingdom.GetInfluenceIn(pc.their_kingdom);
		float num = factor.def.field.GetFloat("max", pc);
		if (!pc.our_kingdom.is_player)
		{
			num *= 0.2f;
		}
		return influenceIn / 100f * num;
	}

	public static float pc_their_influence_in_our_kingdom(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float influenceIn = pc.their_kingdom.GetInfluenceIn(pc.our_kingdom);
		float num = factor.def.field.GetFloat("max", pc);
		if (!pc.their_kingdom.is_player)
		{
			num *= 0.2f;
		}
		return influenceIn / 100f * num;
	}

	public static float pc_ask_for_crusade_cooldown(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (Crusade.IsOnCooldown(pc.their_kingdom.game, pc.their_kingdom))
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_have_many_trade_agreements(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		int num = factor.def.field.GetInt("unpunished_agreements", pc);
		if (pc.their_kingdom.tradeAgreementsWith.Count <= num)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("penalty_per_excessive_agreement", pc) * (float)(pc.their_kingdom.tradeAgreementsWith.Count - num);
	}

	public static float pc_we_have_many_trade_agreements(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		int num = factor.def.field.GetInt("unpunished_agreements", pc);
		if (pc.our_kingdom.tradeAgreementsWith.Count <= num)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("penalty_per_excessive_agreement", pc) * (float)(pc.our_kingdom.tradeAgreementsWith.Count - num);
	}

	public static float pc_our_diplomat_is_negotiating_peace(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		PeaceTalksStatus peaceTalksStatus = null;
		Character character = null;
		for (int i = 0; i < pc.our_kingdom.court.Count; i++)
		{
			character = pc.our_kingdom.court[i];
			if (character != null)
			{
				peaceTalksStatus = character.FindStatus<PeaceTalksStatus>();
				if (peaceTalksStatus != null && peaceTalksStatus.kingdom == pc.their_kingdom)
				{
					break;
				}
				peaceTalksStatus = null;
			}
		}
		if (peaceTalksStatus == null || character == null)
		{
			return 0f;
		}
		float num = (float)Math.Floor((pc.game.time - peaceTalksStatus.time) / factor.def.field.GetFloat("tick", pc));
		float num2 = factor.def.field.GetFloat("per_tick", pc);
		float num3 = factor.def.field.GetFloat("level_mul", pc);
		float num4 = factor.def.field.GetFloat("base", pc);
		float num5 = character.GetClassLevel();
		float classLevelNormalized = character.GetClassLevelNormalized();
		return Math.Min(num * (num2 + classLevelNormalized), num4 + num5 * num3);
	}

	public static float pc_their_diplomat_is_negotiating_peace(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		PeaceTalksStatus peaceTalksStatus = null;
		Character character = null;
		for (int i = 0; i < pc.their_kingdom.court.Count; i++)
		{
			character = pc.their_kingdom.court[i];
			if (character != null)
			{
				peaceTalksStatus = character.FindStatus<PeaceTalksStatus>();
				if (peaceTalksStatus != null && peaceTalksStatus.kingdom == pc.our_kingdom)
				{
					break;
				}
				peaceTalksStatus = null;
			}
		}
		if (peaceTalksStatus == null || character == null)
		{
			return 0f;
		}
		float num = (float)Math.Floor((pc.game.time - peaceTalksStatus.time) / factor.def.field.GetFloat("tick", pc));
		float num2 = factor.def.field.GetFloat("per_tick", pc);
		float num3 = factor.def.field.GetFloat("level_mul", pc);
		float num4 = factor.def.field.GetFloat("base", pc);
		float num5 = character.GetClassLevel();
		float classLevelNormalized = character.GetClassLevelNormalized();
		return Math.Min(num * (num2 + classLevelNormalized), num4 + num5 * num3);
	}

	public static float pc_their_diplomat_skill_level(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Character character = null;
		float num = factor.def.field.GetFloat("max", pc);
		float num2 = factor.def.field.GetFloat("base", pc);
		for (int i = 0; i < pc.their_kingdom.court.Count; i++)
		{
			character = pc.their_kingdom.court[i];
			if (character != null && character.IsDiplomat())
			{
				Kingdom mission_kingdom = character.mission_kingdom;
				if (mission_kingdom != null && mission_kingdom == pc.our_kingdom)
				{
					return num2 + character.GetClassLevelNormalized() * num;
				}
			}
		}
		return 0f;
	}

	public static float pc_we_have_right_to_expand(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("max", pc);
		float vmax = factor.def.field.GetFloat("time", pc);
		float num2 = factor.def.field.GetFloat("pow", pc, 1f);
		float num3 = factor.def.field.GetFloat("ai_mul", pc, 1f);
		float num4 = 0f;
		num4 = Game.map_clamp(pc.their_kingdom.TimeInPeace(), 0f, vmax, 0f, 1f);
		num4 = num * (float)Math.Pow(num4, num2);
		if (!pc.their_kingdom.is_player)
		{
			num4 *= num3;
		}
		return num4;
	}

	public static float pc_they_are_evil_empire(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float vmin = factor.def.field.GetFloat("min", pc);
		float vmax = factor.def.field.GetFloat("max", pc);
		float num = factor.def.field.GetFloat("ai_mul", pc, 1f);
		float num2 = factor.def.field.GetFloat("points", pc, 30f);
		int count = pc.their_kingdom.realms.Count;
		float num3 = num2 * Game.map_clamp(count, vmin, vmax, 0f, 1f);
		if (!pc.their_kingdom.is_player)
		{
			num3 *= num;
		}
		return num3;
	}

	public static float pc_their_target_is_evil_empire(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float vmin = factor.def.field.GetFloat("min", pc);
		float vmax = factor.def.field.GetFloat("max", pc);
		float num = factor.def.field.GetFloat("ai_mul", pc, 1f);
		float num2 = factor.def.field.GetFloat("points", pc, 30f);
		War arg = pc.offer.GetArg<War>(0);
		if (arg.GetEnemies(pc.their_kingdom) == null)
		{
			return 0f;
		}
		int side = arg.GetSide(pc.their_kingdom);
		int side2 = arg.EnemySide(side);
		Kingdom leader = arg.GetLeader(side2);
		int count = leader.realms.Count;
		float num3 = num2 * Game.map_clamp(count, vmin, vmax, 0f, 1f);
		if (!leader.is_player)
		{
			num3 *= num;
		}
		return num3;
	}

	public static float pc_they_plan_invasion_against_us(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Pact pact = Pact.Find(Pact.Type.Offensive, pc.their_kingdom, pc.our_kingdom);
		if (pact == null)
		{
			return 0f;
		}
		if (pact.leader != pc.their_kingdom)
		{
			return 0f;
		}
		float val = factor.def.field.GetFloat("max", pc);
		float num = factor.def.field.GetFloat("per_ally_province", pc);
		int num2 = 0;
		for (int i = 0; i < pact.members.Count; i++)
		{
			if (pact.members[i] != pc.their_kingdom)
			{
				num2 += pact.members[i].realms.Count;
			}
		}
		return Math.Max((float)num2 * num, val);
	}

	public static float pc_we_plan_invasion_and_allies_are_ready(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Pact pact = Pact.Find(Pact.Type.Offensive, pc.our_kingdom, pc.their_kingdom);
		if (pact == null)
		{
			return 0f;
		}
		if (pact.leader != pc.our_kingdom)
		{
			return 0f;
		}
		float val = factor.def.field.GetFloat("max", pc);
		float num = factor.def.field.GetFloat("per_ally_province", pc);
		float num2 = factor.def.field.GetFloat("per_ally_war", pc);
		float num3 = factor.def.field.GetFloat("per_ally", pc);
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		float num7 = 0f;
		Pact defensive_pact = null;
		Pact offensive_pact = null;
		if (!War.PredictStartMembers(pc.our_kingdom, pc.their_kingdom, out defensive_pact, out offensive_pact, cache_attackers, cache_defenders))
		{
			return 0f;
		}
		for (int i = 0; i < pact.members.Count; i++)
		{
			if (pact.members[i] != pc.our_kingdom && cache_attackers.Contains(pact.members[i]))
			{
				num6++;
				num4 += pact.members[i].realms.Count;
				num5 += pact.members[i].wars.Count;
			}
		}
		num7 = (float)num6 * num3 + (float)num4 * num + (float)num5 * num2;
		if (num7 <= 0f)
		{
			return 0f;
		}
		return Math.Max(num7, val);
	}

	public static float pc_their_defensive_pact_against_us(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Pact pact = Pact.Find(Pact.Type.Defensive, pc.their_kingdom, pc.our_kingdom);
		if (pact == null)
		{
			return 0f;
		}
		float val = factor.def.field.GetFloat("max", pc);
		float num = factor.def.field.GetFloat("per_ally_province", pc);
		int num2 = 0;
		for (int i = 0; i < pact.members.Count; i++)
		{
			if (pact.members[i] != pc.their_kingdom)
			{
				num2 += pact.members[i].realms.Count;
			}
		}
		return Math.Max((float)num2 * num, val);
	}

	public static float pc_their_vassals(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.vassalStates.Count <= 0)
		{
			return 0f;
		}
		float val = factor.def.field.GetFloat("max", pc);
		float num = factor.def.field.GetFloat("per_march", pc);
		float num2 = factor.def.field.GetFloat("per_march_province", pc);
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < pc.their_kingdom.vassalStates.Count; i++)
		{
			Kingdom kingdom = pc.their_kingdom.vassalStates[i];
			if (kingdom != pc.their_kingdom && kingdom.vassalage.def.type == Vassalage.Type.March)
			{
				num3++;
				num4 += kingdom.realms.Count;
			}
		}
		return Math.Max((float)num3 * num + (float)num4 * num2, val);
	}

	public static float pc_their_sovereign(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Kingdom sovereignState = pc.their_kingdom.sovereignState;
		if (sovereignState == null)
		{
			return 0f;
		}
		float val = factor.def.field.GetFloat("max", pc);
		float num = factor.def.field.GetFloat("scuttage_factor", pc);
		float num2 = factor.def.field.GetFloat("per_ally", pc);
		float num3 = factor.def.field.GetFloat("per_province", pc);
		int num4 = 1;
		int num5 = 0;
		float num6 = 0f;
		num5 += sovereignState.realms.Count;
		for (int i = 0; i < sovereignState.vassalStates.Count; i++)
		{
			Kingdom kingdom = sovereignState.vassalStates[i];
			if (kingdom != pc.their_kingdom && kingdom.vassalage.def.type == Vassalage.Type.March)
			{
				num4++;
				num5 += kingdom.realms.Count;
			}
		}
		num6 = Math.Max((float)num4 * num2 + (float)num5 * num3, val);
		if (pc.their_kingdom.vassalage != null && pc.their_kingdom.vassalage.def.type == Vassalage.Type.Scuttage)
		{
			num6 *= num;
		}
		return num6;
	}

	public static float pc_we_are_leader_and_they_are_big_enemy_supporter(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Kingdom kingdom = pc.our_kingdom;
		Kingdom kingdom2 = pc.their_kingdom;
		War war = kingdom.FindWarWith(kingdom2);
		if (war == null)
		{
			return 0f;
		}
		if (war.GetAllies(kingdom)[0] == kingdom)
		{
			return 0f;
		}
		if (war.GetEnemies(kingdom)[0] == kingdom2)
		{
			return 0f;
		}
		float val = factor.def.field.GetFloat("max", pc);
		return Math.Max(factor.def.field.GetFloat("per_province", pc) * (float)kingdom2.realms.Count, val);
	}

	public static float pc_we_hate_our_alliance_leader(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		Kingdom kingdom = pc.our_kingdom;
		Kingdom k = pc.their_kingdom;
		War war = kingdom.FindWarWith(k);
		if (war == null)
		{
			return 0f;
		}
		List<Kingdom> allies = war.GetAllies(kingdom);
		if (allies[0] == kingdom)
		{
			return 0f;
		}
		float relationship = kingdom.GetRelationship(allies[0]);
		if (relationship > 0f)
		{
			return 0f;
		}
		float rmax = factor.def.field.GetFloat("max", pc);
		return Game.map_clamp(relationship * -1f, 0f, RelationUtils.Def.maxRelationship, 0f, rmax);
	}

	public static float pc_we_love_our_alliance_leader(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		Kingdom kingdom = pc.our_kingdom;
		Kingdom k = pc.their_kingdom;
		War war = kingdom.FindWarWith(k);
		if (war == null)
		{
			return 0f;
		}
		List<Kingdom> allies = war.GetAllies(kingdom);
		if (allies[0] == pc.our_kingdom)
		{
			return 0f;
		}
		float relationship = kingdom.GetRelationship(allies[0]);
		if (relationship < 0f)
		{
			return 0f;
		}
		float rmax = factor.def.field.GetFloat("max", pc);
		return Game.map_clamp(relationship, 0f, RelationUtils.Def.maxRelationship, 0f, rmax);
	}

	public static float pc_we_are_losing_war_against_target(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		return 1f - arg.CalcDefeatExpectation(pc.our_kingdom);
	}

	public static float pc_we_are_winning_war_against_target(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		return offer.GetArg<War>(0).CalcVictoryExpectation(pc.our_kingdom);
	}

	public static float pc_they_are_winning_war_against_target(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		return offer.GetArg<War>(0).CalcVictoryExpectation(pc.their_kingdom);
	}

	public static float pc_they_are_losing_war_against_target(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		return offer.GetArg<War>(0).CalcDefeatExpectation(pc.their_kingdom);
	}

	public static float pc_our_side_in_war_is_full(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		float num = factor.def.field.GetFloat("max_supporters", pc);
		List<Kingdom> allies = arg.GetAllies(pc.our_kingdom);
		if (allies == null)
		{
			return 0f;
		}
		if ((float)(allies.Count - 1) >= num)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_target_side_in_war_is_full(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		float num = factor.def.field.GetFloat("max_supporters", pc);
		List<Kingdom> enemies = arg.GetEnemies(pc.their_kingdom);
		if (enemies == null)
		{
			return 0f;
		}
		if ((float)(enemies.Count - 1) >= num)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_their_side_in_war_is_full(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		float num = factor.def.field.GetFloat("max_supporters", pc);
		List<Kingdom> allies = arg.GetAllies(pc.their_kingdom);
		if (allies == null)
		{
			return 0f;
		}
		if ((float)(allies.Count - 1) >= num)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_our_stances_with_target_side(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		float num = factor.def.field.GetFloat("nap", pc);
		float num2 = factor.def.field.GetFloat("vassal", pc);
		float num3 = factor.def.field.GetFloat("liege", pc);
		float num4 = factor.def.field.GetFloat("trade", pc);
		float num5 = factor.def.field.GetFloat("family", pc);
		float num6 = factor.def.field.GetFloat("target_mul", pc);
		float num7 = 0f;
		Kingdom kingdom = pc.our_kingdom;
		List<Kingdom> enemies = arg.GetEnemies(pc.their_kingdom);
		if (enemies == null)
		{
			return num7;
		}
		int side = arg.GetSide(pc.their_kingdom);
		Kingdom leader = arg.GetLeader(arg.EnemySide(side));
		for (int i = 0; i < enemies.Count; i++)
		{
			KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(pc.our_kingdom, enemies[i]);
			float num8 = 0f;
			if (kingdomAndKingdomRelation.stance.IsNonAgression())
			{
				num8 += num;
			}
			if (kingdom.sovereignState == enemies[i])
			{
				num8 += num3;
			}
			if (enemies[i].sovereignState == kingdom)
			{
				num8 += num2;
			}
			if (kingdomAndKingdomRelation.stance.IsTrade())
			{
				num8 += num4;
			}
			if (kingdomAndKingdomRelation.stance.IsMarriage())
			{
				num8 += num5;
			}
			if (leader == enemies[i])
			{
				num8 *= num6;
			}
			num7 += num8;
		}
		return num7;
	}

	public static float pc_our_relation_with_target_side(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		float num = factor.def.field.GetFloat("rel_hated", pc);
		float num2 = factor.def.field.GetFloat("rel_loved", pc);
		float rmin = factor.def.field.GetFloat("per_hated", pc);
		float rmax = factor.def.field.GetFloat("per_loved", pc);
		float num3 = factor.def.field.GetFloat("target_mul", pc);
		float num4 = 0f;
		_ = pc.our_kingdom;
		List<Kingdom> enemies = arg.GetEnemies(pc.their_kingdom);
		if (enemies == null)
		{
			return 0f;
		}
		int side = arg.GetSide(pc.their_kingdom);
		int side2 = arg.EnemySide(side);
		Kingdom leader = arg.GetLeader(side2);
		for (int i = 0; i < enemies.Count; i++)
		{
			float relationship = pc.our_kingdom.GetRelationship(enemies[i]);
			float num5 = 0f;
			if (relationship <= num)
			{
				num5 += Game.map_clamp(relationship, -1000f, num, rmin, 0f);
			}
			if (relationship >= num2)
			{
				num5 += Game.map_clamp(relationship, num2, 1000f, 0f, rmax);
			}
			if (enemies[i] == leader)
			{
				num5 *= num3;
			}
			num4 += num5;
		}
		if (num4 < 0f)
		{
			num4 = 0f;
		}
		return num4;
	}

	public static float pc_we_have_supporters(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War war = pc.their_kingdom.FindWarWith(pc.our_kingdom);
		if (war == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("per_supporter", pc);
		List<Kingdom> allies = war.GetAllies(pc.our_kingdom);
		if (allies == null)
		{
			return 0f;
		}
		return num * (float)(allies.Count - 1);
	}

	public static float pc_they_have_supporters(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War war = pc.their_kingdom.FindWarWith(pc.our_kingdom);
		if (war == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("per_supporter", pc);
		List<Kingdom> allies = war.GetAllies(pc.their_kingdom);
		if (allies == null)
		{
			return 0f;
		}
		return num * (float)(allies.Count - 1);
	}

	public static float pc_their_target_has_supporters(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		List<Kingdom> enemies = offer.GetArg<War>(0).GetEnemies(pc.their_kingdom);
		if (enemies == null)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("per_supporter", pc) * (float)(enemies.Count - 1);
	}

	public static float pc_they_are_not_target_neighbor(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		int side = arg.GetSide(pc.our_kingdom);
		Kingdom enemyLeader = arg.GetEnemyLeader(side);
		if (pc.their_kingdom.neighbors.Contains(enemyLeader))
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_not_neighbor_to_any_opposing_kingdom_of_their_war(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (pc.our_kingdom?.neighbors == null || pc.their_kingdom == null || offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		if (arg == null)
		{
			return 0f;
		}
		List<Kingdom> enemies = arg.GetEnemies(pc.their_kingdom);
		if (enemies == null)
		{
			return 0f;
		}
		foreach (Kingdom item in enemies)
		{
			if (pc.our_kingdom.neighbors.Contains(item))
			{
				return 0f;
			}
		}
		return 1f;
	}

	public static float pc_they_are_target_neighbor(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		int side = arg.GetSide(pc.our_kingdom);
		Kingdom enemyLeader = arg.GetEnemyLeader(side);
		if (pc.their_kingdom.neighbors.Contains(enemyLeader))
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_are_target_neighbor(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		int side = arg.GetSide(pc.their_kingdom);
		Kingdom enemyLeader = arg.GetEnemyLeader(side);
		if (pc.our_kingdom.neighbors.Contains(enemyLeader))
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_agree_to_support(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		if (arg.GetEnemyLeader(arg.GetSide(pc.their_kingdom)) == null)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_nap_recently_broken_because_king_died(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.rel.nap_broken_dead_king_kingdom == null)
		{
			return 0f;
		}
		if (pc.rel.nap_broken_king_death_time + factor.def.field.GetFloat("max_time", pc) > pc.our_kingdom.game.time)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_nap_not_recently_broken_because_king_died(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.rel.nap_broken_dead_king_kingdom == null)
		{
			return 1f;
		}
		if (pc.rel.nap_broken_king_death_time + factor.def.field.GetFloat("max_time", pc) < pc.our_kingdom.game.time)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_lead_this_jihad_and_they_are_muslim(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.IsCaliphate())
		{
			return 0f;
		}
		if (!pc.their_kingdom.is_muslim)
		{
			return 0f;
		}
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count < 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		if (arg == null || !arg.IsJihad())
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_muslim_and_they_want_us_to_support_jihad(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_muslim)
		{
			return 0f;
		}
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count < 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		if (arg == null || !arg.IsJihad() || !arg.GetLeader(pc.their_kingdom).IsCaliphate())
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_muslim_and_they_want_us_to_fight_against_jihad(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_muslim)
		{
			return 0f;
		}
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count < 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		if (arg == null || !arg.IsJihad() || arg.GetLeader(pc.their_kingdom).IsCaliphate())
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_both_christians_and_they_want_us_to_fight_against_jihad(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_christian || !pc.their_kingdom.is_christian)
		{
			return 0f;
		}
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count < 1)
		{
			return 0f;
		}
		War arg = offer.GetArg<War>(0);
		Kingdom kingdom = arg?.GetEnemyLeader(pc.their_kingdom);
		if (kingdom == null || !kingdom.IsCaliphate() || !arg.IsJihad())
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_muslim_supporting_jihad_and_they_fight_in_the_same_jihad(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_muslim)
		{
			return 0f;
		}
		if (pc.our_kingdom.wars.Find((War j) => j.IsJihad() && j != pc.our_kingdom.jihad && j.GetLeader(pc.our_kingdom).IsCaliphate() && (j.attackers.Contains(pc.their_kingdom) || j.defenders.Contains(pc.their_kingdom))) == null)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_christian_and_they_lead_the_jihad_we_are_both_in(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_christian)
		{
			return 0f;
		}
		War jihad = pc.their_kingdom.jihad;
		if (jihad == null || (!jihad.attackers.Contains(pc.their_kingdom) && !jihad.defenders.Contains(pc.their_kingdom)))
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_lead_jihad_and_they_are_not_muslim(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.IsCaliphate() || pc.our_kingdom.jihad == null || pc.their_kingdom.is_muslim)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_both_christians_and_target_is_caliphate(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_christian || !pc.their_kingdom.is_christian)
		{
			return 0f;
		}
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count < 1)
		{
			return 0f;
		}
		Kingdom arg = offer.GetArg<Kingdom>(0);
		if (arg == null || !arg.IsCaliphate())
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_both_caliphates(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.IsCaliphate() || !pc.their_kingdom.IsCaliphate())
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_are_caliphate_and_we_are_muslim(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.our_kingdom.is_muslim || !pc.their_kingdom.IsCaliphate())
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_we_are_at_war(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.IsEnemy(pc.their_kingdom))
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_we_produce_that_gold_slowly(ProsAndCons pc, Factor factor)
	{
		Offer offer = pc.offer;
		int num = factor.def.field.GetInt("offer_arg_index", pc);
		if (offer == null || offer.args == null || num < 0 || num >= offer.args.Count || pc.our_kingdom == null)
		{
			return 0f;
		}
		float num2 = factor.def.field.GetInt("per_perc_ratio", pc);
		int num3 = offer.GetArg(num);
		float num4 = pc.our_kingdom.income.Get(ResourceType.Gold) - pc.our_kingdom.expenses.Get(ResourceType.Gold);
		if (num4 < 1f)
		{
			return factor.def.field.GetInt("max", pc);
		}
		return (float)num3 / num4 * 100f / num2;
	}

	public static float pc_our_king_is_in_other_prison(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Character king = pc.our_kingdom.GetKing();
		List<Character> prisoners = pc.their_kingdom.prisoners;
		if (prisoners == null || king == null)
		{
			return 0f;
		}
		if (!king.IsPrisoner())
		{
			return 0f;
		}
		if (prisoners.Contains(king))
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_hold_our_king(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Character king = pc.our_kingdom.GetKing();
		List<Character> prisoners = pc.their_kingdom.prisoners;
		if (prisoners == null || king == null)
		{
			return 0f;
		}
		for (int i = 0; i < prisoners.Count; i++)
		{
			if (prisoners[i] == king)
			{
				return 1f;
			}
		}
		return 0f;
	}

	public static float pc_they_hold_our_prince(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		List<Character> list = pc.our_kingdom.royalFamily?.Children;
		List<Character> prisoners = pc.their_kingdom.prisoners;
		if (prisoners == null || list == null)
		{
			return 0f;
		}
		for (int i = 0; i < prisoners.Count; i++)
		{
			for (int j = 0; j < list.Count; j++)
			{
				if (prisoners[i] == list[j])
				{
					return 1f;
				}
			}
		}
		return 0f;
	}

	public static float pc_we_will_hate_them_if_we_give_them_gold(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count != 1)
		{
			return 0f;
		}
		float valueOfModifier = KingdomAndKingdomRelation.GetValueOfModifier(pc.game, "rel_demand_gold", offer);
		float num = pc.our_kingdom.GetRelationship(pc.their_kingdom) + valueOfModifier;
		if (num >= 0f)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("result_mult", pc) * (0f - num) / RelationUtils.Def.maxRelationship;
	}

	public static float pc_our_trade_level(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Character merchantIn = pc.our_kingdom.GetMerchantIn(pc.their_kingdom);
		if (merchantIn == null || merchantIn.mission_kingdom != pc.their_kingdom)
		{
			return 0f;
		}
		float rmax = factor.def.field.GetFloat("max", pc);
		return pc.game.Map(merchantIn.trade_level, merchantIn.GetMinTradeLevel(), merchantIn.GetMaxTradeLevel(), 0f, rmax);
	}

	public static float pc_their_trade_level(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Character merchantIn = pc.their_kingdom.GetMerchantIn(pc.our_kingdom);
		if (merchantIn == null || merchantIn.mission_kingdom != pc.our_kingdom)
		{
			return 0f;
		}
		float rmax = factor.def.field.GetFloat("max", pc);
		return pc.game.Map(merchantIn.trade_level, merchantIn.GetMinTradeLevel(), merchantIn.GetMaxTradeLevel(), 0f, rmax);
	}

	public static float pc_they_have_too_many_defensive_pacts_against_them(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		float num = factor.def.field.GetFloat("pacts_num", pc);
		int num2 = 0;
		foreach (Pact item in pc.their_kingdom.pacts_against)
		{
			if (item.type == Pact.Type.Defensive)
			{
				num2++;
			}
			if ((float)num2 >= num)
			{
				return 1f;
			}
		}
		return 0f;
	}

	public static float pc_they_want_princess_from_marriage(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count < 2)
		{
			return 0f;
		}
		Character character = offer.GetArg(0).obj_val as Character;
		if (!character.IsPrincess())
		{
			character = offer.GetArg(1).obj_val as Character;
		}
		if (!character.IsPrincess())
		{
			return 0f;
		}
		if (character.GetKingdom() != pc.our_kingdom)
		{
			return 0f;
		}
		float num = 0f;
		if (pc.their_kingdom.is_player)
		{
			return factor.def.field.GetFloat("they_are_player", pc);
		}
		return factor.def.field.GetFloat("they_are_ai", pc);
	}

	public static float pc_they_want_princess_in_marriage_while_our_king_is_venerable(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.royalFamily.Sovereign == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.royalFamily.Sovereign.age != Character.Age.Venerable)
		{
			return 0f;
		}
		Offer offer = pc.offer;
		if (offer == null || offer.args == null || offer.args.Count < 2)
		{
			return 0f;
		}
		Character character = offer.GetArg(0).obj_val as Character;
		if (!character.IsPrincess())
		{
			character = offer.GetArg(1).obj_val as Character;
		}
		if (!character.IsPrincess())
		{
			return 0f;
		}
		if (character.GetKingdom() != pc.our_kingdom)
		{
			return 0f;
		}
		float num = 0f;
		if (pc.their_kingdom.is_player)
		{
			return factor.def.field.GetFloat("they_are_player", pc);
		}
		return factor.def.field.GetFloat("they_are_ai", pc);
	}

	public static float pc_we_have_high_taxes(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("tax_level_" + pc.our_kingdom.taxLevel, pc);
	}

	public static float pc_they_have_high_taxes(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		return factor.def.field.GetFloat("tax_level_" + pc.their_kingdom.taxLevel, pc);
	}

	public static float pc_they_lead_a_crusade(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc?.game?.religions?.catholic?.crusade?.leader?.GetKingdom() == pc.their_kingdom)
		{
			return 1f;
		}
		return 0f;
	}

	public static float pc_they_recently_disobeyed_papacy(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		GameRule gameRule = ObjRules.Get(pc.their_kingdom, create: false)?.Find("RecentlyDisobeyedPapacyRule");
		if (gameRule == null)
		{
			return 0f;
		}
		if (gameRule.start_time == Time.Zero)
		{
			return 0f;
		}
		float timeout = gameRule.def.timeout;
		float num = Game.clamp(pc.game.time - gameRule.start_time, 0f, timeout);
		float num2 = factor.def.field.GetFloat("max", pc);
		float num3 = factor.def.field.GetFloat("pow", pc);
		float num4 = (1f - (float)Math.Pow(num / timeout, num3)) * num2;
		if (num4 < 0f)
		{
			num4 = 0f;
		}
		return num4;
	}

	public static float pc_they_recently_aided_papacy(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		GameRule gameRule = ObjRules.Get(pc.their_kingdom, create: false)?.Find("RecentlyAidedPapacyRule");
		if (gameRule == null)
		{
			return 0f;
		}
		if (gameRule.start_time == Time.Zero)
		{
			return 0f;
		}
		float timeout = gameRule.def.timeout;
		float num = Game.clamp(pc.game.time - gameRule.start_time, 0f, timeout);
		float num2 = factor.def.field.GetFloat("max", pc);
		float num3 = factor.def.field.GetFloat("pow", pc);
		float num4 = (1f - (float)Math.Pow(num / timeout, num3)) * num2;
		if (num4 < 0f)
		{
			num4 = 0f;
		}
		return num4;
	}

	public static float pc_they_are_player(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.their_kingdom.is_player)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_are_player_by_difficulty(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (!pc.their_kingdom.is_player)
		{
			return 0f;
		}
		int ai_difficulty = pc.game.rules.ai_difficulty;
		float result = 0f;
		switch (ai_difficulty)
		{
		case 0:
			result = factor.def.field.GetFloat("easy", pc);
			break;
		case 1:
			result = factor.def.field.GetFloat("normal", pc);
			break;
		case 2:
			result = factor.def.field.GetFloat("hard", pc);
			break;
		case 3:
			result = factor.def.field.GetFloat("very_hard", pc);
			break;
		}
		return result;
	}

	public static float pc_they_are_Germany_and_we_are_their_vassals(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.sovereignState != pc.their_kingdom)
		{
			return 0f;
		}
		if (pc.their_kingdom.Name != "Germany")
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_our_liege_have_many_vassals(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.our_kingdom.sovereignState != pc.their_kingdom)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_groom_not_heir_nor_king_and_both_kindoms_are_AI(ProsAndCons pc, Factor factor)
	{
		if (!(pc.offer is MarriageOffer { args: not null } marriageOffer) || marriageOffer.args.Count < 2)
		{
			return 0f;
		}
		if (pc.our_kingdom == null || pc.our_kingdom.is_player || pc.their_kingdom == null || pc.their_kingdom.is_player)
		{
			return 0f;
		}
		Character character = marriageOffer.GetArg(1).obj_val as Character;
		if (character == null || character.sex != Character.Sex.Male)
		{
			character = marriageOffer.GetArg(1).obj_val as Character;
		}
		if (character == null || character.sex != Character.Sex.Male)
		{
			return 0f;
		}
		Kingdom kingdom = character.GetKingdom();
		if (kingdom == null)
		{
			return 0f;
		}
		if (kingdom.royalFamily.Heir == character)
		{
			return 0f;
		}
		if (kingdom.royalFamily.Sovereign == character)
		{
			return 0f;
		}
		return 1f;
	}

	public static float pc_they_have_no_rebels_near_us(ProsAndCons pc, Factor factor)
	{
		if (pc.our_kingdom == null || pc.their_kingdom == null)
		{
			return 0f;
		}
		if (pc.their_kingdom.rebellions.Count == 0)
		{
			return 0f;
		}
		int num = factor.def.field.GetInt("distance", pc);
		for (int i = 0; i < pc.their_kingdom.rebellions.Count; i++)
		{
			Rebellion rebellion = pc.their_kingdom.rebellions[i];
			if (rebellion == null)
			{
				continue;
			}
			for (int j = 0; j < rebellion.rebels.Count; j++)
			{
				Rebel rebel = rebellion.rebels[j];
				if (rebel?.army?.realm_in != null && pc.game.KingdomAndRealmDistance(pc.our_kingdom.id, rebel.army.realm_in.id, num + 1) != -1)
				{
					return 0f;
				}
			}
		}
		return 1f;
	}
}
