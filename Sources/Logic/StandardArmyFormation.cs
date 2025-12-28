using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic;

public class StandardArmyFormation : ArmyFormation
{
	public enum ArmyFormationType
	{
		Default,
		RangedAtFront,
		OneLine,
		TwoLines,
		TwoLinesRangedAtFront,
		TwoLinesAndReserve,
		TwoLinesAndReserveRangedAtFront
	}

	public struct StandardArmyFormationSettings
	{
		public ArmyFormationType formation_type;

		public float rows_spacing;

		public float lines_spacing;

		public float center_line_offset;

		public float flanks_line_offset;

		public float reserve_line_offset;

		public int reserve_line_limit;

		public int reserve_row_limit;

		public int center_line_limit;

		public int center_row_limit;

		public int flank_line_limit;

		public int flank_row_limit;

		public float pos_check_delta;

		public bool one_sided;

		public bool ignore_right;

		public float emergency_offset;

		public bool no_radius_test;

		public static StandardArmyFormationSettings Default_settings => new StandardArmyFormationSettings(ArmyFormationType.Default);

		public static StandardArmyFormationSettings Close_settings => new StandardArmyFormationSettings(ArmyFormationType.TwoLinesAndReserve, 1f, 1f, 20f, 20f);

		private StandardArmyFormationSettings(ArmyFormationType formation_type, float rows_spacing = 1f, float lines_spacing = 5f, float center_line_offset = 30f, float flanks_line_offset = 15f, float reserve_line_offset = 0f, int reserve_line_limit = 3, int reserve_row_limit = 5, int center_line_limit = 6, int center_row_limit = 7, int flank_line_limit = 4, int flank_row_limit = 3, float pos_check_delta = 5f, bool one_sided = false, bool ignore_right = false, float emergency_offset = 25f, bool no_radius_test = false)
		{
			this.formation_type = formation_type;
			this.rows_spacing = rows_spacing;
			this.lines_spacing = lines_spacing;
			this.center_line_offset = center_line_offset;
			this.flanks_line_offset = flanks_line_offset;
			this.reserve_line_offset = reserve_line_offset;
			this.reserve_line_limit = reserve_line_limit;
			this.reserve_row_limit = reserve_row_limit;
			this.center_line_limit = center_line_limit;
			this.center_row_limit = center_row_limit;
			this.flank_line_limit = flank_line_limit;
			this.flank_row_limit = flank_row_limit;
			this.pos_check_delta = pos_check_delta;
			this.one_sided = one_sided;
			this.ignore_right = ignore_right;
			this.emergency_offset = emergency_offset;
			this.no_radius_test = no_radius_test;
		}
	}

	public StandardArmyFormationSettings settings;

	public const float MIN_SLOT_WIDTH = 10f;

	public const float MIN_SLOT_HEIGHT = 5f;

	public List<Squad> center_front_squads = new List<Squad>();

	public List<Squad> center_back_squads = new List<Squad>();

	public List<Squad> reserve_squads = new List<Squad>();

	public List<Squad> flanks_squads = new List<Squad>();

	public StandardArmyFormation()
	{
		settings = StandardArmyFormationSettings.Default_settings;
	}

	public StandardArmyFormation(StandardArmyFormationSettings settings, bool one_sided = false, bool ignore_right = false)
	{
		this.settings = settings;
		this.settings.one_sided = one_sided;
		this.settings.ignore_right = ignore_right;
	}

	public StandardArmyFormation(ArmyFormationType formation_type, bool one_sided = false, bool ignore_right = false)
	{
		settings = StandardArmyFormationSettings.Default_settings;
		settings.formation_type = formation_type;
		settings.one_sided = one_sided;
		settings.ignore_right = ignore_right;
	}

	public StandardArmyFormation(float center_line_offset, float flanks_line_offset, float rows_spacing = 1f, float lines_spacing = 5f, bool one_sided = false, bool ignore_right = false, ArmyFormationType formation_type = ArmyFormationType.Default)
	{
		settings = StandardArmyFormationSettings.Default_settings;
		settings.rows_spacing = rows_spacing;
		settings.lines_spacing = lines_spacing;
		settings.center_line_offset = center_line_offset;
		settings.flanks_line_offset = flanks_line_offset;
		settings.one_sided = one_sided;
		settings.ignore_right = ignore_right;
		settings.formation_type = formation_type;
	}

	private void ClearFormation()
	{
		center_front_squads.Clear();
		center_back_squads.Clear();
		reserve_squads.Clear();
		flanks_squads.Clear();
	}

