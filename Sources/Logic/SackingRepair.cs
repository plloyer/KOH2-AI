namespace Logic;

public class SackingRepair : Component
{
	public class Def : Logic.Def
	{
		public float sacking_gold_mul = 0.5f;

		public float sacking_worker_deaths = 0.1f;

		public float sacking_rebellious_deaths = 0.25f;

		public float sacking_levy_deaths = 0.5f;

		public float sacking_burned_structures = 0.25f;

		public float sacking_repair_coef = 0.5f;

		public float sacking_repair_gold_coef = 0.5f;

		public float sacking_recovery_production_mod = 1f;

		public float quick_recovery_mod = 2f;

		public float sacked_food_penalty = -5f;

		public override bool Load(Game game)
		{
			sacking_gold_mul = base.field.GetFloat("sacking_gold_mul", null, sacking_gold_mul);
			sacking_worker_deaths = base.field.GetFloat("sacking_worker_deaths", null, sacking_worker_deaths);
			sacking_rebellious_deaths = base.field.GetFloat("sacking_rebellious_deaths", null, sacking_rebellious_deaths);
			sacking_levy_deaths = base.field.GetFloat("sacking_levy_deaths", null, sacking_levy_deaths);
			sacking_burned_structures = base.field.GetFloat("sacking_burned_structures", null, sacking_burned_structures);
			sacking_repair_coef = base.field.GetFloat("sacking_repair_coef", null, sacking_repair_coef);
			sacking_repair_gold_coef = base.field.GetFloat("sacking_repair_gold_coef", null, sacking_repair_gold_coef);
			sacking_recovery_production_mod = base.field.GetFloat("sacking_recovery_production_mod", null, sacking_recovery_production_mod);
			quick_recovery_mod = base.field.GetFloat("quick_recovery_mod", null, quick_recovery_mod);
			sacked_food_penalty = base.field.GetFloat("sacked_food_penalty", null, sacked_food_penalty);
			return true;
		}
	}

	public Def def;

	private Castle castle;

	private float refresh_interval = 1f;

	public bool running;

	public SackingRepair(Castle castle)
		: base(castle)
	{
		this.castle = castle;
		def = castle.game.defs.Get<Def>("SackingRepair");
	}

	public void Begin()
	{
		if (castle == null)
		{
			return;
		}
		UpdateAfter(refresh_interval);
		if (castle.IsAuthority())
		{
			if (castle.burned_buildings.Count > 0)
			{
				castle.structure_build.BeginRepair(castle.burned_buildings[0]);
			}
			if (!running)
			{
				castle.SendState<Castle.SackedStructuresState>();
				castle.NotifyListeners("repair_started");
			}
			else
			{
				castle.NotifyListeners("repair_progress");
			}
			running = true;
		}
	}

	public void Reset()
	{
		if (castle != null)
		{
			StopUpdating();
			running = false;
		}
	}

	public override void OnUpdate()
	{
		if (castle == null || (!castle.sacked && castle.burned_buildings.Count == 0))
		{
			return;
		}
		UpdateAfter(refresh_interval);
		if (!castle.IsAuthority())
		{
			return;
		}
		if (castle.burned_buildings.Count == 0)
		{
			castle.sack_damage -= castle.GetRealm().income.Get(ResourceType.Hammers) * refresh_interval * (castle.quick_recovery ? def.quick_recovery_mod : 1f);
		}
		else
		{
			castle.sack_damage = castle.GetBaseSackDamage() - ((castle.structure_build == null) ? 0f : castle.structure_build.current_production_amount);
		}
		if (castle.sack_damage <= 0f)
		{
			castle.sack_damage = 0f;
			if (castle.burned_buildings.Count == 0)
			{
				castle.sacked = false;
				castle.SendState<Castle.SackedStructuresState>();
				castle.NotifyListeners("structures_sacked");
				castle.NotifyListeners("structures_changed");
				Reset();
				return;
			}
		}
		castle.SendState<Castle.SackDamageState>();
	}
}
