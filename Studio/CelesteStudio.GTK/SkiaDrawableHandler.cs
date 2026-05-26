using Cairo;
using CelesteStudio.Controls;
using CelesteStudio.Util;
using Eto.Forms;
using Eto.GtkSharp;
using Eto.GtkSharp.Forms;
using SkiaSharp;
using System;

namespace CelesteStudio.GTK;

public class SkiaDrawableHandler : GtkPanel<Gtk.EventBox, SkiaDrawable, Control.ICallback>, SkiaDrawable.IHandler {
    private Gtk.Box content = null!;

    protected override WeakConnector CreateConnector() => new SkiaDrawableConnector();
    protected new SkiaDrawableConnector Connector => (SkiaDrawableConnector)base.Connector;

    public void Create() {
        Control = new Gtk.EventBox();
        Control.Events |= Gdk.EventMask.ExposureMask;
        Control.CanFocus = false;
        Control.CanDefault = true;
        Control.Events |= Gdk.EventMask.ButtonPressMask;

        content = new Gtk.Box(Gtk.Orientation.Vertical, 0);
        Control.Add(content);
    }

    protected override void Initialize() {
        base.Initialize();
        Control.Drawn += Connector.HandleDrawn;
        Control.ButtonPressEvent += Connector.HandleDrawableButtonPressEvent;
        Control.AddEvents((int)Gdk.EventMask.KeyPressMask);
        Control.KeyPressEvent += Connector.HandleDrawableKeyPressEvent;
    }

    protected class SkiaDrawableConnector : GtkPanelEventConnector {
        private new SkiaDrawableHandler? Handler => (SkiaDrawableHandler)base.Handler;

        private SKBitmap? bitmap;
        private SKSurface? surface;
        private ImageSurface? imageSurface;
        private int scaleFactor;

        public void HandleDrawableButtonPressEvent(object o, Gtk.ButtonPressEventArgs args) {
            var handler = Handler;
            if (handler == null) {
                return;
            }

            if (handler.CanFocus) {
                handler.Control.GrabFocus();
            }
        }

        [GLib.ConnectBefore]
        public void HandleDrawableKeyPressEvent(object o, Gtk.KeyPressEventArgs args) {
            var handler = Handler;
            if (handler == null || !TryGetKeypadText(args.Event, out string text, out Keys key, out Keys suppressKey)) {
                return;
            }

            var keyArgs = new KeyEventArgs(key | args.Event.State.ToEtoKey(), KeyEventType.KeyDown, text[0]);
            handler.Callback.OnKeyDown(handler.Widget, keyArgs);
            handler.Widget.SuppressNextKeyDown = suppressKey;
            if (!keyArgs.Handled) {
                handler.Callback.OnTextInput(handler.Widget, new TextInputEventArgs(text));
            }

            args.RetVal = true;
        }

        private static bool TryGetKeypadText(Gdk.EventKey e, out string text, out Keys key, out Keys suppressKey) {
            text = string.Empty;
            key = Keys.None;
            suppressKey = Keys.None;

            if ((e.State & (Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.SuperMask | Gdk.ModifierType.ShiftMask)) != 0) {
                return false;
            }

            (text, key, suppressKey) = e.Key switch {
                Gdk.Key.KP_0 => ("0", Keys.Keypad0, Keys.Insert),
                Gdk.Key.KP_1 => ("1", Keys.Keypad1, Keys.End),
                Gdk.Key.KP_2 => ("2", Keys.Keypad2, Keys.Down),
                Gdk.Key.KP_3 => ("3", Keys.Keypad3, Keys.PageDown),
                Gdk.Key.KP_4 => ("4", Keys.Keypad4, Keys.Left),
                Gdk.Key.KP_6 => ("6", Keys.Keypad6, Keys.Right),
                Gdk.Key.KP_7 => ("7", Keys.Keypad7, Keys.Home),
                Gdk.Key.KP_8 => ("8", Keys.Keypad8, Keys.Up),
                Gdk.Key.KP_9 => ("9", Keys.Keypad9, Keys.PageUp),
                Gdk.Key.KP_Decimal => (".", Keys.Decimal, Keys.Delete),
                Gdk.Key.KP_Separator => (",", Keys.Decimal, Keys.Delete),
                _ => (string.Empty, Keys.None, Keys.None),
            };

            return key != Keys.None;
        }

        [GLib.ConnectBefore]
        public void HandleDrawn(object o, Gtk.DrawnArgs args) {
            if (Handler == null) {
                return;
            }

            var drawable = Handler.Widget;
            bool preRender = drawable.PreRenderImage;
            int scale = Handler.Control.ScaleFactor;
            if (drawable.CanDraw) {
                int width = drawable.ImageWidth, height = drawable.ImageHeight;
                int pixelWidth = width * scale, pixelHeight = height * scale;
                if (surface == null || imageSurface == null || pixelWidth != bitmap?.Width || pixelHeight != bitmap?.Height || scale != scaleFactor) {
                    var colorType = SKImageInfo.PlatformColorType;

                    bitmap?.Dispose();
                    bitmap = new SKBitmap(pixelWidth, pixelHeight, colorType, SKAlphaType.Premul);
                    IntPtr pixels = bitmap.GetPixels();

                    surface?.Dispose();
                    surface = SKSurface.Create(new SKImageInfo(bitmap.Info.Width, bitmap.Info.Height, colorType, SKAlphaType.Premul), pixels, bitmap.Info.RowBytes);
                    surface.Canvas.Scale(scale);
                    surface.Canvas.Flush();

                    imageSurface?.Dispose();
                    imageSurface = new ImageSurface(pixels, Format.Argb32, bitmap.Width, bitmap.Height, bitmap.Info.RowBytes) {
                        DeviceScale = new PointD(scale, scale)
                    };
                    scaleFactor = scale;

                    if (preRender) {
                        var canvas = surface.Canvas;
                        using (new SKAutoCanvasRestore(canvas, true)) {
                            canvas.Clear(drawable.BackgroundColor.ToSkia());
                            drawable.Draw(surface);
                        }
                    }
                }

                if (!preRender) {
                    var canvas = surface.Canvas;
                    using (new SKAutoCanvasRestore(canvas, true)) {
                        canvas.Clear(drawable.BackgroundColor.ToSkia());
                        canvas.Translate(-drawable.DrawX, -drawable.DrawY);
                        drawable.Draw(surface);
                    }
                }
            } else {
                drawable.Invalidate();
            }

            if (imageSurface != null) {
                if (preRender) {
                    args.Cr.SetSourceSurface(imageSurface, drawable.Padding.Left, drawable.Padding.Top);
                } else {
                    args.Cr.SetSourceSurface(imageSurface, drawable.DrawX + drawable.Padding.Left, drawable.DrawY + drawable.Padding.Top);
                }
                args.Cr.Paint();
            }
        }
    }

    public bool CanFocus {
        get => Control.CanFocus;
        set => Control.CanFocus = value;
    }

    protected override void SetContainerContent(Gtk.Widget containerContent) {
        content.Add(containerContent);
    }

    protected override void SetBackgroundColor(Eto.Drawing.Color? color) {
        // Handled by ourselves
        Invalidate(false);
    }
}
