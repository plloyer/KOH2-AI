using System.Collections.Generic;

namespace Logic;

public class CapturePoint : MapObject
{
	public class Def : Logic.Def
	{
		public float radius_mod = 1f;

		public DT.Field capture_time;

		public bool count_victory = true;

		public string OurCapturePointTakenByUs;

		public string OurCapturePointTakenByEnemy;

		public string EnemyCapturePointTakenByUs;

		public string EnemyCapturePointTakenByEnemy;

		public string EnemyAttemptsTakeOurCapturePoint;

		public string WeAttemptTakeOurCapturePoint;

		public string EnemyAttemptTakeEnemyCapturePoint;

		public string WeAttemptTakeEnemyCapturePoint;

		public override bool Load(Game game)
		{
			radius_mod = base.field.GetFloat("radius_mod", null, radius_mod);
			capture_time = base.field.FindChild("capture_time");
			count_victory = base.field.GetBool("count_victory", null, count_victory);
			OurCapturePointTakenByUs = base.field.GetString("OurCapturePointTakenByUs");
			OurCapturePointTakenByEnemy = base.field.GetString("OurCapturePointTakenByEnemy");
			EnemyCapturePointTakenByUs = base.field.GetString("EnemyCapturePointTakenByUs");
			EnemyCapturePointTakenByEnemy = base.field.GetString("EnemyCapturePointTakenByEnemy");
			EnemyAttemptsTakeOurCapturePoint = base.field.GetString("EnemyAttemptsTakeOurCapturePoint");
			WeAttemptTakeOurCapturePoint = base.field.GetString("WeAttemptTakeOurCapturePoint");
			EnemyAttemptTakeEnemyCapturePoint = base.field.GetString("EnemyAttemptTakeEnemyCapturePoint");
			WeAttemptTakeEnemyCapturePoint = base.field.GetString("WeAttemptTakeEnemyCapturePoint");
			return true;
		}

		public float CaptureTime(CapturePoint cp)
		{
			if (capture_time == null)
			{
				return 1f;
			}
			return capture_time.Float(cp);
		}

		public float CaptureSpeed(CapturePoint cp)
		{
			return 1f / CaptureTime(cp);
		}
	}

	public int battle_side = 1;

	public int original_battle_side = 1;

	public ComputableValue capture_progress;

	public bool is_capturing;

	public Battle battle;

	public bool IsInsideWall = true;

	public float radius = 1f;

	public Def def;

	public List<int> paids = new List<int>();

	public bool has_friendly_troops;

	public List<Squad> ally_squads = new List<Squad>();

	public bool has_enemy_troops;

	private int friendly_squads;

	public List<Squad> capturing_squads = new List<Squad>();

	public List<Fortification> fortifications = new List<Fortification>();

	public float capture_speed { get; private set; }

	public Fortification fortification
	{
		get
		{
			if (fortifications.Count == 0)
			{
				return null;
			}
			if (fortifications.Count == 1)
			{
				return fortifications[0];
			}
			Fortification result = null;
			for (int i = 0; i < fortifications.Count; i++)
			{
				if (fortifications[i].def.type == Fortification.Type.Gate)
				{
					result = fortifications[i];
					break;
				}
			}
			return result;
		}
	}

	public CapturePoint(Battle battle, PPos pos, float radius, bool inside_wall, string type, int battle_side)
		: base(battle.batte_view_game, pos, battle.GetSideKingdom(battle_side).id)
	{
		if (type != null)
		{
			def = game.defs.Find<Def>(type + "CapturePoint");
		}
		if (def == null)
		{
			def = game.defs.GetBase<Def>();
		}
		this.battle = battle;
		if (battle.capture_points == null)
		{
			battle.capture_points = new List<CapturePoint>();
		}
		battle.capture_points.Add(this);
		this.radius = radius * def.radius_mod;
		capture_progress = new ComputableValue(1f, 0f, battle.batte_view_game, 0f, 1f);
		IsInsideWall = inside_wall;
		if (type == "Towers" || type == "Gates")
		{
			IsInsideWall = true;
		}
		this.battle_side = battle_side;
		original_battle_side = this.battle_side;
		battle.NotifyListeners("capture_points_changed");
		CheckSquadControl();
		UpdateInBatch(game.update_1sec);
	}

	protected override void OnDestroy()
	{
		if (battle != null)
		{
			battle.capture_points.Remove(this);
			battle.ai[battle_side].DelCapturePoint(this);
			battle.ai[1 - battle_side].DelCapturePoint(this);
		}
		base.OnDestroy();
	}

