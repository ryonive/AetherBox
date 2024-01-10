using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
namespace AetherBox.Helpers;
public static class ArrayExtensions
{
	public static IEnumerable<(T Value, int Index)> WithIndex<T>(this IEnumerable<T> list)
	{
		return list.Select((T x, int i) => (x: x, i: i));
	}

	public static IEnumerable<T> WithoutIndex<T>(this IEnumerable<(T Value, int Index)> list)
	{
		return list.Select(((T Value, int Index) x) => x.Value);
	}

	public static IEnumerable<int> WithoutValue<T>(this IEnumerable<(T Value, int Index)> list)
	{
		return list.Select(((T Value, int Index) x) => x.Index);
	}

	public static int IndexOf<T>(this IEnumerable<T> array, Predicate<T> predicate)
	{
		int i = 0;
		foreach (T obj in array)
		{
			if (predicate(obj))
			{
				return i;
			}
			i++;
		}
		return -1;
	}

	public static int IndexOf<T>(this IEnumerable<T> array, T needle) where T : notnull
	{
		int i = 0;
		foreach (T obj in array)
		{
			if (needle.Equals(obj))
			{
				return i;
			}
			i++;
		}
		return -1;
	}

	public static bool FindFirst<T>(this IEnumerable<T> array, Predicate<T> predicate, [NotNullWhen(true)] out T? result)
	{
		foreach (T obj in array)
		{
			if (predicate(obj))
			{
				result = obj;
				return true;
			}
		}
		result = default(T);
		return false;
	}

	public static bool FindFirst<T>(this IEnumerable<T> array, T needle, [NotNullWhen(true)] out T? result) where T : notnull
	{
		foreach (T obj in array)
		{
			if (obj.Equals(needle))
			{
				result = obj;
				return true;
			}
		}
		result = default(T);
		return false;
	}
}
