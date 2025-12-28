namespace Logic;

public class WaterCross : Component
{
	private Army army;

	public bool running;

	public bool is_fast;

	public bool can_interrupt;

	public bool teleport;

	public int end_segment;

	public int start_segment;

	private Time start_time;

	private float duration;

	private float offset;

	public WaterCross(Army army)
		: base(army)
	{
		this.army = army;
	}

	public void Begin(int s_segment, float duration, bool can_interrupt, int end_segment, bool is_fast = false, bool teleport = false)
	{
		if (army != null)
		{
			this.end_segment = end_segment;
			start_segment = s_segment;
			this.is_fast = is_fast;
			this.can_interrupt = can_interrupt;
			this.teleport = teleport;
			this.duration = duration;
			offset = 0f;
			Resume();
		}
	}

	public void Resume()
	{
		start_time = base.game.time;
		running = true;
		army.movement.Pause(pause: true);
		UpdateAfter(duration);
	}

	public void Begin(float duration, bool can_interrupt, bool is_fast = false, bool teleport = false, float offset = 0f)
	{
		if (army != null)
		{
			this.is_fast = is_fast;
			this.can_interrupt = can_interrupt;
			this.teleport = teleport;
			this.duration = duration;
			start_time = base.game.time;
			this.offset = offset;
			Resume();
		}
	}

	public void Stop(bool success = false, bool teleport_valid = false)
	{
		if (!running)
		{
			return;
		}
		can_interrupt = false;
		running = false;
		is_fast = false;
		army.movement.Pause(pause: false);
		if (offset > 0f && teleport_valid)
		{
			if (army.movement?.path != null)
			{
				army.movement.AdvanceTo(army.movement.path.t + offset);
			}
		}
		else if (teleport && teleport_valid && army.movement.path != null && army.movement.path.segments.Count > end_segment)
		{
			army.movement.path.t = army.movement.path.segments[end_segment].t;
			army.movement.path.segment_idx = end_segment;
			army.ResetSegmentIdxCheck();
			army.movement.AdvanceTo(army.movement.path.t);
			army.leader?.NotifyListeners("army_river_or_island_crossing");
		}
		if (success && !teleport)
		{
			army.leader?.NotifyListeners("army_moved_embark_disembark");
		}
		PathData.Node node = base.game.path_finding.data.GetNode(army.position);
		army.is_in_water = node.ocean || node.coast;
		army.SetWorldSpeed();
		army.NotifyListeners("moved_in_water");
	}

	public float Progress()
	{
		return (base.game.time - start_time) / duration;
	}

	public override void OnUpdate()
	{
		if (running && army != null)
		{
			Stop(success: true, teleport_valid: true);
		}
	}
}
