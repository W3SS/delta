﻿using System;
using Microsoft.Xna.Framework;

namespace Delta.UI
{
    public class DebugScreen : Screen
    {
        PerformanceMetrics _performanceMetrics = new PerformanceMetrics();

        public DebugScreen()
            : base()
        {
            _performanceMetrics.AutoSize = true;
            _performanceMetrics.Position = new Vector2(0, 0);
            _performanceMetrics.ForeColor = Color.White;
            _performanceMetrics.BackColor = Color.Gray;
            Add(_performanceMetrics);
            Add(new Controls.Textbox() { BackColor = Color.Gray, Position = new Vector2(50, 50), Size = new Vector2(100, 100), AutoSize = false, VerticalTextAlignment = Controls.VerticalTextAlignment.Center, HorizontalTextAlignment = Controls.HorizontalTextAlignment.Center});
        }
    }
}
