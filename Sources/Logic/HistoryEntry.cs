namespace Logic;

public class HistoryEntry
{
	public Vars vars;

	public DT.Field field;

	public HistoryEntry(DT.Field field, Vars vars)
	{
		this.field = field;
		this.vars = vars;
	}

	public DT.Field GetDefField()
	{
		return field;
	}

	public override string ToString()
	{
		return field.key + ": " + vars;
	}
}
