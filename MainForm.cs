namespace TaskbarSearchText;

/// <summary>
/// Windows 10 任务栏搜索框文字设置窗口。
/// </summary>
internal sealed class MainForm : Form
{
    private readonly TextBox _searchTextBox;
    private readonly RoundedButton _applyButton;
    private readonly RoundedButton _restoreButton;
    private readonly LinkLabel _historyLinkLabel;
    private readonly Label _currentStatusLabel;

    public MainForm()
    {
        Text = "Windows 10 Search Text Changer";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 285);
        MinimumSize = new Size(536, 324);
        MaximumSize = new Size(536, 324);
        BackColor = Color.White;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        Panel headerPanel = CreateHeaderPanel();
        Label inputLabel = CreateInputLabel();
        _historyLinkLabel = CreateHistoryLinkLabel();
        _searchTextBox = CreateSearchTextBox();
        _currentStatusLabel = CreateCurrentStatusLabel();
        _applyButton = CreateActionButton("应用", 232, true);
        _restoreButton = CreateActionButton("恢复默认", 354, false);

        _applyButton.Click += ApplyButton_Click;
        _restoreButton.Click += RestoreButton_Click;
        _historyLinkLabel.LinkClicked += HistoryLinkLabel_LinkClicked;

        Controls.Add(headerPanel);
        Controls.Add(inputLabel);
        Controls.Add(_historyLinkLabel);
        Controls.Add(_searchTextBox);
        Controls.Add(_currentStatusLabel);
        Controls.Add(_applyButton);
        Controls.Add(_restoreButton);