	public void SetDefaultFormation(List<Squad> squads)
	{
		ClearFormation();
		UpdateFormationSquads(squads);
		center_front_squads.AddRange(formation_squads.GetAllNotRangedInfantry());
		center_back_squads.AddRange(formation_squads.ranged);
		reserve_squads.Add(formation_squads.commander);
		reserve_squads.AddRange(formation_squads.noble);
		flanks_squads.AddRange(formation_squads.GetAllCavalryWithoutNoble());
	}

	public void SetOneLineFormation(List<Squad> squads)
	{
		ClearFormation();
		UpdateFormationSquads(squads);
		center_front_squads.AddRange(formation_squads.GetAll());
	}

	public void SetTwoLineFormation(List<Squad> squads)
	{
		ClearFormation();
		UpdateFormationSquads(squads);
		center_front_squads.AddRange(formation_squads.GetAllNotRangedInfantry());
		center_front_squads.AddRange(formation_squads.cavalry);
		center_back_squads.Add(formation_squads.commander);
		center_back_squads.AddRange(formation_squads.noble);
		center_back_squads.AddRange(formation_squads.ranged);
		center_back_squads.AddRange(formation_squads.ranged_cavalry);
	}

	public void SetTwoLineRangedAtFrontFormation(List<Squad> squads)
	{
		ClearFormation();
		UpdateFormationSquads(squads);
		center_back_squads.Add(formation_squads.commander);
		center_back_squads.AddRange(formation_squads.noble);
		center_back_squads.AddRange(formation_squads.GetAllNotRangedInfantry());
		center_back_squads.AddRange(formation_squads.cavalry);
		center_front_squads.AddRange(formation_squads.ranged);
		center_front_squads.AddRange(formation_squads.ranged_cavalry);
	}

	public void SetTwoLineAndReserveFormation(List<Squad> squads)
	{
		ClearFormation();
		UpdateFormationSquads(squads);
		center_front_squads.AddRange(formation_squads.GetAllNotRangedInfantry());
		center_front_squads.AddRange(formation_squads.cavalry);
		center_back_squads.AddRange(formation_squads.ranged);
		center_back_squads.AddRange(formation_squads.ranged_cavalry);
		reserve_squads.Add(formation_squads.commander);
		reserve_squads.AddRange(formation_squads.noble);
	}

	public void SetTwoLineAndReserveRangedAtFrontFormation(List<Squad> squads)
	{
		ClearFormation();
		UpdateFormationSquads(squads);
		center_back_squads.AddRange(formation_squads.GetAllNotRangedInfantry());
		center_back_squads.AddRange(formation_squads.cavalry);
		center_front_squads.AddRange(formation_squads.ranged);
		center_front_squads.AddRange(formation_squads.ranged_cavalry);
		reserve_squads.Add(formation_squads.commander);
		reserve_squads.AddRange(formation_squads.noble);
	}

	public void SetRangedAtFrontFormation(List<Squad> squads)
	{
		ClearFormation();
		UpdateFormationSquads(squads);
		center_front_squads.AddRange(formation_squads.ranged);
		center_back_squads.AddRange(formation_squads.GetAllNotRangedInfantry());
		reserve_squads.Add(formation_squads.commander);
		reserve_squads.AddRange(formation_squads.noble);
		flanks_squads.AddRange(formation_squads.GetAllCavalryWithoutNoble());
	}

	public void SetCustomFormation(List<Squad> center_front_squads, List<Squad> center_back_squads, List<Squad> reserve_squads, List<Squad> flanks_squads)
	{
		ClearFormation();
		this.center_front_squads = center_front_squads;
		this.center_back_squads = center_back_squads;
		this.reserve_squads = reserve_squads;
		this.flanks_squads = flanks_squads;
	}

	private void SetActualFormation(List<Squad> squads)
	{
		switch (settings.formation_type)
		{
		case ArmyFormationType.Default:
			SetDefaultFormation(squads);
			break;
		case ArmyFormationType.RangedAtFront:
			SetRangedAtFrontFormation(squads);
			break;
		case ArmyFormationType.OneLine:
			SetOneLineFormation(squads);
			break;
		case ArmyFormationType.TwoLines:
			SetTwoLineFormation(squads);
			break;
		case ArmyFormationType.TwoLinesRangedAtFront:
			SetTwoLineRangedAtFrontFormation(squads);
			break;
		case ArmyFormationType.TwoLinesAndReserve:
			SetTwoLineAndReserveFormation(squads);
			break;
		case ArmyFormationType.TwoLinesAndReserveRangedAtFront:
			SetTwoLineAndReserveRangedAtFrontFormation(squads);
			break;
		}
	}

