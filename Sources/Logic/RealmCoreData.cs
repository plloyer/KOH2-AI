namespace Logic;

public class RealmCoreData
{
	public int kingdom_id;

	public bool is_core;

	public Time last_captured;

	public RealmCoreData(int kingdom_id, bool is_core, Time last_captured)
	{
		this.kingdom_id = kingdom_id;
		this.is_core = is_core;
		this.last_captured = last_captured;
	}
}
