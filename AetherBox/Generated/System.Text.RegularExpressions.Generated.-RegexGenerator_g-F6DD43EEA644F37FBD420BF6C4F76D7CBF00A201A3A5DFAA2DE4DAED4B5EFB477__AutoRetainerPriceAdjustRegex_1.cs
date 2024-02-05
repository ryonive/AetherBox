// AetherBox, Version=69.5.0.1, Culture=neutral, PublicKeyToken=null
// System.Text.RegularExpressions.Generated.<RegexGenerator_g>F6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__AutoRetainerPriceAdjustRegex_1
using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
namespace AetherBox.Generated;
[GeneratedCode("System.Text.RegularExpressions.Generator", "7.0.9.7226")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__AutoRetainerPriceAdjustRegex_1 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan))
				{
					int start;
					start = runtextpos;
					Capture(0, start, runtextpos = start + 1);
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
						if (!char.IsAsciiDigit(span[i]))
						{
							runtextpos = pos + i;
							return true;
						}
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__AutoRetainerPriceAdjustRegex_1 Instance = new _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__AutoRetainerPriceAdjustRegex_1();

	private _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__AutoRetainerPriceAdjustRegex_1()
	{
		pattern = "[^0-9]";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF6DD43EEA644F37FBD420BF6C4F76D7CBF00A201A3A5DFAA2DE4DAED4B5EFB477__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
