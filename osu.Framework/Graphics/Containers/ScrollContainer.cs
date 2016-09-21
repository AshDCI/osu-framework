﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Containers
{
    public class ScrollContainer : LargeContainer
    {
        public bool ScrollDraggerOnLeft;

        private AutoSizeContainer content = new AutoSizeContainer();
        private ScrollBar scrollbar;

        /// <summary>
        /// Vertical size of available content (content.Size)
        /// </summary>
        private float availableContent;

        private float displayableContent => ActualSize.Y;

        private float current;

        private float currentClamped => MathHelper.Clamp(current, 0, availableContent - displayableContent);

        protected override Container AddTarget => content;

        public override void Load()
        {
            base.Load();

            AddTopLevel(scrollbar = new ScrollBar(offset)
            {
                Anchor = ScrollDraggerOnLeft ? Anchor.TopLeft : Anchor.TopRight,
                Origin = ScrollDraggerOnLeft ? Anchor.TopLeft : Anchor.TopRight
            });

            Add(content);

            content.OnAutoSize += contentAutoSize;
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if (!base.Invalidate(invalidation, source, shallPropagate))
                return false;

            if ((invalidation & Invalidation.SizeInParentSpace) > 0)
                updateSize();

            return true;
        }

        private void contentAutoSize()
        {
            if (availableContent == content.Size.Y)
                return;

            availableContent = content.Size.Y;
            updateSize();
            offset(0);

            scrollbar.Alpha = availableContent > displayableContent ? 1 : 0;
        }

        protected override bool OnDragStart(InputState state) => true;

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            offset(0, false, false);
            return base.OnMouseDown(state, args);
        }

        protected override bool OnDrag(InputState state)
        {
            offset(-state.Mouse.Delta.Y / Scale.Y, false, false);
            return base.OnDrag(state);
        }

        protected override bool OnDragEnd(InputState state)
        {
            //forces a clamped state to return to correct location.
            offset(-state.Mouse.Delta.Y / Scale.Y * 10);

            return base.OnDragEnd(state);
        }

        protected override bool OnWheelDown(InputState state)
        {
            offset(80);
            return base.OnWheelDown(state);
        }

        protected override bool OnWheelUp(InputState state)
        {
            offset(-80);
            return base.OnWheelUp(state);
        }

        private void offset(float value, bool clamp = true, bool animated = true)
        {
            scrollTo(current + value, clamp, animated);
        }

        private void scrollTo(float value, bool clamp = true, bool animated = true)
        {
            current = value;

            if (clamp && current != currentClamped)
            {
                updateScroll(false);
                current = currentClamped;
            }

            updateScroll(animated);
        }

        private void updateSize()
        {
            scrollbar.ScaleTo(new Vector2(1, Math.Min(1, displayableContent / availableContent)), 200, EasingTypes.OutExpo);
        }

        private void updateScroll(bool animated = true)
        {
            float adjusted = (current + currentClamped) / 2;

            scrollbar?.MoveToY(adjusted * scrollbar.Scale.Y, animated ? 800 : 0, EasingTypes.OutExpo);
            content.MoveToY(-adjusted, animated ? 800 : 0, EasingTypes.OutExpo);
        }

        private class ScrollBar : Container
        {
            private readonly Action<float, bool, bool> offsetDelegate;

            private Color4 hoverColour = Color4.White;
            private Color4 defaultColour = Color4.LightGray;
            private Color4 highlightColour = Color4.GreenYellow;
            private Box box;

            public ScrollBar(Action<float, bool, bool> offsetDelegate)
            {
                this.offsetDelegate = offsetDelegate;
            }

            public override void Load()
            {
                base.Load();

                Add(box = new Box
                {
                    SizeMode = InheritMode.XY
                });

                Anchor = Anchor.TopRight;
                Origin = Anchor.TopRight;
                SizeMode = InheritMode.Y;
                Size = new Vector2(10, 1);
                Colour = defaultColour;
            }

            protected override bool OnHover(InputState state)
            {
                FadeColour(hoverColour, 100);
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                FadeColour(defaultColour, 100);
            }

            protected override bool OnDragStart(InputState state) => true;

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                //note that we are changing the colour of the box here as to not interfere with the hover effect.
                box.FadeColour(highlightColour, 100);
                return true;
            }

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
                box.FadeColour(Color4.White, 100);

                return base.OnMouseUp(state, args);
            }

            protected override bool OnDrag(InputState state)
            {
                offsetDelegate(state.Mouse.Delta.Y, true, false);
                return true;
            }
        }
    }
}
