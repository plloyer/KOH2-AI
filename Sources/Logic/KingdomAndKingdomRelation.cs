using System;
using System.Collections.Generic;
using Logic.ExtensionMethods;

namespace Logic;

public class KingdomAndKingdomRelation
{
	public static int created = 0;

	public static int destroyed = 0;

	public static bool debugDisableRelationsFade = false;

	public static bool alreadyThinkingProposeOffer = false;

	public RelationUtils.Stance stance;

	public Time stance_time;

	public Time war_time;

	public Time peace_time;

	public Time alliance_time;

	public Time nap_time;

	public Time vassalage_time;

	public Time trade_time;

	public Time marriage_time;

	public Time last_rel_change_time;

	public bool nap_k1_king_venerable;

	public bool nap_k2_king_venerable;

	public Kingdom nap_broken_dead_king_kingdom;

	public Time nap_broken_king_death_time;

	public List<Pact> commonPacts;

	public Time trade_route_last_update;

	public float perm_relationship;

	public float temp_relationship;

	public Time fade_time;

	public static KingdomAndKingdomRelation Default = new KingdomAndKingdomRelation();

	public static KingdomAndKingdomRelation Own = new KingdomAndKingdomRelation
	{
		stance = RelationUtils.Stance.Own,
		stance_time = Time.Zero,
		war_time = Time.Zero,
		peace_time = Time.Zero,
		alliance_time = Time.Zero,
		nap_time = Time.Zero,
		vassalage_time = Time.Zero,
		trade_time = Time.Zero,
		marriage_time = Time.Zero,
		last_rel_change_time = Time.Zero,
		nap_k1_king_venerable = false,
		nap_k2_king_venerable = false,
		nap_broken_dead_king_kingdom = null,
		nap_broken_king_death_time = Time.Zero,
		perm_relationship = 1f,
		temp_relationship = 0f,
		fade_time = Time.Zero
	};

	private static int max_validators = 0;

	public bool is_zero
	{
		get
		{
			if (perm_relationship == 0f)
			{
				return temp_relationship == 0f;
			}
			return false;
		}
	}

	public bool is_default
	{
		get
		{
			if (stance == RelationUtils.Stance.Peace && perm_relationship == 0f && temp_relationship == 0f && !nap_k1_king_venerable && !nap_k2_king_venerable && nap_broken_dead_king_kingdom == null && (commonPacts == null || commonPacts.Count == 0) && stance_time == Time.Zero && war_time == Time.Zero && peace_time == Time.Zero && alliance_time == Time.Zero && nap_time == Time.Zero && vassalage_time == Time.Zero && trade_time == Time.Zero && marriage_time == Time.Zero && last_rel_change_time == Time.Zero && nap_broken_king_death_time == Time.Zero)
			{
				return fade_time == Time.Zero;
			}
			return false;
		}
	}

	public KingdomAndKingdomRelation()
	{
		stance = RelationUtils.Stance.Peace;
		stance_time = Time.Zero;
		war_time = Time.Zero;
		peace_time = Time.Zero;
		alliance_time = Time.Zero;
		nap_time = Time.Zero;
		vassalage_time = Time.Zero;
		trade_time = Time.Zero;
		marriage_time = Time.Zero;
		last_rel_change_time = Time.Zero;
		nap_k1_king_venerable = false;
		nap_k2_king_venerable = false;
		nap_broken_dead_king_kingdom = null;
		nap_broken_king_death_time = Time.Zero;
		trade_route_last_update = Time.Zero;
		perm_relationship = 0f;
		temp_relationship = 0f;
		fade_time = Time.Zero;
	}

