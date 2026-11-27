# Windows 进程内存读取示例

本解决方案包含两个 .NET 10 控制台项目：`MemoryReader` 从指定进程的已知地址读取数据，`MemoryDemo` 提供可供读取的示例数据。仅对自己拥有或获授权访问的进程使用。需要 64 位 Windows 和 .NET 10 SDK。

## 运行演示

在当前目录打开两个终端。第一个终端运行：

```powershell
dotnet run --project MemoryDemo
```

保持演示进程运行，在第二个终端执行它输出的命令。例如：

```powershell
dotnet run --project MemoryReader -- 1234 0x12345678 int32
dotnet run --project MemoryReader -- 1234 0x87654321 utf8 23
```

实际的进程 ID、地址及字符串字节数以演示程序输出为准。它们会随进程重启而变化；按 Enter 退出演示进程后，地址不再有效。

### 在 Visual Studio 中运行

在解决方案资源管理器中右键 `MemoryDemo`，选择“调试”并启动新实例，保持其窗口运行。然后右键 `MemoryReader` 并启动新实例。读取器不带参数启动时会提示输入：将演示窗口显示的进程 ID、`int32` 地址和类型 `int32` 依次输入，即可看到 `123456`。读取字符串时，输入 UTF-8 地址、类型 `utf8` 和演示窗口显示的字节数。读取完成后按 Enter 退出。

也可以在终端运行 `dotnet run --project MemoryReader`，按照同样的提示输入。

## 读取其他程序

```text
MemoryReader <进程ID> <地址> <int32|int64|float|double|bytes|utf8> [字节数]
```

地址可用十进制或 `0x` 前缀的十六进制表示。`bytes` 和 `utf8` 必须指定读取的字节数，范围为 1–4096；其他类型的大小由类型决定。`bytes` 输出十六进制；`utf8` 在读取的范围内遇到零字节时截断。数值类型按 Windows 的小端字节序解码。读取结果输出到标准输出，错误输出到标准错误并返回非零退出码。

读取器只申请 `PROCESS_VM_READ` 权限，不修改目标内存。目标进程和内存地址需要自行确定；本示例不搜索内存、不解析模块基址或多级指针。目标进程必须仍在运行，指定的整个地址范围必须可读，且当前用户必须有权访问；系统保护进程或更高权限的进程可能拒绝访问。读取器以 x64 运行，可以表示 32 位或 64 位目标进程的地址。程序重启、ASLR 或内存释放都可能使之前的地址失效。
