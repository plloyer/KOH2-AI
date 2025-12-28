using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logic;

public class OfferGenerator : Object
{
	private delegate Offer CreateOfferDelegate(Offer.Def def, Object from, Object to);

	public static OfferGenerator instance;

	private List<string> diplomacyOffers = new List<string>();

	public Dictionary<int, List<int>> randomIndexLists = new Dictionary<int, List<int>>();

	public Dictionary<int, List<Value>> argPossibleValueLists = new Dictionary<int, List<Value>>();

	private Dictionary<string, CreateOfferDelegate> delegates = new Dictionary<string, CreateOfferDelegate>();

	private Defs.Registry reg;

	public OfferGenerator(Game game)
		: base(game)
	{
		randomIndexLists = new Dictionary<int, List<int>>();
		reg = game.defs.Get(typeof(Offer.Def));
		BuildDelegates();
		BuildIndexLists();
		CreateDebugLists();
	}

	private void BuildDelegates()
	{
		foreach (KeyValuePair<string, Def> def2 in reg.defs)
		{
			Offer.Def def = def2.Value as Offer.Def;
			Type type = Type.GetType("Logic." + def.field.key);
			if (!(type == null))
			{
				MethodInfo method = type.GetMethod("Create");
				if (!(method == null))
				{
					CreateOfferDelegate value = (CreateOfferDelegate)Delegate.CreateDelegate(typeof(CreateOfferDelegate), method);
					delegates.Add(def.field.key, value);
				}
			}
		}
	}

	private void BuildIndexLists()
	{
		foreach (KeyValuePair<string, Def> def2 in reg.defs)
		{
			Offer.Def def = def2.Value as Offer.Def;
			if (def.dt_def.field.based_on.key == "DiplomacyOffer" && def.field.GetBool("allow_random_generation") && !def.field.GetBool("disabled"))
			{
				diplomacyOffers.Add(def2.Key);
			}
		}
	}

	private void CreateDebugLists()
	{
		foreach (KeyValuePair<string, Def> def2 in reg.defs)
		{
			Offer.Def def = def2.Value as Offer.Def;
			Offer.dbg_offers_data[def.dt_def.field.key] = new DBGOffersData();
		}
	}

	public Offer TryGenerateRandomOfferLight(string treshold_name, Object from, Object to, Offer parent = null, int parentArgIndex = 0, bool forceParentArg = true, float evalToCoverMin = 0f, float evalToCoverMax = 0f, string rel_change_type = "neutral", int recursionDepth = 0)
	{
		return GenerateOffer(treshold_name, diplomacyOffers[game.Random(0, diplomacyOffers.Count)], from, to, parent, parentArgIndex, forceParentArg, evalToCoverMin, evalToCoverMax, rel_change_type, recursionDepth);
	}

	public Offer TryGenerateRandomOfferHeavy(string treshold_name, Object from, Object to, Offer parent = null, int parentArgIndex = 0, bool forceParentArg = false, float evalToCoverMin = 0f, float evalToCoverMax = 0f, string rel_change_type = "neutral", int recursionDepth = 0)
	{
		List<int> list = GenerateRandomIndexes(diplomacyOffers.Count, recursionDepth);
		for (int i = 0; i < list.Count; i++)
		{
			Offer offer = GenerateOffer(treshold_name, diplomacyOffers[list[i]], from, to, parent, parentArgIndex, forceParentArg, evalToCoverMin, evalToCoverMax, rel_change_type, recursionDepth);
			if (offer != null)
			{
				return offer;
			}
		}
		return null;
	}

	public Offer CreateNewOffer(Offer.Def def, Object from, Object to, List<Value> args = null, Offer parent = null, int parentArgIndex = 0)
	{
		CreateOfferDelegate value = null;
		if (!delegates.TryGetValue(def.field.key, out value) || value == null)
		{
			Game.Log("Missing 'Create' delegate for type: " + def.field.key, Game.LogType.Warning);
			return null;
		}
		Offer offer = value(def, from, to);
		if (parent != null)
		{
			offer.SetParent(parent);
			parent.SetArg(parentArgIndex, offer);
		}
		offer.args = args;
		return offer;
	}

