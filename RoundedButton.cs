using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace TaskbarSearchText;

/// <summary>
/// 带有圆角和鼠标悬停效果的自绘按钮。
/// </summary>
internal sealed class RoundedButton : Button
{
    private bool _isHovered;
    private bool _isPressed;
    private int _cornerRadius = 8;
    private Color _buttonColor = Color.FromArgb(0, 120, 215);
    private Color _hoverColor = Color.FromArgb(16, 110, 190);
    private Color _pressedColor = Color.FromArgb(0, 90, 158);
    private Color _borderColor = Color.FromArgb(0, 90, 158);

    public RoundedButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
    }

    [DefaultValue(8)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Math.Max(0, value);
            Invalidate();
        }
    }

    public Color ButtonColor
    {
        get => _buttonColor;
        set
        {
            _buttonColor = value;
            Invalidate();
        }
    }

    public Color HoverColor
    {
        get => _hoverColor;
        set
        {
            _hoverColor = value;
            Invalidate();
        }
    }

    public Color PressedColor
    {
        get => _pressedColor;
        set
        {
            _pressedColor = value;
            Invalidate();
        }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Color fillColor = Enabled
            ? _isPressed
                ? PressedColor
                : _isHovered
                    ? HoverColor
                    : ButtonColor
            : Color.FromArgb(190, 190, 190);

        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = CreateRoundedRectangle(bounds, CornerRadius);
        using SolidBrush brush = new(fillColor);
        using Pen borderPen = new(BorderColor, 1);

        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(borderPen, path);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            Enabled ? ForeColor : Color.White,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _isPressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        GraphicsPath path = new();
        int diameter = Math.Min(Math.Max(radius * 2, 1), Math.Min(bounds.Width, bounds.Height));

        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        Rectangle arc = new(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);

        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}
