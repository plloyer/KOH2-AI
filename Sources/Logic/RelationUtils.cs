using System;
using System.Collections.Generic;

namespace Logic;

public static class RelationUtils
{
	public class Def : Logic.Def
	{
		public static float minRelationship = -1000f;

		public static float maxRelationship = 1000f;

		public static float fade_temp_per_sec = 1f;

		public static float fade_on_new_king_near = 0.2f;

		public static float fade_on_new_king_far = 0.6f;

		public static float fade_near_per_sec = 0f;

		public static float fade_far_per_sec = 0.25f;

		public static float fade_with_pope_per_sec_mul = 0.5f;

		public static float fade_distance_near = 2f;

		public static float fade_distance_far = 4f;

		public static float fade_vassalage = 0f;

		public static float fade_nap = 0.5f;

		public static float fade_trade = 0.1f;

		public static float fade_in_war = 0f;

		public static float fade_ally = 0f;

		public static float fade_marriage = 0f;

		public static float fade_papacy_and_catholics = 0.2f;

		public static float fade_min_relationship = 0f;

		public static float max_spread_reaction_kingdom_distance = 0f;

		public static float truce_time = 30f;

		public static string truce_for_kingdoms;

		public static List<float> relationThresholds = new List<float>();

		private void LoadFadeSettings(DT.Field f)
		{
			DT.Field field = base.field.FindChild("fade_settings");
			if (field != null)
			{
				fade_with_pope_per_sec_mul = field.GetFloat("fade_with_pope_per_sec_mul", null, 0.5f);
				fade_temp_per_sec = field.GetFloat("fade_temp_per_sec", null, 1f);
				fade_on_new_king_near = field.GetFloat("fade_on_new_king_near", null, 0.2f);
				fade_on_new_king_far = field.GetFloat("fade_on_new_king_far", null, 0.6f);
				fade_near_per_sec = field.GetFloat("fade_near_per_sec");
				fade_far_per_sec = field.GetFloat("fade_far_per_sec", null, 0.2f);
				fade_distance_near = field.GetFloat("fade_distance_near", null, 2f);
				fade_distance_far = field.GetFloat("fade_distance_far", null, 4f);
				fade_vassalage = field.GetFloat("fade_vassalage");
				fade_nap = field.GetFloat("fade_nap", null, 0.25f);
				fade_trade = field.GetFloat("fade_trade", null, 0.2f);
				fade_in_war = field.GetFloat("fade_in_war");
				fade_ally = field.GetFloat("fade_ally");
				fade_marriage = field.GetFloat("fade_marriage");
				fade_papacy_and_catholics = field.GetFloat("fade_papacy_and_catholics", null, 0.2f);
				fade_min_relationship = field.GetFloat("fade_min_relationship", null, 10f);
			}
		}

		public void LoadRelationshipThresholds(DT.Field f)
		{
			DT.Field field = base.field.FindChild("relationship_thresholds");
			if (field != null)
			{
				relationThresholds.Clear();
				int num = field.NumValues();
				for (int i = 0; i < num; i++)
				{
					relationThresholds.Add(field.Value(i));
				}
			}
		}

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			minRelationship = field.GetFloat("minRelationship", null, minRelationship);
			maxRelationship = field.GetFloat("maxRelationship", null, maxRelationship);
			max_spread_reaction_kingdom_distance = field.GetInt("max_spread_reaction_kingdom_distance", null, 2);
			truce_time = field.GetFloat("truce_time", null, truce_time);
			truce_for_kingdoms = field.GetString("truce_for_kingdoms", null, "custom");
			LoadFadeSettings(field);
			LoadRelationshipThresholds(field);
			return true;
		}

		public static float GetLowerTreshold(RelationshipType type)
		{
			if (type < RelationshipType.Hostile || (int)type >= relationThresholds.Count)
			{
				return minRelationship;
			}
			return relationThresholds[(int)type];
		}

