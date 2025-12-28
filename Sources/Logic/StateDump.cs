using System.Collections.Generic;
using System.Text;

namespace Logic;

public class StateDump
{
	private StringBuilder txt = new StringBuilder(4096);

	private Stack<string> sections = new Stack<string>(8);

	private string Line(string key, Value value, bool allowNullValue)
	{
		if (key == null)
		{
			return null;
		}
		if (value.type == Value.Type.Null)
		{
			if (!allowNullValue)
			{
				return null;
			}
			return key;
		}
		return $"{key}: {value}";
	}

	public string OpenSection(string section)
	{
		if (string.IsNullOrEmpty(section))
		{
			Game.Log("State dump section name is empty!", Game.LogType.Error);
		}
		Append(section);
		sections.Push(section);
		return section;
	}

	public string OpenSection(string key, Value value)
	{
		string section = Line(key, value, allowNullValue: true);
		return OpenSection(section);
	}

	public void CloseSection(string section)
	{
		if (sections.Peek() == section)
		{
			sections.Pop();
			return;
		}
		Game.Log("Mismatching state dump sections: expected '" + sections.Peek() + "', found '" + section + "'", Game.LogType.Error);
	}

	public void CloseSection(string key, Value value)
	{
		string section = Line(key, value, allowNullValue: true);
		CloseSection(section);
	}

	public void Append(string line)
	{
		if (line != null)
		{
			txt.Append('\t', sections.Count);
			txt.AppendLine(line);
		}
	}

	public void Append(string key, Value? value)
	{
		if (value.HasValue)
		{
			string line = Line(key, value.Value, allowNullValue: false);
			Append(line);
		}
	}

	public override string ToString()
	{
		return txt.ToString();
	}
}
