// AetherBox, Version=69.5.0.1, Culture=neutral, PublicKeyToken=null
// System.Text.RegularExpressions.Generated.<RegexGenerator_g>F6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__ArgumentRegex_2
using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
namespace AetherBox.Generated;
[GeneratedCode("System.Text.RegularExpressions.Generator", "7.0.9.7226")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__ArgumentRegex_2 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int pos;
				pos = runtextpos;
				if ((uint)pos < (uint)inputSpan.Length)
				{
					ReadOnlySpan<char> span = inputSpan.Slice(pos);
					for (int i = 0; i < span.Length; i++)
					{
						if (span[i] != ' ')
						{
							runtextpos = pos + i;
							return true;
						}
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int pos;
				pos = runtextpos;
				int matchStart;
				matchStart = pos;
				int lazyloop_pos;
				lazyloop_pos = 0;
				ReadOnlySpan<char> slice = inputSpan.Slice(pos);
				int atomic_stackpos;
				atomic_stackpos = 0;
				int alternation_starting_pos;
				alternation_starting_pos = pos;
				if (slice.IsEmpty || slice[0] != '"' || (uint)slice.Length < 2u || slice[1] == '\n')
				{
					goto IL_00f8;
				}
				pos += 2;
				slice = inputSpan.Slice(pos);
				lazyloop_pos = pos;
				while (slice.IsEmpty || slice[0] != '"')
				{
					if (_003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					pos = lazyloop_pos;
					slice = inputSpan.Slice(pos);
					if (!slice.IsEmpty && slice[0] != '\n')
					{
						pos++;
						slice = inputSpan.Slice(pos);
						lazyloop_pos = slice.IndexOfAny('\n', '"');
						if ((uint)lazyloop_pos < (uint)slice.Length && slice[lazyloop_pos] != '\n')
						{
							pos += lazyloop_pos;
							slice = inputSpan.Slice(pos);
							lazyloop_pos = pos;
							continue;
						}
					}
					goto IL_00f8;
				}
				pos++;
				slice = inputSpan.Slice(pos);
				goto IL_0131;
				IL_00f8:
				pos = alternation_starting_pos;
				slice = inputSpan.Slice(pos);
				int iteration;
				iteration = slice.IndexOf(' ');
				if (iteration < 0)
				{
					iteration = slice.Length;
				}
				if (iteration == 0)
				{
					return false;
				}
				slice = slice.Slice(iteration);
				pos += iteration;
				goto IL_0131;
				IL_0131:
				runtextpos = pos;
				Capture(0, matchStart, pos);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__ArgumentRegex_2 Instance = new _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__ArgumentRegex_2();

	private _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__ArgumentRegex_2()
	{
		pattern = "[\\\"].+?[\\\"]|[^ ]+";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
