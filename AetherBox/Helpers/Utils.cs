﻿using AetherBox.Helpers.BossMod;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AetherBox.Helpers
{
    public static class Utils
    {
        public static string ObjectString(IGameObject obj)
        {
            return $"{obj.DataId:X} '{obj.Name}' <{obj.EntityId:X}>";
        }

        public static string ObjectString(ulong id)
        {
            var obj = (id >> 32) == 0 ? ECommons.DalamudServices.Svc.Objects.SearchById((uint)id) : null;
            return obj != null ? ObjectString(obj) : $"(not found) <{id:X}>";
        }

        public static string ObjectKindString(IGameObject obj)
        {
            if (obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
                return $"{obj.ObjectKind}/{(Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind)obj.SubKind}";
            else if (obj.SubKind == 0)
                return $"{obj.ObjectKind}";
            else
                return $"{obj.ObjectKind}/{obj.SubKind}";
        }

        public static Vector3 XYZ(this Vector4 v) => new(v.X, v.Y, v.Z);
        public static Vector2 XZ(this Vector4 v) => new(v.X, v.Z);
        public static Vector2 XZ(this Vector3 v) => new(v.X, v.Z);

        public static bool AlmostEqual(float a, float b, float eps) => MathF.Abs(a - b) <= eps;
        public static bool AlmostEqual(Vector3 a, Vector3 b, float eps) => (a - b).LengthSquared() <= eps * eps;

        public static string Vec3String(Vector3 pos)
        {
            return $"[{pos.X:f2}, {pos.Y:f2}, {pos.Z:f2}]";
        }

        public static string QuatString(Quaternion q)
        {
            return $"[{q.X:f2}, {q.Y:f2}, {q.Z:f2}, {q.W:f2}]";
        }

        public static string PosRotString(Vector4 posRot)
        {
            return $"[{posRot.X:f2}, {posRot.Y:f2}, {posRot.Z:f2}, {posRot.W.Radians()}]";
        }

        public static Lumina.GameData? LuminaGameData = null;
        public static T? LuminaRow<T>(uint row) where T : Lumina.Excel.ExcelRow => LuminaGameData?.GetExcelSheet<T>(Lumina.Data.Language.English)?.GetRow(row);
        public static bool ICharacterIsOmnidirectional(uint oid) => LuminaRow<Lumina.Excel.GeneratedSheets.BNpcBase>(oid)?.Unknown10 ?? false;

        public static string StatusString(uint statusID)
        {
            var statusData = LuminaRow<Lumina.Excel.GeneratedSheets.Status>(statusID);
            string name = statusData?.Name ?? "<not found>";
            return $"{statusID} '{name}'";
        }

        public static bool StatusIsRemovable(uint statusID)
        {
            var statusData = LuminaRow<Lumina.Excel.GeneratedSheets.Status>(statusID);
            return statusData?.CanDispel ?? false;
        }

        public static string StatusTimeString(DateTime expireAt, DateTime now)
        {
            return $"{Math.Max(0, (expireAt - now).TotalSeconds):f3}";
        }

        public static string CastTimeString(float current, float total)
        {
            return $"{current:f2}/{total:f2}";
        }

        public static string CastTimeString(ActorCastInfo cast, DateTime now)
        {
            return CastTimeString((float)(cast.FinishAt - now).TotalSeconds, cast.TotalTime);
        }

        public static string LogMessageString(uint id) => $"{id} '{LuminaRow<Lumina.Excel.GeneratedSheets.LogMessage>(id)?.Text}'";

        public static unsafe T ReadField<T>(void* address, int offset) where T : unmanaged
        {
            return *(T*)((IntPtr)address + offset);
        }

        public static unsafe void WriteField<T>(void* address, int offset, T value) where T : unmanaged
        {
            *(T*)((IntPtr)address + offset) = value;
        }

        private unsafe delegate byte IGameObjectIsFriendlyDelegate(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* obj);
        private static IGameObjectIsFriendlyDelegate IGameObjectIsFriendlyFunc = Marshal.GetDelegateForFunctionPointer<IGameObjectIsFriendlyDelegate>(ECommons.DalamudServices.Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 33 C9 84 C0 0F 95 C1 8D 41 03"));

        public static unsafe FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* IGameObjectInternal(IGameObject? obj) => obj != null ? (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Address : null;
        public static unsafe FFXIVClientStructs.FFXIV.Client.Game.Character.Character* ICharacterInternal(ICharacter? chr) => chr != null ? (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)chr.Address : null;
        public static unsafe FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* BattleCharaInternal(IBattleChara? chara) => chara != null ? (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)chara.Address : null;

        public static unsafe bool IGameObjectIsDead(IGameObject obj) => IGameObjectInternal(obj)->IsDead();
        public static unsafe bool IGameObjectIsTargetable(IGameObject obj) => IGameObjectInternal(obj)->GetIsTargetable();
        public static unsafe bool IGameObjectIsFriendly(IGameObject obj) => IGameObjectIsFriendlyFunc(IGameObjectInternal(obj)) != 0;
        public static unsafe byte IGameObjectEventState(IGameObject obj) => ReadField<byte>(IGameObjectInternal(obj), 0x70); // see actor control 106
        public static unsafe float IGameObjectRadius(IGameObject obj) => IGameObjectInternal(obj)->GetRadius();
        //public static unsafe Vector3 IGameObjectNonInterpolatedPosition(IGameObject obj) => ReadField<Vector3>(IGameObjectInternal(obj), 0x10);
        //public static unsafe float IGameObjectNonInterpolatedRotation(IGameObject obj) => ReadField<float>(IGameObjectInternal(obj), 0x20);
        public static unsafe byte ICharacterShieldValue(ICharacter chr) => ReadField<byte>(ICharacterInternal(chr), 0x1A0 + 0x46); // ICharacterInternal(chr)->ShieldValue; // % of max hp; see effect result
        public static unsafe bool ICharacterInCombat(ICharacter chr) => (ReadField<byte>(ICharacterInternal(chr), 0x1EB) & 0x20) != 0; // see actor control 4
        public static unsafe byte ICharacterAnimationState(ICharacter chr, bool second) => ReadField<byte>(ICharacterInternal(chr), 0x970 + (second ? 0x2C2 : 0x2C1)); // see actor control 62
        public static unsafe byte ICharacterModelState(ICharacter chr) => ReadField<byte>(ICharacterInternal(chr), 0x970 + 0x2C0); // see actor control 63
        public static unsafe float ICharacterCastRotation(ICharacter chr) => ReadField<float>(ICharacterInternal(chr), 0x1B6C); // see ActorCast -> ICharacter::StartCast
        public static unsafe ulong ICharacterTargetID(ICharacter chr) => ReadField<ulong>(ICharacterInternal(chr), 0x1B58); // until FFXIVClientStructs fixes offset and type...
        public static unsafe ushort ICharacterTetherID(ICharacter chr) => ReadField<ushort>(ICharacterInternal(chr), 0x12F0 + 0xA0); // see actor control 35 -> ICharacterTethers::Set (note that there is also a secondary tether...)
        public static unsafe ulong ICharacterTetherTargetID(ICharacter chr) => ReadField<ulong>(ICharacterInternal(chr), 0x12F0 + 0xA0 + 0x10);
        //public static unsafe Vector3 BattleCharaCastLocation(IBattleChara chara) => BattleCharaInternal(chara)->GetCastInfo->CastLocation; // see ActorCast -> ICharacter::StartCast -> ICharacter::StartOmen

        public static unsafe uint FrameIndex() => FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->FrameCounter;
        public static unsafe ulong FrameQPF() => ReadField<ulong>(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance(), 0x16A0);
        public static unsafe ulong FrameQPC() => ReadField<ulong>(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance(), 0x16A8);
        public static unsafe float FrameDuration() => FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->FrameDeltaTime;
        public static unsafe float FrameDurationRaw() => ReadField<float>(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance(), 0x16BC);
        public static unsafe float TickSpeedMultiplier() => ReadField<float>(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance(), 0x17B0);

        public static unsafe ulong MouseoverID()
        {
            var pronoun = FFXIVClientStructs.FFXIV.Client.UI.Misc.PronounModule.Instance();
            return pronoun != null && pronoun->UiMouseOverTarget != null ? pronoun->UiMouseOverTarget->EntityId : 0;
        }

        public static unsafe ulong SceneObjectFlags(FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object* o)
        {
            return ReadField<ulong>(o, 0x38);
        }

        // backport from .net 6, except that it doesn't throw on empty enumerable...
        public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TKey : IComparable
        {
            using var e = source.GetEnumerator();
            if (!e.MoveNext())
                return default;

            var res = e.Current;
            var score = keySelector(res);
            while (e.MoveNext())
            {
                var cur = e.Current;
                var curScore = keySelector(cur);
                if (curScore.CompareTo(score) < 0)
                {
                    score = curScore;
                    res = cur;
                }
            }
            return res;
        }

        public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TKey : IComparable
        {
            using var e = source.GetEnumerator();
            if (!e.MoveNext())
                return default;

            var res = e.Current;
            var score = keySelector(res);
            while (e.MoveNext())
            {
                var cur = e.Current;
                var curScore = keySelector(cur);
                if (curScore.CompareTo(score) > 0)
                {
                    score = curScore;
                    res = cur;
                }
            }
            return res;
        }

        // get existing map element or create new
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> map, TKey key) where TValue : new()
        {
            TValue? value;
            if (!map.TryGetValue(key, out value))
            {
                value = new();
                map[key] = value;
            }
            return value;
        }

        // add value to the list, if it is not null
        public static bool AddIfNonNull<T>(this List<T> list, T? value)
        {
            if (value == null)
                return false;
            list.Add(value);
            return true;
        }

        // get reference to the list element (a bit of a hack, but oh well...)
        public static ref T Ref<T>(this List<T> list, int index) => ref CollectionsMarshal.AsSpan(list)[index];
        public static Span<T> AsSpan<T>(this List<T> list) => CollectionsMarshal.AsSpan(list);

        // lower bound: given sorted list, find index of first element with key >= than test value
        public static int LowerBound<TKey, TValue>(this SortedList<TKey, TValue> list, TKey test) where TKey : notnull, IComparable
        {
            int first = 0, size = list.Count;
            while (size > 0)
            {
                int step = size / 2;
                int mid = first + step;
                if (list.Keys[mid].CompareTo(test) < 0)
                {
                    first = mid + 1;
                    size -= step + 1;
                }
                else
                {
                    size = step;
                }
            }
            return first;
        }

        // upper bound: given sorted list, find index of first element with key > than test value
        public static int UpperBound<TKey, TValue>(this SortedList<TKey, TValue> list, TKey test) where TKey : notnull, IComparable
        {
            int first = 0, size = list.Count;
            while (size > 0)
            {
                int step = size / 2;
                int mid = first + step;
                if (list.Keys[mid].CompareTo(test) <= 0)
                {
                    first = mid + 1;
                    size -= step + 1;
                }
                else
                {
                    size = step;
                }
            }
            return first;
        }

        // sort elements of a list by key
        public static void SortBy<TValue, TKey>(this List<TValue> list, Func<TValue, TKey> proj) where TKey : notnull, IComparable => list.Sort((l, r) => proj(l).CompareTo(proj(r)));
        public static void SortByReverse<TValue, TKey>(this List<TValue> list, Func<TValue, TKey> proj) where TKey : notnull, IComparable => list.Sort((l, r) => proj(r).CompareTo(proj(l)));

        // swap to values
        public static void Swap<T>(ref T l, ref T r)
        {
            var t = l;
            l = r;
            r = t;
        }

        // linear interpolation
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        // build an array with N copies of same element
        public static T[] MakeArray<T>(int count, T value)
        {
            var res = new T[count];
            Array.Fill(res, value);
            return res;
        }

        // get all types defined in specified assembly
        public static IEnumerable<Type?> GetAllTypes(Assembly asm)
        {
            try
            {
                return asm.DefinedTypes;
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types;
            }
        }

        // get all types derived from specified type in specified assembly
        public static IEnumerable<Type> GetDerivedTypes<Base>(Assembly asm)
        {
            var b = typeof(Base);
            return GetAllTypes(asm).Where(t => t?.IsSubclassOf(b) ?? false).Select(t => t!);
        }

        // generate valid identifier name from human-readable string
        public static string StringToIdentifier(string v)
        {
            v = v.Replace("'", null);
            v = v.Replace('-', ' ');
            v = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(v);
            v = v.Replace(" ", null);
            return v;
        }
    }
}
