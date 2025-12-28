namespace Logic;

public interface IVars
{
	Value GetVar(string key, IVars vars = null, bool as_value = true);
}
