using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic;

public class SquadPowerGrid
{
	public struct Threat
	{
		public float base_threat;

		public float infantry_threat;

		public float anti_cavalry_threat;

		public float salvos_about_to_land_threat;

		public static Threat Zero => new Threat(0f, 0f, 0f, 0f);

		public Threat(float base_threat, float infantry_threat, float anti_cavalry_threat, float salvos_about_to_land_threat)
		{
			this.base_threat = base_threat;
			this.infantry_threat = infantry_threat;
			this.anti_cavalry_threat = anti_cavalry_threat;
			this.salvos_about_to_land_threat = salvos_about_to_land_threat;
		}

		public override string ToString()
		{
			return $"{base_threat}, {infantry_threat}, {anti_cavalry_threat}, {salvos_about_to_land_threat}";
		}

		public static Threat operator +(Threat pt1, Threat pt2)
		{
			return new Threat(pt1.base_threat + pt2.base_threat, pt1.infantry_threat + pt2.infantry_threat, pt1.anti_cavalry_threat + pt2.anti_cavalry_threat, pt1.salvos_about_to_land_threat + pt2.salvos_about_to_land_threat);
		}

		public static Threat operator -(Threat pt1, Threat pt2)
		{
			return new Threat(pt1.base_threat - pt2.base_threat, pt1.infantry_threat - pt2.infantry_threat, pt1.anti_cavalry_threat - pt2.anti_cavalry_threat, pt1.salvos_about_to_land_threat - pt2.salvos_about_to_land_threat);
		}

		public static Threat operator -(Threat pt)
		{
			return new Threat(0f - pt.base_threat, 0f - pt.infantry_threat, 0f - pt.anti_cavalry_threat, 0f - pt.salvos_about_to_land_threat);
		}

		public static Threat operator *(Threat pt, float f)
		{
			return new Threat(pt.base_threat * f, pt.infantry_threat * f, pt.anti_cavalry_threat * f, pt.salvos_about_to_land_threat * f);
		}

		public static Threat operator *(float f, Threat pt)
		{
			return new Threat(pt.base_threat * f, pt.infantry_threat * f, pt.anti_cavalry_threat * f, pt.salvos_about_to_land_threat * f);
		}
	}

	private const float hypotenuse = 1.4142f;

	public int tile_size_x;

	public int tile_size_y;

	public int width;

	public int height;

	public int grid_width;

	public int grid_height;

	public bool dirty = true;

	public int battle_side;

	public Battle battle;

	public Threat[,] grid;

