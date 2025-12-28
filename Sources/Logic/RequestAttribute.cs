using System;

namespace Logic;

[AttributeUsage(AttributeTargets.Method)]
public class RequestAttribute : Attribute
{
	public bool no_response;

	public int timeout = 3000;
}
