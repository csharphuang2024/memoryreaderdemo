using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

return args.Length == 0 ? RunInteractive() : Run(args);

static int RunInteractive()
{
    Console.WriteLine("请先启动 MemoryDemo 或其他目标程序，并获取其进程 ID 和内存地址。");
    string? processId = Prompt("进程 ID: ");
    string? address = Prompt("内存地址（十进制或 0x 开头的十六进制）: ");
    string? type = Prompt("数据类型（int32、int64、float、double、bytes、utf8）: ");
    if (processId is null || address is null || type is null)
    {
        Console.Error.WriteLine("输入已结束，无法读取完整参数。");
        return 1;
    }

    string[] arguments;
    if (type.Equals("bytes", StringComparison.OrdinalIgnoreCase) || type.Equals("utf8", StringComparison.OrdinalIgnoreCase))
    {
        string? size = Prompt("读取字节数（1-4096）: ");
        if (size is null)
        {
            Console.Error.WriteLine("输入已结束，缺少读取字节数。");
            return 1;
        }
        arguments = [processId, address, type, size];
    }
    else
    {
        arguments = [processId, address, type];
    }

    int result = Run(arguments);
    if (!Console.IsInputRedirected)
    {
        Console.WriteLine("按 Enter 退出。");
        Console.ReadLine();
    }
    return result;
}

static string? Prompt(string message)
{
    Console.Write(message);
    return Console.ReadLine()?.Trim();
}

static int Run(string[] args)
{
    if (args.Length < 3 || args.Length > 4 ||
        !int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out int processId) || processId <= 0 ||
        !TryParseAddress(args[1], out ulong address) || address == 0)
    {
        PrintUsage();
        return 1;
    }

    string type = args[2].ToLowerInvariant();
    int size;
    switch (type)
    {
        case "int32":
        case "float":
            size = 4;
            break;
        case "int64":
        case "double":
            size = 8;
            break;
        case "bytes":
        case "utf8":
            if (args.Length != 4 || !int.TryParse(args[3], NumberStyles.None, CultureInfo.InvariantCulture, out size) || size < 1 || size > 4096)
            {
                Console.Error.WriteLine("bytes 和 utf8 类型需要指定 1 到 4096 的读取字节数。");
                PrintUsage();
                return 1;
            }
            break;
        default:
            Console.Error.WriteLine($"不支持的数据类型: {args[2]}");
            PrintUsage();
            return 1;
    }

    if (args.Length == 4 && type != "bytes" && type != "utf8")
    {
        Console.Error.WriteLine("只有 bytes 和 utf8 类型需要指定读取字节数。");
        return 1;
    }

    using SafeProcessHandle process = NativeMethods.OpenProcess(NativeMethods.ProcessVmRead, false, processId);
    if (process.IsInvalid)
    {
        var error = new Win32Exception(Marshal.GetLastPInvokeError());
        Console.Error.WriteLine($"无法打开进程 {processId}: {error.Message}");
        return 1;
    }

    byte[] buffer = new byte[size];
    if (!NativeMethods.ReadProcessMemory(process, unchecked((nint)address), buffer, (nuint)buffer.Length, out nuint bytesRead))
    {
        var error = new Win32Exception(Marshal.GetLastPInvokeError());
        Console.Error.WriteLine($"读取地址 0x{address:X} 失败: {error.Message}");
        return 1;
    }

    if (bytesRead != (nuint)buffer.Length)
    {
        Console.Error.WriteLine($"读取不完整: 期望 {buffer.Length} 字节，实际读取 {bytesRead} 字节。");
        return 1;
    }

    string value = type switch
    {
        "int32" => BinaryPrimitives.ReadInt32LittleEndian(buffer).ToString(CultureInfo.InvariantCulture),
        "int64" => BinaryPrimitives.ReadInt64LittleEndian(buffer).ToString(CultureInfo.InvariantCulture),
        "float" => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(buffer)).ToString("R", CultureInfo.InvariantCulture),
        "double" => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(buffer)).ToString("R", CultureInfo.InvariantCulture),
        "bytes" => Convert.ToHexString(buffer),
        "utf8" => Encoding.UTF8.GetString(buffer.AsSpan(0, Array.IndexOf(buffer, (byte)0) is int end && end >= 0 ? end : buffer.Length)),
        _ => throw new InvalidOperationException("未知的数据类型。")
    };
    Console.WriteLine(value);
    return 0;
}

static bool TryParseAddress(string text, out ulong address)
{
    if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    {
        return ulong.TryParse(text.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address);
    }

    return ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out address);
}

static void PrintUsage()
{
    Console.Error.WriteLine("用法: MemoryReader <进程ID> <地址(十进制或0x十六进制)> <int32|int64|float|double|bytes|utf8> [字节数]");
    Console.Error.WriteLine("bytes 和 utf8 必须指定读取的字节数（1-4096）。");
}

internal static class NativeMethods
{
    internal const uint ProcessVmRead = 0x0010;

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern SafeProcessHandle OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReadProcessMemory(SafeProcessHandle process, nint baseAddress, [Out] byte[] buffer, nuint size, out nuint bytesRead);
}
