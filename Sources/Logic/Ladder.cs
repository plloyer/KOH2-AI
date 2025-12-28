using System.Collections.Generic;

namespace Logic;

public class Ladder : MapObject
{
	public class Def : Logic.Def
	{
		public float ladder_leftover_duration = 30f;

		public float max_t_appear = 10f;

		public override bool Load(Game game)
		{
			ladder_leftover_duration = base.field.GetFloat("ladder_leftover_duration", null, ladder_leftover_duration);
			max_t_appear = base.field.GetFloat("max_t_appear", null, max_t_appear);
			return base.Load(game);
		}
	}

	public int paID;

	public List<Squad> squads = new List<Squad>();

	public Squad cur_squad;

	public Time time_despawn_start;

	public Def def;

	public bool rotating;

	public ComputableValue ladder_rot_progress;

	private Battle battle;

	public bool visible = true;

	public static bool mass_destroying;

	public Ladder(Battle battle, int paID, PPos position)
		: base(battle.batte_view_game, position, battle.kingdom_id)
	{
		this.battle = battle;
		def = game.defs.GetBase<Def>();
		this.paID = paID;
		if (battle.ladders == null)
		{
			battle.ladders = new Dictionary<int, Ladder>();
		}
		if (battle.batte_view_game?.path_finding?.data != null)
		{
			PathData.PassableArea pA = battle.batte_view_game.path_finding.data.pointers.GetPA(paID - 1);
			pA.HasLadder = true;
			battle.batte_view_game.path_finding.data.pointers.SetPA(paID - 1, pA);
		}
		battle.ladders[paID] = this;
		ladder_rot_progress = new ComputableValue(0f, 0f, battle.batte_view_game, 0f, 1f);
		UpdateNextFrame();
	}

	public override IRelationCheck GetStanceObj()
	{
		return null;
	}

	public void AddSquad(Squad sq)
	{
		if (!sq.ladders.ContainsKey(paID))
		{
			sq.ladders[paID] = this;
			squads.Add(sq);
		}
	}

	public void DelSquad(Squad sq)
	{
		if (cur_squad == sq)
		{
			cur_squad = null;
		}
		int count = squads.Count;
		squads.Remove(sq);
		sq.ladders.Remove(paID);
		if (squads.Count == 0 && count != 0)
		{
			time_despawn_start = game.time;
		}
	}

	public void DelSquad(int i)
	{
		int count = squads.Count;
		squads.RemoveAt(i);
		if (squads.Count == 0 && count != 0)
		{
			time_despawn_start = game.time;
		}
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		bool flag = false;
		for (int num = squads.Count - 1; num >= 0; num--)
		{
			Squad squad = squads[num];
			if (squad == null || !squad.IsValid() || squad.NumTroops() == 0)
			{
				DelSquad(num);
				flag = true;
			}
			else if (squad.climbing && squad.cur_ladder_paid == paID)
			{
				if (cur_squad == null)
				{
					cur_squad = squad;
				}
				flag = true;
				break;
			}
		}
		if (flag)
		{
			StartRotating();
		}
		else
		{
			cur_squad = null;
		}
		if (!Area_valid() || (squads.Count == 0 && !visible))
		{
			Destroy();
		}
		else
		{
			UpdateNextFrame();
		}
	}

	public void StartRotating()
	{
		if (!rotating && ladder_rot_progress.Get() != 1f && ladder_rot_progress.GetRate() != 1f && cur_squad != null)
		{
			rotating = true;
			ladder_rot_progress.SetRate(1f);
			battle.OnSquadPlaceLadder(cur_squad.battle_side);
		}
	}

	public bool Area_valid()
	{
		return game.path_finding.data.pointers.GetPA(paID - 1).enabled;
	}

	protected override void OnDestroy()
	{
		if (battle.batte_view_game?.path_finding?.data != null)
		{
			PathData.PassableArea pA = battle.batte_view_game.path_finding.data.pointers.GetPA(paID - 1);
			pA.HasLadder = false;
			battle.batte_view_game.path_finding.data.pointers.SetPA(paID - 1, pA);
		}
		base.OnDestroy();
		if (!mass_destroying)
		{
			battle.ladders.Remove(paID);
		}
	}

	public static void ClearAll(Battle battle)
	{
		if (battle.ladders == null)
		{
			return;
		}
		mass_destroying = true;
		foreach (KeyValuePair<int, Ladder> ladder in battle.ladders)
		{
			ladder.Value.Destroy();
		}
		mass_destroying = false;
		battle.ladders.Clear();
	}
}