		public static float GetUpperTreshold(RelationshipType type)
		{
			int num = (int)(type + 1);
			if (num < 0 || num >= relationThresholds.Count)
			{
				return maxRelationship;
			}
			return relationThresholds[num];
		}
	}

	public enum RelationshipType
	{
		Hostile,
		Negative,
		Reserved,
		Neutral,
		Sympathetic,
		Trusting,
		Friendly,
		All
	}

	[Flags]
	public enum Stance
	{
		None = 0,
		War = 1,
		Peace = 2,
		Alliance = 4,
		NonAggression = 8,
		Vassal = 0x10,
		Sovereign = 0x20,
		AnyVassalage = 0x40,
		Trade = 0x80,
		Marriage = 0x100,
		Own = 0x200,
		All = 0x3FF
	}

	public class SpreadWarData : IDisposable
	{
		private SpreadWarData previous;

		public War war;

		public int side;

		public SpreadWarData(War war, int side = -1)
		{
			previous = spreadWarData;
			this.war = war;
			this.side = side;
			spreadWarData = this;
		}

		public void SetWarSide(int side = 0)
		{
			this.side = side;
		}

		public void Dispose()
		{
			spreadWarData = previous;
		}
	}

	public interface IRelChangeValidator
	{
		bool ValidateRelChange(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget);
	}

	public class Fact : IRelChangeValidator
	{
		public virtual bool ValidateRelChange(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget)
		{
			return true;
		}
	}

	public class WarFact : Fact
	{
		public enum Action
		{
			Attack,
			Join,
			Leave,
			Conclude
		}

		public War war;

		public Kingdom kActor;

		public int actorSide;

		public Action action;

		public WarFact(War war, Kingdom kActor, int actorSide, Action action)
		{
			this.war = war;
			this.kActor = kActor;
			this.actorSide = actorSide;
			this.action = action;
		}

		public override bool ValidateRelChange(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget)
		{
			if (kActor != kSrc && kActor != kTgt)
			{
				return true;
			}
			return action switch
			{
				Action.Attack => ValidateAttack(kSrc, kTgt, perm, temp, indirectTarget), 
				Action.Join => ValidateJoin(kSrc, kTgt, perm, temp, indirectTarget), 
				Action.Leave => ValidateLeave(kSrc, kTgt, perm, temp, indirectTarget), 
				Action.Conclude => ValidateConclude(kSrc, kTgt, perm, temp, indirectTarget), 
				_ => true, 
			};
		}

		public bool ValidateAttack(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget)
		{
			if (kActor != kSrc)
			{
				return true;
			}
			if (war.defender != kTgt)
			{
				return true;
			}
			if (perm + temp > 0f)
			{
				return false;
			}
			return true;
		}

		public bool ValidateJoin(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget)
		{
			Kingdom k = ((kActor == kSrc) ? kTgt : kSrc);
			if (perm + temp > 0f)
			{
				if (war.IsEnemy(k, actorSide))
				{
					return false;
				}
			}
			else if (perm + temp < 0f && war.IsAlly(k, actorSide))
			{
				return false;
			}
			return true;
		}

		public bool ValidateLeave(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget)
		{
			return true;
		}

		public bool ValidateConclude(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget)
		{
			if (!war.IsEnemy(kSrc, kTgt) && !war.IsAlly(kSrc, kTgt))
			{
				return true;
			}
			if (perm + temp < 0f)
			{
				return false;
			}
			return true;
		}
	}

	public static LinkedList<IRelChangeValidator> validators = new LinkedList<IRelChangeValidator>();

	public static SpreadWarData spreadWarData = null;

	public static Stance DefaultGetStance(this IRelationCheck obj1, IRelationCheck obj2)
	{
		IRelationCheck relationCheck = obj1?.GetStanceObj();
		IRelationCheck relationCheck2 = obj2?.GetStanceObj();
		if (relationCheck == null || relationCheck2 == null)
		{
			return Stance.Peace;
		}
		if (relationCheck2 is Kingdom k)
		{
			return relationCheck.GetStance(k);
		}
		if (relationCheck2 is Settlement s)
		{
			return relationCheck.GetStance(s);
		}
		if (relationCheck2 is Rebellion r)
		{
			return relationCheck.GetStance(r);
		}
		if (relationCheck2 is Crusade k2)
		{
			return relationCheck.GetStance(k2);
		}
		return Stance.Peace;
	}

	public static Stance DefaultGetWarStance(this IRelationCheck obj1, IRelationCheck obj2)
	{
		if (obj1 == null || obj2 == null)
		{
			return Stance.Peace;
		}
		Stance stance = obj1.DefaultGetStance(obj2);
		if ((stance & Stance.War) != Stance.None)
		{
			return Stance.War;
		}
		if ((stance & Stance.Peace) != Stance.None)
		{
			return Stance.Peace;
		}
		if ((stance & Stance.Alliance) != Stance.None)
		{
			return Stance.Alliance;
		}
		if ((stance & Stance.Own) != Stance.None)
		{
			return Stance.Own;
		}
		Game.Log("Get war stance returned no results", Game.LogType.Error);
		return Stance.Peace;
	}

	public static bool DefaultIsEnemy(this IRelationCheck obj1, IRelationCheck obj2)
	{
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return (obj1.DefaultGetStance(obj2) & Stance.War) != 0;
	}

	public static bool DefaultIsNeutral(this IRelationCheck obj1, IRelationCheck obj2)
	{
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return (obj1.DefaultGetStance(obj2) & Stance.Peace) != 0;
	}

	public static bool DefaultIsAlly(this IRelationCheck obj1, IRelationCheck obj2)
	{
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return (obj1.DefaultGetStance(obj2) & Stance.Alliance) != 0;
	}

	public static bool DefaultIsAllyOrVassal(this IRelationCheck obj1, IRelationCheck obj2)
	{
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return (obj1.DefaultGetStance(obj2) & (Stance.Alliance | Stance.AnyVassalage)) != 0;
	}

	public static bool DefaultIsAllyOrOwn(this IRelationCheck obj1, IRelationCheck obj2)
	{
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return (obj1.DefaultGetStance(obj2) & (Stance.Alliance | Stance.Own)) != 0;
	}

	public static bool DefaultIsOwnStance(this IRelationCheck obj1, IRelationCheck obj2)
	{
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return (obj1.DefaultGetStance(obj2) & Stance.Own) != 0;
	}

	public static bool DefaultHasStance(this IRelationCheck obj1, IRelationCheck obj2, Stance stance)
	{
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return (obj1.DefaultGetStance(obj2) & stance) != 0;
	}

	public static bool DefaultHasStanceAll(this IRelationCheck obj1, IRelationCheck obj2, Stance stance)
	{
		if (obj1 == null || obj2 == null)
		{
			return false;
		}
		return (obj1.DefaultGetStance(obj2) & stance) == stance;
	}
}
