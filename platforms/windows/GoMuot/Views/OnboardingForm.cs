using System;
using System.Drawing;
using System.Windows.Forms;
using GoMuot.Core;
using GoMuot.Services;

namespace GoMuot.Views;

public sealed class OnboardingForm : Form
{
    private readonly SettingsService _settings;
    private readonly Panel[] _pages;
    private readonly Panel[] _dots;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly Button _finishButton;
    private int _pageIndex;

    public OnboardingForm(SettingsService settings)
    {
        _settings = settings;

        Text = "Gõ Mượt";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 540);
        BackColor = Color.FromArgb(246, 246, 241);
        Font = new Font("Segoe UI", 9F);
        Icon = IconHelper.CreateWindowIcon(32);

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = BackColor,
        };

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(252, 252, 248),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var pageHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(44),
            BackColor = card.BackColor,
        };

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            BackColor = card.BackColor,
        };

        _pages = new[]
        {
            BuildWelcomePage(),
            BuildSimpleTelexPage(),
            BuildReadyPage(),
        };

        foreach (var page in _pages)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            pageHost.Controls.Add(page);
        }

        var dotsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 20,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = card.BackColor,
            Padding = new Padding(0),
            Margin = new Padding(0),
        };

        _dots = new[]
        {
            CreateDot(),
            CreateDot(),
            CreateDot(),
        };

        foreach (var dot in _dots)
        {
            dotsRow.Controls.Add(dot);
        }

        _previousButton = CreateActionButton("[ quay lại ]");
        _previousButton.Click += (_, _) => ChangePage(-1);

        _nextButton = CreateActionButton("[ tiếp theo ]");
        _nextButton.Click += (_, _) => ChangePage(1);

        _finishButton = CreateActionButton("[ bắt đầu gõ tiếng Việt ]");
        _finishButton.AutoSize = true;
        _finishButton.Click += Finish_Click;

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = card.BackColor,
            Padding = new Padding(44, 12, 44, 12),
        };

        buttonRow.Controls.Add(_finishButton);
        buttonRow.Controls.Add(_nextButton);
        buttonRow.Controls.Add(_previousButton);

        footer.Controls.Add(buttonRow);
        footer.Controls.Add(dotsRow);

        card.Controls.Add(pageHost);
        card.Controls.Add(footer);
        contentHost.Controls.Add(card);
        Controls.Add(contentHost);

        UpdatePage();
    }

    private Panel BuildWelcomePage()
    {
        var panel = CreatePagePanel();

        var title = CreateLabel("gõ mượt", 28F, FontStyle.Bold, "Consolas", Color.FromArgb(21, 21, 21), 0, 26);
        var subtitle = CreateLabel("Simple Telex cho Windows", 13F, FontStyle.Regular, "Consolas", Color.FromArgb(91, 91, 87), 0, 18);
        var summary = CreateInfoBox(
            "Một bộ gõ gọn, ít tuỳ chọn, vào việc ngay.\r\n" +
            "Không đổi mode liên tục. Không menu rối.\r\n" +
            "Bật lên, gõ `dd aw ow uw`, xong.");

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        panel.Controls.Add(summary);
        return panel;
    }

    private Panel BuildSimpleTelexPage()
    {
        var panel = CreatePagePanel();

        var title = CreateLabel("Simple Telex", 22F, FontStyle.Bold, "Consolas", Color.FromArgb(21, 21, 21), 0, 12);
        var subtitle = CreateLabel("Một mode duy nhất. Không cần nghĩ nhiều.", 12F, FontStyle.Regular, "Consolas", Color.FromArgb(91, 91, 87), 0, 18);
        var mapping = CreateInfoBox(
            "Simple Telex\r\n" +
            "dd -> đ   aw -> ă   aa -> â   ow -> ơ   uw -> ư\r\n" +
            "w đứng riêng sẽ giữ nguyên là w.");
        var hotkey = CreateInfoBox("Hotkey: Ctrl + Space để bật hoặc tắt bộ gõ.");

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        panel.Controls.Add(mapping);
        panel.Controls.Add(hotkey);
        return panel;
    }

    private Panel BuildReadyPage()
    {
        var panel = CreatePagePanel();

        var title = CreateLabel("ready", 28F, FontStyle.Bold, "Consolas", Color.FromArgb(21, 21, 21), 0, 24);
        var details = CreateInfoBox(
            "GoMuot đang chạy trong system tray.\r\n" +
            "right click: bật/tắt, about, feedback\r\n" +
            "double click hoặc Ctrl + Space: toggle nhanh");
        var note = CreateInfoBox("Mẹo: Win + Space vẫn để Windows tự xử lý đổi input source.");

        panel.Controls.Add(title);
        panel.Controls.Add(details);
        panel.Controls.Add(note);
        return panel;
    }

    private FlowLayoutPanel CreatePagePanel()
    {
        return new FlowLayoutPanel
        {
            BackColor = Color.FromArgb(252, 252, 248),
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
    }

    private static Label CreateLabel(
        string text,
        float size,
        FontStyle style,
        string fontFamily,
        Color color,
        int top,
        int bottom)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(380, 0),
            Font = new Font(fontFamily, size, style),
            ForeColor = color,
            Margin = new Padding(0, top, 0, bottom),
        };
    }

    private static Panel CreateInfoBox(string text)
    {
        var box = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(246, 246, 241),
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(16),
        };

        var label = new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(340, 0),
            Font = new Font("Consolas", 11F),
            ForeColor = Color.FromArgb(21, 21, 21),
        };

        box.Controls.Add(label);
        return box;
    }

    private static Panel CreateDot()
    {
        return new Panel
        {
            Width = 8,
            Height = 8,
            Margin = new Padding(4, 4, 4, 4),
            BackColor = Color.FromArgb(212, 212, 204),
        };
    }

    private static Button CreateActionButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Standard,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(8, 0, 0, 0),
        };
    }

    private void ChangePage(int delta)
    {
        int next = Math.Clamp(_pageIndex + delta, 0, _pages.Length - 1);
        if (next == _pageIndex)
        {
            return;
        }

        _pageIndex = next;
        UpdatePage();
    }

    private void UpdatePage()
    {
        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].Visible = i == _pageIndex;
            _dots[i].BackColor = i == _pageIndex
                ? Color.FromArgb(21, 21, 21)
                : Color.FromArgb(212, 212, 204);
        }

        _previousButton.Visible = _pageIndex > 0;
        _nextButton.Visible = _pageIndex < _pages.Length - 1;
        _finishButton.Visible = _pageIndex == _pages.Length - 1;
    }

    private void Finish_Click(object? sender, EventArgs e)
    {
        _settings.Save();
        DialogResult = DialogResult.OK;
        Close();
    }
}