	public void OnChanged(Kingdom k1, Kingdom k2, bool send_state = true)
	{
		if (k1 != null && k2 != null && k1 != k2)
		{
			if (k1.id > k2.id)
			{
				Kingdom kingdom = k1;
				k1 = k2;
				k2 = kingdom;
			}
			last_rel_change_time = k2.game.time;
			fade_time = k2.game.time;
			k2.game?.economy?.OnRelationChanged(k1, k2, this);
			if (send_state)
			{
				k2.SendSubstate<Kingdom.RelationsState.KingdomRelationState>(k1.id);
			}
		}
	}

	public static void InitRelations(Kingdom k)
	{
		if (k.relations == null)
		{
			k.relations = new List<KingdomAndKingdomRelation>();
		}
		if (k.relations.Count != k.id - 1)
		{
			k.relations.Clear();
			for (int i = 0; i < k.id - 1; i++)
			{
				k.relations.Add(null);
			}
		}
	}

	public static KingdomAndKingdomRelation Get(Kingdom k1, Kingdom k2, bool calc_fade = true, bool create_if_not_found = false)
	{
		if (k1 == null || k2 == null)
		{
			return Default;
		}
		if (k1.id == k2.id)
		{
			return Own;
		}
		if (k1.id > k2.id)
		{
			Kingdom kingdom = k1;
			k1 = k2;
			k2 = kingdom;
		}
		if (create_if_not_found)
		{
			InitRelations(k2);
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = k2.relations[k1.id - 1];
		if (kingdomAndKingdomRelation == null)
		{
			if (create_if_not_found)
			{
				created++;
				kingdomAndKingdomRelation = new KingdomAndKingdomRelation();
				k2.relations[k1.id - 1] = kingdomAndKingdomRelation;
				return kingdomAndKingdomRelation;
			}
			return Default;
		}
		if (calc_fade)
		{
			kingdomAndKingdomRelation.CalcFadeWithTime(k1, k2);
		}
		return kingdomAndKingdomRelation;
	}

	public static void FadeWithNewKing(Kingdom src)
	{
		if (Game.isLoadingSaveGame || debugDisableRelationsFade || src == null || src.IsDefeated())
		{
			return;
		}
		foreach (Kingdom kingdom in src.game.kingdoms)
		{
			if (kingdom == null || kingdom.IsDefeated() || kingdom == src)
			{
				continue;
			}
			KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(src, kingdom, calc_fade: true, create_if_not_found: true);
			if (kingdomAndKingdomRelation.is_zero)
			{
				continue;
			}
			float fade_nap = RelationUtils.Def.fade_nap;
			float fade_in_war = RelationUtils.Def.fade_in_war;
			float fade_marriage = RelationUtils.Def.fade_marriage;
			float fade_ally = RelationUtils.Def.fade_ally;
			float fade_vassalage = RelationUtils.Def.fade_vassalage;
			float fade_papacy_and_catholics = RelationUtils.Def.fade_papacy_and_catholics;
			if ((src.sovereignState == kingdom || kingdom.sovereignState == src) && fade_vassalage == 0f)
			{
				continue;
			}
			bool flag = kingdomAndKingdomRelation.stance.IsNonAgression();
			if (flag && fade_nap == 0f)
			{
				continue;
			}
			bool flag2 = kingdomAndKingdomRelation.stance.IsWar();
			if (flag2 && fade_in_war == 0f)
			{
				continue;
			}
			bool flag3 = kingdomAndKingdomRelation.stance.IsMarriage();
			if (flag3 && fade_marriage == 0f)
			{
				continue;
			}
			bool flag4 = kingdomAndKingdomRelation.stance.IsAlliance();
			if (flag4 && fade_ally == 0f)
			{
				continue;
			}
			bool flag5 = false;
			if (src.is_catholic && !src.excommunicated && kingdom.IsPapacy())
			{
				flag5 = true;
			}
			else if (kingdom.is_catholic && !kingdom.excommunicated && kingdom.IsPapacy())
			{
				flag5 = true;
			}
			if (!flag5 || fade_papacy_and_catholics != 0f)
			{
				float num = 1f;
				if (flag && num > fade_nap)
				{
					num = fade_nap;
				}
				if (flag2 && num > fade_in_war)
				{
					num = fade_in_war;
				}
				if (flag3 && num > fade_marriage)
				{
					num = fade_marriage;
				}
				if (flag4 && num > fade_ally)
				{
					num = fade_ally;
				}
				if (flag5 && num > fade_papacy_and_catholics)
				{
					num = fade_papacy_and_catholics;
				}
				float val = src.DistanceToKingdom(kingdom);
				float fade_on_new_king_near = RelationUtils.Def.fade_on_new_king_near;
				float fade_on_new_king_far = RelationUtils.Def.fade_on_new_king_far;
				float fade_distance_near = RelationUtils.Def.fade_distance_near;
				float fade_distance_far = RelationUtils.Def.fade_distance_far;
				float num2 = Game.map_clamp(val, fade_distance_near, fade_distance_far, fade_on_new_king_near, fade_on_new_king_far);
				float num3 = 1f - num * num2;
				kingdomAndKingdomRelation.perm_relationship *= num3;
				kingdomAndKingdomRelation.temp_relationship *= num3;
				if (kingdomAndKingdomRelation.perm_relationship > -20f && kingdomAndKingdomRelation.perm_relationship < 20f)
				{
					kingdomAndKingdomRelation.perm_relationship = 0f;
				}
				if (kingdomAndKingdomRelation.temp_relationship > -20f && kingdomAndKingdomRelation.temp_relationship < 20f)
				{
					kingdomAndKingdomRelation.temp_relationship = 0f;
				}
				kingdomAndKingdomRelation.OnChanged(src, kingdom);
			}
		}
	}

	public static void Set(Kingdom k1, Kingdom k2, KingdomAndKingdomRelation rel, bool send_state = true)
	{
	}

	public static RelationUtils.Stance GetStance(Kingdom k1, Kingdom k2)
	{
		RelationUtils.Stance stance = Get(k1, k2, calc_fade: false).stance;
		if (k2.sovereignState == k1)
		{
			stance |= RelationUtils.Stance.Sovereign;
		}
		if (k1.sovereignState == k2)
		{
			stance |= RelationUtils.Stance.Vassal;
		}
		return stance;
	}

	public static void SetStance(Kingdom k1, Kingdom k2, RelationUtils.Stance stance)
	{
		if (k1.id > k2.id)
		{
			Kingdom kingdom = k1;
			k1 = k2;
			k2 = kingdom;
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(k1, k2, calc_fade: false, create_if_not_found: true);
		RelationUtils.Stance st = kingdomAndKingdomRelation.stance;
		kingdomAndKingdomRelation.stance = stance;
		if (!st.IsWar() && stance.IsWar())
		{
			kingdomAndKingdomRelation.war_time = k1.game.time;
		}
		if (st.IsWar() && !stance.IsWar())
		{
			kingdomAndKingdomRelation.peace_time = k1.game.time;
		}
		if (!st.IsAlliance() && stance.IsAlliance())
		{
			kingdomAndKingdomRelation.alliance_time = k1.game.time;
		}
		if (!stance.IsNonAgression() && st.IsNonAgression())
		{
			kingdomAndKingdomRelation.nap_time = k1.game.time;
		}
		if (stance.IsNonAgression() && !st.IsNonAgression())
		{
			kingdomAndKingdomRelation.nap_time = k1.game.time;
			kingdomAndKingdomRelation.nap_k1_king_venerable = k1.royalFamily.Sovereign.age == Character.Age.Venerable;
			kingdomAndKingdomRelation.nap_k2_king_venerable = k2.royalFamily.Sovereign.age == Character.Age.Venerable;
		}
		if ((stance.IsAnyVassalage() && !st.IsAnyVassalage()) || (!stance.IsAnyVassalage() && st.IsAnyVassalage()))
		{
			kingdomAndKingdomRelation.vassalage_time = k1.game.time;
		}
		if ((stance.IsTrade() && !st.IsTrade()) || (!stance.IsTrade() && st.IsTrade()))
		{
			kingdomAndKingdomRelation.trade_time = k1.game.time;
		}
		if ((stance.IsMarriage() && !st.IsMarriage()) || (!stance.IsMarriage() && st.IsMarriage()))
		{
			kingdomAndKingdomRelation.marriage_time = k1.game.time;
		}
		kingdomAndKingdomRelation.stance_time = k1.game.time;
		kingdomAndKingdomRelation.OnChanged(k1, k2);
	}

	public static bool ShouldUnsetNonAgression(Kingdom k1, Kingdom k2)
	{
		KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(k1, k2, calc_fade: false, create_if_not_found: true);
		bool result = !kingdomAndKingdomRelation.nap_k2_king_venerable && !kingdomAndKingdomRelation.nap_k2_king_venerable;
		if (k1.id > k2.id)
		{
			if (kingdomAndKingdomRelation.nap_k2_king_venerable)
			{
				kingdomAndKingdomRelation.nap_k2_king_venerable = false;
				kingdomAndKingdomRelation.OnChanged(k1, k2);
				return result;
			}
		}
		else if (kingdomAndKingdomRelation.nap_k1_king_venerable)
		{
			kingdomAndKingdomRelation.nap_k1_king_venerable = false;
			kingdomAndKingdomRelation.OnChanged(k1, k2);
		}
		return result;
	}

	public static void SetNonAgressionBrokenKingDeath(Kingdom k1, Kingdom k2)
	{
		KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(k1, k2, calc_fade: false, create_if_not_found: true);
		kingdomAndKingdomRelation.nap_broken_king_death_time = k1.game.time;
		kingdomAndKingdomRelation.nap_broken_dead_king_kingdom = k1;
		kingdomAndKingdomRelation.OnChanged(k1, k2);
	}

	public static bool HasTradeAgreement(Kingdom k1, Kingdom k2)
	{
		return Get(k1, k2).stance.IsTrade();
	}

	public static void UpdateTradeRouteTime(Kingdom k1, Kingdom k2, Game game)
	{
		KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(k1, k2, calc_fade: false, create_if_not_found: true);
		kingdomAndKingdomRelation.trade_route_last_update = game.time;
		kingdomAndKingdomRelation.OnChanged(k1, k2);
	}

	public static void ClearTruceTime(Kingdom k1, Kingdom k2, Game game)
	{
		KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(k1, k2, calc_fade: false, create_if_not_found: true);
		kingdomAndKingdomRelation.peace_time = game.time - 1f - RelationUtils.Def.truce_time * 60f;
		kingdomAndKingdomRelation.OnChanged(k1, k2);
	}

	public static Time GetTradeRouteLastUpdateTime(Kingdom k1, Kingdom k2)
	{
		return Get(k1, k2).trade_route_last_update;
	}

	public static bool GetMarriage(Kingdom k1, Kingdom k2)
	{
		return Get(k1, k2).stance.IsMarriage();
	}

	public static List<Pact> GetCommonPacts(Kingdom k1, Kingdom k2)
	{
		return Get(k1, k2).commonPacts;
	}

	public static void AddCommonPact(Kingdom k1, Kingdom k2, Pact pact, bool send_state = true)
	{
		KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(k1, k2, calc_fade: false, create_if_not_found: true);
		if (kingdomAndKingdomRelation.commonPacts == null)
		{
			kingdomAndKingdomRelation.commonPacts = new List<Pact>();
		}
		if (!kingdomAndKingdomRelation.commonPacts.Contains(pact))
		{
			kingdomAndKingdomRelation.commonPacts.Add(pact);
			kingdomAndKingdomRelation.OnChanged(k1, k2, send_state);
			if (send_state)
			{
				k1.SendSubstate<Kingdom.RelationsState.CommonPactsState>(k2.id);
			}
		}
	}

	public static void DelCommonPact(Kingdom k1, Kingdom k2, Pact pact, bool send_state = true)
	{
		KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(k1, k2, calc_fade: false, create_if_not_found: true);
		if (kingdomAndKingdomRelation.commonPacts == null)
		{
			kingdomAndKingdomRelation.commonPacts = new List<Pact>();
		}
		if (kingdomAndKingdomRelation.commonPacts.Remove(pact) && send_state)
		{
			kingdomAndKingdomRelation.OnChanged(k1, k2, send_state);
			k1.SendSubstate<Kingdom.RelationsState.CommonPactsState>(k2.id);
		}
	}

	private static float _fade_to_zero(float value, float factor)
	{
		if (value < 0f)
		{
			value += factor;
			if (value > 0f)
			{
				value = 0f;
			}
		}
		else
		{
			value -= factor;
			if (value < 0f)
			{
				value = 0f;
			}
		}
		if (value > 0f && value < RelationUtils.Def.fade_min_relationship)
		{
			value = 0f;
		}
		if (value < 0f && value > 0f - RelationUtils.Def.fade_min_relationship)
		{
			value = 0f;
		}
		return value;
	}

	private static bool _should_keep_positive_rels(Kingdom a, Kingdom b)
	{
		for (int i = 0; i < a.court.Count; i++)
		{
			Character character = a.court[i];
			if (character != null && (character.IsMerchant() || character.IsDiplomat()) && character.mission_kingdom == b)
			{
				return true;
			}
		}
		return false;
	}

	public void CalcFadeWithTime(Kingdom a = null, Kingdom b = null)
	{
		if (a == null || b == null)
		{
			return;
		}
		Time time = a.game.time;
		Time time2 = fade_time;
		fade_time = time;
		if (debugDisableRelationsFade || is_zero)
		{
			return;
		}
		float num = 1f;
		if (a.is_catholic && !a.excommunicated && b.is_catholic && !b.excommunicated && (a.IsPapacy() || b.IsPapacy()))
		{
			num = RelationUtils.Def.fade_with_pope_per_sec_mul;
		}
		float factor = (time - time2) * num * RelationUtils.Def.fade_temp_per_sec;
		float num2 = (time - time2) * num * RelationUtils.Def.fade_near_per_sec;
		float num3 = (time - time2) * num * RelationUtils.Def.fade_far_per_sec;
		temp_relationship = _fade_to_zero(temp_relationship, factor);
		if ((num2 != 0f || num3 != 0f) && perm_relationship != 0f && (!(perm_relationship > 0f) || (!_should_keep_positive_rels(a, b) && !_should_keep_positive_rels(b, a))))
		{
			float fade_distance_near = RelationUtils.Def.fade_distance_near;
			float fade_distance_far = RelationUtils.Def.fade_distance_far;
			float num4 = Game.map_clamp(a.DistanceToKingdom(b), fade_distance_near, fade_distance_far, num2, num3);
			num4 *= num;
			if (num4 != 0f)
			{
				perm_relationship = _fade_to_zero(perm_relationship, num4);
			}
		}
	}

	public static float ClampRelationship(float val)
	{
		if (val < RelationUtils.Def.minRelationship)
		{
			val = RelationUtils.Def.minRelationship;
		}
		else if (val > RelationUtils.Def.maxRelationship)
		{
			val = RelationUtils.Def.maxRelationship;
		}
		return val;
	}

	public float GetRelationship()
	{
		return ClampRelationship(perm_relationship + temp_relationship);
	}

	public static float GetRelationship(Kingdom k1, Kingdom k2)
	{
		return Get(k1, k2).GetRelationship();
	}

	private static float _clamp_temp_rel(float perm, float temp)
	{
		if (perm + temp > RelationUtils.Def.maxRelationship)
		{
			temp = RelationUtils.Def.maxRelationship - perm;
		}
		if (perm + temp < RelationUtils.Def.minRelationship)
		{
			temp = RelationUtils.Def.minRelationship - perm;
		}
		return temp;
	}

	public static DT.Field GetRelationModifierField(Game game, string name)
	{
		return (game.dt.FindDef("RelationModifiers")?.field)?.FindChild(name);
	}

	public static void AddRelationship(Kingdom k1, Kingdom k2, float perm, float temp = 0f, Reason r = null, string mode = "add")
	{
		KingdomAndKingdomRelation kingdomAndKingdomRelation = Get(k1, k2, calc_fade: true, create_if_not_found: true);
		float relationship = kingdomAndKingdomRelation.GetRelationship();
		switch (mode)
		{
		case "add":
			perm += kingdomAndKingdomRelation.perm_relationship;
			temp += kingdomAndKingdomRelation.temp_relationship;
			break;
		case "max":
			perm = Math.Max(kingdomAndKingdomRelation.perm_relationship, perm);
			temp = Math.Max(kingdomAndKingdomRelation.temp_relationship, temp);
			break;
		case "min":
			perm = Math.Min(kingdomAndKingdomRelation.perm_relationship, perm);
			temp = Math.Min(kingdomAndKingdomRelation.temp_relationship, temp);
			break;
		}
		perm = Game.clamp(perm, RelationUtils.Def.minRelationship, RelationUtils.Def.maxRelationship);
		temp = _clamp_temp_rel(perm, temp);
		kingdomAndKingdomRelation.perm_relationship = perm;
		kingdomAndKingdomRelation.temp_relationship = temp;
		k1.FireEvent("relation_modified", r, k1.id, k2.id);
		k2.FireEvent("relation_modified", r, k1.id, k2.id);
		kingdomAndKingdomRelation.OnChanged(k1, k2);
		RelationshipChangeAnalytics(k1, k2, (int)relationship, (int)kingdomAndKingdomRelation.GetRelationship(), r?.field?.key);
	}

	public static void Modify(string mod_name, Kingdom kSrc, Kingdom kTgt, IVars vars, float value_multiplier = 1f)
	{
		Game game = kSrc.game;
		DT.Field relationModifierField = GetRelationModifierField(game, mod_name);
		if (relationModifierField == null)
		{
			game.Warning("Relation modifier '" + mod_name + "' not found");
			return;
		}
		using (Game.Profile("Modify relationship"))
		{
			Modify(relationModifierField, kSrc, kTgt, null, vars, value_multiplier);
		}
	}

	public static void Modify(DT.Field mod_field, Kingdom kSrc, Kingdom kTgt, Kingdom kIndirectTarget, IVars vars, float value_multiplier)
	{
		if (mod_field == null)
		{
			return;
		}
		float num = mod_field.Float(0, vars) * value_multiplier;
		float num2 = mod_field.Float(1, vars) * value_multiplier;
		float num3 = mod_field.Float(2, vars, 1f);
		string text = mod_field.String(3, vars, "add");
		if (num > -0.001f && num < 0.001f && num2 > -0.001f && num2 < 0.001f)
		{
			return;
		}
		if (RelationUtils.validators.Count > max_validators)
		{
			max_validators = RelationUtils.validators.Count;
			Game.Log("Rel Validators Count: " + max_validators, Game.LogType.Message);
		}
		for (LinkedListNode<RelationUtils.IRelChangeValidator> linkedListNode = RelationUtils.validators.Last; linkedListNode != null; linkedListNode = linkedListNode.Previous)
		{
			if (!linkedListNode.Value.ValidateRelChange(kSrc, kTgt, num, num2, kIndirectTarget))
			{
				return;
			}
		}
		Modify(mod_field, num, num2, kSrc, kTgt, kIndirectTarget, vars, text);
		if (text != "add" || kSrc.game.isInVideoMode)
		{
			return;
		}
		List<Kingdom> kingdoms = kSrc.game.kingdoms;
		if (kIndirectTarget != null || num3 == 0f)
		{
			return;
		}
		num *= num3;
		num2 *= num3;
		int num4 = kSrc.game.Random(0, kingdoms.Count);
		for (int i = 0; i < kingdoms.Count; i++)
		{
			num4 = (num4 + 1) % kingdoms.Count;
			Kingdom kingdom = kingdoms[num4];
			if (kingdom == null || kingdom == kTgt || kingdom.is_player || kingdom.IsDefeated())
			{
				continue;
			}
			float relationship = GetRelationship(kingdom, kTgt);
			if (!(relationship > -0.001f) || !(relationship < 0.001f))
			{
				float num5 = relationship / RelationUtils.Def.maxRelationship;
				float num6 = 0.2f;
				if (num5 > 0f - num6 && num5 < num6)
				{
					num5 = num6 * (float)Math.Sign(num5);
				}
				float num7 = Game.map_clamp(kingdom.DistanceToKingdom(kTgt), 1f, 4f, 1f, 0.5f);
				num5 *= num7;
				float perm = num * num5;
				float temp = num2 * num5;
				Modify(mod_field, perm, temp, kSrc, kingdom, kTgt, vars);
			}
		}
		if (kIndirectTarget != null || !kSrc.is_player || !kSrc.game.ai.enabled || alreadyThinkingProposeOffer)
		{
			return;
		}
		alreadyThinkingProposeOffer = true;
		Kingdom kingdom2 = null;
		Kingdom kingdom3 = null;
		float num8 = float.MinValue;
		float num9 = float.MinValue;
		Kingdom kingdom4 = null;
		Kingdom kingdom5 = null;
		float num10 = float.MaxValue;
		float num11 = float.MaxValue;
		for (int j = 0; j < kingdoms.Count; j++)
		{
			Kingdom kingdom6 = kingdoms[j];
			if (kingdom6 == null || kingdom6 == kTgt || kingdom6.is_player || kingdom6.IsDefeated())
			{
				continue;
			}
			float relationship2 = GetRelationship(kingdom6, kTgt);
			if (relationship2 > num8)
			{
				if (num8 > num9)
				{
					kingdom3 = kingdom2;
					num9 = num8;
				}
				kingdom2 = kingdom6;
				num8 = relationship2;
			}
			else if (relationship2 > num9)
			{
				kingdom3 = kingdom6;
				num9 = relationship2;
			}
			if (relationship2 < num10)
			{
				kingdom4 = kingdom6;
				num10 = relationship2;
				if (num10 > num11)
				{
					kingdom5 = kingdom4;
					num11 = num10;
				}
			}
			else if (relationship2 < num11)
			{
				kingdom5 = kingdom6;
				num11 = relationship2;
			}
		}
		string offer_rel_change_type = ((num + num2 > 0f) ? "positive" : "negative");
		if (kTgt?.ai != null && kTgt.ai.Enabled(KingdomAI.EnableFlags.Diplomacy) && kTgt.ai.CalcDiplomaticImportance(kSrc) != 0)
		{
			kTgt.ai.ThinkProposeOfferTo(kSrc, offer_rel_change_type);
		}
		if (kingdom2?.ai != null && kingdom2.ai.Enabled(KingdomAI.EnableFlags.Diplomacy) && kingdom2.ai.CalcDiplomaticImportance(kSrc) != 0)
		{
			kingdom2.ai.ThinkProposeOfferTo(kSrc, offer_rel_change_type);
		}
		if (kingdom3?.ai != null && kingdom3.ai.Enabled(KingdomAI.EnableFlags.Diplomacy) && kingdom3.ai.CalcDiplomaticImportance(kSrc) != 0)
		{
			kingdom3.ai.ThinkProposeOfferTo(kSrc, offer_rel_change_type);
		}
		if (kingdom4?.ai != null && kingdom4.ai.Enabled(KingdomAI.EnableFlags.Diplomacy) && kingdom4.ai.CalcDiplomaticImportance(kSrc) != 0)
		{
			kingdom4.ai.ThinkProposeOfferTo(kSrc, offer_rel_change_type);
		}
		if (kingdom5?.ai != null && kingdom5.ai.Enabled(KingdomAI.EnableFlags.Diplomacy) && kingdom5.ai.CalcDiplomaticImportance(kSrc) != 0)
		{
			kingdom5.ai.ThinkProposeOfferTo(kSrc, offer_rel_change_type);
		}
		alreadyThinkingProposeOffer = false;
	}

	public static void RelationshipChangeAnalytics(Kingdom k1, Kingdom k2, int old_val, int new_val, string changeType)
	{
		bool flag = k1?.is_player ?? false;
		bool flag2 = k2?.is_player ?? false;
		if ((!flag && !flag2) || Game.isLoadingSaveGame || !Game.fullGameStateReceived)
		{
			return;
		}
		for (int i = 0; i < 7; i++)
		{
			float upperTreshold = RelationUtils.Def.GetUpperTreshold((RelationUtils.RelationshipType)i);
			if ((float)old_val < upperTreshold && upperTreshold <= (float)new_val)
			{
				if (flag)
				{
					SendToKingdom(k1, k2, new_val, changeType);
				}
				if (flag2)
				{
					SendToKingdom(k2, k1, new_val, changeType);
				}
				break;
			}
			if ((float)old_val >= upperTreshold && upperTreshold > (float)new_val)
			{
				if (flag)
				{
					SendToKingdom(k1, k2, new_val, changeType);
				}
				if (flag2)
				{
					SendToKingdom(k2, k1, new_val, changeType);
				}
				break;
			}
		}
		static void SendToKingdom(Kingdom kingdom, Kingdom other, int relationship, string relationshipChangeType)
		{
			Vars vars = new Vars();
			vars.Set("targetKingdom", other.Name);
			vars.Set("kingdomRelation", relationship);
			if (!string.IsNullOrEmpty(relationshipChangeType))
			{
				vars.Set("relationshipChangeType", relationshipChangeType);
			}
			kingdom.FireEvent("analytics_relationship_change", vars, kingdom.id);
		}
	}

	public static void Modify(DT.Field mod_field, float perm, float temp, Kingdom kSrc, Kingdom kTgt, Kingdom kIndirectTarget, IVars vars, string mode = "add")
	{
		Reason r = ((!kSrc.is_player) ? null : new Reason(kSrc, kTgt, mod_field, perm + temp, kIndirectTarget == null, kIndirectTarget, vars));
		kSrc.AddRelationReason(r);
		AddRelationship(kSrc, kTgt, perm, temp, r, mode);
	}

	public static float GetValueOfModifier(Game game, string modName, IVars vars = null)
	{
		DT.Field relationModifierField = GetRelationModifierField(game, modName);
		if (relationModifierField == null)
		{
			game.Warning("Relation modifier '" + modName + "' not found");
			return 0f;
		}
		return relationModifierField.Value(0, vars);
	}

	public static bool GetValuesOfModifier(Game game, string modName, IVars vars, out float perm, out float temp)
	{
		perm = 0f;
		temp = 0f;
		DT.Field relationModifierField = GetRelationModifierField(game, modName);
		if (relationModifierField == null)
		{
			game.Warning("Relation modifier '" + modName + "' not found");
			return false;
		}
		perm = relationModifierField.Value(0, vars);
		temp = relationModifierField.Value(1, vars);
		return true;
	}

	public override string ToString()
	{
		return $"{stance}({perm_relationship:F0} + {temp_relationship:F0} = {GetRelationship():F0})";
	}
}
