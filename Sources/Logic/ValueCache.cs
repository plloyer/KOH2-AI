namespace Logic;

public class ValueCache
{
	public delegate Value calc_func_delegate();

	public Game game;

	private Value last_value;

	private Time last_calc_time;

	public calc_func_delegate CalcFunc;

	public ValueCache(calc_func_delegate calc_func, Game game)
	{
		this.game = game;
		CalcFunc = calc_func;
		last_value = 0;
		last_calc_time = Time.Zero;
	}

	public Value GetValue()
	{
		if (game.real_time_total_per_frame == last_calc_time)
		{
			return last_value;
		}
		last_calc_time = game.real_time_total_per_frame;
		last_value = CalcFunc();
		return last_value;
	}
}
