﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Delta.UI
{
    public abstract class Control : BaseControl
    {
        internal Color CurrentColor { get; set; }

        public Color BackColor { get; set; }
        public Color HighlightedColor { get; set; }
        public Color ClickedColor { get; set; }
        public Color DisabledColor { get; set; }
        public Color FocusedColor { get; set; }
        public Color SelectedColor { get; set; }

        public Control()
            : base()
        {
            BackColor = Color.Black * 0.5f;
            HighlightedColor = Color.Black * 0.75f;
            ClickedColor = Color.Black;
            DisabledColor = Color.Gray * 0.5f;
            SelectedColor = Color.White * 0.75f;
            FocusedColor = Color.DeepSkyBlue * 0.5f;
        }

        public Control(string name)
            : base(name)
        {
        }

        protected internal override void OnInvalidate()
        {
            base.OnInvalidate();
            UpdateColor();
        }

        protected internal virtual void UpdateColor()
        {
            if (IsEnabled)
            {
                if (IsClicked)
                    CurrentColor = ClickedColor;
                else if (IsSelected)
                    CurrentColor = SelectedColor;
                else if (IsFocused)
                    CurrentColor = FocusedColor;
                else if (IsHighlighted)
                    CurrentColor = HighlightedColor;
                else
                    CurrentColor = BackColor;
            }
            else
                CurrentColor = DisabledColor;
        }

#if WINDOWS
        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();
            if (G.Input.Mouse.LeftButton.IsDown)
            {
                if (G.UI.ClickedControl == this)
                {
                    IsClicked = true;
                    Invalidate();
                }
                else
                    return;
            }
            else
            {
                IsHighlighted = true;
                Invalidate();
            }
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            if (G.Input.Mouse.LeftButton.IsDown)
            {
                if (G.UI.ClickedControl == this)
                {
                    IsHighlighted = false;
                    IsClicked = false;
                    Invalidate();
                }
                else
                    return;
            }
            else
            {
                IsHighlighted = false;
                Invalidate();
            }
        }

        protected override void OnMouseDown()
        {
            base.OnMouseDown();
            Invalidate();
        }

        protected override void OnMouseUp()
        {
            base.OnMouseUp();
            Invalidate();
        }
#endif

        protected override void Draw(DeltaTime time, SpriteBatch spriteBatch)
        {
            if (CurrentColor.A > 0)
                spriteBatch.DrawRectangle(AbsolutePosition, RenderSize, CurrentColor);
        }

    }
}
