# Test harness: focus the game window, send input, capture the screen.
param([string]$Shot, [string]$Click, [int[]]$Keys, [int]$SettleMs = 800)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, IntPtr extra);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint type);
}
"@

$proc = Get-Process 'How to Fish' -ErrorAction Stop | Select-Object -First 1
[Win]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 900

# Insert/Delete/Home/End/PageUp/PageDown/arrows are "extended" keys: without
# KEYEVENTF_EXTENDEDKEY (0x0001) they inject as their numpad twins and the game never
# sees the key you asked for. Release happens after the capture so the press is visible.
$extended = @(0x2D, 0x2E, 0x24, 0x23, 0x21, 0x22, 0x25, 0x26, 0x27, 0x28, 0xA3, 0xA5)
foreach ($vk in $Keys) {
    $scan = [byte][Win]::MapVirtualKey([uint32]$vk, 0)
    $flags = if ($extended -contains $vk) { 1 } else { 0 }
    [Win]::keybd_event([byte]$vk, $scan, $flags, [IntPtr]::Zero)
    "key down 0x{0:X} extended={1}" -f $vk, ($flags -eq 1)
    Start-Sleep -Milliseconds $SettleMs
}
if ($Click) {
    $xy = $Click.Split(',')
    [Win]::SetCursorPos([int]$xy[0], [int]$xy[1])
    Start-Sleep -Milliseconds 250
    [Win]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)   # left down
    Start-Sleep -Milliseconds 80
    [Win]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)   # left up
    "clicked $Click"
    Start-Sleep -Milliseconds $SettleMs
}


if ($Shot) {
    Start-Sleep -Milliseconds 400
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.X, $b.Y, 0, 0, $bmp.Size)
    $bmp.Save($Shot, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    "shot $Shot ($($b.Width)x$($b.Height))"
}

foreach ($vk in $Keys) {
    $scan = [byte][Win]::MapVirtualKey([uint32]$vk, 0)
    $flags = if ($extended -contains $vk) { 3 } else { 2 }   # KEYUP (+EXTENDED)
    [Win]::keybd_event([byte]$vk, $scan, $flags, [IntPtr]::Zero)
}
