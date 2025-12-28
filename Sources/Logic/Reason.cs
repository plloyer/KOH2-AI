using System;

namespace Logic;

public class Reason : BaseObject, IVars
{
	public class FullData : Data
	{
		public float initial_value;

		public bool is_direct;

		public NID indirectTarget_nid = NID.Null;

		public NID source_nid = NID.Null;

		public NID target_nid = NID.Null;

		public string field_key = string.Empty;

		private Data vars_data;

		public float timestamp;

		public static FullData Create()
		{
			return new FullData();
		}

		public override string ToString()
		{
			return $"Reason {field_key} src:{source_nid} tgt:{target_nid} init_val:{initial_value} timestamp: {timestamp}";
		}

		public override bool InitFrom(object obj)
		{
			Reason reason = obj as Reason;
			initial_value = reason.initial_value;
			is_direct = reason.isDirect;
			indirectTarget_nid = reason.indirectTarget;
			source_nid = reason.source;
			target_nid = reason.target;
			field_key = reason.field.key;
			if (reason.vars != null)
			{
				vars_data = Data.Create(reason.vars);
				if (vars_data == null)
				{
					Game.Log($"{reason} Vars data is null for vars: {reason.vars}", Game.LogType.Error);
				}
			}
			timestamp = reason.timestamp - Time.Zero;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteFloat(initial_value, "initial_value");
			ser.WriteBool(is_direct, "is_direct");
			ser.WriteNID(indirectTarget_nid, "indirectTarget_nid");
			ser.WriteNID(source_nid, "source_nid");
			ser.WriteNID(target_nid, "target_nid");
			ser.WriteStr(field_key, "field_key");
			ser.WriteData(vars_data, "vars_data");
			ser.WriteFloat(timestamp, "timestamp");
		}

		public override void Load(Serialization.IReader ser)
		{
			initial_value = ser.ReadFloat("initial_value");
			is_direct = ser.ReadBool("is_direct");
			indirectTarget_nid = ser.ReadNID("indirectTarget_nid");
			source_nid = ser.ReadNID("source_nid");
			target_nid = ser.ReadNID("target_nid");
			field_key = ser.ReadStr("field_key");
			vars_data = ser.ReadData("vars_data");
			timestamp = ser.ReadFloat("timestamp");
		}

		public override object GetObject(Game game)
		{
			return new Reason();
		}

		public override bool ApplyTo(object obj, Game game)
		{
			Reason reason = obj as Reason;
			reason.initial_value = initial_value;
			reason.isDirect = is_direct;
			reason.indirectTarget = indirectTarget_nid.GetObj(game);
			reason.source = source_nid.GetObj(game);
			reason.target = target_nid.GetObj(game);
			reason.field = KingdomAndKingdomRelation.GetRelationModifierField(game, field_key);
			if (reason.field == null)
			{
				game.Error($"{reason} Cant find Reason def: {field_key}");
			}
			if (vars_data != null && vars_data.GetObject(game) is IVars vars)
			{
				if (!vars_data.ApplyTo(vars, game))
				{
					Game.Log($"{reason} Could not apply {vars_data} to {vars}", Game.LogType.Error);
				}
				reason.vars = vars;
			}
			reason.timestamp = Time.Zero + timestamp;
			return true;
		}
	}

	public float initial_value;

	public bool isDirect;

	public Object indirectTarget;

	public Object source;

	public Object target;

	public DT.Field field;

	public IVars vars;

	public Time timestamp;

	public float value
	{
		get
		{
			if (source == null || source.game == null)
			{
				return 0f;
			}
			float num = initial_value;
			float num2 = (source.game.session_time - timestamp) / 1000f;
			float num3 = Math.Abs(num);
			float num4 = Math.Sign(num);
			if (num3 - num2 < 0f)
			{
				return 0.001f * num4;
			}
			return num - num2 * num4;
		}
		private set
		{
		}
	}

	public override bool IsFullSerializable()
	{
		return true;
	}

	public Reason()
	{
	}

	public Reason(Object source, Object target, DT.Field field, float value, bool isDirect, Object indirectTarget, IVars vars)
	{
		this.source = source;
		this.target = target;
		this.field = field;
		initial_value = value;
		this.isDirect = isDirect;
		this.indirectTarget = indirectTarget;
		this.vars = vars;
		timestamp = ((source != null && source.game != null) ? source.game.session_time : Time.Zero);
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "source":
			return source;
		case "target":
			return target;
		case "indirect_target":
			return indirectTarget;
		case "direct":
			return isDirect;
		case "value":
			return value;
		case "context":
			return new Value(this.vars);
		default:
			if (this.vars != null && this.vars != this)
			{
				return this.vars.GetVar(key, vars, as_value);
			}
			return Value.Unknown;
		}
	}

	public bool IsAffectedBy(Kingdom k)
	{
		return target == k;
	}

	public override string ToString()
	{
		Kingdom kingdom = source as Kingdom;
		Kingdom kingdom2 = target as Kingdom;
		Kingdom kingdom3 = indirectTarget as Kingdom;
		string text = ((kingdom != null) ? kingdom.Name : ((source == null) ? "null" : source.ToString()));
		string text2 = ((kingdom2 != null) ? kingdom2.Name : ((target == null) ? "null" : target.ToString()));
		string text3 = ((kingdom3 != null) ? kingdom3.Name : ((indirectTarget == null) ? "null" : indirectTarget.ToString()));
		string text4 = field?.key ?? "null";
		string text5 = text + " -> " + text2 + ": " + text4;
		if (!isDirect)
		{
			text5 = text5 + "(" + text3 + ")";
		}
		return text5 + " -> " + value;
	}
}
