using System.Collections.Generic;

namespace Logic;

public class PerLevelValues
{
	public struct Item
	{
		public int level;

		public Value value;
	}

	public List<Item> items;

	public Value per_level = Value.Null;

	public static PerLevelValues Parse<T>(DT.Field field, IVars vars = null, bool no_null = false)
	{
		if (field == null)
		{
			if (!no_null)
			{
				return null;
			}
			return new PerLevelValues();
		}
		PerLevelValues perLevelValues = null;
		Value value = Value.Null;
		List<string> list = field.Keys();
		for (int i = 0; i < list.Count; i++)
		{
			string path = list[i];
			DT.Field field2 = field.FindChild(path);
			if (field2.key == "per_level")
			{
				value = ReadValue<T>(field2, vars);
			}
			else
			{
				if (!int.TryParse(field2.key, out var result) || result < 0)
				{
					continue;
				}
				Value value2 = ReadValue<T>(field2, vars);
				if (value2.is_valid)
				{
					if (perLevelValues == null)
					{
						perLevelValues = new PerLevelValues();
					}
					if (perLevelValues.items == null)
					{
						perLevelValues.items = new List<Item>();
					}
					perLevelValues.items.Add(new Item
					{
						level = result,
						value = value2
					});
				}
			}
		}
		if (perLevelValues == null)
		{
			if (field.value.obj_val is List<DT.SubValue> list2)
			{
				perLevelValues = new PerLevelValues();
				perLevelValues.items = new List<Item>();
				for (int j = 0; j < list2.Count; j++)
				{
					Value value3 = ReadValue<T>(list2[j].value);
					perLevelValues.items.Add(new Item
					{
						level = j + 1,
						value = value3
					});
				}
			}
			else
			{
				Value value4 = ReadValue<T>(field, vars);
				if (value4.is_valid)
				{
					perLevelValues = new PerLevelValues();
					perLevelValues.items = new List<Item>();
					perLevelValues.items.Add(new Item
					{
						level = 1,
						value = value4
					});
				}
			}
		}
		if (perLevelValues == null && value.is_valid)
		{
			perLevelValues = new PerLevelValues();
		}
		if (perLevelValues != null)
		{
			perLevelValues.per_level = value;
		}
		if (perLevelValues == null && no_null)
		{
			perLevelValues = new PerLevelValues();
		}
		return perLevelValues;
	}

	private static Value ReadValue<T>(DT.Field f, IVars vars)
	{
		if (typeof(T) == typeof(Resource))
		{
			return Resource.Parse(f, vars);
		}
		return ReadValue<T>(f.Value(vars));
	}

	private static Value ReadValue<T>(Value val)
	{
		if (!val.is_valid)
		{
			return val;
		}
		if (typeof(T) == typeof(int))
		{
			return DT.Int(val);
		}
		if (typeof(T) == typeof(float))
		{
			return DT.Float(val);
		}
		if (typeof(T) == typeof(string))
		{
			return DT.String(val);
		}
		if (typeof(T) == typeof(Resource) && val.type == Value.Type.String)
		{
			return Resource.Parse((string)val.obj_val);
		}
		return Value.Null;
	}

	public bool Empty()
	{
		if (items == null)
		{
			return !per_level.is_valid;
		}
		return false;
	}

	public int GetInt(int level, bool per_level = true, bool flat = true)
	{
		int num = 0;
		if (flat)
		{
			Value flat2 = GetFlat(level);
			if (flat2.is_valid)
			{
				num = flat2;
			}
		}
		if (per_level && this.per_level.is_valid)
		{
			num += (int)this.per_level * level;
		}
		return num;
	}

	public float GetFloat(int level, bool per_level = true, bool flat = true, bool clamp_min = false)
	{
		float num = 0f;
		if (flat)
		{
			Value flat2 = GetFlat(level, clamp_min);
			if (flat2.is_valid)
			{
				num = flat2;
			}
		}
		if (per_level && this.per_level.is_valid)
		{
			num += (float)this.per_level * (float)level;
		}
		return num;
	}

	public string GetString(int level)
	{
		return GetFlat(level);
	}

	public Resource GetResources(int level, bool per_level = true, bool flat = true)
	{
		Resource resource = null;
		if (flat)
		{
			resource = GetFlat(level).obj_val as Resource;
		}
		if (!per_level || !this.per_level.is_valid)
		{
			return resource;
		}
		if (resource == null && level == 1)
		{
			return this.per_level.obj_val as Resource;
		}
		Resource resource2 = new Resource();
		resource2.Add(resource, 1f);
		resource2.Add(this.per_level.obj_val as Resource, level);
		return resource2;
	}

	public Value GetFlat(int level, bool clamp_min = false)
	{
		Value result = Value.Null;
		if (items != null)
		{
			if (clamp_min && items.Count > 0)
			{
				result = items[0].value;
			}
			for (int i = 0; i < items.Count; i++)
			{
				Item item = items[i];
				if (item.level > level)
				{
					break;
				}
				if (item.value.is_valid)
				{
					result = item.value;
				}
			}
		}
		return result;
	}

	public override string ToString()
	{
		string text = "";
		if (items != null)
		{
			if (items.Count == 1 && items[0].level == 1)
			{
				text = items[0].value.ToString();
			}
			else
			{
				for (int i = 0; i < items.Count; i++)
				{
					Item item = items[i];
					if (text != "")
					{
						text += "; ";
					}
					text = text + item.level + " = " + (string)item.value;
				}
				if (text != "")
				{
					text = "{ " + text + " }";
				}
			}
		}
		if (per_level.is_valid)
		{
			if (text != "")
			{
				text += " + ";
			}
			text = text + per_level.ToString() + " per level";
		}
		return text;
	}
}
