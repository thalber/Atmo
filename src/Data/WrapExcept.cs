using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Atmo.Data;
/// <summary>
/// Wraps another <see cref="IArgPayload"/>, rerouting selected properties to something different. Instantiate via <see cref="Utils.Except{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public struct WrapExcept<T> : IArgPayload
	where T : IArgPayload
{
	/// <summary>
	/// Wrapped object
	/// </summary>
	public readonly T wrapped;

	/// <summary>
	/// <see cref="I32"/> property backing.
	/// </summary>
	public FakeProp<int>? prop_I32;
	/// <summary>
	/// <see cref="F32"/> property backing.
	/// </summary>
	public FakeProp<float>? prop_F32;
	/// <summary>
	/// <see cref="Bool"/> property backing.
	/// </summary>
	public FakeProp<bool>? prop_Bool;
	/// <summary>
	/// <see cref="Str"/> property backing.
	/// </summary>
	public FakeProp<string>? prop_Str;
	/// <summary>
	/// <see cref="Vec"/> property backing
	/// </summary>
	public FakeProp<Vector4>? prop_Vec;
	/// <summary>
	/// Creates a new instance with given prop backings. Every argument passed as non null will reroute the property.
	/// </summary>
	
	public WrapExcept(
		T wrap,
		FakeProp<int>? p_i32 = null,
		FakeProp<float>? p_f32 = null,
		FakeProp<string>? p_str = null,
		FakeProp<bool>? p_bool = null,
		FakeProp<Vector4>? p_vec = null)
	{
		wrapped = wrap;
		prop_Str = p_str;
		prop_I32 = p_i32;
		prop_F32 = p_f32;
		prop_Bool = p_bool;
		prop_Vec = p_vec;
	}

	/// <inheritdoc/>
	public string Raw
	{
		get => string.Empty;
		set { }
	}
	/// <inheritdoc/>
	public float F32
	{
		get => prop_F32?.a?.Invoke() ?? wrapped.F32;
		set
		{
			if (prop_F32 is null)
			{
				wrapped.F32 = value;
			}
			else prop_F32.b?.Invoke(value);
		}
	}
	/// <inheritdoc/>
	public int I32
	{
		get => prop_I32?.a?.Invoke() ?? wrapped.I32;
		set
		{
			if (prop_I32 is null)
			{
				wrapped.I32 = value;
			}
			else prop_I32?.b?.Invoke(value);
		}
	}
	/// <inheritdoc/>
	public string Str
	{
		get => prop_Str?.a?.Invoke() ?? wrapped.Str;
		set
		{
			if (prop_Str is null)
			{
				wrapped.Str = value;
			}
			else prop_Str?.b?.Invoke(value);
		}
	}
	/// <inheritdoc/>
	public bool Bool
	{
		get => prop_Bool?.a?.Invoke() ?? wrapped.Bool;
		set
		{
			if (prop_Bool is null)
			{
				wrapped.Bool = value;
			}
			else prop_Bool?.b?.Invoke(value);
		}
	}
	/// <inheritdoc/>
	public Vector4 Vec
	{
		get => prop_Vec?.a?.Invoke() ?? wrapped.Vec;
		set
		{
			if (prop_Vec is null)
			{
				wrapped.Vec = value;
			}
			else prop_Vec?.b?.Invoke(value);
		}
	}
	/// <inheritdoc/>
	public void GetEnum<TE>(out TE? value) where TE : Enum
	{
		if (!TryParseEnum(Str, out value))
		{
			value = (TE)Convert.ChangeType(I32, typeof(TE));
		};
	}
	/// <inheritdoc/>
	public void SetEnum<TE>(in TE value) where TE : Enum
	{
		Str = value.ToString();
		I32 = (int)Convert.ChangeType(value, typeof(int));
	}
	/// <inheritdoc/>
	public ArgType DataType => ArgType.OTHER;
}
