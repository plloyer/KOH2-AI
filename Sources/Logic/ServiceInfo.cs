using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace Logic;

public class Request : Coroutine.IResume, Coroutine.ICaller
{
	public delegate Request CreateFunc1(string service_id, string method_name, params object[] args);

	public delegate Request CreateFunc2(Type service_type, string service_id, string method_name, params object[] args);

	public delegate Request CreateFunc3(TypeInfo ti, string service_id, string method_name, params object[] args);

	public class MethodInfo
	{
		public class Arg
		{
			public string name;

			public ParameterInfo param_info;

			public Value default_value;

			public override string ToString()
			{
				string text = $"{param_info.ParameterType} {name}";
				if (!default_value.is_unknown)
				{
					text += $" = {default_value}";
				}
				return text;
			}
		}

		public RequestAttribute attr;

		public TypeInfo ti;

		public string name;

		public List<Arg> args = new List<Arg>();

		public int required_args;

		public System.Reflection.MethodInfo method_info;

		public bool MatchArgs(object[] args)
		{
			if (args.Length < required_args || args.Length > this.args.Count)
			{
				return false;
			}
			for (int i = 0; i < args.Length; i++)
			{
				object obj = args[i];
				Arg arg = this.args[i];
				Type type = obj?.GetType();
				Type parameterType = arg.param_info.ParameterType;
				if (!(parameterType == typeof(Value)) && type != parameterType && (type != null || !parameterType.IsClass))
				{
					return false;
				}
			}
			return true;
		}

		public bool MatchArgs(List<Value> args)
		{
			if (args.Count < required_args || args.Count > this.args.Count)
			{
				return false;
			}
			for (int i = 0; i < args.Count; i++)
			{
				Value value = args[i];
				Arg arg = this.args[i];
				Type type = value.CSType();
				Type parameterType = arg.param_info.ParameterType;
				if (!(parameterType == typeof(Value)) && type != parameterType && (type != null || !parameterType.IsClass))
				{
					return false;
				}
			}
			return true;
		}

		public override string ToString()
		{
			string text = $"{method_info.ReturnType} {name}(";
			for (int i = 0; i < args.Count; i++)
			{
				Arg arg = args[i];
				if (i > 0)
				{
					text += ", ";
				}
				text += arg.ToString();
			}
			return text + ")";
		}
	}

	private struct ServiceInfo
	{
		public string service_id;

		public object service;

		public TypeInfo ti;
	}

	private class RequestData : Data
	{
		public Multiplayer multiplayer;

		public int request_id;

		public string service_id;

		public string method_name;

		public List<Data> args;