	public bool CreateFormationWithWallCheck(PPos pos, float heading, List<Squad> squads, out Dictionary<Squad, PPos> offsets, Func<PPos, float, bool> IsPassable, Func<Vector3, bool> IsInsideWall, bool inside_wall = false, bool use_hungarian_algorithm = false)
	{
		offsets = new Dictionary<Squad, PPos>();
		SetActualFormation(squads);
		float furthest_right_offset = 0f;
		float furthest_left_offset = 0f;
		List<PPos> list = SetUpReserve(reserve_squads, pos, heading, inside_wall, settings.one_sided, IsPassable, IsInsideWall, out furthest_right_offset, out furthest_left_offset, wall_check: true);
		AssignSquads(reserve_squads, list, ref offsets, pos, heading, use_hungarian_algorithm);
		float furthest_right_offset2 = 0f;
		float furthest_left_offset2 = 0f;
		List<PPos> list2 = SetUpCenter(center_front_squads, center_back_squads, pos, heading, inside_wall, settings.one_sided, IsPassable, IsInsideWall, out furthest_right_offset2, out furthest_left_offset2, wall_check: true);
		List<Squad> list3 = new List<Squad>(center_front_squads);
		list3.AddRange(center_back_squads);
		AssignSquads(list3, list2, ref offsets, pos, heading, use_hungarian_algorithm);
		float right_offset = ((furthest_right_offset2 > furthest_right_offset) ? furthest_right_offset2 : furthest_right_offset);
		float left_offset = ((furthest_left_offset2 < furthest_left_offset) ? furthest_left_offset2 : furthest_left_offset);
		List<PPos> list4 = SetUpFlanks(flanks_squads, pos, heading, inside_wall, right_offset, left_offset, IsPassable, IsInsideWall, wall_check: true);
		AssignSquads(flanks_squads, list4, ref offsets, pos, heading, use_hungarian_algorithm);
		base.offsets = new Dictionary<Squad, PPos>(offsets);
		return true;
	}

	public override bool CreateFormation(PPos pos, float heading, List<Squad> squads, out Dictionary<Squad, PPos> offsets, Func<PPos, float, bool> IsCorrectPosFunc, bool use_hungarian_algorithm = false)
	{
		offsets = new Dictionary<Squad, PPos>();
		SetActualFormation(squads);
		float furthest_right_offset = 0f;
		float furthest_left_offset = 0f;
		List<PPos> list = SetUpReserve(reserve_squads, pos, heading, inside_wall: false, settings.one_sided, IsCorrectPosFunc, null, out furthest_right_offset, out furthest_left_offset);
		AssignSquads(reserve_squads, list, ref offsets, pos, heading, use_hungarian_algorithm);
		float furthest_right_offset2 = 0f;
		float furthest_left_offset2 = 0f;
		List<PPos> list2 = SetUpCenter(center_front_squads, center_back_squads, pos, heading, inside_wall: false, settings.one_sided, IsCorrectPosFunc, null, out furthest_right_offset2, out furthest_left_offset2);
		List<PPos> range = list2.GetRange(0, center_front_squads.Count);
		List<PPos> range2 = list2.GetRange(center_front_squads.Count, center_back_squads.Count);
		AssignSquads(center_front_squads, range, ref offsets, pos, heading, use_hungarian_algorithm);
		AssignSquads(center_back_squads, range2, ref offsets, pos, heading, use_hungarian_algorithm);
		float right_offset = ((furthest_right_offset2 > furthest_right_offset) ? furthest_right_offset2 : furthest_right_offset);
		float left_offset = ((furthest_left_offset2 < furthest_left_offset) ? furthest_left_offset2 : furthest_left_offset);
		List<PPos> list3 = SetUpFlanks(flanks_squads, pos, heading, inside_wall: false, right_offset, left_offset, IsCorrectPosFunc, null);
		AssignSquads(flanks_squads, list3, ref offsets, pos, heading, use_hungarian_algorithm);
		base.offsets = new Dictionary<Squad, PPos>(offsets);
		return true;
	}

