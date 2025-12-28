using System;
using System.Collections.Generic;

namespace Logic;

public class CrownAuthority : Component, IVars
{
	public class Def : Logic.Def
	{
		public int max;

		public int min;

		public float[] requirementGoldBaseAmounts;

		public int[] requirementGoldRealmThresholds;

		public float[] requirementGoldRealmMods;

		public float[] requirementPietyAmounts;

		public float[] incomeMods;

		public float[] corruptionMods;

		public float ca_positive_stability_multiplier = 2f;

		public float ca_negative_stability_multiplier = 4f;

		public int[] rebellionIndependenceInitialIntervals;

		public int[] occupationRebelRiskMods;

		public float[] relationMods;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			List<int> list = new List<int>();
			List<float> list2 = new List<float>();
			min = field.GetInt("min");
			max = field.GetInt("max");
			DT.Field field2 = field.FindChild("requirementGoldBaseAmounts");
			for (int i = 0; i < field2.NumValues(); i++)
			{
				list2.Add(DT.Float(field2.Value(i)));
			}
			requirementGoldBaseAmounts = list2.ToArray();
			list2.Clear();
			field2 = field.FindChild("requirementGoldRealmThresholds");
			if (field2 != null)
			{
				for (int j = 0; j < field2.NumValues(); j++)
				{
					list.Add(DT.Int(field2.Value(j)));
				}
				requirementGoldRealmThresholds = list.ToArray();
				list.Clear();
			}
			field2 = field.FindChild("requirementGoldRealmMods");
			if (field2 != null)
			{
				for (int k = 0; k < field2.NumValues(); k++)
				{
					list2.Add(DT.Float(field2.Value(k)));
				}
				requirementGoldRealmMods = list2.ToArray();
				list2.Clear();
			}
			field2 = field.FindChild("requirementPietyAmounts");
			if (field2 != null)
			{
				for (int l = 0; l < field2.NumValues(); l++)
				{
					list2.Add(DT.Float(field2.Value(l)));
				}
				requirementPietyAmounts = list2.ToArray();
				list2.Clear();
			}
			field2 = field.FindChild("incomeMods");
			for (int m = 0; m < field2.NumValues(); m++)
			{
				list2.Add(DT.Float(field2.Value(m)));
			}
			incomeMods = list2.ToArray();
			list2.Clear();
			field2 = field.FindChild("corruptionMods");
			for (int n = 0; n < field2.NumValues(); n++)
			{
				list2.Add(DT.Float(field2.Value(n)));
			}
			corruptionMods = list2.ToArray();
			list2.Clear();
			ca_positive_stability_multiplier = field.GetFloat("ca_stability_multiplier", null, ca_positive_stability_multiplier);
			ca_negative_stability_multiplier = field.GetFloat("ca_negative_stability_multiplier", null, ca_negative_stability_multiplier);
			field2 = field.FindChild("rebellionIndependenceInitialTimeInterval");
			for (int num = 0; num < field2.NumValues(); num++)
			{
				list.Add(DT.Int(field2.Value(num)));
			}
			rebellionIndependenceInitialIntervals = list.ToArray();
			list.Clear();
			field2 = field.FindChild("occupationRebelRiskMods");
			for (int num2 = 0; num2 < field2.NumValues(); num2++)
			{
				list.Add(DT.Int(field2.Value(num2)));
			}
			occupationRebelRiskMods = list.ToArray();
			list.Clear();
			field2 = field.FindChild("relationMods");
			for (int num3 = 0; num3 < field2.NumValues(); num3++)
			{
				list2.Add(DT.Float(field2.Value(num3)));
			}
			relationMods = list2.ToArray();
			list2.Clear();
			return true;
		}
	}

	public Def def;

	public Kingdom kingdom;

	private int value;

	private float incomeModifier;

	private float corruptionModifier;

	private float relationModifier;

	public CrownAuthority(Kingdom k)
		: base(k)
	{
		def = base.game.defs.Get<Def>("CrownAuthority");
		kingdom = k;
		Refresh();
	}

	public int GetValue()
	{
		return value;
	}

	public float GetIncomeModifier()
	{
		return incomeModifier;
	}

	public float GetCorruptionModifier()
	{
		return corruptionModifier;
	}

	public int GetStabilityFromCA()
	{
		if (value > 0)
		{
			return (int)((float)value * def.ca_positive_stability_multiplier);
		}
		return (int)((float)value * def.ca_negative_stability_multiplier);
	}

	public int GetRebelIndependenceTimeInitialInterval()
	{
		return CalcRebelIndependenceTimeInitialInterval();
	}

	public int GetOccupationRebelRiskModifier()
	{
		return CalcOccupationRebelRiskValue();
	}

	public float GetRelationshipModifier()
	{
		return relationModifier;
	}

	public int Max()
	{
		float stat = kingdom.GetStat(Stats.ks_max_crown_authority);
		return def.max + (int)stat;
	}

	public int Min()
	{
		float stat = kingdom.GetStat(Stats.ks_min_crown_authority);
		return def.min + (int)stat;
	}

	public float GetFloatValue(float[] valueArray)
	{
		if (valueArray == null)
		{
			return 0f;
		}
		int num = value - def.min;
		if (num < 0)
		{
			return 0f;
		}
		if (num >= valueArray.Length)
		{
			return 0f;
		}
		return valueArray[num];
	}

	public int GetIntValue(int[] valueArray, int offset = 0)
	{
		if (valueArray == null)
		{
			return 0;
		}
		int num = value - def.min;
		if (num < 0)
		{
			return 0;
		}
		if (num >= valueArray.Length)
		{
			return 0;
		}
		num += offset;
		if (num < 0)
		{
			num = 0;
		}
		if (num >= valueArray.Length)
		{
			num = valueArray.Length - 1;
		}
		return valueArray[num];
	}

	private float CalcPietyAmount()
	{
		return GetFloatValue(def.requirementPietyAmounts);
	}

	private float CalcBaseGoldAmount()
	{
		return GetFloatValue(def.requirementGoldBaseAmounts);
	}

	private float CalcRealmGoldModifier(int realmsNum)
	{
		int num = 0;
		for (int i = 0; i < def.requirementGoldRealmThresholds.Length; i++)
		{
			if (realmsNum > DT.Int(def.requirementGoldRealmThresholds[i]))
			{
				num = i;
			}
		}
		return def.requirementGoldRealmMods[num];
	}

	private float CalcIncomeModifier()
	{
		return GetFloatValue(def.incomeMods);
	}

	private float CalcCorruptionModifier()
	{
		return GetFloatValue(def.corruptionMods);
	}

	private int CalcRebelIndependenceTimeInitialInterval()
	{
		return GetIntValue(def.rebellionIndependenceInitialIntervals);
	}

	private int CalcOccupationRebelRiskValue()
	{
		return GetIntValue(def.occupationRebelRiskMods);
	}

	private float CalcRelationValue()
	{
		return GetFloatValue(def.relationMods);
	}

	private float CalcRealmGoldAmount()
	{
		Kingdom obj = base.obj as Kingdom;
		int count = obj.realms.Count;
		float maxGoldCapacity = obj.GetMaxGoldCapacity();
		float num = CalcRealmGoldModifier(count);
		return maxGoldCapacity * num;
	}

	private void ChangeRelations(float amount)
	{
		_ = obj;
	}

	public Resource GetCost()
	{
		Resource resource = new Resource();
		if (value == Max())
		{
			return resource;
		}
		int count = kingdom.realms.Count;
		float num = CalcBaseGoldAmount();
		resource[ResourceType.Gold] = (float)Math.Truncate((double)num * Math.Sqrt(count) / 100.0 * (double)(1f + kingdom.GetStat(Stats.ks_CA_cost_perc) / 100f)) * 100f;
		resource[ResourceType.Piety] = CalcPietyAmount() * (1f + kingdom.GetStat(Stats.ks_CA_cost_tradition_perc) / 100f);
		return resource;
	}

	public void Refresh()
	{
		if ((obj as Kingdom).id != 0)
		{
			float num = relationModifier;
			incomeModifier = CalcIncomeModifier();
			corruptionModifier = CalcCorruptionModifier();
			relationModifier = CalcRelationValue();
			if (num != relationModifier)
			{
				ChangeRelations(relationModifier - num);
			}
		}
	}

	private void OnValueChangd(bool positiveChange, bool really_changed = true)
	{
		Kingdom kingdom = obj as Kingdom;
		kingdom.stability.SpecialEvent(!positiveChange);
		if (really_changed)
		{
			Refresh();
			kingdom.NotifyListeners("crown_authority_change", positiveChange);
		}
	}

	public void ChangeValue(int change)
	{
		if (base.obj.IsAuthority() && change != 0)
		{
			int num = value;
			bool positiveChange = change >= 0;
			int num2 = value;
			value += change;
			value = Math.Max(Min(), Math.Min(Max(), value));
			bool really_changed = num2 != value;
			Kingdom obj = base.obj as Kingdom;
			obj.SendState<Kingdom.CrownAuthorityState>();
			OnValueChangd(positiveChange, really_changed);
			obj.OnChangedAnalytics("crown_authority", num.ToString(), value.ToString());
		}
	}

	public void SetValue(int value, bool send_state = true)
	{
		value = Math.Max(Min(), Math.Min(Max(), value));
		if (value != this.value)
		{
			bool positiveChange = value > this.value;
			this.value = value;
			if (send_state)
			{
				(obj as Kingdom).SendState<Kingdom.CrownAuthorityState>();
			}
			OnValueChangd(positiveChange);
		}
	}

	public void AddModifier(string def_id, IVars vars = null)
	{
		int change = def.field.GetInt(def_id, vars);
		ChangeValue(change);
	}

	public bool IncreaseValueWithGold(bool send_state = true)
	{
		Kingdom kingdom = obj as Kingdom;
		Resource cost = GetCost();
		if (!kingdom.resources.CanAfford(cost, 1f))
		{
			return false;
		}
		CrownAuthority crownAuthority = kingdom.GetCrownAuthority();
		if (crownAuthority.value == crownAuthority.Max())
		{
			return false;
		}
		if (!obj.IsAuthority() && send_state)
		{
			(obj as Kingdom).SendEvent(new Kingdom.IncreaseCrownAuthorityEvent());
			return true;
		}
		kingdom.SubResources(KingdomAI.Expense.Category.Other, cost);
		AddModifier("payGold");
		if (send_state)
		{
			(obj as Kingdom).SendState<Kingdom.CrownAuthorityState>();
		}
		return true;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"cost" => GetCost(), 
			"maxed" => value >= Max(), 
			"kingdom" => obj as Kingdom, 
			"income_bonus" => GetIncomeModifier(), 
			"rebel_risk" => GetStabilityFromCA(), 
			"relationship_bonus" => GetRelationshipModifier(), 
			"corruption" => GetCorruptionModifier(), 
			_ => Value.Unknown, 
		};
	}
}