		public static RequestData Create()
		{
			return new RequestData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Request request))
			{
				return false;
			}
			request_id = request.request_id;
			service_id = request.service_id;
			method_name = request.method?.name;
			if (request.args.Count == 0)
			{
				return true;
			}
			args = new List<Data>(request.args.Count);
			for (int i = 0; i < request.args.Count; i++)
			{
				Data item = request.args[i].CreateData();
				args.Add(item);
			}
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(request_id, "request_id");
			ser.WriteStr(service_id, "service_id");
			ser.WriteStr(method_name, "method_name");
			int num = ((args != null) ? args.Count : 0);
			ser.Write7BitUInt(num, "argc");
			for (int i = 0; i < num; i++)
			{
				Data data = args[i];
				ser.WriteData(data, "arg", i);
			}
		}

		public override void Load(Serialization.IReader ser)
		{
			request_id = ser.Read7BitUInt("request_id");
			service_id = ser.ReadStr("service_id");
			method_name = ser.ReadStr("method_name");
			int num = ser.Read7BitUInt("argc");
			if (num > 0)
			{
				args = new List<Data>(num);
				for (int i = 0; i < num; i++)
				{
					Data item = ser.ReadData("arg", i);
					args.Add(item);
				}
			}
		}

		public override object GetObject(Game game)
		{
			return new Request();
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Request request))
			{
				return false;
			}
			request.request_id = request_id;
			request.service_id = service_id;
			int num = ((args != null) ? args.Count : 0);
			request.args = new List<Value>(num);
			for (int i = 0; i < num; i++)
			{
				Value item = args[i]?.GetValue(game) ?? Value.Null;
				request.args.Add(item);
			}
			if (!service_infos.TryGetValue(service_id, out var value))
			{
				LogError("Received request for unknown service: " + service_id + "." + Req2Str(method_name, request.args));
				return false;
			}
			request.method = value.ti.FindMethod(method_name, request.args);
			if (request.method == null)
			{
				LogError("Received invalid request: " + value.ti.type.Name + " " + service_id + "." + Req2Str(method_name, request.args));
				return false;
			}
			return true;
		}
	}

	private struct ResponseData
	{
		public Multiplayer multiplayer;

		public int request_id;

		public Data result;

		public string error;

		public void Save(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(request_id, "request_id");
			ser.WriteData(result, "result");
			ser.WriteStr(error, "error");
		}

		public void Load(Serialization.IReader ser)
		{
			request_id = ser.Read7BitUInt("request_id");
			result = ser.ReadData("result");
			error = ser.ReadStr("error");
		}
	}

	public class TypeInfo
	{
		public Type type;

		public List<MethodInfo> methods = new List<MethodInfo>();

		public static TypeInfo Get(Type type)
		{
			string fullName = type.FullName;
			if (type_infos.TryGetValue(fullName, out var value))
			{
				return value;
			}
			value = new TypeInfo();
			value.type = type;
			value.AddMethods(type);
			type_infos.Add(fullName, value);
			return value;
		}

		public void AddMethods(Type type)
		{
			System.Reflection.MethodInfo[] array = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (System.Reflection.MethodInfo methodInfo in array)
			{
				object[] customAttributes = methodInfo.GetCustomAttributes(typeof(RequestAttribute), inherit: true);
				if (customAttributes != null && customAttributes.Length != 0)
				{
					AddMethod(customAttributes[0] as RequestAttribute, methodInfo);
				}
			}
		}

		public void AddMethod(RequestAttribute attr, System.Reflection.MethodInfo method)
		{
			MethodInfo methodInfo = new MethodInfo();
			methodInfo.attr = attr;
			methodInfo.ti = this;
			methodInfo.name = method.Name;
			methodInfo.method_info = method;
			methodInfo.required_args = 0;
			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo parameterInfo = parameters[i];
				MethodInfo.Arg arg = new MethodInfo.Arg();
				arg.name = parameterInfo.Name;
				arg.param_info = parameterInfo;
				if (parameterInfo.HasDefaultValue)
				{
					arg.default_value = new Value(parameterInfo.DefaultValue);
				}
				else
				{
					arg.default_value = Value.Unknown;
					methodInfo.required_args = i + 1;
				}
				if (!Data.IsSerializable(parameterInfo.ParameterType))
				{
					LogError(type.FullName + "." + method.Name + " has non-serializable parameter " + parameterInfo.ParameterType.FullName + " " + parameterInfo.Name);
				}
				methodInfo.args.Add(arg);
			}
			Type returnType = method.ReturnType;
			if (returnType != typeof(void) && returnType != typeof(IEnumerator) && !Data.IsSerializable(returnType))
			{
				LogError(type.FullName + "." + method.Name + " has non-serializable return type " + returnType.FullName);
			}
			methods.Add(methodInfo);
		}

		public MethodInfo FindMethod(string name, object[] args)
		{
			if (args == null)
			{
				args = new object[1];
			}
			for (int i = 0; i < methods.Count; i++)
			{
				MethodInfo methodInfo = methods[i];
				if (!(methodInfo.name != name) && methodInfo.MatchArgs(args))
				{
					return methodInfo;
				}
			}
			return null;
		}

		public MethodInfo FindMethod(string name, List<Value> args)
		{
			for (int i = 0; i < methods.Count; i++)
			{
				MethodInfo methodInfo = methods[i];
				if (!(methodInfo.name != name) && methodInfo.MatchArgs(args))
				{
					return methodInfo;
				}
			}
			return null;
		}
	}

	public class TestService
	{
		[Request]
		public void Void0()
		{
		}

		[Request]
		public int Int0()
		{
			return 42;
		}

		[Request]
		public int Int1(int i = 42)
		{
			return i;
		}

		[Request]
		public int Int2(int i1 = 22, int i2 = 20)
		{
			return i1 + i2;
		}

		[Request]
		public string Str1(string s = null)
		{
			return s;
		}

		[Request]
		public string Str2(string s1 = null, string s2 = null)
		{
			return s1 + s2;
		}

		[Request]
		public Value Val1(Value val)
		{
			return val;
		}

		[Request]
		public Value Lst2(List<Value> lst, int idx = 0)
		{
			return lst[idx];
		}

		[Request]
		public void Err()
		{
			Error("an error");
		}

		[Request]
		public void Throw()
		{
			throw new Exception("I messed up");
		}

		[Request]
		public IEnumerator Coro(int i = 1)
		{
			Log($"Current request: {current}");
			yield return new Coroutine.WaitForFrames(i);
			Error("ok");
			yield return Coroutine.Return(i, "whatever");
		}

		[Request(timeout = 500)]
		public IEnumerator TimeOut()
		{
			yield return new Coroutine.WaitForFrames(100);
			Log("Timed out");
		}

		[Request(no_response = true)]
		public IEnumerator NoResponse()
		{
			yield return new Coroutine.WaitForFrames(1);
			yield return Coroutine.Return(Value.Null, "No Response");
		}
	}

	public Multiplayer multiplayer;

	public Stopwatch timer;

	public int request_id;

	public string service_id;

	public MethodInfo method;

	public List<Value> args;

	public Value result;

	public string error;

	public bool completed;

	public Action<Request> on_complete;

	private static Request _current = null;

	public static bool logInFile = false;

	public static FileWriter sendRPCLogWriter = null;

	public static FileWriter receiveRPCLogWriter = null;

	private static Dictionary<string, TypeInfo> type_infos = new Dictionary<string, TypeInfo>();

	private static Dictionary<string, ServiceInfo> service_infos = new Dictionary<string, ServiceInfo>();

	private static Dictionary<string, TypeInfo> service_types = new Dictionary<string, TypeInfo>();

	private static object Lock = new object();

	private static int last_id = 0;

	private static Dictionary<int, Request> pending_requests = new Dictionary<int, Request>();

	private static List<RequestData> incoming_requests = new List<RequestData>();

	private static List<ResponseData> incoming_responses = new List<ResponseData>();

	public static Request current
	{
		get
		{
			if (_current != null)
			{
				return _current;
			}
			return Coroutine.FindCaller<Request>();
		}
		private set
		{
			_current = value;
		}
	}

	public static void AddService(string service_id, object service)
	{
		if (service_infos.ContainsKey(service_id))
		{
			LogError("'" + service_id + "' request service already exists");
			return;
		}
		TypeInfo ti = TypeInfo.Get(service.GetType());
		ServiceInfo value = new ServiceInfo
		{
			service_id = service_id,
			service = service,
			ti = ti
		};
		service_infos.Add(service_id, value);
	}

	public static void DelService(string service_id, object service)
	{
		if (!service_infos.TryGetValue(service_id, out var value))
		{
			LogError(service_id + " request service dos not exist");
		}
		else if (value.service != service)
		{
			LogError($"{service_id} request service is bound to {value.service} instead of {service}");
		}
		else
		{
			service_infos.Remove(service_id);
		}
	}

	public static void BindService(string service_id, Type type)
	{
		if (service_types.TryGetValue(service_id, out var _))
		{
			LogError("'" + service_id + "' request service already bound");
			return;
		}
		TypeInfo value2 = TypeInfo.Get(type);
		service_types.Add(service_id, value2);
	}

	public static void UnbindService(string service_id)
	{
		service_types.Remove(service_id);
	}

	public static void ProcessAll()
	{
		ProcessIncomingRequests();
		ProcessIncomingResponses();
		ProcessTimeOuts();
	}

	public static Request Create(string service_id, string method_name, params object[] args)
	{
		if (!service_types.TryGetValue(service_id, out var value))
		{
			return null;
		}
		if (args == null)
		{
			args = new object[1];
		}
		return Create(value, service_id, method_name, args);
	}

	public static Request Create(Type service_type, string service_id, string method_name, params object[] args)
	{
		return Create(TypeInfo.Get(service_type), service_id, method_name, args);
	}

	public static Request Create(TypeInfo ti, string service_id, string method_name, params object[] args)
	{
		MethodInfo methodInfo = ti?.FindMethod(method_name, args);
		if (methodInfo == null)
		{
			return null;
		}
		Request request = new Request();
		request.service_id = service_id;
		request.method = methodInfo;
		request.result = Value.Unknown;
		request.args = new List<Value>(args.Length);
		foreach (object val in args)
		{
			Value item = new Value(val);
			request.args.Add(item);
		}
		return request;
	}

	public void Send(Multiplayer multiplayer)
	{
		MainThreadUpdates.AssertMainThread("Rq Send");
		if (multiplayer == null)
		{
			string msg = $"Request.Send with null multiplayer: {this}";
			LogError(msg);
			Completed(Value.Unknown, msg);
			return;
		}
		if (!multiplayer.IsOnline())
		{
			string msg2 = $"Request.Send with disconnected multiplayer: {this}";
			LogError(msg2);
			Completed(Value.Unknown, msg2);
			return;
		}
		if (multiplayer.type == Multiplayer.Type.Server)
		{
			LogError($"Request.Send called with Server multiplayer: {this}");
		}
		this.multiplayer = multiplayer;
		if (!method.attr.no_response)
		{
			if (method.attr.timeout > 0)
			{
				timer = Stopwatch.StartNew();
			}
			last_id++;
			if (last_id == 0)
			{
				last_id++;
			}
			request_id = last_id;
			pending_requests.Add(request_id, this);
		}
		else
		{
			Completed(Value.Unknown, null);
		}
		Trace("Send");
		RequestData data = new RequestData();
		data.InitFrom(this);
		if (logInFile)
		{
			string text = $"[{multiplayer.game.time_unscaled}] {data.request_id}-{data.service_id} {multiplayer.type}{multiplayer.uid}";
			text = text + " Send RQ " + data.method_name;
			sendRPCLogWriter?.WriteLine(text);
		}
		multiplayer.QueueSS(delegate
		{
			multiplayer.BeginMessage(Multiplayer.MessageId.REQUEST_SEND);
			data.Save(multiplayer.msg_writer);
			multiplayer.SendMessage(isObjectMessage: false);
		});
	}

	public void Broadcast(List<Multiplayer> players)
	{
		Trace("Broadcast");
		if (!method.attr.no_response)
		{
			LogError($"Request.Broadcast with method expecting response: {this}");
		}
		if (players == null)
		{
			LogError("Request.Broadcast with null players");
			return;
		}
		Completed(Value.Unknown, null);
		RequestData data = new RequestData();
		data.InitFrom(this);
		if (logInFile)
		{
			string text = $"[{this.multiplayer.game.time_unscaled}] {data.request_id}-{data.service_id} {this.multiplayer.type}{this.multiplayer.uid}";
			text = text + " Send RQ " + data.method_name;
			sendRPCLogWriter?.WriteLine(text);
		}
		foreach (Multiplayer multiplayer in players)
		{
			if (!multiplayer.IsOnline())
			{
				LogError($"Request.Broadcast with dsconnected multiplayer: {this}");
				continue;
			}
			if (multiplayer.type != Multiplayer.Type.ServerClient)
			{
				LogError($"Request.Broadcast with {multiplayer.type} multiplayer: {this}");
			}
			multiplayer.QueueSS(delegate
			{
				multiplayer.BeginMessage(Multiplayer.MessageId.REQUEST_SEND);
				data.Save(multiplayer.msg_writer);
				multiplayer.SendMessage(isObjectMessage: false);
			});
		}
	}

	public static Request Send(Multiplayer multiplayer, string service_id, string method_name, params object[] args)
	{
		Request request = Create(service_id, method_name, args);
		if (request == null)
		{
			string msg = "Invalid request: " + service_id + "." + Req2Str(method_name, args);
			LogError(msg);
			if (Coroutine.current != null)
			{
				Coroutine.current.error = msg;
			}
			return null;
		}
		request.Send(multiplayer);
		return request;
	}

	public static Request Send(Multiplayer multiplayer, TypeInfo ti, string service_id, string method_name, params object[] args)
	{
		Request request = Create(ti, service_id, method_name, args);
		if (request == null)
		{
			string msg = "Invalid request: " + ti?.type.Name + " " + service_id + "." + Req2Str(method_name, args);
			LogError(msg);
			if (Coroutine.current != null)
			{
				Coroutine.current.error = msg;
			}
			return null;
		}
		request.Send(multiplayer);
		return request;
	}

	public static Request Send(Multiplayer multiplayer, Type service_type, string service_id, string method_name, params object[] args)
	{
		Request request = Create(service_type, service_id, method_name, args);
		if (request == null)
		{
			string msg = "Invalid request: " + service_type?.Name + " " + service_id + "." + Req2Str(method_name, args);
			LogError(msg);
			if (Coroutine.current != null)
			{
				Coroutine.current.error = msg;
			}
			return null;
		}
		request.Send(multiplayer);
		return request;
	}

	public static Request Broadcast(List<Multiplayer> players, string service_id, string method_name, params object[] args)
	{
		Request request = Create(service_id, method_name, args);
		if (request == null)
		{
			string msg = "Invalid request: " + service_id + "." + Req2Str(method_name, args);
			LogError(msg);
			if (Coroutine.current != null)
			{
				Coroutine.current.error = msg;
			}
			return null;
		}
		request.Broadcast(players);
		return request;
	}

	public static Request Broadcast(List<Multiplayer> players, TypeInfo ti, string service_id, string method_name, params object[] args)
	{
		Request request = Create(ti, service_id, method_name, args);
		if (request == null)
		{
			string msg = "Invalid request: " + ti?.type.Name + " " + service_id + "." + Req2Str(method_name, args);
			LogError(msg);
			if (Coroutine.current != null)
			{
				Coroutine.current.error = msg;
			}
			return null;
		}
		request.Broadcast(players);
		return request;
	}

	public static Request Broadcast(List<Multiplayer> players, Type service_type, string service_id, string method_name, params object[] args)
	{
		Request request = Create(service_type, service_id, method_name, args);
		if (request == null)
		{
			string msg = "Invalid request: " + service_type?.Name + " " + service_id + "." + Req2Str(method_name, args);
			LogError(msg);
			if (Coroutine.current != null)
			{
				Coroutine.current.error = msg;
			}
			return null;
		}
		request.Broadcast(players);
		return request;
	}

	public static void Error(string error)
	{
		if (current != null)
		{
			current.error = error;
		}
	}

	private void Respond(Value result, string error = null)
	{
		if (completed)
		{
			LogError($"Request already responded: {this}");
			return;
		}
		completed = true;
		if (request_id != 0)
		{
			Trace($"Respond: {result} ({error})");
		}
		Respond(multiplayer, request_id, result, error);
	}

	private static void Respond(Multiplayer multiplayer, int request_id, Value result, string error = null)
	{
		if (multiplayer == null)
		{
			LogError("Request.Respond with null multiplayer");
		}
		else if (request_id != 0)
		{
			ResponseData response = default(ResponseData);
			response.request_id = request_id;
			response.result = result.CreateData();
			response.error = error;
			if (logInFile)
			{
				string text = $"[{multiplayer.game.time_unscaled}] {request_id} {multiplayer.type}{multiplayer.uid}";
				text += $" Send RSP {result}-{error}";
				sendRPCLogWriter?.WriteLine(text);
			}
			multiplayer.QueueSS(delegate
			{
				multiplayer.BeginMessage(Multiplayer.MessageId.REQUEST_RESPONSE);
				response.Save(multiplayer.msg_writer);
				multiplayer.SendMessage(isObjectMessage: false);
			});
		}
	}

	private void Handle(Multiplayer multiplayer, object service)
	{
		if (method == null)
		{
			LogWarning($"Handle request {request_id} with null method");
			return;
		}
		this.multiplayer = multiplayer;
		Trace("Handle");
		object[] array = new object[method.args.Count];
		for (int i = 0; i < method.args.Count; i++)
		{
			object obj = ((i < args.Count) ? args[i] : method.args[i].default_value).Object();
			if (method.args[i].param_info.ParameterType == typeof(Value))
			{
				obj = new Value(obj);
			}
			array[i] = obj;
		}
		if (method.method_info.ReturnType == typeof(IEnumerator))
		{
			IEnumerator func = method.method_info.Invoke(service, array) as IEnumerator;
			Coroutine.Start(ToString(), func, this);
			return;
		}
		current = this;
		try
		{
			object val = method.method_info.Invoke(service, array);
			Value value = ((method.method_info.ReturnType == typeof(void)) ? Value.Unknown : new Value(val));
			Respond(value, error);
		}
		catch (Exception innerException)
		{
			if (innerException is TargetInvocationException ex)
			{
				innerException = ex.InnerException;
			}
			LogError(innerException.ToString());
			Respond(Value.Unknown, innerException.Message);
		}
		current = null;
	}

	public bool Resume(Coroutine coro)
	{
		if (!completed)
		{
			return false;
		}
		coro.result = result;
		coro.error = error;
		return true;
	}

	public void OnFinished(Coroutine coro)
	{
		Respond(coro.result, error ?? coro.error);
	}

	public static void OnRequest(Multiplayer multiplayer, Serialization.IReader reader)
	{
		string text = $"{multiplayer.type}{multiplayer.uid}";
		text += " Rec RQ";
		RequestData requestData = new RequestData();
		requestData.multiplayer = multiplayer;
		requestData.Load(reader);
		lock (Lock)
		{
			incoming_requests.Add(requestData);
		}
		if (logInFile)
		{
			receiveRPCLogWriter?.WriteLine($"[{multiplayer.game.time_unscaled}] {requestData.request_id}-{requestData.service_id} {text}' '{requestData.method_name}'");
		}
	}

	public static void OnResponse(Multiplayer multiplayer, Serialization.IReader reader)
	{
		string text = $"{multiplayer.type}{multiplayer.uid} Rec RSP";
		ResponseData item = default(ResponseData);
		item.multiplayer = multiplayer;
		item.Load(reader);
		lock (Lock)
		{
			incoming_responses.Add(item);
		}
		if (logInFile)
		{
			receiveRPCLogWriter?.WriteLine($"[{multiplayer.game.time_unscaled}] {item.request_id} {text} r'{item.result?.GetValue(multiplayer.game)}' e'{item.error}'");
		}
	}

	public static Request FindPending(int req_id)
	{
		pending_requests.TryGetValue(req_id, out var value);
		return value;
	}

	private static void ProcessIncomingRequests()
	{
		List<RequestData> list;
		lock (Lock)
		{
			if (incoming_requests.Count == 0)
			{
				return;
			}
			list = incoming_requests;
			incoming_requests = new List<RequestData>();
		}
		foreach (RequestData item in list)
		{
			if (item.multiplayer == null)
			{
				LogError("Received request from null multiplayer: " + item.service_id + "." + item.method_name);
			}
			else if (item.service_id == null)
			{
				LogError($"Received request from null service_id: {item.method_name} mp:{item.multiplayer} ");
			}
			else
			{
				if (!item.multiplayer.IsOnline())
				{
					continue;
				}
				if (!service_infos.TryGetValue(item.service_id, out var value))
				{
					LogWarning("Received request for unknown service: " + item.service_id + "." + item.method_name);
					Respond(item.multiplayer, item.request_id, Value.Unknown, "Unknown request service: " + item.service_id);
					continue;
				}
				Request request = Data.RestoreObject<Request>(item, item.multiplayer?.game);
				if (request != null)
				{
					request.Handle(item.multiplayer, value.service);
				}
				else
				{
					LogWarning($"{item.request_id} request is null");
				}
			}
		}
	}

	private void Completed(Value result, string error)
	{
		pending_requests.Remove(request_id);
		this.result = result;
		this.error = error;
		completed = true;
		if (on_complete != null)
		{
			on_complete(this);
		}
	}

	private static void ProcessIncomingResponses()
	{
		List<ResponseData> list;
		lock (Lock)
		{
			if (incoming_responses.Count == 0)
			{
				return;
			}
			list = incoming_responses;
			incoming_responses = new List<ResponseData>();
		}
		foreach (ResponseData item in list)
		{
			Request request = FindPending(item.request_id);
			if (request == null)
			{
				LogWarning($"can't find pending request for response: {item.request_id}");
				continue;
			}
			Value value = ((item.result == null) ? Value.Null : item.result.GetValue(item.multiplayer?.game));
			request.Trace($"Completed: {value} ({item.error})");
			request.Completed(value, item.error);
		}
	}

	private static void ProcessTimeOuts()
	{
		List<Request> list = null;
		foreach (KeyValuePair<int, Request> pending_request in pending_requests)
		{
			Request value = pending_request.Value;
			int timeout = value.method.attr.timeout;
			if (timeout > 0 && value.timer.ElapsedMilliseconds >= timeout)
			{
				if (list == null)
				{
					list = new List<Request>();
				}
				list.Add(value);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (Request item in list)
		{
			item.Completed(Value.Unknown, "timeout");
		}
	}

	public override string ToString()
	{
		string text = RequestStr();
		if (completed)
		{
			text += $" -> {result}";
			if (error != null)
			{
				text = text + " (" + error + ")";
			}
		}
		return text;
	}

	public string RequestStr()
	{
		string text = string.Format("[{0}] {1}.{2}(", request_id, service_id, method?.name ?? "<null>");
		for (int i = 0; i < args.Count; i++)
		{
			if (i > 0)
			{
				text += ", ";
			}
			text += $"{method?.args[i].name}: {args[i]}";
		}
		return text + ")";
	}

	public static string Val2Str(object val)
	{
		if (val != null)
		{
			if (!(val is int num))
			{
				if (!(val is float f))
				{
					if (val is string text)
					{
						string text2 = text;
						return "\"" + text2 + "\"";
					}
					return val.GetType().Name + "(" + val.ToString() + ")";
				}
				return DT.FloatToStr(f) + "f";
			}
			int num2 = num;
			return num2.ToString();
		}
		return "null";
	}

	public static string Req2Str(string name, object[] args)
	{
		if (args == null)
		{
			args = new object[1];
		}
		string text = (name ?? "<null>") + "(";
		for (int i = 0; i < args.Length; i++)
		{
			object val = args[i];
			if (i > 0)
			{
				text += ", ";
			}
			text += Val2Str(val);
		}
		return text + ")";
	}

	public static string Req2Str(string name, List<Value> args)
	{
		string text = name + "(";
		if (args != null)
		{
			for (int i = 0; i < args.Count; i++)
			{
				Value value = args[i];
				if (i > 0)
				{
					text += ", ";
				}
				text += value.ToString();
			}
		}
		return text + ")";
	}

	public void Trace(string msg)
	{
	}

	public static void Log(string msg)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		UnityEngine.Debug.Log(msg);
	}

	public static void LogWarning(string msg)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		UnityEngine.Debug.LogWarning(msg);
	}

	public static void LogError(string msg)
	{
		msg = DateTime.Now.ToString("HH:mm:ss.fff: ") + msg;
		UnityEngine.Debug.LogError(msg);
	}
}
