using System.Collections.Generic;

namespace Logic;

public class SkillMods
{
	public class Cell
	{
		public SkillsTable.CellDef def;

		public List<Skill.StatModifier> mods;

		public List<Cell> subcells;

		public Cell(SkillsTable.CellDef def)
		{
			this.def = def;
		}

		public void Refresh(Object owner)
		{
			if (mods == null)
			{
				int num = ((def.mod_defs != null) ? def.mod_defs.Count : 0);
				mods = new List<Skill.StatModifier>(num);
				for (int i = 0; i < num; i++)
				{
					Skill.StatModifier item = new Skill.StatModifier(def.mod_defs[i], owner);
					mods.Add(item);
				}
			}
			for (int j = 0; j < mods.Count; j++)
			{
				mods[j].Apply(owner);
			}
			if (def.subcells == null)
			{
				return;
			}
			for (int k = 0; k < def.subcells.Count; k++)
			{
				SkillsTable.CellDef cellDef = def.subcells[k];
				bool num2 = cellDef.Validate(owner, check_parent: false);
				int num3 = FindSubcell(cellDef.name);
				if (!num2)
				{
					if (num3 >= 0)
					{
						subcells[num3].Revert(owner);
						subcells.RemoveAt(num3);
					}
					continue;
				}
				Cell cell;
				if (num3 < 0)
				{
					cell = new Cell(cellDef);
					if (subcells == null)
					{
						subcells = new List<Cell>();
					}
					subcells.Add(cell);
				}
				else
				{
					cell = subcells[num3];
				}
				cell.Refresh(owner);
			}
		}

		public void Revert(Object owner)
		{
			if (mods != null)
			{
				for (int i = 0; i < mods.Count; i++)
				{
					mods[i].Revert(owner);
				}
			}
			if (subcells != null)
			{
				for (int j = 0; j < subcells.Count; j++)
				{
					subcells[j].Revert(owner);
				}
				subcells = null;
			}
		}

		private int FindSubcell(string name)
		{
			if (subcells == null)
			{
				return -1;
			}
			for (int i = 0; i < subcells.Count; i++)
			{
				if (subcells[i].def.name == name)
				{
					return i;
				}
			}
			return -1;
		}
	}

	public class Row
	{
		public SkillsTable.RowDef def;

		public Skill skill;

		public List<Cell> cells = new List<Cell>();

		public Row(SkillsTable.RowDef tag_def)
		{
			def = tag_def;
		}

		public Row(Skill skill)
		{
			def = skill.def.row_def;
			this.skill = skill;
		}

		public Row(CharacterClass.Def class_def)
		{
			def = class_def.skills_row_def;
		}

		public void Refresh(Object owner)
		{
			if (def.cell_defs == null)
			{
				return;
			}
			for (int i = 0; i < def.cell_defs.Count; i++)
			{
				SkillsTable.CellDef cellDef = def.cell_defs[i];
				bool num = cellDef.Validate(owner, check_parent: false);
				int num2 = FindCell(cellDef.name);
				if (!num)
				{
					if (num2 >= 0)
					{
						cells[num2].Revert(owner);
						cells.RemoveAt(num2);
					}
					continue;
				}
				Cell cell;
				if (num2 < 0)
				{
					cell = new Cell(cellDef);
					cells.Add(cell);
				}
				else
				{
					cell = cells[num2];
				}
				cell.Refresh(owner);
			}
		}

		public void Revert(Object owner)
		{
			for (int i = 0; i < cells.Count; i++)
			{
				cells[i].Revert(owner);
			}
			cells.Clear();
		}

		public int FindCell(string name)
		{
			for (int i = 0; i < cells.Count; i++)
			{
				if (cells[i].def.name == name)
				{
					return i;
				}
			}
			return -1;
		}
	}

	public Object owner;

	public Row class_row;

	public List<Row> tag_rows;

	public SkillMods(Object owner)
	{
		this.owner = owner;
	}

	public List<Skill> GetOwnerSkills()
	{
		if (owner is Character character)
		{
			return character.skills;
		}
		return null;
	}

	public void RevertAll()
	{
		List<Skill> ownerSkills = GetOwnerSkills();
		if (ownerSkills != null)
		{
			for (int i = 0; i < ownerSkills.Count; i++)
			{
				Skill skill = ownerSkills[i];
				if (skill != null && skill.mods_row != null)
				{
					skill.mods_row.Revert(owner);
					skill.mods_row = null;
				}
			}
		}
		if (class_row != null)
		{
			class_row.Revert(owner);
			class_row = null;
		}
		if (tag_rows != null)
		{
			for (int j = 0; j < tag_rows.Count; j++)
			{
				tag_rows[j].Revert(owner);
			}
			tag_rows = null;
		}
	}

	public void Refresh()
	{
		SkillsTable skills_table = owner.game.defs.GetBase<Skill.Def>().skills_table;
		List<Skill> ownerSkills = GetOwnerSkills();
		if (ownerSkills != null)
		{
			for (int i = 0; i < ownerSkills.Count; i++)
			{
				Skill skill = ownerSkills[i];
				if (skill?.def != null)
				{
					if (skill.mods_row == null)
					{
						skill.mods_row = new Row(skill);
					}
					skill.mods_row.Refresh(owner);
				}
			}
		}
		Character character = owner as Character;
		if (class_row != null && character?.class_def != class_row.def.class_def)
		{
			class_row.Revert(owner);
			class_row = null;
		}
		if (character?.class_def != null && character.class_def.skills_row_def != null && class_row == null)
		{
			class_row = new Row(character.class_def);
		}
		if (class_row != null)
		{
			class_row.Refresh(owner);
		}
		for (int j = 0; j < skills_table.tag_rows.Count; j++)
		{
			SkillsTable.RowDef rowDef = skills_table.tag_rows[j];
			bool num = rowDef.Validate(owner);
			int num2 = FindTagRow(rowDef.name);
			if (!num)
			{
				if (num2 >= 0)
				{
					tag_rows[num2].Revert(owner);
					tag_rows.RemoveAt(num2);
				}
				continue;
			}
			Row row;
			if (num2 < 0)
			{
				row = new Row(rowDef);
				if (tag_rows == null)
				{
					tag_rows = new List<Row>();
				}
				tag_rows.Add(row);
			}
			else
			{
				row = tag_rows[num2];
			}
			row.Refresh(owner);
		}
	}

	public int FindTagRow(string tag)
	{
		if (tag_rows == null)
		{
			return -1;
		}
		for (int i = 0; i < tag_rows.Count; i++)
		{
			if (tag_rows[i].def.name == tag)
			{
				return i;
			}
		}
		return -1;
	}
}
