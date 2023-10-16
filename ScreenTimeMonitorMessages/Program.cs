using System;
using System.Runtime.InteropServices;

class Program
{
    const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
    const uint WINEVENT_OUTOFCONTEXT = 0;

    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    static void Main(string[] args)
    {
        WinEventDelegate callback = new WinEventDelegate(WindowChanged);
        IntPtr hook = SetWinEventHook(EVENT_OBJECT_NAMECHANGE, EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, callback, 0, 0, WINEVENT_OUTOFCONTEXT);

        if (hook == IntPtr.Zero)
        {
            Console.WriteLine("Failed to set the hook.");
            return;
        }

        Console.WriteLine("Listening for window change events. Press Enter to exit.");
        Console.ReadLine();

        UnhookWinEvent(hook);
    }

    static void WindowChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (eventType == EVENT_OBJECT_NAMECHANGE && hwnd != IntPtr.Zero)
        {
            System.Text.StringBuilder windowTitle = new System.Text.StringBuilder(256);
            GetWindowText(hwnd, windowTitle, windowTitle.Capacity);
            Console.WriteLine("Window changed: " + windowTitle.ToString());
        }
    }
}
