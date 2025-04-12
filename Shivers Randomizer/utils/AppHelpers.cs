using Shivers_Randomizer.enums;
using Shivers_Randomizer.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Shivers_Randomizer.utils;

internal static class AppHelpers
{
    [DllImport("user32.dll")] public static extern bool GetWindowRect(UIntPtr hWnd, ref RectSpecial rectangle);
    [DllImport("user32.dll")] public static extern bool GetClientRect(UIntPtr hWnd, ref RectSpecial rectangle);
    [DllImport("user32.dll")] public static extern bool PostMessage(UIntPtr hWnd, uint Msg, int wParam, int lParam);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static extern bool IsIconic(UIntPtr hWnd);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static extern void BlockInput([In, MarshalAs(UnmanagedType.Bool)] bool fBlockIt);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static extern void EnableWindow(UIntPtr hWnd, [In, MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [DllImport("KERNEL32.DLL")] public static extern UIntPtr OpenProcess(uint access, bool inheritHandler, uint processId);
    [DllImport("KERNEL32.DLL")] public static extern int VirtualQueryEx(UIntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, int dwLength);
    [DllImport("KERNEL32.DLL", SetLastError = true)] public static extern bool ReadProcessMemory(UIntPtr process, ulong address, byte[] buffer, ulong size, ref uint read);
    [DllImport("KERNEL32.DLL", SetLastError = true)] public static extern bool WriteProcessMemory(UIntPtr process, ulong address, byte[] buffer, uint size, ref uint written);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool SetWindowPos(UIntPtr hWnd, UIntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll", SetLastError = true)] public static extern UIntPtr GetWindow(UIntPtr hWnd, uint uCmd);
    public static int MakeLParam(int x, int y) => y << 16 | x & 0xFFFF;

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION64
    {
        public ulong BaseAddress;
        public ulong AllocationBase;
        public int AllocationProtect;
        public int __alignment1;
        public ulong RegionSize;
        public int State;
        public int Protect;
        public int Type;
        public int __alignment2;
    }

    public struct RectSpecial
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
    }

    public static bool IsKthBitSet(int n, int k)
    {
        return (n & (1 << k)) > 0;
    }

    // Sets the kth bit of a value. 0 indexed
    public static int SetKthBit(int value, int k, bool set)
    {
        if (set) // ON
        {
            value |= (1 << k);
        }
        else // OFF
        {
            value &= ~(1 << k);
        }

        return value;
    }

    public static List<MEMORY_BASIC_INFORMATION64> MemInfo(UIntPtr pHandle)
    {
        UIntPtr address = new();
        List<MEMORY_BASIC_INFORMATION64> memReg = new();
        while (true)
        {
            MEMORY_BASIC_INFORMATION64 memInfo = new();
            int MemDump = VirtualQueryEx(pHandle, address, out memInfo, Marshal.SizeOf(memInfo));
            if (MemDump == 0)
            {
                break;
            }

            if ((memInfo.State & 0x1000) != 0 && (memInfo.Protect & 0x100) == 0)
            {
                memReg.Add(memInfo);
            }

            address = new UIntPtr(memInfo.BaseAddress + memInfo.RegionSize);
        }

        return memReg;
    }

    public static int[] GetScanBytes(byte[] sFor)
    {
        int end = sFor.Length - 1;
        int[] sBytes = new int[256];
        Array.Fill(sBytes, sFor.Length);

        for (int i = 0; i < end; i++)
        {
            sBytes[sFor[i]] = end - i;
        }

        return sBytes;
    }

    public static UIntPtr AobScan2(UIntPtr processHandle, byte[] pattern)
    {
        List<MEMORY_BASIC_INFORMATION64> memReg = MemInfo(processHandle);
        for (int i = 0; i < memReg.Count; i++)
        {
            byte[] buff = new byte[memReg[i].RegionSize];
            uint refzero = 0;
            ReadProcessMemory(processHandle, memReg[i].BaseAddress, buff, memReg[i].RegionSize, ref refzero);

            UIntPtr Result = Scan2(buff, pattern);
            if (Result != UIntPtr.Zero)
            {
                return new UIntPtr(memReg[i].BaseAddress + Result.ToUInt64());
            }
        }

        return UIntPtr.Zero;
    }

    private static UIntPtr Scan2(byte[] sIn, byte[] sFor)
    {
        int pool = 0;
        int end = sFor.Length - 1;
        int[] sBytes = GetScanBytes(sFor);

        while (pool <= sIn.Length - sFor.Length)
        {
            for (int i = end; sIn[pool + i] == sFor[i]; i--)
            {
                if (i == 0)
                {
                    return new UIntPtr((uint)pool);
                }
            }

            pool += sBytes[sIn[pool + end]];
        }

        return UIntPtr.Zero;
    }

    public static UIntPtr AobScanWithWildCard(UIntPtr processHandle, byte[] pattern)
    {
        List<MEMORY_BASIC_INFORMATION64> memReg = MemInfo(processHandle);
        for (int i = 0; i < memReg.Count; i++)
        {
            byte[] buff = new byte[memReg[i].RegionSize];
            uint refzero = 0;
            ReadProcessMemory(processHandle, memReg[i].BaseAddress, buff, memReg[i].RegionSize, ref refzero);

            UIntPtr Result = ScanWithWildcard(buff, pattern, 0xFF);
            if (Result != UIntPtr.Zero)
            {
                return new UIntPtr(memReg[i].BaseAddress + Result.ToUInt64());
            }
        }

        return UIntPtr.Zero;
    }

    private static UIntPtr ScanWithWildcard(byte[] sIn, byte[] sFor, byte wildcard)
    {
        int pool = 0;
        int end = sFor.Length - 1;

        while (pool <= sIn.Length - sFor.Length)
        {
            for (int i = end; i >= 0; i--)
            {
                if (sFor[i] == wildcard || sIn[pool + i] == sFor[i])
                {
                    if (i == 0)
                    {
                        return new UIntPtr((uint)pool);
                    }
                }
                else
                {
                    break; // Break the loop if the byte doesn't match.
                }
            }

            pool++; // Move to the next byte position.
        }

        return UIntPtr.Zero;
    }


    public static void WriteMemoryAnyAddress(UIntPtr processHandle, UIntPtr anyAddress, int offset, int value)
    {
        uint bytesWritten = 0;
        uint numberOfBytes = 1;

        if (value < 256)
        { numberOfBytes = 1; }
        else if (value < 65536)
        { numberOfBytes = 2; }
        else if (value < 16777216)
        { numberOfBytes = 3; }
        else if (value <= 2147483647)
        { numberOfBytes = 4; }

        WriteProcessMemory(processHandle, (ulong)(anyAddress + offset), BitConverter.GetBytes(value), numberOfBytes, ref bytesWritten);
    }

    public static int ReadMemoryAnyAddress(UIntPtr processHandle, UIntPtr anyAddress, int offset, int numbBytesToRead)
    {
        uint bytesRead = 0;
        byte[] buffer = new byte[2];
        ReadProcessMemory(processHandle, (ulong)(anyAddress + offset), buffer, (ulong)buffer.Length, ref bytesRead);

        if (numbBytesToRead == 1)
        {
            return buffer[0];
        }
        else if (numbBytesToRead == 2)
        {
            return (buffer[0] + (buffer[1] << 8));
        }
        else
        {
            return buffer[0];
        }
    }

    public static UIntPtr? LoadedScriptAddress(UIntPtr processHandle, List<Tuple<int, UIntPtr>> scriptsFound, int scriptBeingFound)
    {
        uint bytesRead = 0;
        byte[] buffer = new byte[8];
        Tuple<int, UIntPtr>? script = scriptsFound.FirstOrDefault(t => t.Item1 == scriptBeingFound);
        if (script == null)
        {
            return null;
        }

        ReadProcessMemory(processHandle, (ulong)script.Item2 - 32, buffer, (ulong)buffer.Length, ref bytesRead);

        ulong addressValue = BitConverter.ToUInt64(buffer, 0);
        UIntPtr addressPtr = new(addressValue);

        return addressPtr;
    }

    public static string? GetEnumMemberValue<T>(this T value) where T : Enum
    {
        return typeof(T)
            .GetTypeInfo()
            .DeclaredMembers
            .SingleOrDefault(x => x.Name == value.ToString())
            ?.GetCustomAttribute<EnumMemberAttribute>(false)
            ?.Value;
    }

    public static string? ConvertPotNumberToString(int potNumber)
    {
        return ((IxupiPot)potNumber).GetEnumMemberValue();
    }

    public static bool TryParseEnumMember<TEnum>(string? str, out TEnum? result) where TEnum : Enum
    {
        var enumType = typeof(TEnum);
        foreach (var name in Enum.GetNames(enumType))
        {
            var enumMemberAttribute = enumType.GetField(name)?.GetCustomAttribute<EnumMemberAttribute>(false);
            if (enumMemberAttribute == null ||
                enumMemberAttribute.Value?.Equals(str, StringComparison.OrdinalIgnoreCase) != true)
            {
                if (!Enum.TryParse(enumType, str, true, out var parsed))
                    continue;
                result = (TEnum)parsed!;
                return true;
            }

            result = (TEnum)Enum.Parse(enumType, name);
            return true;
        }

        result = default;
        return false;
    }

    public static void AppendTextWithColor(this RichTextBox rtb, string text, Brush color)
    {
        TextRange range = new(rtb.Document.ContentEnd, rtb.Document.ContentEnd)
        {
            Text = text
        };

        range.ApplyPropertyValue(TextElement.ForegroundProperty, color);
    }

    public static void CheckUpgrade()
    {
        if (Settings.Default.upgradeRequired)
        {
            Settings.Default.Upgrade();
            Settings.Default.upgradeRequired = false;
            Settings.Default.Save();
        }
    }
}