	public Offer.Def GetOfferDef(string def_id)
	{
		Def value = null;
		if (!reg.defs.TryGetValue(def_id, out value))
		{
			return null;
		}
		return value as Offer.Def;
	}

	private Offer GenerateOffer(string treshold_name, string def_id, Object from, Object to, Offer parent = null, int parentArgIndex = 0, bool forceParentArg = false, float evalToCoverMin = 0f, float evalToCoverMax = 0f, string rel_change_type = "neutral", int recursionDepth = 0)
	{
		Offer.Def offerDef = GetOfferDef(def_id);
		if (offerDef == null)
		{
			return null;
		}
		Offer offer = null;
		bool flag = false;
		if (recursionDepth == 0)
		{
			offer = offerDef.GetInstance(from, to);
			flag = true;
		}
		else
		{
			offer = CreateNewOffer(offerDef, from, to);
		}
		offer.AI = true;
		if (offer == null)
		{
			return null;
		}
		if (rel_change_type != "neutral" && offer.def.rel_change_type != rel_change_type)
		{
			return null;
		}
		if (!SetParent(offer, parent, parentArgIndex, forceParentArg))
		{
			return null;
		}
		using (Game.ProfileScope profileScope = Game.Profile("ValidateWithoutArgs"))
		{
			if (offer.ValidateWithoutArgs() != "ok")
			{
				return null;
			}
			float millis = profileScope.Millis;
			if (DBGOffersData.tracking_enabled)
			{
				DBGOffersData dBGOffersData = Offer.dbg_offers_data[offer.def.field.key];
				dBGOffersData.validateWithoutArgs_minTime = Math.Min(dBGOffersData.validateWithoutArgs_minTime, millis);
				dBGOffersData.validateWithoutArgs_maxTime = Math.Max(dBGOffersData.validateWithoutArgs_maxTime, millis);
				dBGOffersData.validateWithoutArgs_totalTime += millis;
				dBGOffersData.validateWithoutArgs_count++;
			}
		}
		using (Game.ProfileScope profileScope2 = Game.Profile("FillOfferArgs"))
		{
			if (!FillOfferArgs(treshold_name, offer, evalToCoverMin, evalToCoverMax, recursionDepth + 1))
			{
				return null;
			}
			float millis2 = profileScope2.Millis;
			if (DBGOffersData.tracking_enabled)
			{
				DBGOffersData dBGOffersData2 = Offer.dbg_offers_data[offer.def.field.key];
				dBGOffersData2.fillOfferArgs_minTime = Math.Min(dBGOffersData2.fillOfferArgs_minTime, millis2);
				dBGOffersData2.fillOfferArgs_maxTime = Math.Max(dBGOffersData2.fillOfferArgs_maxTime, millis2);
				dBGOffersData2.fillOfferArgs_totalTime += millis2;
				dBGOffersData2.fillOfferArgs_count++;
			}
		}
		if (offer.Validate() != "ok")
		{
			return null;
		}
		if (flag)
		{
			Offer offer2 = offer.Copy();
			if (parent != null)
			{
				offer2.SetParent(parent);
				parent.SetArg(parentArgIndex, offer2);
			}
			return offer2;
		}
		return offer;
	}

	private bool SetParent(Offer offer, Offer parent, int parentArgIndex = 0, bool forceParentArg = false)
	{
		if (parent == null)
		{
			return true;
		}
		if (parent.def.max_args == 0)
		{
			return false;
		}
		if (parentArgIndex >= parent.def.max_args)
		{
			return false;
		}
		if (parent.args == null)
		{
			parent.args = new List<Value>();
		}
		if (parentArgIndex >= parent.args.Count)
		{
			offer.SetParent(parent);
			parent.SetArg(parentArgIndex, offer);
			return true;
		}
		Value value = parent.args[parentArgIndex];
		if (!forceParentArg && value.is_valid && !(value.obj_val is EmptyOffer))
		{
			return false;
		}
		offer.SetParent(parent);
		parent.SetArg(parentArgIndex, offer);
		return true;
	}