	public Threat GetInterpolatedCell(float wx, float wy)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (!Valid(num, num2))
		{
			return Threat.Zero;
		}
		float num3 = wx - (float)num;
		float num4 = wy - (float)num2;
		Threat threat = grid[num, num2];
		if (!Valid(num + 1, num2))
		{
			return threat;
		}
		Threat threat2 = grid[num + 1, num2];
		if (!Valid(num, num2 + 1))
		{
			return threat;
		}
		Threat threat3 = grid[num, num2 + 1];
		if (!Valid(num + 1, num2 + 1))
		{
			return threat;
		}
		Threat threat4 = grid[num + 1, num2 + 1];
		Threat threat5 = threat + (threat2 - threat) * num3;
		Threat threat6 = threat3 + (threat4 - threat3) * num3;
		return threat5 + (threat6 - threat5) * num4;
	}

	public SquadPowerGrid(int tile_size_x, int tile_size_y, int width, int height, Battle battle, int battle_side)
	{
		this.tile_size_x = tile_size_x;
		this.tile_size_y = tile_size_y;
		this.width = width;
		this.height = height;
		grid_width = width / tile_size_x;
		grid_height = height / tile_size_y;
		grid = new Threat[grid_width, grid_height];
		this.battle = battle;
		this.battle_side = battle_side;
	}

	public void SetBaseThreat(float wx, float wy, float val)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (Valid(num, num2))
		{
			Threat threat = grid[num, num2];
			threat.base_threat = val;
			grid[num, num2] = threat;
		}
	}

	public float GetBaseThreatInterpolated(float wx, float wy)
	{
		return GetInterpolatedCell(wx, wy).base_threat;
	}

	public float GetAntiCavalryInterpolated(float wx, float wy)
	{
		return GetInterpolatedCell(wx, wy).anti_cavalry_threat;
	}

	public float GetInfantryInterpolated(float wx, float wy)
	{
		return GetInterpolatedCell(wx, wy).infantry_threat;
	}

	public float GetBaseThreat(float wx, float wy)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (!Valid(num, num2))
		{
			return 0f;
		}
		return grid[num, num2].base_threat;
	}

	public bool Valid(int x, int y)
	{
		if (x > 0 && x < grid_width && y > 0)
		{
			return y < grid_height;
		}
		return false;
	}

	public void SetAntiCavalryThreat(float wx, float wy, float val)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (Valid(num, num2))
		{
			Threat threat = grid[num, num2];
			threat.anti_cavalry_threat = val;
			grid[num, num2] = threat;
		}
	}

	public void SetInfantryThreat(float wx, float wy, float val)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (Valid(num, num2))
		{
			Threat threat = grid[num, num2];
			threat.infantry_threat = val;
			grid[num, num2] = threat;
		}
	}

	public void SetSalvosAboutToLandThreat(float wx, float wy, float val)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (Valid(num, num2))
		{
			Threat threat = grid[num, num2];
			threat.salvos_about_to_land_threat = val;
			grid[num, num2] = threat;
		}
	}

	public float GetAntiCavalryThreat(float wx, float wy)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (!Valid(num, num2))
		{
			return 0f;
		}
		return grid[num, num2].anti_cavalry_threat;
	}

	public float GetInfantryThreat(float wx, float wy)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (!Valid(num, num2))
		{
			return 0f;
		}
		return grid[num, num2].infantry_threat;
	}

	public float GetSalvosAboutToLandThreat(float wx, float wy)
	{
		wx /= (float)tile_size_x;
		wy /= (float)tile_size_y;
		int num = Mathf.FloorToInt(wx);
		int num2 = Mathf.FloorToInt(wy);
		if (!Valid(num, num2))
		{
			return 0f;
		}
		return grid[num, num2].salvos_about_to_land_threat;
	}

	public void MarkDirty()
	{
		dirty = true;
	}

	public void Apply()
	{
		if (!dirty)
		{
			return;
		}
		dirty = false;
		for (int i = 0; i < grid_width; i++)
		{
			for (int j = 0; j < grid_height; j++)
			{
				grid[i, j] = Threat.Zero;
			}
		}
		RecalculateSquads();
		RecalculateFortifications();
	}

	private void RecalculateSquads()
	{
		List<Squad> list = battle.squads.Get(battle_side);
		for (int i = 0; i < list.Count; i++)
		{
			Squad squad = list[i];
			if (squad.IsDefeated())
			{
				continue;
			}
			int num = Mathf.FloorToInt(squad.position.x / (float)tile_size_x);
			int num2 = Mathf.FloorToInt(squad.position.y / (float)tile_size_y);
			Point pt = new Point(num, num2);
			if (!Valid(num, num2))
			{
				continue;
			}
			int num3 = 1;
			int num4 = 0;
			bool flag = squad.def.is_ranged && !squad.is_fighting && squad.salvos_left > 0;
			float num5 = squad.Threat();
			if (flag)
			{
				num3 = Mathf.FloorToInt(squad.salvo_def.max_shoot_range / (float)tile_size_x);
				num4 = Mathf.FloorToInt(squad.salvo_def.min_shoot_range / (float)tile_size_x);
				if (squad.ranged_enemy != null)
				{
					PPos pPos = squad.ranged_enemy.VisualPosition();
					int num6 = Mathf.FloorToInt(pPos.x / (float)tile_size_x);
					int num7 = Mathf.FloorToInt(pPos.y / (float)tile_size_y);
					Point pt2 = new Point(num6, num7);
					for (int j = num6 - 1; j <= num6 + 1; j++)
					{
						for (int k = num7 - 1; k <= num7 + 1; k++)
						{
							float num8 = new Point(j, k).Dist(pt2);
							float num9 = 1f - num8 / 2f;
							float salvosAboutToLandThreat = GetSalvosAboutToLandThreat(j * tile_size_x, k * tile_size_y);
							float num10 = num5 * squad.def.CTH_shoot_mod * battle.ai[battle_side].def.threat_salvos_mod * num9;
							SetSalvosAboutToLandThreat(j * tile_size_x, k * tile_size_y, salvosAboutToLandThreat + num10);
						}
					}
				}
			}
			for (int l = num - num3; l <= num + num3; l++)
			{
				for (int m = num2 - num3; m <= num2 + num3; m++)
				{
					float num11 = new Point(l, m).Dist(pt);
					float num12;
					if (flag)
					{
						if (num11 > (float)num3 || (squad.def.is_siege_eq && num11 < (float)num4))
						{
							continue;
						}
						num12 = Math.Max(0.1f, num11 / (float)(num3 + 1));
					}
					else
					{
						num12 = 1f - num11 / 2.4141998f;
					}
					if (Valid(l, m))
					{
						float baseThreat = GetBaseThreat(l * tile_size_x, m * tile_size_y);
						SetBaseThreat(l * tile_size_x, m * tile_size_y, baseThreat + num5 * num12);
						if (list[i].def.CTH_cavalry_mod > 1f)
						{
							float antiCavalryThreat = GetAntiCavalryThreat(l * tile_size_x, m * tile_size_y);
							float num13 = num5 * num12 * squad.def.CTH_cavalry_mod;
							SetAntiCavalryThreat(l * tile_size_x, m * tile_size_y, antiCavalryThreat + num13);
						}
						if (!squad.def.is_cavalry && !squad.def.is_ranged)
						{
							float infantryThreat = GetInfantryThreat(l * tile_size_x, m * tile_size_y);
							float num14 = num5 * num12;
							SetInfantryThreat(l * tile_size_x, m * tile_size_y, infantryThreat + num14);
						}
					}
				}
			}
		}
	}

	private void RecalculateFortifications()
	{
		if (battle.towers == null)
		{
			return;
		}
		List<Fortification> towers = battle.towers;
		for (int i = 0; i < towers.Count; i++)
		{
			Fortification fortification = towers[i];
			if (fortification.battle_side != battle_side || fortification.shoot_comp == null || fortification.IsDefeated())
			{
				continue;
			}
			int num = Mathf.FloorToInt(fortification.position.x / (float)tile_size_x);
			int num2 = Mathf.FloorToInt(fortification.position.y / (float)tile_size_y);
			Point pt = new Point(num, num2);
			if (!Valid(num, num2))
			{
				continue;
			}
			int num3 = Mathf.FloorToInt(fortification.shoot_comp.salvo_def.max_shoot_range / (float)tile_size_x);
			int num4 = Mathf.FloorToInt(fortification.shoot_comp.salvo_def.min_shoot_range / (float)tile_size_x);
			for (int j = num - num3; j <= num + num3; j++)
			{
				for (int k = num2 - num3; k <= num2 + num3; k++)
				{
					float num5 = new Point(j, k).Dist(pt);
					if (!(num5 > (float)num3) && !(num5 < (float)num4))
					{
						float num6 = 1f - num5 / (float)(num3 + 1);
						if (Valid(j, k))
						{
							float baseThreat = GetBaseThreat(j * tile_size_x, k * tile_size_y);
							float num7 = fortification.Threat();
							SetBaseThreat(j * tile_size_x, k * tile_size_y, baseThreat + num7 * num6);
						}
					}
				}
			}
		}
	}
}
