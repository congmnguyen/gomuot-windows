using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GoMuot.Core;

namespace GoMuot.Views;

public sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = $"Giới thiệu {AppMetadata.Name}";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(340, 400);
        BackColor = Color.FromArgb(246, 246, 241);
        Font = new Font("Segoe UI", 9F);
        Icon = IconHelper.CreateWindowIcon(32);

        BuildUi();
    }

    private void BuildUi()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24),
            BackColor = BackColor,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = BackColor,
            Padding = new Padding(0, 12, 0, 0),
        };

        var title = new Label
        {
            Text = AppMetadata.Name,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.FromArgb(21, 21, 21),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 4),
        };

        var subtitle = new Label
        {
            Text = "(Gõ Mượt)",
            Font = new Font("Segoe UI", 11F),
            AutoSize = true,
            ForeColor = Color.FromArgb(91, 91, 87),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 4),
        };

        var version = new Label
        {
            Text = $"Version {AppMetadata.Version}",
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            ForeColor = Color.FromArgb(156, 163, 175),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 18),
        };

        var tagline = new Label
        {
            Text = "Bộ gõ tiếng Việt hiệu suất cao",
            Font = new Font("Segoe UI", 10F),
            AutoSize = true,
            ForeColor = Color.FromArgb(21, 21, 21),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 18),
        };

        var author = new Label
        {
            Text = AppMetadata.Author,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.FromArgb(21, 21, 21),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 6),
        };

        content.Controls.AddRange(new Control[] { title, subtitle, version, tagline, author });

        if (!string.IsNullOrWhiteSpace(AppMetadata.AuthorEmail))
        {
            content.Controls.Add(CreateLink(AppMetadata.AuthorEmail, $"mailto:{AppMetadata.AuthorEmail}", 4));
        }

        if (!string.IsNullOrWhiteSpace(AppMetadata.AuthorLinkedin))
        {
            content.Controls.Add(CreateLink("LinkedIn", AppMetadata.AuthorLinkedin, 4));
        }

        content.Controls.Add(CreateLink("Website", AppMetadata.Website, 16));
        content.Controls.Add(CreateLink("GitHub", AppMetadata.Repository, 4));

        var copyright = new Label
        {
            Text = AppMetadata.Copyright,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(156, 163, 175),
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0),
        };

        var closeButton = new Button
        {
            Text = "Đóng",
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 18, 0, 0),
        };
        closeButton.Click += (_, _) => Close();

        layout.Controls.Add(content, 0, 0);
        layout.Controls.Add(copyright, 0, 1);
        layout.Controls.Add(closeButton, 0, 2);

        Controls.Add(layout);
    }

    private static LinkLabel CreateLink(string text, string url, int topMargin)
    {
        var link = new LinkLabel
        {
            Text = text,
            AutoSize = true,
            LinkColor = Color.FromArgb(21, 21, 21),
            ActiveLinkColor = Color.FromArgb(34, 34, 34),
            VisitedLinkColor = Color.FromArgb(91, 91, 87),
            Margin = new Padding(0, topMargin, 0, 0),
        };
        link.LinkClicked += (_, _) => OpenUrl(url);
        return link;
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }
}