	public bool FillOfferArgs(string treshold_name, Offer offer, float evalToCoverMin = 0f, float evalToCoverMax = 0f, int recursionDepth = 0)
	{
		if (offer.def.max_args == 0)
		{
			if (string.IsNullOrEmpty(treshold_name))
			{
				return true;
			}
			if (evalToCoverMax != 0f)
			{
				float value = offer.Eval(treshold_name, treshold_name == "accept");
				if (Math.Sign(evalToCoverMax) == Math.Sign(value))
				{
					return false;
				}
				if (Math.Abs(evalToCoverMin) - Math.Abs(value) <= 0f && Math.Abs(evalToCoverMax) - Math.Abs(value) >= 0f)
				{
					return true;
				}
			}
			else if (offer.CheckThreshold(treshold_name, treshold_name == "accept"))
			{
				return true;
			}
			return false;
		}
		if (offer.def.field.key.EndsWith("Tribute", StringComparison.Ordinal))
		{
			if (string.IsNullOrEmpty(treshold_name))
			{
				return false;
			}
			evalToCoverMax = offer.Eval(treshold_name, treshold_name == "accept");
			evalToCoverMin = Math.Sign(evalToCoverMax);
			if (offer is PeaceOfferTribute)
			{
				if (offer is PeaceDemandTribute && evalToCoverMax >= 0f)
				{
					return false;
				}
				if (!(offer is PeaceDemandTribute) && evalToCoverMax <= 0f)
				{
					return false;
				}
			}
		}
		if (offer.def.field.key == "AdditionalConditionOffer")
		{
			Offer offer2 = TryGenerateRandomOfferHeavy("", offer.from, offer.to, offer, 0, forceParentArg: true, 0f, 0f, "neutral", ++recursionDepth);
			if (offer2 == null)
			{
				return false;
			}
			ProsAndCons prosAndCons = ProsAndCons.Get(offer2, "accept", reverse_kingdoms: true);
			if (prosAndCons == null)
			{
				Game.Log(offer2.def.field.key + " doesnt have an accept treshold pro-con", Game.LogType.Error);
				return false;
			}
			if (prosAndCons.eval > 0f)
			{
				return false;
			}
			if (Math.Abs(Math.Abs(prosAndCons.def.GetThreshold("accept", out var _, this)) - Math.Abs(prosAndCons.ratio)) > prosAndCons.def.max_consider_treshold_delta.Float(prosAndCons, 0.3f))
			{
				return false;
			}
			evalToCoverMin = prosAndCons.eval * offer.def.pro_con_difference_cover_perc_min / 100f;
			evalToCoverMax = prosAndCons.eval * offer.def.pro_con_difference_cover_perc_max / 100f;
			return FillOfferArgs(treshold_name, 1, offer, evalToCoverMin, evalToCoverMax, recursionDepth);
		}
		return FillOfferArgs(treshold_name, 0, offer, evalToCoverMin, evalToCoverMax, recursionDepth);
	}