	private void CalcSquadsUnder()
	{
		has_friendly_troops = false;
		has_enemy_troops = false;
		friendly_squads = 0;
		ally_squads.Clear();
		capturing_squads.Clear();
		for (int i = 0; i < 2; i++)
		{
			List<Squad> list = battle.squads.Get(i);
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				Squad squad = list[j];
				if (!squad.IsDefeated() && squad.is_main_squad && i == battle_side)
				{
					friendly_squads++;
				}
			}
			for (int k = 0; k < list.Count; k++)
			{
				Squad squad2 = list[k];
				if (squad2.IsDefeated() || squad2.is_inside_walls_or_on_walls != IsInsideWall || squad2.climbing)
				{
					continue;
				}
				PPos pPos = squad2.VisualPosition();
				_ = squad2.position;
				if (pPos.Dist(position) < radius)
				{
					flag = true;
					if (battle_side != i)
					{
						capturing_squads.Add(squad2);
						break;
					}
					ally_squads.Add(squad2);
				}
			}
			if (flag)
			{
				if (battle_side != i)
				{
					has_enemy_troops = true;
				}
				else
				{
					has_friendly_troops = true;
				}
			}
		}
	}

	public void CheckSquadControl()
	{
		if (battle == null || !battle.IsValid() || battle.IsFinishing() || game == null || !game.IsValid())
		{
			return;
		}
		if (fortifications != null && fortifications.Count > 0)
		{
			bool flag = true;
			for (int i = 0; i < fortifications.Count; i++)
			{
				if (!fortifications[i].IsDefeated())
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return;
			}
		}
		capture_speed = def.CaptureSpeed(this);
		float num = capture_progress.Get();
		bool flag2 = is_capturing;
		if (is_capturing)
		{
			if (num <= 0f && capture_progress.GetRate() < 0f)
			{
				FlipSide();
				return;
			}
			if (num >= 1f && capture_progress.GetRate() > 0f)
			{
				is_capturing = false;
				return;
			}
		}
		if (has_enemy_troops)
		{
			if (!has_friendly_troops)
			{
				is_capturing = true;
				capture_progress.SetRate(0f - capture_speed);
			}
			else
			{
				capture_progress.SetRate(0f);
			}
		}
		else if (num >= 1f)
		{
			capture_progress.SetRate(0f);
		}
		else
		{
			capture_progress.SetRate(capture_speed);
		}
		if (is_capturing && !flag2 && num >= 1f)
		{
			NotifyListeners("started_capture");
		}
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		if (!battle.battle_map_finished)
		{
			CalcSquadsUnder();
			CheckSquadControl();
		}
	}

	public void FlipSide()
	{
		SetBattleSide(1 - battle_side);
	}

	public void SetBattleSide(int side)
	{
		if (side == battle_side)
		{
			return;
		}
		int num = battle_side;
		battle.ai[num].DelCapturePoint(this);
		battle.ai[1 - num].DelCapturePoint(this);
		battle_side = side;
		capture_progress.Set(1f);
		capture_progress.SetRate(0f);
		is_capturing = false;
		if (fortifications != null)
		{
			for (int i = 0; i < fortifications.Count; i++)
			{
				Fortification fortification = fortifications[i];
				fortification.battle_side = battle_side;
				if (fortification.gate != null)
				{
					fortification.gate.battle_side = battle_side;
					fortification.gate.ResetAreas();
				}
			}
		}
		if (battle.initiative != null)
		{
			if (this.fortification != null)
			{
				if (this.fortification.def.type == Fortification.Type.Tower)
				{
					if (battle.initiative_side == battle_side)
					{
						battle.initiative.Add(battle.def.initiative_take_tower);
					}
					else
					{
						battle.initiative.Add(0f - battle.def.initiative_lose_tower);
					}
				}
				else if (this.fortification.def.type == Fortification.Type.Gate)
				{
					if (battle.initiative_side == battle_side)
					{
						battle.initiative.Add(battle.def.initiative_take_gate);
					}
					else
					{
						battle.initiative.Add(0f - battle.def.initiative_lose_gate);
					}
				}
			}
			else if (battle.initiative_side == battle_side)
			{
				battle.initiative.Add(battle.def.initiative_take_capture_point);
			}
			else
			{
				battle.initiative.Add(0f - battle.def.initiative_lose_capture_point);
			}
		}
		battle.ai[battle_side].AddCapturePoint(this);
		battle.ai[1 - battle_side].AddCapturePoint(this);
		NotifyListeners("battle_side_changed");
	}

	public float GetImportance()
	{
		Fortification fortification = this.fortification;
		float num = ((fortification != null) ? 0.75f : 0f);
		if (fortification != null)
		{
			switch (fortification.def.type)
			{
			case Fortification.Type.Gate:
				num *= 2f;
				break;
			case Fortification.Type.Tower:
				num *= 1.5f;
				break;
			}
		}
		if (!def.count_victory)
		{
			return 0.5f * (capture_speed + num);
		}
		return 1f;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "controller":
			if (battle_side == 0)
			{
				return battle?.attacker_kingdom;
			}
			return battle?.defender_kingdom;
		case "challenger":
			if (battle_side == 0)
			{
				return battle?.defender_kingdom;
			}
			return battle?.attacker_kingdom;
		case "control":
			return capture_progress.Get() * 100f;
		case "friendly_squads":
			return friendly_squads;
		case "siege_defense":
			return battle.siege_defense;
		case "is_capturing":
			return is_capturing;
		default:
			return base.GetVar(key, vars, as_value);
		}
	}
}