        LoadCurrentSearchText();
        AcceptButton = _applyButton;
    }

    /// <summary>
    /// 创建顶部蓝色标题栏。
    /// </summary>
    private static Panel CreateHeaderPanel()
    {
        Panel panel = new()
        {
            BackColor = Color.FromArgb(0, 120, 215),
            Dock = DockStyle.Top,
            Height = 74
        };

        Label titleLabel = new()
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.White,
            Location = new Point(28, 20),
            Text = "搜索框文字设置"
        };

        panel.Controls.Add(titleLabel);
        return panel;
    }

    /// <summary>
    /// 创建输入框上方的说明标签。
    /// </summary>
    private static Label CreateInputLabel()
    {
        return new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(45, 55, 70),
            Location = new Point(30, 103),
            Text = "搜索框文字"
        };
    }

    /// <summary>
    /// 创建用于打开历史记录菜单的链接标签。
    /// </summary>
    private static LinkLabel CreateHistoryLinkLabel()
    {
        return new LinkLabel
        {
            ActiveLinkColor = Color.FromArgb(0, 90, 158),
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            LinkBehavior = LinkBehavior.HoverUnderline,
            LinkColor = Color.FromArgb(0, 102, 184),
            Location = new Point(440, 103),
            TabStop = false,
            Text = "历史记录",
            VisitedLinkColor = Color.FromArgb(0, 102, 184)
        };
    }

    /// <summary>
    /// 创建搜索框文字输入控件。
    /// </summary>
    private static TextBox CreateSearchTextBox()
    {
        return new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Regular, GraphicsUnit.Point),
            Location = new Point(30, 128),
            MaxLength = 64,
            PlaceholderText = "请输入要显示的文字",
            Size = new Size(460, 30)
        };
    }

    /// <summary>
    /// 创建显示当前搜索框文字的只读状态标签。
    /// </summary>
    private static Label CreateCurrentStatusLabel()
    {
        return new Label
        {
            AutoEllipsis = true,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(0, 90, 158),
            Location = new Point(30, 169),
            Size = new Size(460, 20),
            Text = "当前状态：Windows 默认"
        };
    }

    /// <summary>
    /// 创建统一尺寸和样式的操作按钮。
    /// </summary>
    private static RoundedButton CreateActionButton(string text, int left, bool isPrimary)
    {
        RoundedButton button = new()
        {
            CornerRadius = 8,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = isPrimary ? Color.White : Color.FromArgb(0, 102, 184),
            Location = new Point(left, 202),
            Size = new Size(112, 40),
            Text = text
        };

        if (isPrimary)
        {
            button.ButtonColor = Color.FromArgb(0, 120, 215);
            button.HoverColor = Color.FromArgb(16, 110, 190);
            button.PressedColor = Color.FromArgb(0, 90, 158);
            button.BorderColor = Color.FromArgb(0, 90, 158);
        }
        else
        {
            button.ButtonColor = Color.White;
            button.HoverColor = Color.FromArgb(232, 244, 255);
            button.PressedColor = Color.FromArgb(210, 233, 255);
            button.BorderColor = Color.FromArgb(0, 120, 215);
        }

        return button;
    }

    /// <summary>
    /// 读取当前已设置的自定义文字并显示到输入框。
    /// </summary>
    private void LoadCurrentSearchText()
    {
        try
        {
            string? currentText = RegistryHelper.GetCurrentSearchText();
            _searchTextBox.Text = currentText ?? string.Empty;
            UpdateStatusLabel(currentText);
        }
        catch (Exception ex)
        {
            UpdateStatusLabel(null);
            MessageBox.Show(
                $"读取当前搜索框文字失败：{ex.Message}",
                "读取失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// 将输入文字写入注册表，并重启资源管理器使设置生效。
    /// </summary>
    private void ApplyButton_Click(object? sender, EventArgs e)
    {
        string searchText = _searchTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            MessageBox.Show(
                "请输入搜索框文字。",
                "提示",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            _searchTextBox.Focus();
            return;
        }

        ApplySearchTextWithFeedback(searchText);
    }

    /// <summary>
    /// 应用搜索框文字并根据回读校验结果提示成功或失败。
    /// </summary>
    private void ApplySearchTextWithFeedback(string searchText)
    {
        try
        {
            RegistryHelper.ApplySearchText(searchText);
            bool isVerified = RegistryHelper.VerifySearchText(searchText);

            if (isVerified)
            {
                _searchTextBox.Text = searchText;
                UpdateStatusLabel(searchText);
                ExplorerHelper.Restart();

                MessageBox.Show(
                    "修改成功，资源管理器已重新启动。",
                    "修改成功",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                string? currentText = RegistryHelper.GetCurrentSearchText();
                _searchTextBox.Text = currentText ?? string.Empty;
                UpdateStatusLabel(currentText);

                MessageBox.Show(
                    "修改失败，写入后校验未通过。",
                    "修改失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"应用设置失败：{ex.Message}",
                "错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 显示最多 10 条历史记录，点击条目即可恢复对应文字。
    /// </summary>
    private void HistoryLinkLabel_LinkClicked(
        object? sender,
        LinkLabelLinkClickedEventArgs e)
    {
        IReadOnlyList<string> history;

        try
        {
            history = RegistryHelper.GetHistory();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"读取历史记录失败：{ex.Message}",
                "读取失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        ContextMenuStrip historyMenu = new()
        {
            BackColor = Color.White,
            Font = Font,
            MaximumSize = new Size(480, 480),
            ShowCheckMargin = false,
            ShowImageMargin = false
        };

        if (history.Count == 0)
        {
            historyMenu.Items.Add(new ToolStripMenuItem("暂无历史记录")
            {
                Enabled = false
            });
        }
        else
        {
            foreach (string historyText in history)
            {
                ToolStripMenuItem item = new(historyText);
                item.Click += (_, _) => ApplySearchTextWithFeedback(historyText);
                historyMenu.Items.Add(item);
            }
        }

        historyMenu.Closed += (_, _) => historyMenu.Dispose();
        historyMenu.Show(
            _historyLinkLabel,
            new Point(0, _historyLinkLabel.Height));
    }

    /// <summary>
    /// 删除自定义注册表值，并重启资源管理器恢复系统默认文字。
    /// </summary>
    private void RestoreButton_Click(object? sender, EventArgs e)
    {
        try
        {
            string? restoredText = RegistryHelper.RestoreDefault();
            _searchTextBox.Text = restoredText ?? string.Empty;
            UpdateStatusLabel(restoredText);
            ExplorerHelper.Restart();

            MessageBox.Show(
                "已恢复 Windows 默认搜索框文字，资源管理器已重新启动。",
                "恢复成功",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"恢复默认设置失败：{ex.Message}",
                "错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 更新当前搜索框文字状态标签。
    /// </summary>
    private void UpdateStatusLabel(string? searchText)
    {
        string statusText = string.IsNullOrWhiteSpace(searchText)
            ? "Windows 默认"
            : searchText;

        _currentStatusLabel.Text = $"当前状态：{statusText}";
    }
}