	public bool FillOfferArgs(string treshold_name, int index, Offer offer, float evalToCoverMin = 0f, float evalToCoverMax = 0f, int recursionDepth = 0)
	{
		using (Game.Profile("OfferGenerator.FillOfferArgs"))
		{
			if (offer.def.arg_types[index].StartsWith("Offer", StringComparison.Ordinal))
			{
				if (string.IsNullOrEmpty(treshold_name))
				{
					return false;
				}
				List<int> list = GenerateRandomIndexes(diplomacyOffers.Count, recursionDepth);
				for (int i = 0; i < list.Count; i++)
				{
					Offer offer2 = GenerateOffer(treshold_name, diplomacyOffers[list[i]], offer.from, offer.to, offer, index, forceParentArg: true, 0f, evalToCoverMax, "neutral", ++recursionDepth);
					if (offer2 == null)
					{
						continue;
					}
					float value = offer2.Eval(treshold_name, treshold_name == "accept");
					if (Math.Sign(evalToCoverMin) == Math.Sign(value) || Math.Abs(evalToCoverMax) - Math.Abs(value) < 0f)
					{
						continue;
					}
					float evalToCoverMax2 = (float)Math.Sign(evalToCoverMin) * (Math.Abs(evalToCoverMax) - Math.Abs(value));
					float num;
					if (Math.Abs(evalToCoverMin) - Math.Abs(value) < 0f)
					{
						if (offer.Validate() == "ok")
						{
							return true;
						}
						num = 0f;
					}
					else
					{
						num = (float)Math.Sign(evalToCoverMin) * (Math.Abs(evalToCoverMin) - Math.Abs(value));
					}
					if (index < offer.def.max_args - 1 && FillOfferArgs(treshold_name, ++index, offer, num, evalToCoverMax2, ++recursionDepth))
					{
						return true;
					}
					if (num == 0f)
					{
						return true;
					}
				}
			}
			else
			{
				Offer.tmp_values.Clear();
				if (!offer.GetPossibleArgValues(index, Offer.tmp_values) || Offer.tmp_values.Count == 0)
				{
					return false;
				}
				List<int> list2 = GenerateRandomIndexes(Offer.tmp_values.Count, recursionDepth);
				List<Value> list3 = Offer.tmp_values;
				if (index < offer.def.max_args - 1)
				{
					list3 = SavePossibleArgumentValues(list3, recursionDepth);
				}
				for (int j = 0; j < list2.Count; j++)
				{
					offer.SetArg(index, list3[list2[j]]);
					if (index < offer.def.max_args - 1)
					{
						if (FillOfferArgs(treshold_name, index + 1, offer, evalToCoverMin, evalToCoverMax, ++recursionDepth))
						{
							return true;
						}
					}
					else
					{
						if (!(offer.Validate() == "ok"))
						{
							continue;
						}
						if (string.IsNullOrEmpty(treshold_name))
						{
							return true;
						}
						if (evalToCoverMax != 0f)
						{
							float value2 = offer.Eval(treshold_name, treshold_name == "accept");
							if (Math.Sign(evalToCoverMin) != Math.Sign(value2) && Math.Abs(evalToCoverMin) - Math.Abs(value2) <= 0f && Math.Abs(evalToCoverMax) - Math.Abs(value2) >= 0f)
							{
								return true;
							}
						}
						else if (offer.CheckThreshold(treshold_name, treshold_name == "accept"))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
	}

	public List<int> GenerateRandomIndexes(int indexesCount, int recursionDepth = 0)
	{
		if (!randomIndexLists.TryGetValue(recursionDepth, out var value))
		{
			randomIndexLists[recursionDepth] = new List<int>();
			value = randomIndexLists[recursionDepth];
		}
		if (value.Count != indexesCount)
		{
			if (value.Count > indexesCount)
			{
				value.RemoveRange(indexesCount, value.Count - indexesCount);
				for (int i = 0; i < value.Count; i++)
				{
					value[i] = i;
				}
			}
			else if (value.Count < indexesCount)
			{
				for (int j = 0; j < value.Count; j++)
				{
					value[j] = j;
				}
				while (value.Count < indexesCount)
				{
					value.Add(value.Count);
				}
			}
		}
		game.Shuffle(value);
		return value;
	}

	public List<Value> SavePossibleArgumentValues(List<Value> values, int recursionDepth = 0)
	{
		if (!argPossibleValueLists.TryGetValue(recursionDepth, out var value))
		{
			argPossibleValueLists[recursionDepth] = new List<Value>();
			value = argPossibleValueLists[recursionDepth];
		}
		value.Clear();
		value.AddRange(values);
		return value;
	}
}
