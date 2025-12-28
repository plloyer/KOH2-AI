using System.Collections.Generic;
using System.Text;

namespace Logic;

public class Table
{
	private class Row
	{
		public List<string> cells = new List<string>();
	}

	private List<Row> rows = new List<Row>();

	private int max_columns;

	public static Dictionary<string, string> remap_cells;

	public char delimiter { get; private set; }

	public int NumRows => rows.Count;

	public int NumCols => max_columns;

	public string Get(int row, int col)
	{
		if (row < 0 || row >= NumRows || col < 0 || col >= NumCols)
		{
			return null;
		}
		Row row2 = rows[row];
		if (col >= row2.cells.Count)
		{
			return "";
		}
		return row2.cells[col];
	}

	public float GetFloat(int row, int col, float def_val = 0f)
	{
		string text = Get(row, col);
		if (text == null || text == "")
		{
			return def_val;
		}
		if (DT.ParseFloat(text, out var f))
		{
			return f;
		}
		return def_val;
	}

	public int GetInt(int row, int col, int def_val = 0)
	{
		string text = Get(row, col);
		if (string.IsNullOrEmpty(text))
		{
			return def_val;
		}
		if (DT.ParseInt(text, out var i))
		{
			return i;
		}
		return def_val;
	}

	public bool GetBool(int row, int col, bool def_val = false)
	{
		string text = Get(row, col);
		if (string.IsNullOrEmpty(text))
		{
			return def_val;
		}
		if (DT.ParseBool(text, out var b))
		{
			return b;
		}
		return def_val;
	}

	public void Clear()
	{
		rows.Clear();
		max_columns = 0;
	}

	public static Table FromFile(string filename, char delimiter = '\0')
	{
		Table table = new Table();
		if (table.Load(filename, delimiter))
		{
			return table;
		}
		table.Clear();
		table = null;
		return null;
	}

	public bool Load(string filename, char delimiter)
	{
		string text;
		try
		{
			text = DT.ReadTextFile(filename);
		}
		catch
		{
			return false;
		}
		return Parse(text, delimiter, filename);
	}

	public static Table FromString(string text, char delimiter = '\0', string filename = null)
	{
		if (text == null)
		{
			return null;
		}
		Table table = new Table();
		if (table.Parse(text, delimiter, filename))
		{
			return table;
		}
		table.Clear();
		table = null;
		return null;
	}

	private string ReadQuottedCell(string text, ref int idx, char delimiter)
	{
		char c = text[idx++];
		int length = text.Length;
		StringBuilder stringBuilder = new StringBuilder();
		char c2;
		while (true)
		{
			if (idx >= length)
			{
				return null;
			}
			c2 = text[idx++];
			if (c2 != c)
			{
				stringBuilder.Append(c2);
				continue;
			}
			if (idx >= length || text[idx] != c)
			{
				break;
			}
			stringBuilder.Append(c);
			idx++;
		}
		if (idx >= length)
		{
			return stringBuilder.ToString();
		}
		c2 = text[idx];
		if (c2 != delimiter && c2 != '\n' && c2 != '\r')
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	public string ReadCell(string text, ref int idx, char delimiter)
	{
		int length = text.Length;
		if (idx < length && text[idx] == '"')
		{
			return ReadQuottedCell(text, ref idx, delimiter);
		}
		int num = idx;
		while (idx < length)
		{
			char c = text[idx];
			if (c == delimiter || c == '\n' || c == '\r')
			{
				break;
			}
			idx++;
		}
		return text.Substring(num, idx - num).Trim();
	}

	public static char DecideDelimiter(string text)
	{
		bool flag = false;
		int i;
		for (i = 0; i < text.Length && i <= 1024; i++)
		{
			char c = text[i];
			if (c == '"')
			{
				flag = !flag;
			}
			else if (!flag)
			{
				switch (c)
				{
				case '\t':
				case ',':
				case ';':
					return c;
				}
			}
		}
		Game.Log("Could not decide table delimiter: '" + text.Substring(0, i) + "'", Game.LogType.Error);
		return ',';
	}

	private string RemapCell(int row, int col, string cell)
	{
		if (remap_cells == null)
		{
			return cell;
		}
		if (row == 0)
		{
			if (!remap_cells.TryGetValue(cell, out var value))
			{
				return cell;
			}
			return value;
		}
		if (rows.Count == 0)
		{
			return null;
		}
		Row row2 = rows[0];
		if (col >= row2.cells.Count)
		{
			return null;
		}
		if (row2.cells[col] == null)
		{
			return null;
		}
		return cell;
	}

	public bool Parse(string text, char delimiter = '\0', string filename = null)
	{
		Clear();
		if (text == null)
		{
			return false;
		}
		if (delimiter == '\0')
		{
			delimiter = DecideDelimiter(text);
		}
		this.delimiter = delimiter;
		int idx = 0;
		int length = text.Length;
		Row row = new Row();
		int num = 0;
		while (idx < length)
		{
			string text2 = ReadCell(text, ref idx, delimiter);
			if (text2 == null)
			{
				Game.Log($"Error parsing CSV file '{filename}' at row {rows.Count + 1}, col {num + 1} ('{(char)(65 + num)}')", Game.LogType.Error);
				return false;
			}
			text2 = RemapCell(rows.Count, num, text2);
			if (text2 != null || rows.Count == 0)
			{
				row.cells.Add(text2);
			}
			num++;
			if (idx >= length)
			{
				break;
			}
			char c = text[idx];
			idx++;
			if (c != delimiter && (c == '\n' || c == '\r'))
			{
				if (idx < length && text[idx] == 23 - c)
				{
					idx++;
				}
				AddRow(row);
				row = new Row();
				num = 0;
			}
		}
		if (row.cells.Count > 0)
		{
			AddRow(row);
		}
		if (rows.Count > 0)
		{
			row = rows[0];
			for (int num2 = row.cells.Count - 1; num2 >= 0; num2--)
			{
				if (row.cells[num2] == null)
				{
					row.cells.RemoveAt(num2);
				}
			}
		}
		return true;
	}

	private void AddRow(Row r)
	{
		rows.Add(r);
		int num;
		if (rows.Count == 1 && remap_cells != null)
		{
			num = 0;
			for (int i = 0; i < r.cells.Count; i++)
			{
				if (r.cells[i] != null)
				{
					num++;
				}
			}
		}
		else
		{
			num = r.cells.Count;
		}
		if (num > max_columns)
		{
			max_columns = num;
		}
	}
}
