using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NavMeshViewer;

public sealed class MainForm : Form
{
    private readonly NavMeshView _view;
    private readonly ToolStripStatusLabel _status;

    public MainForm(string[] args)
    {
        Text = "GameServer NavMesh 可视化";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);

        _view = new NavMeshView
        {
            Dock = DockStyle.Fill,
        };

        var toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
        };

        toolStrip.Items.Add(CreateButton("打开 .rnav", OpenNavMesh));
        toolStrip.Items.Add(CreateButton("打开建筑数据", OpenMapData));
        toolStrip.Items.Add(CreateButton("适配视图", () => _view.Fit()));
        toolStrip.Items.Add(CreateButton("清除路径", () => _view.ClearPath()));

        var statusStrip = new StatusStrip
        {
            Dock = DockStyle.Bottom,
        };
        _status = new ToolStripStatusLabel
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "左键设置起点，右键设置终点，中键拖动，滚轮缩放",
        };
        statusStrip.Items.Add(_status);

        Controls.Add(_view);
        Controls.Add(toolStrip);
        Controls.Add(statusStrip);

        _view.StatusChanged += text => _status.Text = text;

        Shown += (_, _) => LoadInitial(args);
    }

    private static ToolStripButton CreateButton(string text, Action action)
    {
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void LoadInitial(string[] args)
    {
        string? navPath = args.FirstOrDefault(a => a.EndsWith(".rnav", StringComparison.OrdinalIgnoreCase));
        string? mapPath = args.FirstOrDefault(a => a.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase));

        navPath ??= FindDefaultFile(Path.Combine("NavData", "navmesh.rnav"));
        mapPath ??= FindDefaultFile(Path.Combine("Client", "Assets", "BundleRes", "MapData", "OpenWorld", "BuildingData.bytes"));

        if (!string.IsNullOrEmpty(navPath) && File.Exists(navPath))
            _view.LoadNavMesh(navPath);
        else
            _status.Text = "未找到 navmesh.rnav，请点击“打开 .rnav”";

        if (!string.IsNullOrEmpty(mapPath) && File.Exists(mapPath))
            _view.LoadMapData(mapPath);
    }

    private void OpenNavMesh()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 .rnav 导航数据",
            Filter = "Recast navmesh (*.rnav)|*.rnav|所有文件 (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _view.LoadNavMesh(dialog.FileName);
    }

    private void OpenMapData()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 BuildingData.bytes",
            Filter = "地图建筑数据 (*.bytes)|*.bytes|所有文件 (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _view.LoadMapData(dialog.FileName);
    }

    private static string? FindDefaultFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            string serverCandidate = Path.Combine(directory.FullName, "Server", relativePath);
            if (File.Exists(serverCandidate))
                return serverCandidate;

            directory = directory.Parent;
        }

        return null;
    }
}