	public bool CreateInsideWallsFormation(PPos pos, float heading, List<Squad> squads, out Dictionary<Squad, PPos> offsets, Func<PPos, float, bool> IsCorrectPosFunc, bool use_hungarian_algorithm = false)
	{
		StandardArmyFormationSettings standardArmyFormationSettings = settings;
		settings.formation_type = ArmyFormationType.OneLine;
		int num = Mathf.CeilToInt(Mathf.Sqrt(squads.Count));
		if (settings.one_sided)
		{
			num *= 2;
		}
		int center_line_limit = num * 2;
		settings.center_line_limit = center_line_limit;
		settings.center_row_limit = num;
		settings.rows_spacing = 0f;
		settings.lines_spacing = 2f;
		settings.no_radius_test = true;
		bool result = CreateFormation(pos, heading, squads, out offsets, IsCorrectPosFunc, use_hungarian_algorithm);
		settings = standardArmyFormationSettings;
		return result;
	}

	private List<PPos> SetUpCenter(List<Squad> front_squads, List<Squad> back_squads, PPos ref_pos, float heading, bool inside_wall, bool ignore_central_row, Func<PPos, float, bool> IsCorrectPosFunc, Func<Vector3, bool> IsInsideWall, out float furthest_right_offset, out float furthest_left_offset, bool wall_check = false)
	{
		int line = 0;
		int row = 0;
		float last_left_offset = 0f;
		float last_right_offset = 0f;
		float offset_x = settings.center_line_offset;
		float lowest_x_offset = offset_x;
		bool no_more_correct_pos = false;
		float furthest_right = 0f;
		float furthest_left = 0f;
		List<PPos> offsets = new List<PPos>();
		List<PPos> list = new List<PPos>();
		for (int i = 0; i < 3; i++)
		{
			switch (i)
			{
			case 1:
				ResetValues();
				offset_x = settings.center_line_offset + settings.emergency_offset;
				lowest_x_offset = offset_x;
				break;
			case 2:
				ResetValues();
				offset_x = settings.center_line_offset - settings.emergency_offset;
				lowest_x_offset = offset_x;
				break;
			}
			foreach (Squad front_squad in front_squads)
			{
				PPos item = FindNextPosition_Shortcut(front_squad);
				if (!no_more_correct_pos)
				{
					offsets.Add(item);
					list.Add(item);
					continue;
				}
				break;
			}
			if (no_more_correct_pos)
			{
				continue;
			}
			offset_x = lowest_x_offset - settings.lines_spacing;
			line++;
			row = 0;
			foreach (Squad back_squad in back_squads)
			{
				PPos item2 = FindNextPosition_Shortcut(back_squad);
				if (!no_more_correct_pos)
				{
					offsets.Add(item2);
					list.Add(item2);
					continue;
				}
				break;
			}
			if (!no_more_correct_pos)
			{
				break;
			}
		}
		if (no_more_correct_pos)
		{
			int num = front_squads.Count + back_squads.Count;
			if (list.Count >= num)
			{
				for (int num2 = list.Count - 1; num2 >= num; num2--)
				{
					list.RemoveAt(num2);
				}
				offsets = list;
			}
			else
			{
				int count = list.Count;
				if (count == 0)
				{
					list.Add(ref_pos);
				}
				while (list.Count < num)
				{
					int index = UnityEngine.Random.Range(0, count);
					list.Add(list[index]);
				}
				offsets = list;
			}
		}
		furthest_right_offset = furthest_right;
		furthest_left_offset = furthest_left;
		return offsets;
		PPos FindNextPosition_Shortcut(Squad sq)
		{
			return FindNextPosition(ref_pos, heading, inside_wall, ignore_central_row, settings.center_row_limit, settings.center_line_limit, sq, ref line, ref row, ref last_right_offset, ref last_left_offset, ref offset_x, ref lowest_x_offset, ref no_more_correct_pos, ref furthest_right, ref furthest_left, IsCorrectPosFunc, IsInsideWall, 0f, 0f, wall_check);
		}
		void ResetValues()
		{
			line = 0;
			row = 0;
			last_left_offset = 0f;
			last_right_offset = 0f;
			no_more_correct_pos = false;
			furthest_right = 0f;
			furthest_left = 0f;
			offsets = new List<PPos>();
		}
	}

