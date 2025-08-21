#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UpgradeApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class UpgradableApp
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public string Available { get; set; }
        public string Status { get; set; } = "";
        public string Recommendation { get; set; } = "";
        public override string ToString()
        {
            return $"{Name} ({Id}) - {Version} -> {Available}";
        }
    }

    public class MainForm : Form
    {
        private Button btnCheck, btnUpgrade, btnUpgradeAll, btnSearch, btnInstall, btnUninstall, btnResearch, btnLogs, btnExport;
        private TextBox txtSearch, txtInstallId, txtLogs;
        private ListView lstApps;
        private ComboBox cmbSource;
        private ProgressBar progressBar;
        private CheckBox chkVerbose;
        private readonly List<UpgradableApp> upgradableApps = new();
        private static readonly HttpClient httpClient = new();
        private bool isAdvancedMode = true;
        private string selectedAiModel = "claude-sonnet-4-20250514";

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Winget Package Manager";
            this.Size = new Size(900, 600);
            this.MinimumSize = new Size(800, 500);
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterScreen;
            ApplySystemTheme();
            CreateMenuBar();

            var topPanel = new TableLayoutPanel { 
                Dock = DockStyle.Top, Height = 140, ColumnCount = 6, RowCount = 4, 
                Padding = new(15), BackColor = Color.FromArgb(45, 45, 48)
            };
            float[] colWidths = { 16F, 16F, 16F, 16F, 18F, 18F };
            float[] rowHeights = { 35F, 30F, 20F, 15F };
            for (int i = 0; i < 6; i++) topPanel.ColumnStyles.Add(new(SizeType.Percent, colWidths[i]));
            for (int i = 0; i < 4; i++) topPanel.RowStyles.Add(new(SizeType.Percent, rowHeights[i]));
            
            (btnCheck, btnUpgrade, btnUpgradeAll, btnSearch, btnResearch, btnLogs, btnExport) = 
                (CreateButton("🔄 Check Updates", SystemColors.Highlight), CreateButton("⬆️ Upgrade Selected", Color.Green),
                 CreateButton("⬆️ Upgrade All", Color.Green), CreateButton("🔍 Search", SystemColors.ControlDark),
                 CreateButton("🤖 AI Research", Color.Purple), CreateButton("📄 Logs", Color.Gray), CreateButton("📤 Export", Color.Orange));
            
            txtSearch = new() { 
                PlaceholderText = "Search packages...", Dock = DockStyle.Fill, Margin = new(3),
                BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                Font = new("Segoe UI", 9F)
            };
            chkVerbose = new() { 
                Text = "Verbose", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White, Font = new("Segoe UI", 9F)
            };
            
            var lblSource = new Label { 
                Text = "Source:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill,
                ForeColor = Color.White, Font = new("Segoe UI", 9F)
            };
            cmbSource = new() { 
                DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new(3),
                BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            cmbSource.Items.AddRange(new[] { "winget", "msstore", "all" });
            cmbSource.SelectedIndex = 0;
            
            var lblInstall = new Label { 
                Text = "Install:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill,
                ForeColor = Color.White, Font = new("Segoe UI", 9F)
            };
            txtInstallId = new() { 
                PlaceholderText = "Package ID", Dock = DockStyle.Fill, Margin = new(3),
                BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                Font = new("Segoe UI", 9F)
            };
            (btnInstall, btnUninstall) = (CreateButton("Install", Color.Green), CreateButton("Uninstall", Color.Crimson));
            
            progressBar = new() { 
                Dock = DockStyle.Fill, Style = ProgressBarStyle.Marquee, Visible = false, 
                Margin = new(3, 0, 3, 0), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.FromArgb(0, 120, 215)
            };
            
            topPanel.Controls.Add(btnCheck, 0, 0);
            topPanel.Controls.Add(btnUpgrade, 1, 0);
            topPanel.Controls.Add(btnUpgradeAll, 2, 0);
            topPanel.Controls.Add(btnSearch, 3, 0);
            topPanel.Controls.Add(btnResearch, 4, 0);
            topPanel.Controls.Add(btnLogs, 5, 0);
            
            topPanel.Controls.Add(btnExport, 2, 3);
            topPanel.SetColumnSpan(btnExport, 2);
            
            topPanel.Controls.Add(txtSearch, 0, 1);
            topPanel.SetColumnSpan(txtSearch, 2);
            topPanel.Controls.Add(chkVerbose, 2, 1);
            
            topPanel.Controls.Add(lblSource, 3, 1);
            topPanel.Controls.Add(cmbSource, 4, 1);
            topPanel.Controls.Add(lblInstall, 5, 1);
            
            topPanel.Controls.Add(txtInstallId, 0, 2);
            topPanel.SetColumnSpan(txtInstallId, 2);
            topPanel.Controls.Add(btnInstall, 2, 2);
            topPanel.Controls.Add(btnUninstall, 3, 2);
            
            topPanel.Controls.Add(progressBar, 4, 2);
            topPanel.SetColumnSpan(progressBar, 2);
            
            var splitter = new SplitContainer { 
                Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300, 
                Margin = new(15, 0, 15, 15), BackColor = Color.FromArgb(45, 45, 48),
                SplitterWidth = 8, Panel1MinSize = 200, Panel2MinSize = 100
            };
            
            lstApps = new() { 
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false, 
                CheckBoxes = true, MultiSelect = true, BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.White, Font = new("Segoe UI", 9F), BorderStyle = BorderStyle.None
            };
            
            txtLogs = new() { 
                Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, 
                Font = new("Consolas", 9F), BackColor = Color.FromArgb(20, 20, 20), 
                ForeColor = Color.FromArgb(0, 255, 127), Text = "=== Winget Package Manager Logs ===\n",
                BorderStyle = BorderStyle.None
            };
            
            splitter.Panel1.Controls.Add(lstApps);
            splitter.Panel2.Controls.Add(txtLogs);
            string[] columns = { "Name:250", "ID:200", "Current Version:120", "Available Version:120", "Source:80", "Status:100", "AI Recommendation:200" };
            foreach (var col in columns) { var parts = col.Split(':'); lstApps.Columns.Add(parts[0], int.Parse(parts[1])); }
            
            this.Controls.Add(splitter);
            this.Controls.Add(topPanel);
            
            var handlers = new (Button btn, EventHandler handler)[] {
                (btnCheck, BtnCheck_Click), (btnUpgrade, BtnUpgrade_Click), (btnUpgradeAll, BtnUpgradeAll_Click),
                (btnSearch, BtnSearch_Click), (btnInstall, BtnInstall_Click), (btnUninstall, BtnUninstall_Click),
                (btnResearch, BtnResearch_Click), (btnLogs, BtnLogs_Click), (btnExport, ExportUpgradeList)
            };
            foreach (var (btn, handler) in handlers) btn.Click += handler;
            this.Resize += MainForm_Resize;
            UpdateUIMode();
        }
        
        private void CreateMenuBar()
        {
            var menuStrip = new MenuStrip();
            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("User Guide", null, ShowHelp);
            helpMenu.DropDownItems.Add("About", null, ShowAbout);
            var settingsMenu = new ToolStripMenuItem("Settings");
            settingsMenu.DropDownItems.Add("UI Mode", null, ShowUISettings);
            settingsMenu.DropDownItems.Add("AI Model", null, ShowAISettings);

            menuStrip.Items.AddRange(new[] { helpMenu, settingsMenu });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }
        
        private void ExportUpgradeList(object sender, EventArgs e)
        {
            if (upgradableApps.Count == 0)
            {
                MessageBox.Show("No upgrade data to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"WingetUpgrades_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var content = new StringBuilder();
                    content.AppendLine("WINGET PACKAGE UPGRADE LIST");
                    content.AppendLine(new string('=', 50));
                    content.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    content.AppendLine($"Total packages: {upgradableApps.Count}");
                    content.AppendLine();
                    
                    foreach (var app in upgradableApps)
                    {
                        content.AppendLine($"Name: {app.Name}");
                        content.AppendLine($"ID: {app.Id}");
                        content.AppendLine($"Current: {app.Version}");
                        content.AppendLine($"Available: {app.Available}");
                        if (!string.IsNullOrEmpty(app.Status)) content.AppendLine($"Status: {app.Status}");
                        if (!string.IsNullOrEmpty(app.Recommendation)) content.AppendLine($"AI Recommendation: {app.Recommendation}");
                        content.AppendLine(new string('-', 30));
                    }
                    
                    File.WriteAllText(saveDialog.FileName, content.ToString());
                    MessageBox.Show($"Export completed: {saveDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private Button CreateButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text = text, Dock = DockStyle.Fill, Margin = new(3), BackColor = backColor, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand, 
                UseVisualStyleBackColor = false, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 20), 
                Math.Min(255, backColor.G + 20), 
                Math.Min(255, backColor.B + 20));
            return btn;
        }
        
        private void ApplySystemTheme()
        {
            try
            {
                var isDarkMode = IsSystemDarkMode();
                this.BackColor = isDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(248, 249, 250);
                this.ForeColor = isDarkMode ? Color.FromArgb(220, 220, 220) : Color.FromArgb(33, 37, 41);
            }
            catch
            {
                this.BackColor = Color.FromArgb(30, 30, 30);
                this.ForeColor = Color.White;
            }
        }
        
        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int i && i == 0;
            }
            catch { return false; }
        }
        
        private void UpdateUIMode()
        {
            if (isAdvancedMode) return;
            chkVerbose.Visible = false;
            cmbSource.Visible = false;
            txtInstallId.Visible = false;
            btnInstall.Visible = false;
            btnUninstall.Visible = false;
        }
        
        private void LoadSettings()
        {
            try
            {
                if (File.Exists("settings.json"))
                {
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText("settings.json"));
                    if (settings?.ContainsKey("isAdvancedMode") == true) isAdvancedMode = (bool)settings["isAdvancedMode"];
                    if (settings?.ContainsKey("selectedAiModel") == true) selectedAiModel = settings["selectedAiModel"]?.ToString() ?? selectedAiModel;
                }
            }
            catch { }
        }
        
        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, object> { ["isAdvancedMode"] = isAdvancedMode, ["selectedAiModel"] = selectedAiModel };
                File.WriteAllText("settings.json", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
        
        private void ShowHelp(object sender, EventArgs e)
        {
            var help = new Form { Text = "User Guide", Size = new(600, 500), StartPosition = FormStartPosition.CenterParent };
            var helpText = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
                Text = "WINGET PACKAGE MANAGER - USER GUIDE\n\n" +
                       "BASIC OPERATIONS:\n" +
                       "• Check Updates: Click 'Check' to scan for available updates\n" +
                       "• Upgrade Apps: Select apps and click 'Upgrade Selected'\n" +
                       "• Upgrade All: Click 'Upgrade All' to update everything\n" +
                       "• Search: Enter package name and click 'Search'\n\n" +
                       "ADVANCED FEATURES:\n" +
                       "• AI Research: Get detailed upgrade recommendations\n" +
                       "• Install/Uninstall: Manage packages by ID\n" +
                       "• Verbose Logging: Enable detailed command output\n" +
                       "• Source Selection: Choose winget, msstore, or all\n\n" +
                       "UI MODES:\n" +
                       "• Simple: Basic upgrade functionality only\n" +
                       "• Advanced: Full feature set with AI integration\n\n" +
                       "TIPS:\n" +
                       "• Use checkboxes to select multiple apps\n" +
                       "• View logs for detailed operation information\n" +
                       "• AI research provides security and compatibility insights"
            };
            help.Controls.Add(helpText);
            help.ShowDialog();
        }
        
        private void ShowAbout(object sender, EventArgs e)
        {
            MessageBox.Show("Winget Package Manager v2.0\nAI-Enhanced Windows Package Management\n\nBuilt with C# and Claude AI Integration", 
                           "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void ShowUISettings(object sender, EventArgs e)
        {
            var settings = new Form { Text = "UI Settings", Size = new(300, 150), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2 };
            var lblMode = new Label { Text = "UI Mode:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var cmbMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cmbMode.Items.AddRange(new[] { "Simple", "Advanced" });
            cmbMode.SelectedIndex = isAdvancedMode ? 1 : 0;
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill };
            panel.Controls.Add(lblMode, 0, 0);
            panel.Controls.Add(cmbMode, 1, 0);
            panel.Controls.Add(btnOK, 0, 1);
            panel.Controls.Add(btnCancel, 1, 1);
            settings.Controls.Add(panel);
            settings.AcceptButton = btnOK;
            settings.CancelButton = btnCancel;
            if (settings.ShowDialog() == DialogResult.OK)
            {
                var wasAdvanced = isAdvancedMode;
                isAdvancedMode = cmbMode.SelectedIndex == 1;
                UpdateUIMode();
                SaveSettings();
                if (wasAdvanced != isAdvancedMode) CreateMenuBar();
            }
        }
        
        private void ShowAISettings(object sender, EventArgs e)
        {
            var settings = new Form { Text = "AI Model Settings", Size = new(350, 150), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog };
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2 };
            var lblModel = new Label { Text = "AI Model:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var cmbModel = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cmbModel.Items.AddRange(new[] { "claude-sonnet-4-20250514", "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022", "claude-3-opus-20240229" });
            if (cmbModel.Items.Contains(selectedAiModel)) cmbModel.SelectedItem = selectedAiModel;
            else cmbModel.SelectedIndex = 0;
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill };
            panel.Controls.Add(lblModel, 0, 0);
            panel.Controls.Add(cmbModel, 1, 0);
            panel.Controls.Add(btnOK, 0, 1);
            panel.Controls.Add(btnCancel, 1, 1);
            settings.Controls.Add(panel);
            settings.AcceptButton = btnOK;
            settings.CancelButton = btnCancel;
            if (settings.ShowDialog() == DialogResult.OK)
            {
                selectedAiModel = cmbModel.SelectedItem?.ToString() ?? selectedAiModel;
                SaveSettings();
            }
        }
        
        private void MainForm_Resize(object sender, EventArgs e)
        {
            foreach (ColumnHeader column in lstApps.Columns)
            {
                if (column.Index == 0) column.Width = (int)(lstApps.Width * 0.3);
                else if (column.Index == 1) column.Width = (int)(lstApps.Width * 0.25);
                else if (column.Index == 6) column.Width = (int)(lstApps.Width * 0.2);
                else column.Width = (int)(lstApps.Width * 0.09);
            }
        }

        private async void BtnCheck_Click(object sender, EventArgs e)
        {
            lstApps.Items.Clear();
            upgradableApps.Clear();
            progressBar.Visible = true;
            btnCheck.Enabled = false;

            await Task.Run(() =>
            {
                var source = cmbSource.SelectedItem?.ToString() == "all" ? "" : $"--source {cmbSource.SelectedItem}";
                var command = $"winget upgrade {source}{(chkVerbose.Checked ? " --verbose" : "")}";
                LogMessage($"Executing: {command}");
                var output = RunPowerShell(command);
                LogMessage($"Output: {output[..Math.Min(500, output.Length)]}...");
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool headerFound = false;
                
                foreach (var line in lines)
                {
                    if (!headerFound)
                    {
                        if (line.Trim().StartsWith("Name") && line.Contains("Id") && line.Contains("Version"))
                        {
                            headerFound = true;
                        }
                        continue;
                    }
                    if (line.Trim().Length == 0 || line.StartsWith("-")) continue;

                    var parts = System.Text.RegularExpressions.Regex.Split(line.Trim(), @"\s{2,}");
                    if (parts.Length >= 4)
                    {
                        // Handle cases where winget output has different column arrangements
                        var name = parts[0];
                        var id = parts[1];
                        var currentVer = parts[2];
                        var availableVer = parts[3];
                        
                        // Skip if available version looks like a source name
                        if (availableVer.ToLower().Contains("winget") || availableVer.ToLower().Contains("msstore"))
                        {
                            if (parts.Length > 4) availableVer = parts[4];
                            else continue; // Skip this entry if we can't find proper version
                        }
                        
                        var app = new UpgradableApp
                        {
                            Name = name,
                            Id = id,
                            Version = currentVer,
                            Available = availableVer
                        };
                        upgradableApps.Add(app);
                        
                        this.Invoke(new Action(() =>
                        {
                            var source = "winget";
                            // Try to find actual source in remaining parts
                            for (int i = 4; i < parts.Length; i++)
                            {
                                if (parts[i].ToLower().Contains("winget") || parts[i].ToLower().Contains("msstore"))
                                {
                                    source = parts[i];
                                    break;
                                }
                            }
                            var item = new ListViewItem(new[] { app.Name, app.Id, app.Version, app.Available, source, "", "" });
                            item.Tag = app;
                            lstApps.Items.Add(item);
                        }));
                    }
                }
            });
            
            progressBar.Visible = false;
            btnCheck.Enabled = true;
            
            if (upgradableApps.Count == 0)
            {
                MessageBox.Show("No upgradable apps found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void BtnUpgrade_Click(object sender, EventArgs e)
        {
            var checkedItems = lstApps.CheckedItems.Cast<ListViewItem>().ToList();
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Please check at least one app to upgrade.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedApps = checkedItems.Select(item => (UpgradableApp)item.Tag).ToList();
            var confirm = MessageBox.Show(
                $"Upgrade {selectedApps.Count} selected apps?",
                "Confirm Upgrade",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            progressBar.Visible = true;
            btnUpgrade.Enabled = false;

            await Task.Run(() =>
            {
                foreach (var app in selectedApps)
                {
                    var command = $"winget upgrade --id \"{app.Id}\" --accept-source-agreements --accept-package-agreements --silent{(chkVerbose.Checked ? " --verbose" : "")}";
                    LogMessage($"Upgrading {app.Name}: {command}");
                    var result = RunPowerShell(command);
                    var success = !result.Contains("error", StringComparison.OrdinalIgnoreCase) && !result.Contains("failed", StringComparison.OrdinalIgnoreCase);
                    app.Status = success ? "✅ Success" : "❌ Failed";
                    LogMessage($"{app.Name} result: {app.Status} - {result[..Math.Min(200, result.Length)]}");
                    
                    this.Invoke(() => {
                        var item = checkedItems.FirstOrDefault(i => i.Tag == app);
                        if (item?.SubItems.Count > 5) item.SubItems[5].Text = app.Status;
                    });
                }
            });

            progressBar.Visible = false;
            btnUpgrade.Enabled = true;
            var successCount = selectedApps.Count(a => a.Status.Contains("Success"));
            MessageBox.Show($"Completed: {successCount}/{selectedApps.Count} apps upgraded successfully.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private async void BtnUpgradeAll_Click(object sender, EventArgs e)
        {
            if (upgradableApps.Count == 0)
            {
                MessageBox.Show("No apps to upgrade.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var confirm = MessageBox.Show($"Upgrade all {upgradableApps.Count} apps?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            
            progressBar.Visible = true;
            btnUpgradeAll.Enabled = false;
            
            await Task.Run(() => 
            {
                var verbose = chkVerbose.Checked ? " --verbose" : "";
                var command = $"winget upgrade --all --accept-source-agreements --accept-package-agreements --silent{verbose}";
                LogMessage($"Executing: {command}");
                var result = RunPowerShell(command);
                LogMessage($"Upgrade all result: {result.Substring(0, Math.Min(500, result.Length))}...");
            });
            
            progressBar.Visible = false;
            btnUpgradeAll.Enabled = true;
            MessageBox.Show("All apps upgraded.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            BtnCheck_Click(sender, e);
        }
        
        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) return;
            
            lstApps.Items.Clear();
            progressBar.Visible = true;
            btnSearch.Enabled = false;
            
            await Task.Run(() =>
            {
                var verbose = chkVerbose.Checked ? " --verbose" : "";
                var command = $"winget search \"{txtSearch.Text}\"{verbose}";
                LogMessage($"Executing: {command}");
                var output = RunPowerShell(command);
                LogMessage($"Search result: {output.Substring(0, Math.Min(300, output.Length))}...");
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool headerFound = false;
                
                foreach (var line in lines)
                {
                    if (!headerFound)
                    {
                        if (line.Trim().StartsWith("Name") && line.Contains("Id"))
                        {
                            headerFound = true;
                        }
                        continue;
                    }
                    if (line.Trim().Length == 0 || line.StartsWith("-")) continue;
                    
                    var parts = System.Text.RegularExpressions.Regex.Split(line.Trim(), @"\s{2,}");
                    if (parts.Length >= 2)
                    {
                        this.Invoke(new Action(() =>
                        {
                            var item = new ListViewItem(new[] { parts[0], parts[1], parts.Length > 2 ? parts[2] : "", "", parts.Length > 3 ? parts[3] : "winget", "", "" });
                            lstApps.Items.Add(item);
                        }));
                    }
                }
            });
            
            progressBar.Visible = false;
            btnSearch.Enabled = true;
        }
        
        private async void BtnInstall_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInstallId.Text)) return;
            
            progressBar.Visible = true;
            btnInstall.Enabled = false;
            
            await Task.Run(() => 
            {
                var verbose = chkVerbose.Checked ? " --verbose" : "";
                var command = $"winget install --id \"{txtInstallId.Text}\" --accept-source-agreements --accept-package-agreements --silent{verbose}";
                LogMessage($"Installing: {command}");
                var result = RunPowerShell(command);
                LogMessage($"Install result: {result.Substring(0, Math.Min(300, result.Length))}...");
            });
            
            progressBar.Visible = false;
            btnInstall.Enabled = true;
            MessageBox.Show($"Installation of {txtInstallId.Text} completed.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private async void BtnUninstall_Click(object sender, EventArgs e)
        {
            var checkedItems = lstApps.CheckedItems.Cast<ListViewItem>().ToList();
            if (checkedItems.Count == 0 && string.IsNullOrWhiteSpace(txtInstallId.Text))
            {
                MessageBox.Show("Check apps or enter package ID to uninstall.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var confirm = MessageBox.Show("Uninstall selected packages?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            
            progressBar.Visible = true;
            btnUninstall.Enabled = false;
            
            await Task.Run(() =>
            {
                var verbose = chkVerbose.Checked ? " --verbose" : "";
                if (checkedItems.Count > 0)
                {
                    foreach (var item in checkedItems)
                    {
                        var command = $"winget uninstall --id \"{item.SubItems[1].Text}\" --silent{verbose}";
                        LogMessage($"Uninstalling: {command}");
                        var result = RunPowerShell(command);
                        LogMessage($"Uninstall result: {result.Substring(0, Math.Min(200, result.Length))}...");
                    }
                }
                else
                {
                    var command = $"winget uninstall --id \"{txtInstallId.Text}\" --silent{verbose}";
                    LogMessage($"Uninstalling: {command}");
                    var result = RunPowerShell(command);
                    LogMessage($"Uninstall result: {result.Substring(0, Math.Min(200, result.Length))}...");
                }
            });
            
            progressBar.Visible = false;
            btnUninstall.Enabled = true;
            MessageBox.Show("Uninstallation completed.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private async void BtnResearch_Click(object sender, EventArgs e)
        {
            var checkedItems = lstApps.CheckedItems.Cast<ListViewItem>().ToList();
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Please check apps to research.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            progressBar.Visible = true;
            btnResearch.Enabled = false;
            
            var recommendations = new List<(UpgradableApp app, string recommendation)>();
            
            await Task.Run(async () =>
            {
                foreach (var item in checkedItems)
                {
                    var app = (UpgradableApp)item.Tag;
                    LogMessage($"Researching {app.Name} ({app.Version} -> {app.Available})");
                    var recommendation = await GetAIRecommendation(app);
                    app.Recommendation = recommendation;
                    recommendations.Add((app, recommendation));
                    LogMessage($"AI recommendation for {app.Name}: {recommendation}");
                    
                    this.Invoke(new Action(() =>
                    {
                        if (item?.SubItems.Count > 6)
                        {
                            var summary = recommendation?.Split('\n')?[0] ?? "No summary";
                            item.SubItems[6].Text = summary.Length > 50 ? summary.Substring(0, 47) + "..." : summary;
                        }
                    }));
                }
            });
            
            progressBar.Visible = false;
            btnResearch.Enabled = true;
            
            ShowRecommendationsPopup(recommendations);
        }
        
        private async Task<string> GetAIRecommendation(UpgradableApp app)
        {
            try
            {
                var apiKey = LoadApiKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    LogMessage("ERROR: No API key found in config.json");
                    return "No API key configured";
                }
                
                var requestBody = new
                {
                    model = selectedAiModel,
                    max_tokens = 400,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = CreateSoftwareResearchPrompt(app.Name, app.Id, app.Version, app.Available)
                        }
                    }
                };
                
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                
                LogMessage($"Sending API request for {app.Name}");
                var response = await httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    LogMessage($"API response received: {responseJson.Substring(0, Math.Min(200, responseJson.Length))}...");
                    var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    return result.GetProperty("content")[0].GetProperty("text").GetString() ?? "No recommendation available";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"API Error {response.StatusCode}: {errorContent}");
                    return $"API Error: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in GetAIRecommendation: {ex.Message}");
                return "Research failed";
            }
        }
        
        private string LoadApiKey()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                LogMessage($"Loading API key from: {configPath}");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<JsonElement>(json);
                    var key = config.GetProperty("AnthropicApiKey").GetString() ?? "";
                    LogMessage($"API key loaded: {(string.IsNullOrEmpty(key) ? "EMPTY" : "OK")}");
                    return key;
                }
                else
                {
                    LogMessage($"Config file not found: {configPath}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading API key: {ex.Message}");
            }
            return "";
        }
        
        private static string CreateSoftwareResearchPrompt(string softwareName, string packageId, string currentVersion, string newVersion)
        {
            return $@"Research and analyze {softwareName} upgrade from {currentVersion} to {newVersion}. 

First, search for and review the official website, release notes, and changelog for {softwareName} to gather current information about this version upgrade. Then provide a concise assessment:

1. RECOMMENDATION: Upgrade immediately/plan upgrade/wait/avoid and why
2. KEY CHANGES: Most important new features, fixes, or breaking changes
3. SECURITY: Critical security fixes or vulnerabilities addressed
4. RISKS: Main risks of upgrading vs not upgrading
5. TIMELINE: Recommended upgrade timeframe

Base your analysis on the latest information from the software's official sources. Keep response under 300 words. Focus on actionable insights only.";
        }

        private string RunPowerShell(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using (var process = Process.Start(psi))
            {
                if (process == null) return "Process failed to start";
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(error))
                    output += "\n" + error;
                return output;
            }
        }
        
        private void LogMessage(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            void UpdateLog() { 
                if (txtLogs != null) {
                    txtLogs.AppendText(logEntry); 
                    txtLogs.SelectionStart = txtLogs.Text.Length; 
                    txtLogs.ScrollToCaret(); 
                }
            }
            
            if (txtLogs?.InvokeRequired == true) txtLogs.Invoke(UpdateLog); else UpdateLog();
        }
        
        private void BtnLogs_Click(object sender, EventArgs e)
        {
            var splitter = this.Controls.OfType<SplitContainer>().FirstOrDefault();
            if (splitter != null)
            {
                splitter.Panel2Collapsed = !splitter.Panel2Collapsed;
                btnLogs.Text = splitter.Panel2Collapsed ? "📄 Show Logs" : "📄 Hide Logs";
            }
        }
        
        private void ShowRecommendationsPopup(List<(UpgradableApp app, string recommendation)> recommendations)
        {
            var popup = new Form
            {
                Text = "🤖 Comprehensive Software Analysis",
                Size = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent,
                MinimumSize = new Size(800, 600),
                Font = new Font("Segoe UI", 9F),
                WindowState = FormWindowState.Maximized
            };
            
            var richText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(10),
                BackColor = SystemColors.Window,
                SelectionTabs = new int[] { 200 }
            };
            
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            var btnCopy = new Button
            {
                Text = "📋 Copy All",
                Size = new Size(100, 30),
                Location = new Point(10, 10),
                BackColor = SystemColors.Highlight,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            var btnSave = new Button
            {
                Text = "💾 Save MD",
                Size = new Size(90, 30),
                Location = new Point(120, 10),
                BackColor = Color.DarkGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            var btnClose = new Button
            {
                Text = "✖ Close",
                Size = new Size(80, 30),
                Location = new Point(220, 10),
                BackColor = SystemColors.ControlDark,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            var content = new StringBuilder();
            content.AppendLine("🤖 COMPREHENSIVE SOFTWARE UPGRADE ANALYSIS");
            content.AppendLine(new string('=', 70));
            content.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Analyzed {recommendations.Count} software package(s)");
            content.AppendLine();
            
            foreach (var (app, recommendation) in recommendations)
            {
                content.AppendLine($"📦 {app.Name.ToUpper()}");
                content.AppendLine($"Package ID: {app.Id}");
                content.AppendLine($"Version Upgrade: {app.Version} → {app.Available}");
                content.AppendLine(new string('-', 50));
                content.AppendLine();
                content.AppendLine(recommendation);
                content.AppendLine();
                content.AppendLine(new string('=', 70));
                content.AppendLine();
            }
            
            richText.Text = content.ToString();
            
            btnCopy.Click += (s, e) =>
            {
                Clipboard.SetText(richText.Text);
                btnCopy.Text = "✅ Copied!";
                Task.Delay(2000).ContinueWith(t => btnCopy.Invoke(new Action(() => btnCopy.Text = "📋 Copy All")));
            };
            
            btnSave.Click += (s, e) =>
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                    DefaultExt = "md",
                    FileName = $"AI_Research_{DateTime.Now:yyyyMMdd_HHmmss}.md"
                };
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var markdown = CreateMarkdownContent(recommendations);
                        File.WriteAllText(saveDialog.FileName, markdown);
                        btnSave.Text = "✅ Saved!";
                        Task.Delay(2000).ContinueWith(t => btnSave.Invoke(new Action(() => btnSave.Text = "💾 Save MD")));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            
            btnClose.Click += (s, e) => popup.Close();
            
            buttonPanel.Controls.AddRange(new Control[] { btnCopy, btnSave, btnClose });
            popup.Controls.AddRange(new Control[] { richText, buttonPanel });
            
            popup.ShowDialog(this);
        }
        
        private string CreateMarkdownContent(List<(UpgradableApp app, string recommendation)> recommendations)
        {
            var markdown = new StringBuilder();
            markdown.AppendLine("# 🤖 AI Software Upgrade Analysis");
            markdown.AppendLine();
            markdown.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            markdown.AppendLine($"**Analyzed:** {recommendations.Count} software package(s)");
            markdown.AppendLine();
            
            foreach (var (app, recommendation) in recommendations)
            {
                markdown.AppendLine($"## 📦 {app.Name}");
                markdown.AppendLine();
                markdown.AppendLine($"- **Package ID:** `{app.Id}`");
                markdown.AppendLine($"- **Version Upgrade:** `{app.Version}` → `{app.Available}`");
                markdown.AppendLine();
                markdown.AppendLine("### Analysis");
                markdown.AppendLine();
                markdown.AppendLine(recommendation);
                markdown.AppendLine();
                markdown.AppendLine("---");
                markdown.AppendLine();
            }
            
            return markdown.ToString();
        }
    }
}
