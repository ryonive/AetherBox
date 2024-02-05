// AetherBox, Version=69.5.0.1, Culture=neutral, PublicKeyToken=null
// System.Text.RegularExpressions.Generated.<RegexGenerator_g>F6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__SeStringRegex_0
using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
namespace AetherBox.Generated;
[GeneratedCode("System.Text.RegularExpressions.Generator", "7.0.9.7226")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__SeStringRegex_0 : Regex
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
				if (pos <= inputSpan.Length - 3)
				{
					ReadOnlySpan<char> span = inputSpan.Slice(pos);
					int i;
					for (i = 0; i < span.Length - 2; i++)
					{
						int indexOfPos;
						indexOfPos = span.Slice(i).IndexOf('{');
						if (indexOfPos < 0)
						{
							break;
						}
						i += indexOfPos;
						if ((uint)(i + 1) >= (uint)span.Length)
						{
							break;
						}
						if (char.IsDigit(span[i + 1]))
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
				int capture_starting_pos;
				capture_starting_pos = 0;
				ReadOnlySpan<char> slice = inputSpan.Slice(pos);
				if (slice.IsEmpty || slice[0] != '{')
				{
					UncaptureUntil(0);
					return false;
				}
				pos++;
				slice = inputSpan.Slice(pos);
				capture_starting_pos = pos;
				int iteration;
				for (iteration = 0; (uint)iteration < (uint)slice.Length && char.IsDigit(slice[iteration]); iteration++)
				{
				}
				if (iteration == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				slice = slice.Slice(iteration);
				pos += iteration;
				Capture(1, capture_starting_pos, pos);
				if (slice.IsEmpty || slice[0] != '}')
				{
					UncaptureUntil(0);
					return false;
				}
				Capture(0, matchStart, runtextpos = pos + 1);
				return true;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__SeStringRegex_0 Instance = new _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__SeStringRegex_0();

	private _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__SeStringRegex_0()
	{
		pattern = "\\{(\\d+)\\}";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
