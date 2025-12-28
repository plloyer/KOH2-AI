using System;
using System.Collections.Generic;

namespace Logic;

public class Vassalage : BaseObject, IVars
{
	public enum Type
	{
		Unknown,
		Vassal,
		Scuttage,
		March,
		SacredLand
	}

	public class Def : Logic.Def
	{
		public string name;

		public Type type;

		public List<StatModifier.Def> mods = new List<StatModifier.Def>();

		public override bool Load(Game game)
		{
			name = base.field.key;
			if (name.EndsWith("Vassalage", StringComparison.Ordinal))
			{
				name = name.Substring(0, name.Length - 9);
			}
			if (!Enum.TryParse<Type>(name, out type))
			{
				type = Type.Unknown;
			}
			LoadMods(game);
			return true;
		}

		private void LoadMods(Game game)
		{
			mods.Clear();
			DT.Field field = base.field;
			if (field?.children == null)
			{
				return;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!(field2.type != "mod"))
				{
					StatModifier.Def item = new StatModifier.Def(this, field2, field2.key);
					mods.Add(item);
				}
			}
		}

		public override bool Validate(Game game)
		{
			ValidateMods(game);
			return true;
		}

		private void ValidateMods(Game game)
		{
			if (mods.Count == 0)
			{
				if (!IsBase())
				{
					Game.Log(base.field.Path(include_file: true) + ": Tradition '" + name + "' has no mods", Game.LogType.Warning);
				}
				return;
			}
			Stats.Def def = game.defs.Get<Stats.Def>("KingdomStats");
			for (int i = 0; i < mods.Count; i++)
			{
				StatModifier.Def def2 = mods[i];
				if (!def.HasStat(def2.stat_name))
				{
					Game.Log(def2.field.Path(include_file: true) + ": unknown stat: '" + def2.stat_name + "'", Game.LogType.Error);
					mods.RemoveAt(i);
					i--;
				}
			}
		}

		public override Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return base.GetVar(key, vars, as_value);
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return base.field.Path() + ".name";
		}
	}

	public class StatModifier : Stat.Modifier, IVars
	{
		public class Def
		{
			public DT.Field field;

			public Vassalage.Def vassalage_def;

			public string stat_name;

			public Type type;

			public Def(Vassalage.Def vassalage_def, DT.Field mf, string stat_name)
			{
				field = mf;
				this.vassalage_def = vassalage_def;
				this.stat_name = stat_name;
				if (mf.FindChild("perc") != null)
				{
					type = Type.Perc;
				}
				else if (mf.FindChild("base") != null)
				{
					type = Type.Base;
				}
				else if (mf.FindChild("unscaled") != null)
				{
					type = Type.Unscaled;
				}
			}

			public float CalcValue(int rank)
			{
				field.NumValues();
				return field.Float(0);
			}

			public override string ToString()
			{
				return vassalage_def.name + "." + stat_name;
			}
		}

		public Def def;

		public StatModifier(Def def)
		{
			this.def = def;
		}

		public override DT.Field GetField()
		{
			return def.field;
		}

		public override DT.Field GetNameField()
		{
			return def?.vassalage_def?.field;
		}

		public override bool IsConst()
		{
			return true;
		}

		public override string ToString()
		{
			return def.ToString() + Stat.Modifier.ToString(value, type);
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (!(base.owner is IVars vars2))
			{
				return Value.Unknown;
			}
			return vars2.GetVar(key, vars, as_value);
		}
	}

	public class RefData : Data
	{
		public NID kingdom_nid;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(Vassalage of " + kingdom_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Vassalage { owner: not null } vassalage))
			{
				return false;
			}
			kingdom_nid = vassalage.owner;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(kingdom_nid, "kingdom_nid");
		}

		public override void Load(Serialization.IReader ser)
		{
			kingdom_nid = ser.ReadNID("kingdom_nid");
		}

		public override object GetObject(Game game)
		{
			return ((Kingdom)kingdom_nid.GetObj(game))?.vassalage;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			Kingdom kingdom = (Kingdom)kingdom_nid.GetObj(game);
			if (kingdom == null || kingdom.vassalage == null)
			{
				return false;
			}
			return true;
		}
	}

	public Def def;

	public Kingdom owner;

	public int rank;

	public List<StatModifier> mods = new List<StatModifier>();

	public Vassalage(Def def)
	{
		this.def = def;
		CreateMods();
	}

	public void SetVasal(Kingdom owner)
	{
		DelMods();
		this.owner = owner;
		AddMods();
	}

	public void ChangeDef(Def def)
	{
		DelMods();
		this.def = def;
		CreateMods();
		AddMods();
	}

	private void CreateMods()
	{
		mods.Clear();
		for (int i = 0; i < def.mods.Count; i++)
		{
			StatModifier item = new StatModifier(def.mods[i]);
			mods.Add(item);
		}
	}

	private void DelMods()
	{
		if (owner == null)
		{
			return;
		}
		_ = owner.stats;
		for (int i = 0; i < mods.Count; i++)
		{
			StatModifier statModifier = mods[i];
			if (statModifier.stat != null)
			{
				statModifier.stat.DelModifier(statModifier);
			}
		}
	}

	private void AddMods()
	{
		if (owner == null)
		{
			return;
		}
		Stats stats = owner.stats;
		if (stats != null)
		{
			for (int i = 0; i < mods.Count; i++)
			{
				StatModifier statModifier = mods[i];
				statModifier.value = statModifier.def.CalcValue(rank);
				stats.AddModifier(statModifier.def.stat_name, statModifier);
			}
			owner.InvalidateIncomes();
		}
	}

	public override string ToString()
	{
		if (def == null)
		{
			return "<Unknown vassalage>";
		}
		return def.id;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "def"))
		{
			if (key == "name")
			{
				return def?.GetNameKey();
			}
			return Value.Null;
		}
		return def;
	}
}
