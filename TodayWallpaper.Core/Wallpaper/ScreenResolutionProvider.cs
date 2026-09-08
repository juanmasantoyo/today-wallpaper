using System.Runtime.InteropServices;

namespace TodayWallpaper.Core.Wallpaper;

/// <summary>
/// Returns the primary monitor physical dimensions.
/// Uses <c>EnumDisplaySettings</c> to retrieve native physical pixel resolution
/// regardless of Windows DPI scaling / virtualization, falling back to <c>GetDeviceCaps</c> and <c>GetSystemMetrics</c>.
/// Safe to call from console/Worker and UI processes.
/// </summary>
public sealed class ScreenResolutionProvider : IScreenResolutionProvider
{
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int DESKTOPVERTRES = 117;
    private const int DESKTOPHORZRES = 118;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public short dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDpiVersion;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    /// <inheritdoc/>
    public int GetWidth()
    {
        var (width, _) = GetPhysicalResolution();
        return width > 0 ? width : Math.Max(1920, GetSystemMetrics(SM_CXSCREEN));
    }

    /// <inheritdoc/>
    public int GetHeight()
    {
        var (_, height) = GetPhysicalResolution();
        return height > 0 ? height : Math.Max(1080, GetSystemMetrics(SM_CYSCREEN));
    }

    private static (int width, int height) GetPhysicalResolution()
    {
        try
        {
            var dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm) && dm.dmPelsWidth > 0 && dm.dmPelsHeight > 0)
            {
                return (dm.dmPelsWidth, dm.dmPelsHeight);
            }
        }
        catch
        {
            // Fall through to GetDeviceCaps
        }

        try
        {
            var hdc = GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                try
                {
                    int w = GetDeviceCaps(hdc, DESKTOPHORZRES);
                    int h = GetDeviceCaps(hdc, DESKTOPVERTRES);
                    if (w > 0 && h > 0)
                    {
                        return (w, h);
                    }
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdc);
                }
            }
        }
        catch
        {
            // Fall through
        }

        return (0, 0);
    }
}
