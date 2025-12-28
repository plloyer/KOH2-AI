namespace Logic;

public class ClimbingBuff : SquadBuff
{
	public class Def : Logic.Def
	{
	}

	public ClimbingBuff(Squad squad, Logic.Def def, DT.Field field = null)
		: base(squad, def, field)
	{
	}

	public new static SquadBuff Create(Squad owner, Logic.Def def, DT.Field field = null)
	{
		return new ClimbingBuff(owner, def, field);
	}

	public override void Init(Squad squad, Logic.Def def, DT.Field field = null)
	{
		base.Init(squad, def, field);
	}

	public override bool Validate()
	{
		if (!base.Validate())
		{
			return false;
		}
		if (squad.climbing)
		{
			return true;
		}
		if (squad.position.paID > 0 && squad.battle.towers != null)
		{
			PathData.PassableArea pA = squad.game.path_finding.data.pointers.GetPA(squad.position.paID - 1);
			if (pA.type != PathData.PassableArea.Type.Tower && pA.type != PathData.PassableArea.Type.Wall)
			{
				return false;
			}
			Fortification fortification = null;
			float reservationRadius = squad.GetReservationRadius();
			float num = reservationRadius * reservationRadius;
			for (int i = 0; i < squad.battle.towers.Count; i++)
			{
				Fortification fortification2 = squad.battle.towers[i];
				if (!fortification2.IsDefeated() && fortification2.battle_side != squad.battle_side)
				{
					float num2 = fortification2.position.SqrDist(squad.position);
					if (num2 < num)
					{
						fortification = fortification2;
						num = num2;
					}
				}
			}
			if (fortification != null)
			{
				return true;
			}
		}
		return false;
	}

	public override string DebugUIText()
	{
		return "C";
	}
}
