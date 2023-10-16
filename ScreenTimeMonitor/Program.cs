using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

#region Win32 API
const int WH_CALLWNDPROC = 4;
const int WM_KEYDOWN = 0x0100;

[DllImport("user32.dll")]
static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

[DllImport("user32.dll")]
static extern bool UnhookWindowsHookEx(IntPtr hhk);

[DllImport("user32.dll")] 
static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

[DllImport("kernel32.dll")]
static extern uint GetCurrentThreadId();



[DllImport("user32.dll")]
static extern IntPtr GetForegroundWindow();

[DllImport("user32.dll")]
static extern int GetWindowTextLength(IntPtr hWnd);

[DllImport("user32.dll")]
static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxLength);

[DllImport("user32.dll")]
static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

[DllImport("kernel32.dll")]
static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, uint processId);

[DllImport("psapi.dll")]
static extern uint GetProcessImageFileName(IntPtr hProcess, StringBuilder fileName, int size);
#endregion



var lastHandle = nint.Zero;

Directory.CreateDirectory("./outputFiles");
await using var file = File.Open($"./outputFiles/output{DateTime.Now:yyyy-M-dd--HH-mm-ss}.json", FileMode.Create,
    FileAccess.ReadWrite);
if (file.ReadByte() != '[')
{
    file.WriteByte(Convert.ToByte('['));
}




while (true)
{
    var currHandle = GetForegroundWindow();

    if (currHandle != lastHandle)
    {
        lastHandle = currHandle;

        var (processName, windowName) = GetProcessNameAndWindowName(currHandle);


        
        if (file.Position > 1)
            file.WriteByte(Convert.ToByte(','));
       
        await JsonSerializer.SerializeAsync(file,
            new WindowInfo(windowName, processName, DateTime.Now), typeof(WindowInfo), JsonContext.Default);
        Console.WriteLine($"{processName} \t {windowName}");
        file.WriteByte(Convert.ToByte(']'));
        file.Position--;
    }

    Thread.Sleep(500);
}


(string processName, string windowName) GetProcessNameAndWindowName(IntPtr currHandle)
{
    var length = GetWindowTextLength(currHandle);
    var builder = new StringBuilder(length + 1);
    GetWindowText(currHandle, builder, length + 1);
    var windowName = builder.ToString();

    uint procId;
    GetWindowThreadProcessId(currHandle, out procId);

    var hProc = OpenProcess(0x400, false, procId); // PROCESS_QUERY_INFORMATION | PROCESS_VM_READ

    var procBuilder = new StringBuilder(1024);
    GetProcessImageFileName(hProc, procBuilder, 1024);
    var processName = System.IO.Path.GetFileName(procBuilder.ToString());
    return (processName, windowName);
}

delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

class WindowInfo
{
    public WindowInfo(string windowName, string processName, DateTime timeStamp)
    {
        this.WindowName = windowName;
        this.ProcessName = processName;
        this.TimeStamp = timeStamp;
    }

    public string WindowName { get; init; }
    public string ProcessName { get; init; }
    public DateTime TimeStamp { get; init; }

    
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(WindowInfo))]
sealed partial class JsonContext : JsonSerializerContext
{
}