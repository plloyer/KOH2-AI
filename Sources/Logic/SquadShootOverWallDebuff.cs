namespace Logic;

public class SquadShootOverWallDebuff : SquadBuff
{
	public class Def : Logic.Def
	{
		public float base_cth_shoot = -25f;

		public override bool Load(Game game)
		{
			base_cth_shoot = base.field.GetFloat("base_cth_shoot", null, base_cth_shoot);
			return base.Load(game);
		}
	}

	public Def shoot_over_wall_def;

	public SquadShootOverWallDebuff(Squad squad, Logic.Def def, DT.Field field = null)
		: base(squad, def, field)
	{
	}

	public new static SquadBuff Create(Squad owner, Logic.Def def, DT.Field field = null)
	{
		return new SquadShootOverWallDebuff(owner, def, field);
	}

	public override void Init(Squad squad, Logic.Def def, DT.Field field = null)
	{
		shoot_over_wall_def = def as Def;
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
			return shoot_over_wall_def.base_cth_shoot;
		}
		return 0f;
	}

	public override bool Validate()
	{
		if (!base.Validate())
		{
			return false;
		}
		if (base.squad == null || base.squad.ranged_enemy == null)
		{
			return false;
		}
		if (base.squad.ranged_enemy is Squad)
		{
			Squad squad = (Squad)base.squad.ranged_enemy;
			if (squad == null)
			{
				return false;
			}
			return base.squad.battle.IsOutsideWall(base.squad.position) != base.squad.battle.IsOutsideWall(squad.position);
		}
		return false;
	}

	public override string DebugUIText()
	{
		return "O";
	}
}
