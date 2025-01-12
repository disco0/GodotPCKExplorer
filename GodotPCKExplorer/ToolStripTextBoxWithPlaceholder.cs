﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// https://stackoverflow.com/a/50920121/8980874

[ToolboxBitmap(typeof(ToolStripTextBox))]
public class ToolStripTextBoxWithPlaceholder : ToolStripTextBox
{
    private const int EM_SETCUEBANNER = 0x1501;
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

    public ToolStripTextBoxWithPlaceholder()
    {
        this.Control.HandleCreated += Control_HandleCreated;
    }

    private void Control_HandleCreated(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(cueBanner))
            UpdateCueBanner();
    }

    string cueBanner;
    public string CueBanner
    {
        get { return cueBanner; }
        set
        {
            cueBanner = value;
            UpdateCueBanner();
        }
    }

    private void UpdateCueBanner()
    {
        if (!GodotPCKExplorer.Utils.IsRunningOnMono())
            SendMessage(this.Control.Handle, EM_SETCUEBANNER, 0, cueBanner);
    }
}