namespace Logic;

public class Component : Updateable
{
	public Object obj;

	public Game game => obj?.game;

	public Component(Object obj)
	{
		this.obj = obj;
		obj.AddComponent(this);
	}

	public void Destroy()
	{
		obj?.RemoveComponent(this);
		OnDestroy();
		StopUpdating();
		obj = null;
	}

	public override string ToString()
	{
		if (obj == null)
		{
			return "[Destroyed]" + rtti.name;
		}
		return $"{rtti.name} of {obj}";
	}

	public void Log(string msg)
	{
		Game.Log($"{this}: {msg}", Game.LogType.Message);
	}

	public void Warning(string msg)
	{
		Game.Log($"{this}: {msg}", Game.LogType.Warning);
	}

	public void Error(string msg)
	{
		Game.Log($"{this}: {msg}", Game.LogType.Error);
	}

	public virtual void OnStart()
	{
	}

	public virtual void OnDestroy()
	{
	}

	public void UpdateNextFrame()
	{
		if (obj == null)
		{
			Error("UpdateNextFrame() called on destroyed component");
		}
		else if (!obj.IsValid())
		{
			Error("UpdateNextFrame() called on " + obj.obj_state.ToString() + " object component");
		}
		else
		{
			game.scheduler.RegisterForNextFrame(this);
		}
	}

	public void UpdateAfter(float seconds, bool exact = false)
	{
		if (obj == null)
		{
			Error("UpdateAfter() called on destroyed component");
		}
		else if (!obj.IsValid())
		{
			Error("UpdateAfter() called on " + obj.obj_state.ToString() + " object component");
		}
		else
		{
			game.scheduler.RegisterAfterSeconds(this, seconds, exact);
		}
	}

	public void UpdateInBatch(Scheduler.UpdateBatch batch)
	{
		if (obj == null)
		{
			Error("UpdateInBatch() called on destroyed component");
		}
		else if (!obj.IsValid())
		{
			Error("UpdateInBatch() called on " + obj.obj_state.ToString() + " object component");
		}
		else
		{
			game.scheduler.RegisterInBatch(this, batch);
		}
	}

	public void StopUpdating()
	{
		game?.scheduler?.Unregister(this);
	}
}
