namespace Logic;

public class BaseObject
{
	public Reflection.TypeInfo rtti;

	public BaseObject()
	{
		rtti = Reflection.GetTypeInfo(GetType());
	}

	public override string ToString()
	{
		return rtti.name;
	}

	public virtual string GetDumpStateKey()
	{
		return rtti.name;
	}

	public virtual Value GetDumpStateValue()
	{
		return ToString();
	}

	public virtual void DumpState(StateDump dump, int verbosity)
	{
		string dumpStateKey = GetDumpStateKey();
		if (dumpStateKey != null)
		{
			Value dumpStateValue = GetDumpStateValue();
			string section = dump.OpenSection(dumpStateKey, dumpStateValue);
			DumpInnerState(dump, verbosity);
			dump.CloseSection(section);
		}
	}

	public virtual void DumpInnerState(StateDump dump, int verbosity)
	{
	}

	public virtual Reflection.TypeInfo GetRefDataRTTI()
	{
		for (Reflection.TypeInfo base_rtti = rtti; base_rtti != null; base_rtti = base_rtti.base_rtti)
		{
			if (base_rtti.ref_data_rtti != null)
			{
				return base_rtti.ref_data_rtti;
			}
		}
		return null;
	}

	public virtual bool IsRefSerializable()
	{
		Reflection.TypeInfo refDataRTTI = GetRefDataRTTI();
		if (refDataRTTI == null)
		{
			return false;
		}
		if (refDataRTTI.create == null)
		{
			return false;
		}
		return true;
	}

	public virtual Data CreateRefData()
	{
		Reflection.TypeInfo refDataRTTI = GetRefDataRTTI();
		if (refDataRTTI == null)
		{
			return null;
		}
		if (refDataRTTI.create == null)
		{
			return null;
		}
		if (!(refDataRTTI.create() is Data data))
		{
			return null;
		}
		data.rtti = refDataRTTI;
		if (!data.InitFrom(this))
		{
			Game.Log("Could not init " + data.ToString() + " from " + ToString(), Game.LogType.Error);
			return null;
		}
		return data;
	}

	public virtual Reflection.TypeInfo GetFullDataRTTI()
	{
		for (Reflection.TypeInfo base_rtti = rtti; base_rtti != null; base_rtti = base_rtti.base_rtti)
		{
			if (base_rtti.full_data_rtti != null)
			{
				return base_rtti.full_data_rtti;
			}
		}
		return null;
	}

	public virtual bool IsFullSerializable()
	{
		Reflection.TypeInfo fullDataRTTI = GetFullDataRTTI();
		if (fullDataRTTI == null)
		{
			return false;
		}
		if (fullDataRTTI.create == null)
		{
			return false;
		}
		return true;
	}

	public virtual Data CreateFullData()
	{
		Reflection.TypeInfo fullDataRTTI = GetFullDataRTTI();
		if (fullDataRTTI == null)
		{
			return null;
		}
		if (fullDataRTTI.create == null)
		{
			return null;
		}
		if (!(fullDataRTTI.create() is Data data))
		{
			return null;
		}
		data.rtti = fullDataRTTI;
		if (!data.InitFrom(this))
		{
			Game.Log("Could not init " + data.ToString() + " from " + ToString(), Game.LogType.Error);
			return null;
		}
		return data;
	}

	public virtual bool IsSerializable()
	{
		if (!IsRefSerializable())
		{
			return IsFullSerializable();
		}
		return true;
	}

	public virtual Data CreateData()
	{
		if (IsRefSerializable())
		{
			return CreateRefData();
		}
		return CreateFullData();
	}
}
