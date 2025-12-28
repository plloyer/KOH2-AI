using UnityEngine;

namespace Logic;

public class SquadHighGroundBuff : SquadBuff
{
	public class Def : Logic.Def
	{
		public float base_cth_shoot_max = 25f;

		public float base_cth_shoot_min = -25f;

		public float min_height_diff = 1f;

		public float max_height_diff = 15f;

		public override bool Load(Game game)
		{
			base_cth_shoot_max = base.field.GetFloat("base_cth_shoot_max", null, base_cth_shoot_max);
			base_cth_shoot_min = base.field.GetFloat("base_cth_shoot_min", null, base_cth_shoot_min);
			min_height_diff = base.field.GetFloat("min_height_diff", null, min_height_diff);
			max_height_diff = base.field.GetFloat("max_height_diff", null, max_height_diff);
			return base.Load(game);
		}
	}

	public Def high_ground_def;

	public SquadHighGroundBuff(Squad squad, Logic.Def def, DT.Field field = null)
		: base(squad, def, field)
	{
	}

	public new static SquadBuff Create(Squad owner, Logic.Def def, DT.Field field = null)
	{
		return new SquadHighGroundBuff(owner, def, field);
	}

	public override void Init(Squad squad, Logic.Def def, DT.Field field = null)
	{
		high_ground_def = def as Def;
		base.Init(squad, def, field);
	}

	public override float GetCTHShootMod()
	{
		if (!squad.def.is_ranged)
		{
			return 0f;
		}
		if (base.enabled)
		{
			float num = 0f;
			float heightDifference = GetHeightDifference();
			if (heightDifference > 0f)
			{
				heightDifference -= high_ground_def.min_height_diff;
				heightDifference /= high_ground_def.max_height_diff - high_ground_def.min_height_diff;
				return Mathf.Lerp(0f, high_ground_def.base_cth_shoot_max, heightDifference);
			}
			heightDifference += high_ground_def.min_height_diff;
			heightDifference /= high_ground_def.max_height_diff - high_ground_def.min_height_diff;
			return Mathf.Lerp(0f, high_ground_def.base_cth_shoot_min, 0f - heightDifference);
		}
		return 0f;
	}

	public void CalcMod(float h1, float h2, out float res, out float height_difference)
	{
		float num = 0f;
		float num2 = (height_difference = h1 - h2);
		if (num2 > 0f)
		{
			num2 -= high_ground_def.min_height_diff;
			num2 /= high_ground_def.max_height_diff - high_ground_def.min_height_diff;
			num = Mathf.Lerp(0f, high_ground_def.base_cth_shoot_max, num2);
		}
		else
		{
			num2 += high_ground_def.min_height_diff;
			num2 /= high_ground_def.max_height_diff - high_ground_def.min_height_diff;
			num = Mathf.Lerp(0f, high_ground_def.base_cth_shoot_min, 0f - num2);
		}
		res = num;
	}

	public override bool Validate()
	{
		if (!base.Validate())
		{
			return false;
		}
		return Mathf.Abs(GetHeightDifference()) >= high_ground_def.min_height_diff;
	}

	public float GetHeightDifference()
	{
		if (base.squad == null || base.squad.ranged_enemy == null)
		{
			return 0f;
		}
		if (base.squad.ranged_enemy is Squad)
		{
			Squad squad = (Squad)base.squad.ranged_enemy;
			if (squad == null)
			{
				return 0f;
			}
			return base.squad.actual_position_height - squad.actual_position_height;
		}
		return 0f;
	}

	public override string DebugUIText()
	{
		if (GetCTHShootMod() > 0f)
		{
			return "H";
		}
		return "L";
	}
}
