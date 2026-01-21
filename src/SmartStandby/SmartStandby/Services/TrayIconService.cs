using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace SmartStandby.Services;

/// <summary>
/// A lightweight Taskbar Icon implementation using P/Invoke (Shell_NotifyIcon).
/// This avoids dependency on external libraries which caused build issues.
/// </summary>
public class TrayIconService : IDisposable
{
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint WM_USER = 0x0400;
    private const uint WM_TRAYICON = WM_USER + 1;

    // Structs for Shell_NotifyIcon
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, [In] ref NOTIFYICONDATA lpdata);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    private readonly IntPtr _hwnd;
    private readonly uint _id = 1001;
    private bool _isDisposed;

    public TrayIconService(IntPtr windowHandle)
    {
        _hwnd = windowHandle;
        InitializeIcon();
    }

    private void InitializeIcon()
    {
        var nid = new NOTIFYICONDATA();
        nid.cbSize = Marshal.SizeOf(nid);
        nid.hWnd = _hwnd;
        nid.uID = _id;
        nid.uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE;
        nid.uCallbackMessage = WM_TRAYICON;
        nid.szTip = "Smart Standby";

        // Load standard application icon (or custom .ico if available)
        // For MVP, we use the Application Icon (IDI_APPLICATION = 32512) or try to load from file
        // Here we just use a system default generic icon as placeholder if we can't load asset
        nid.hIcon = LoadImage(IntPtr.Zero, "#32512", 1, 16, 16, 0x00008000); // IDI_APPLICATION

        Shell_NotifyIcon(NIM_ADD, ref nid);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        var nid = new NOTIFYICONDATA();
        nid.cbSize = Marshal.SizeOf(nid);
        nid.hWnd = _hwnd;
        nid.uID = _id;
        Shell_NotifyIcon(NIM_DELETE, ref nid);
        
        _isDisposed = true;
    }
}