	private List<PPos> SetUpReserve(List<Squad> squads, PPos ref_pos, float heading, bool inside_wall, bool ignore_central_row, Func<PPos, float, bool> IsCorrectPosFunc, Func<Vector3, bool> IsInsideWall, out float furthest_right_offset, out float furthest_left_offset, bool wall_check = false)
	{
		int line = 0;
		int row = 0;
		float last_left_offset = 0f;
		float last_right_offset = 0f;
		float offset_x = 0f;
		float lowest_x_offset = offset_x;
		bool no_more_correct_pos = false;
		float furthest_right = 0f;
		float furthest_left = 0f;
		List<PPos> offsets = new List<PPos>();
		List<PPos> list = new List<PPos>();
		for (int i = 0; i < 3; i++)
		{
			switch (i)
			{
			case 1:
				ResetValues();
				offset_x = settings.reserve_line_offset + settings.emergency_offset;
				lowest_x_offset = offset_x;
				break;
			case 2:
				ResetValues();
				offset_x = settings.reserve_line_offset - settings.emergency_offset;
				lowest_x_offset = offset_x;
				break;
			}
			foreach (Squad squad in squads)
			{
				PPos item = FindNextPosition(ref_pos, heading, inside_wall, ignore_central_row, settings.reserve_row_limit, settings.reserve_line_limit, squad, ref line, ref row, ref last_right_offset, ref last_left_offset, ref offset_x, ref lowest_x_offset, ref no_more_correct_pos, ref furthest_right, ref furthest_left, IsCorrectPosFunc, IsInsideWall, 0f, 0f, wall_check);
				if (!no_more_correct_pos)
				{
					offsets.Add(item);
					list.Add(item);
					continue;
				}
				break;
			}
		}
		if (no_more_correct_pos)
		{
			int count = squads.Count;
			if (list.Count >= count)
			{
				for (int num = list.Count - 1; num >= count; num--)
				{
					list.RemoveAt(num);
				}
				offsets = list;
			}
			else
			{
				int count2 = list.Count;
				if (count2 == 0)
				{
					list.Add(ref_pos);
				}
				while (list.Count < count)
				{
					int index = UnityEngine.Random.Range(0, count2);
					list.Add(list[index]);
				}
				offsets = list;
			}
		}
		furthest_right_offset = furthest_right;
		furthest_left_offset = furthest_left;
		return offsets;
		void ResetValues()
		{
			line = 0;
			row = 0;
			last_left_offset = 0f;
			last_right_offset = 0f;
			no_more_correct_pos = false;
			furthest_right = 0f;
			furthest_left = 0f;
			offsets = new List<PPos>();
		}
	}

	private List<PPos> SetUpFlanks(List<Squad> squads, PPos ref_pos, float heading, bool inside_wall, float right_offset, float left_offset, Func<PPos, float, bool> IsCorrectPosFunc, Func<Vector3, bool> IsInsideWall, bool wall_check = false)
	{
		int line = 0;
		int row = 0;
		float last_left_offset = 0f;
		float last_right_offset = 0f;
		float offset_x = settings.flanks_line_offset;
		float lowest_x_offset = offset_x;
		bool no_more_correct_pos = false;
		float furthest_right = 0f;
		float furthest_left = 0f;
		List<PPos> offsets = new List<PPos>();
		List<PPos> list = new List<PPos>();
		for (int i = 0; i < 3; i++)
		{
			switch (i)
			{
			case 1:
				ResetValues();
				offset_x = settings.flanks_line_offset + settings.emergency_offset;
				lowest_x_offset = offset_x;
				break;
			case 2:
				ResetValues();
				offset_x = settings.flanks_line_offset - settings.emergency_offset;
				lowest_x_offset = offset_x;
				break;
			}
			foreach (Squad squad in squads)
			{
				PPos item = FindNextPosition(ref_pos, heading, inside_wall, ignore_row_0: true, settings.flank_row_limit * 2 + 1, settings.flank_line_limit, squad, ref line, ref row, ref last_right_offset, ref last_left_offset, ref offset_x, ref lowest_x_offset, ref no_more_correct_pos, ref furthest_right, ref furthest_left, IsCorrectPosFunc, IsInsideWall, right_offset, left_offset, wall_check);
				if (!no_more_correct_pos)
				{
					offsets.Add(item);
					list.Add(item);
					continue;
				}
				break;
			}
			if (!no_more_correct_pos)
			{
				break;
			}
		}
		if (no_more_correct_pos)
		{
			int count = squads.Count;
			if (list.Count >= count)
			{
				for (int num = list.Count - 1; num >= count; num--)
				{
					list.RemoveAt(num);
				}
				offsets = list;
			}
			else
			{
				int count2 = list.Count;
				if (count2 == 0)
				{
					list.Add(ref_pos);
				}
				while (list.Count < count)
				{
					int index = UnityEngine.Random.Range(0, count2);
					list.Add(list[index]);
				}
				offsets = list;
			}
		}
		return offsets;
		void ResetValues()
		{
			line = 0;
			row = 0;
			last_left_offset = 0f;
			last_right_offset = 0f;
			no_more_correct_pos = false;
			furthest_right = 0f;
			furthest_left = 0f;
			offsets = new List<PPos>();
		}
	}

