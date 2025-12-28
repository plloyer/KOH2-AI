namespace Logic;

public class Prestige : Component
{
	public class Def : Logic.Def
	{
		public float max = 10000f;

		public override bool Load(Game game)
		{
			DT.Field field = dt_def.field;
			max = field.GetFloat("max", null, max);
			return true;
		}
	}

	public Def def;

	private Kingdom kingdom;

	public float prestige;

	public Prestige(Kingdom kingdom)
		: base(kingdom)
	{
		this.kingdom = kingdom;
		def = base.game.defs.GetBase<Def>();
	}

	public float GetMaxPrestige()
	{
		return def.max;
	}

	public float GetPrestige()
	{
		return prestige;
	}

	public float GetModifierValue(string mod_name, Vars vars = null)
	{
		return def.field.GetFloat(mod_name, vars);
	}

	public bool AddPrestigeModifier(string mod_name, Vars vars = null, float valueMultiplier = 1f, bool send_state = true)
	{
		if (!kingdom.IsAuthority())
		{
			return false;
		}
		float val = GetModifierValue(mod_name, vars) * valueMultiplier;
		AddPrestige(val, send_state);
		return true;
	}

	public bool AddPrestige(float val, bool send_state = true)
	{
		if (!kingdom.IsAuthority())
		{
			return false;
		}
		prestige += val;
		if (send_state)
		{
			kingdom.SendState<Kingdom.PrestigeState>();
		}
		return true;
	}
}
