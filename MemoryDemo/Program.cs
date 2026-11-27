using System.Runtime.InteropServices;
using System.Text;

nint numberAddress = nint.Zero;
nint textAddress = nint.Zero;
try
{
    numberAddress = Marshal.AllocHGlobal(sizeof(int));
    Marshal.WriteInt32(numberAddress, 123456);

    byte[] text = Encoding.UTF8.GetBytes("Hello from MemoryDemo!\0");
    textAddress = Marshal.AllocHGlobal(text.Length);
    Marshal.Copy(text, 0, textAddress, text.Length);

    int processId = Environment.ProcessId;
    string numberPointer = $"0x{(nuint)numberAddress:X}";
    string textPointer = $"0x{(nuint)textAddress:X}";
    Console.WriteLine($"进程 ID: {processId}");
    Console.WriteLine($"int32 地址: {numberPointer}，值: 123456");
    Console.WriteLine($"UTF-8 地址: {textPointer}，字节数（含结尾零）: {text.Length}");
    Console.WriteLine("请在另一个终端从解决方案目录运行：");
    Console.WriteLine($"dotnet run --project MemoryReader -- {processId} {numberPointer} int32");
    Console.WriteLine($"dotnet run --project MemoryReader -- {processId} {textPointer} utf8 {text.Length}");
    Console.WriteLine("按 Enter 退出演示进程。");
    Console.ReadLine();
}
finally
{
    Marshal.FreeHGlobal(textAddress);
    Marshal.FreeHGlobal(numberAddress);
}