	private int GetRowValue(int row)
	{
		if (row % 2 != 1)
		{
			return -row / 2;
		}
		return row - (row - 1) / 2;
	}

	private PPos FindNextPosition(PPos ref_pos, float heading, bool inside_wall, bool ignore_row_0, float row_limit, float line_limit, Squad sq, ref int line, ref int row, ref float last_right_offset, ref float last_left_offset, ref float offset_x, ref float lowest_x_offset, ref bool no_more_correct_positions, ref float furthest_right_offset, ref float furthest_left_offset, Func<PPos, float, bool> IsCorrectPosFunc, Func<Vector3, bool> IsInsideWall, float initial_right_offset = 0f, float initial_left_offset = 0f, bool wall_check = false)
	{
		PPos pPos = default(PPos);
		float cur_width = sq.formation.cur_width;
		cur_width = ((cur_width > 10f) ? cur_width : 10f);
		float cur_height = sq.formation.cur_height;
		cur_height = ((cur_height > 5f) ? cur_height : 5f);
		int num = Mathf.CeilToInt(cur_width * 0.5f / settings.pos_check_delta);
		bool flag = false;
		while ((float)line < line_limit)
		{
			while ((float)row < row_limit)
			{
				if (row == 0 && ignore_row_0)
				{
					last_right_offset = initial_right_offset;
					last_left_offset = initial_left_offset;
				}
				else
				{
					bool flag2 = (float)GetRowValue(row) > 0f;
					if (!settings.one_sided || (!(settings.ignore_right && flag2) && (settings.ignore_right || flag2)))
					{
						float y = (flag2 ? settings.pos_check_delta : (0f - settings.pos_check_delta));
						pPos = new PPos(y: (row == 0) ? 0f : ((!flag2) ? (last_left_offset - settings.rows_spacing - cur_width * 0.5f) : (last_right_offset + settings.rows_spacing + cur_width * 0.5f)), x: offset_x - cur_height * 0.5f);
						PPos pPos2 = pPos + new PPos(0f, y);
						for (int i = 0; i < num; i++)
						{
							pPos2 = pPos + new PPos(0f, y) * i;
							PPos pPos3 = ref_pos + pPos.GetRotated(0f - heading);
							if (((settings.no_radius_test || !IsCorrectPosFunc(pPos3, 5f)) && (!settings.no_radius_test || !IsCorrectPosFunc(pPos3, 0f))) || (wall_check && IsInsideWall(pPos3) != inside_wall))
							{
								continue;
							}
							pPos = pPos2;
							flag = true;
							if (row == 0)
							{
								last_right_offset = pPos.y + cur_width * 0.5f;
								last_left_offset = pPos.y - cur_width * 0.5f;
							}
							else if (flag2)
							{
								last_right_offset = pPos.y + cur_width * 0.5f;
							}
							else
							{
								last_left_offset = pPos.y - cur_width * 0.5f;
							}
							float num2 = pPos.x - cur_height * 0.5f;
							if (num2 < lowest_x_offset)
							{
								lowest_x_offset = num2;
							}
							if (row == 0)
							{
								if (last_right_offset > furthest_right_offset)
								{
									furthest_right_offset = last_right_offset;
								}
								if (last_left_offset < furthest_left_offset)
								{
									furthest_left_offset = last_left_offset;
								}
							}
							else if (flag2)
							{
								if (last_right_offset > furthest_right_offset)
								{
									furthest_right_offset = last_right_offset;
								}
							}
							else if (last_left_offset < furthest_left_offset)
							{
								furthest_left_offset = last_left_offset;
							}
							row++;
							if ((float)row >= row_limit)
							{
								row = 0;
								line++;
								offset_x = lowest_x_offset - settings.lines_spacing;
							}
							break;
						}
						if (flag)
						{
							break;
						}
						if (flag2)
						{
							last_right_offset = pPos2.y;
						}
						else
						{
							last_left_offset = pPos2.y;
						}
					}
				}
				row++;
			}
			if (flag)
			{
				break;
			}
			offset_x = lowest_x_offset - settings.lines_spacing;
			line++;
			row = 0;
		}
		if (!flag)
		{
			no_more_correct_positions = true;
		}
		return pPos;
	}
}
