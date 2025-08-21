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
using System.Threading;
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

    public class SpinningProgressForm : Form
    {
        private readonly System.Windows.Forms.Timer timer = new();
        private int rotationAngle = 0;
        private readonly Image iconImage;
        private readonly Label statusLabel;

        public SpinningProgressForm(string message = "Processing...")
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(200, 150);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ShowInTaskbar = false;
            this.TopMost = true;

            // Load the icon
            try
            {
                iconImage = Image.FromFile("Logo.ico");
            }
            catch
            {
                iconImage = SystemIcons.Information.ToBitmap();
            }

            // Status label
            statusLabel = new Label
            {
                Text = message,
                ForeColor = Color.White,
                Font = new Font("Calibri", 10F),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 30
            };

            this.Controls.Add(statusLabel);

            // Timer for spinning animation
            timer.Interval = 50;
            timer.Tick += (s, e) =>
            {
                rotationAngle = (rotationAngle + 10) % 360;
                this.Invalidate();
            };

            this.Paint += OnPaint;
            timer.Start();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (iconImage == null) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Calculate center position
            var centerX = this.Width / 2;
            var centerY = (this.Height - statusLabel.Height) / 2;
            var iconSize = 48;

            // Save the current graphics state
            var state = g.Save();

            // Translate to center, rotate, then translate back
            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(rotationAngle);
            g.TranslateTransform(-iconSize / 2, -iconSize / 2);

            // Draw the rotated icon
            g.DrawImage(iconImage, 0, 0, iconSize, iconSize);

            // Restore graphics state
            g.Restore(state);
        }

        public void UpdateMessage(string message)
        {
            if (statusLabel != null)
                statusLabel.Text = message;
        }

        public void CenterOnParent(Form parent)
        {
            if (parent != null)
            {
                this.Location = new Point(
                    parent.Location.X + (parent.Width - this.Width) / 2,
                    parent.Location.Y + (parent.Height - this.Height) / 2
                );
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Dispose();
                iconImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class MainForm : Form, IDisposable
    {
        private Button btnCheck, btnUpgrade, btnUpgradeAll, btnInstall, btnUninstall, btnResearch, btnLogs, btnExport, btnHelp, btnSettings, btnListAll, btnRepair;
        private TextBox txtLogs;
        private ListView lstApps;
        private ComboBox cmbSource;
        private CheckBox chkVerbose;
        private SplitContainer splitter;
        private readonly List<UpgradableApp> upgradableApps = new();
        private readonly object upgradableAppsLock = new();
        private readonly SemaphoreSlim httpSemaphore = new(1, 1);
        private static readonly HttpClient httpClient = new();
        private bool isAdvancedMode = true;
        private string selectedAiModel = "claude-sonnet-4-20250514";
        private bool usePerplexity = true;
        
        // Modern font with fallbacks for better compatibility
        private static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
        {
            try
            {
                return new Font("Calibri", size, style);
            }
            catch
            {
                try
                {
                    return new Font("Segoe UI", size, style);
                }
                catch
                {
                    return new Font(FontFamily.GenericSansSerif, size, style);
                }
            }
        }

        private Panel CreateWelcomePanel()
        {
            var welcomePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                Visible = true
            };

            // Get time-based greeting
            var hour = DateTime.Now.Hour;
            var greeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
            var userName = Environment.UserName;

            // Main greeting label
            var greetingLabel = new Label
            {
                Text = $"🧿 {greeting}, {userName}",
                Font = CreateFont(26F, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 230, 230),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "How can I help you manage your packages today?",
                Font = CreateFont(14F, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 160, 160),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Action suggestions - Claude-inspired cards
            var actionsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };

            var actionCards = new[]
            {
                ("🔄 Check Updates", "Scan for available package updates", Color.FromArgb(55, 125, 255)),
                ("🤖 AI Research", "Get intelligent upgrade recommendations", Color.FromArgb(147, 51, 234)),
                ("📋 List All Apps", "View your complete software inventory", Color.FromArgb(107, 114, 128)),
                ("📤 Export", "Save package information and reports", Color.FromArgb(251, 146, 60))
            };

            foreach (var (title, description, color) in actionCards)
            {
                var card = new Panel
                {
                    Width = 180,
                    Height = 100,
                    BackColor = Color.FromArgb(30, 30, 30),
                    Margin = new Padding(8),
                    Cursor = Cursors.Hand
                };

                var cardTitle = new Label
                {
                    Text = title,
                    Font = CreateFont(11F, FontStyle.Bold),
                    ForeColor = color,
                    Location = new Point(12, 12),
                    AutoSize = true
                };

                var cardDesc = new Label
                {
                    Text = description,
                    Font = CreateFont(9F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(180, 180, 180),
                    Location = new Point(12, 35),
                    Size = new Size(156, 50)
                };

                card.Controls.Add(cardTitle);
                card.Controls.Add(cardDesc);
                actionsPanel.Controls.Add(card);
            }

            // Position everything centered
            var centerPanel = new Panel
            {
                AutoSize = true,
                Anchor = AnchorStyles.None
            };

            greetingLabel.Location = new Point(0, 0);
            subtitleLabel.Location = new Point(0, 40);
            actionsPanel.Location = new Point(0, 80);

            centerPanel.Controls.Add(greetingLabel);
            centerPanel.Controls.Add(subtitleLabel);
            centerPanel.Controls.Add(actionsPanel);

            // Center the panel in the welcome panel
            centerPanel.Location = new Point(
                (welcomePanel.Width - centerPanel.Width) / 2,
                (welcomePanel.Height - centerPanel.Height) / 2
            );

            welcomePanel.Controls.Add(centerPanel);
            welcomePanel.Tag = "welcome"; // For easy identification

            return welcomePanel;
        }

        private void HideWelcomePanel()
        {
            var welcomePanel = splitter.Panel1.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "welcome");
            if (welcomePanel != null)
                welcomePanel.Visible = false;
        }

        private void ShowWelcomePanel()
        {
            var welcomePanel = splitter.Panel1.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "welcome");
            if (welcomePanel != null)
                welcomePanel.Visible = true;
        }

        // Safe string operations to prevent null reference exceptions
        private static string SafeSubstring(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return "";
            if (maxLength <= 0) return "";
            return input.Length <= maxLength ? input : input[..maxLength] + "...";
        }

        // Validate form state before operations
        private bool IsFormValid()
        {
            return !this.IsDisposed && this.Created;
        }

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "WingetWizard - AI-Enhanced Package Manager";
            this.Size = new Size(900, 600);
            this.MinimumSize = new Size(800, 500);
            this.Font = new Font("Calibri", 11F);
            this.StartPosition = FormStartPosition.CenterScreen;
            try { this.Icon = new Icon("Logo.ico"); } 
            catch (Exception ex) { LogMessage($"Icon load failed: {ex.Message}"); }
            ApplySystemTheme();

            var topPanel = new TableLayoutPanel { 
                Dock = DockStyle.Top, Height = 110, ColumnCount = 9, RowCount = 2, 
                Padding = new(20), BackColor = Color.FromArgb(25, 25, 25)
            };
            float[] colWidths = { 11F, 11F, 11F, 11F, 11F, 12F, 11F, 11F, 11F };
            float[] rowHeights = { 50F, 50F };
            for (int i = 0; i < 9; i++) topPanel.ColumnStyles.Add(new(SizeType.Percent, colWidths[i]));
            for (int i = 0; i < 2; i++) topPanel.RowStyles.Add(new(SizeType.Percent, rowHeights[i]));
            
            // Modern Claude-inspired color palette
            var primaryBlue = Color.FromArgb(55, 125, 255);      // Claude-style blue
            var successGreen = Color.FromArgb(34, 197, 94);      // Modern green
            var accentOrange = Color.FromArgb(251, 146, 60);     // Claude orange accent
            var warningAmber = Color.FromArgb(245, 158, 11);     // Warm amber
            var purpleAI = Color.FromArgb(147, 51, 234);         // AI purple
            var neutralGray = Color.FromArgb(107, 114, 128);     // Sophisticated gray
            var darkBlue = Color.FromArgb(30, 58, 138);          // Deep blue
            var darkGreen = Color.FromArgb(20, 83, 45);          // Forest green
            var crimsonRed = Color.FromArgb(220, 38, 127);       // Modern crimson

            (btnCheck, btnUpgrade, btnUpgradeAll, btnListAll, btnResearch, btnLogs, btnExport, btnHelp, btnSettings) = 
                (CreateButton("🔄 Check Updates", primaryBlue), CreateButton("⬆️ Upgrade Selected", successGreen),
                 CreateButton("🚀 Upgrade All", darkGreen), CreateButton("📋 List All Apps", neutralGray),
                 CreateButton("🤖 AI Research", purpleAI), CreateButton("📄 Show Logs", Color.FromArgb(75, 85, 99)), 
                 CreateButton("📤 Export", accentOrange), CreateButton("❓ Help", darkBlue), 
                 CreateButton("⚙️ Settings", Color.FromArgb(55, 65, 81)));
            
            (btnInstall, btnUninstall, btnRepair) = (CreateButton("📦 Install Selected", successGreen), CreateButton("🗑️ Uninstall Selected", crimsonRed), CreateButton("🔧 Repair Selected", warningAmber));
            
            chkVerbose = new() { 
                Text = "Verbose", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White, Font = new("Calibri", 11F)
            };
            

            cmbSource = new() { 
                DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new(3),
                BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            cmbSource.Items.AddRange(new[] { "winget", "msstore", "all" });
            cmbSource.SelectedIndex = 0;
            
            topPanel.Controls.Add(btnCheck, 0, 0);
            topPanel.Controls.Add(btnUpgrade, 1, 0);
            topPanel.Controls.Add(btnUpgradeAll, 2, 0);
            topPanel.Controls.Add(btnListAll, 3, 0);
            topPanel.Controls.Add(btnResearch, 4, 0);
            topPanel.Controls.Add(btnLogs, 5, 0);
            topPanel.Controls.Add(btnExport, 6, 0);
            topPanel.Controls.Add(btnHelp, 7, 0);
            topPanel.Controls.Add(btnSettings, 8, 0);
            
            topPanel.Controls.Add(btnInstall, 0, 1);
            topPanel.Controls.Add(btnUninstall, 1, 1);
            topPanel.Controls.Add(btnRepair, 2, 1);
            topPanel.Controls.Add(chkVerbose, 3, 1);
            topPanel.Controls.Add(cmbSource, 4, 1);
            
            splitter = new SplitContainer { 
                Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300, 
                Margin = new(20, 10, 20, 20), BackColor = Color.FromArgb(35, 35, 35),
                SplitterWidth = 6, Panel1MinSize = 200, Panel2MinSize = 100,
                Panel2Collapsed = true
            };
            
            lstApps = new() { 
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false, 
                CheckBoxes = true, MultiSelect = true, BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(230, 230, 230), Font = CreateFont(11F), BorderStyle = BorderStyle.None
            };
            
            txtLogs = new() { 
                Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, 
                Font = new("Consolas", 11F), BackColor = Color.FromArgb(18, 18, 18), 
                ForeColor = Color.FromArgb(34, 197, 94), Text = "=== WingetWizard Logs ===\n",
                BorderStyle = BorderStyle.None
            };
            
            // Create welcome overlay for when no packages are loaded
            var welcomePanel = CreateWelcomePanel();
            
            splitter.Panel1.Controls.Add(lstApps);
            splitter.Panel1.Controls.Add(welcomePanel);
            splitter.Panel2.Controls.Add(txtLogs);
            string[] columns = { "Name:250", "ID:200", "Current Version:120", "Available Version:120", "Source:80", "Status:100", "AI Recommendation:200" };
            foreach (var col in columns) { var parts = col.Split(':'); lstApps.Columns.Add(parts[0], int.Parse(parts[1])); }
            
            this.Controls.Add(splitter);
            this.Controls.Add(topPanel);
            
            var handlers = new (Button btn, EventHandler handler)[] {
                (btnCheck, BtnCheck_Click), (btnUpgrade, BtnUpgrade_Click), (btnUpgradeAll, BtnUpgradeAll_Click),
                (btnListAll, BtnListAll_Click), (btnInstall, BtnInstall_Click), (btnUninstall, BtnUninstall_Click),
                (btnRepair, BtnRepair_Click), (btnResearch, BtnResearch_Click), (btnLogs, BtnLogs_Click), 
                (btnExport, ExportUpgradeList), (btnHelp, ShowHelpMenu), (btnSettings, ShowSettingsMenu)
            };
            foreach (var (btn, handler) in handlers) btn.Click += handler;
            this.Resize += MainForm_Resize;
            UpdateUIMode();
        }
        
        private void ShowHelpMenu(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("User Guide", null, ShowHelp);
            menu.Items.Add("About", null, ShowAbout);
            menu.Show(btnHelp, new Point(0, btnHelp.Height));
        }
        
        private void ShowSettingsMenu(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("UI Mode", null, ShowUISettings);
            menu.Items.Add("AI Settings", null, ShowAISettings);
            menu.Items.Add("Reset API Keys", null, ResetApiKeys);
            menu.Show(btnSettings, new Point(0, btnSettings.Height));
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
                    content.AppendLine("WINGETWIZARD PACKAGE UPGRADE LIST");
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
                Text = text, 
                Dock = DockStyle.Fill, 
                Margin = new(6), // Increased margin for card-like spacing
                BackColor = backColor, 
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, 
                Font = CreateFont(10.5F, FontStyle.Bold), 
                Cursor = Cursors.Hand, 
                UseVisualStyleBackColor = false, 
                AutoSize = false, 
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            // Modern flat design with subtle borders
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            
            // Sophisticated hover effects
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 15), 
                Math.Min(255, backColor.G + 15), 
                Math.Min(255, backColor.B + 15));
                
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, backColor.R - 10), 
                Math.Max(0, backColor.G - 10), 
                Math.Max(0, backColor.B - 10));
                
            return btn;
        }
        
        private void ApplySystemTheme()
        {
            try
            {
                var isDarkMode = IsSystemDarkMode();
                // Claude-inspired sophisticated dark theme
                this.BackColor = isDarkMode ? Color.FromArgb(15, 15, 15) : Color.FromArgb(248, 249, 250);
                this.ForeColor = isDarkMode ? Color.FromArgb(230, 230, 230) : Color.FromArgb(33, 37, 41);
            }
            catch (Exception ex)
            {
                LogMessage($"Theme application failed: {ex.Message}");
                // Fallback to sophisticated dark theme
                this.BackColor = Color.FromArgb(15, 15, 15);
                this.ForeColor = Color.FromArgb(230, 230, 230);
            }
        }
        
        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int i && i == 0;
            }
            catch (Exception) 
            { 
                // LogMessage($"Registry access failed: {ex.Message}");
                return false; 
            }
        }
        
        private void UpdateUIMode()
        {
            if (isAdvancedMode) return;
            chkVerbose.Visible = false;
            cmbSource.Visible = false;
            btnInstall.Visible = false;
            btnUninstall.Visible = false;
            btnRepair.Visible = false;
        }
        
        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(settingsPath, Encoding.UTF8));
                    if (settings?.ContainsKey("isAdvancedMode") == true) isAdvancedMode = settings["isAdvancedMode"].GetBoolean();
                    if (settings?.ContainsKey("selectedAiModel") == true) selectedAiModel = settings["selectedAiModel"].GetString() ?? selectedAiModel;
                    if (settings?.ContainsKey("usePerplexity") == true) usePerplexity = settings["usePerplexity"].GetBoolean();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Settings load error: {ex.Message}");
                // Fall back to default settings
                isAdvancedMode = true;
                selectedAiModel = "claude-sonnet-4-20250514";
                usePerplexity = true;
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var settings = new Dictionary<string, object>
                {
                    ["isAdvancedMode"] = isAdvancedMode,
                    ["selectedAiModel"] = selectedAiModel,
                    ["usePerplexity"] = usePerplexity
                };
                
                // Preserve existing API keys
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath, Encoding.UTF8);
                    var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
                    foreach (var kvp in existing)
                    {
                        if (kvp.Key.EndsWith("ApiKey") && !settings.ContainsKey(kvp.Key))
                        {
                            settings[kvp.Key] = kvp.Value;
                        }
                    }
                }
                
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
                LogMessage($"Settings saved with {settings.Count} keys");
            }
            catch (Exception ex)
            {
                LogMessage($"Settings save error: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void ResetApiKeys(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear all stored API keys?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                    if (File.Exists(settingsPath))
                    {
                        var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(settingsPath, Encoding.UTF8)) ?? new();
                        settings.Remove("AnthropicApiKey");
                        settings.Remove("PerplexityApiKey");
                        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
                        MessageBox.Show("API keys cleared.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Failed to reset keys: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
        
        private void ShowHelp(object sender, EventArgs e)
        {
            var help = new Form 
            { 
                Text = "🧿 WingetWizard User Guide", 
                Size = new(900, 700), 
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                MinimumSize = new(800, 600)
            };

            var richTextBox = new RichTextBox
            {
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Calibri", 11F),
                Margin = new Padding(20)
            };

            FormatHelpContent(richTextBox);
            help.Controls.Add(richTextBox);
            help.ShowDialog();
        }

        private void FormatHelpContent(RichTextBox rtb)
        {
            rtb.Clear();

            // Main Title
            AppendFormattedText(rtb, "🧿 WingetWizard", Color.FromArgb(100, 200, 255), 22, FontStyle.Bold);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "AI-Enhanced Windows Package Manager", Color.FromArgb(150, 200, 255), 14, FontStyle.Italic);
            rtb.AppendText("\n\n");

            // Quick Start Section
            AppendFormattedText(rtb, "🚀 Quick Start", Color.FromArgb(255, 200, 100), 16, FontStyle.Bold);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "1. ", Color.FromArgb(100, 255, 100), 11, FontStyle.Bold);
            AppendFormattedText(rtb, "Click ", Color.FromArgb(200, 200, 200), 11, FontStyle.Regular);
            AppendFormattedText(rtb, "🔄 Check Updates", Color.FromArgb(100, 200, 255), 11, FontStyle.Bold);
            AppendFormattedText(rtb, " to scan for available upgrades", Color.FromArgb(200, 200, 200), 11, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "2. ", Color.FromArgb(100, 255, 100), 11, FontStyle.Bold);
            AppendFormattedText(rtb, "Select packages using checkboxes", Color.FromArgb(200, 200, 200), 11, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "3. ", Color.FromArgb(100, 255, 100), 11, FontStyle.Bold);
            AppendFormattedText(rtb, "Use ", Color.FromArgb(200, 200, 200), 11, FontStyle.Regular);
            AppendFormattedText(rtb, "🤖 AI Research", Color.FromArgb(200, 100, 255), 11, FontStyle.Bold);
            AppendFormattedText(rtb, " for intelligent upgrade analysis", Color.FromArgb(200, 200, 200), 11, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "4. ", Color.FromArgb(100, 255, 100), 11, FontStyle.Bold);
            AppendFormattedText(rtb, "Click ", Color.FromArgb(200, 200, 200), 11, FontStyle.Regular);
            AppendFormattedText(rtb, "⬆️ Upgrade Selected", Color.FromArgb(100, 255, 100), 11, FontStyle.Bold);
            AppendFormattedText(rtb, " to apply updates", Color.FromArgb(200, 200, 200), 11, FontStyle.Regular);
            rtb.AppendText("\n\n");

            // Package Operations Section
            AppendFormattedText(rtb, "📦 Package Operations", Color.FromArgb(255, 200, 100), 16, FontStyle.Bold);
            rtb.AppendText("\n\n");

            var packageOps = new[]
            {
                ("🔄", "Check Updates", "Scan for available package updates", Color.FromArgb(100, 200, 255)),
                ("📦", "Upgrade Selected", "Update only checked packages individually", Color.FromArgb(100, 255, 100)),
                ("🚀", "Upgrade All", "Update all available packages at once", Color.FromArgb(50, 255, 50)),
                ("📋", "List All Apps", "View complete inventory of installed software", Color.FromArgb(100, 200, 255)),
                ("📦", "Install Selected", "Install new packages from checked items", Color.FromArgb(100, 255, 100)),
                ("🗑️", "Uninstall Selected", "Remove checked packages safely", Color.FromArgb(255, 100, 100)),
                ("🔧", "Repair Selected", "Fix corrupted or problematic installations", Color.FromArgb(255, 150, 50))
            };

            foreach (var (emoji, title, description, color) in packageOps)
            {
                AppendFormattedText(rtb, $"  {emoji} ", Color.White, 12, FontStyle.Regular);
                AppendFormattedText(rtb, title, color, 12, FontStyle.Bold);
                rtb.AppendText("\n");
                AppendFormattedText(rtb, $"    {description}", Color.FromArgb(180, 180, 180), 10, FontStyle.Regular);
                rtb.AppendText("\n\n");
            }

            // AI Features Section
            AppendFormattedText(rtb, "🤖 AI-Powered Features", Color.FromArgb(255, 200, 100), 16, FontStyle.Bold);
            rtb.AppendText("\n\n");

            var aiFeatures = new[]
            {
                ("🧠", "AI Research", "Comprehensive upgrade analysis with security assessment", Color.FromArgb(200, 100, 255)),
                ("🔍", "Dual AI Providers", "Claude AI (knowledge-based) + Perplexity (real-time web research)", Color.FromArgb(150, 200, 255)),
                ("🌐", "Live Web Research", "Current security advisories and compatibility information", Color.FromArgb(100, 255, 200)),
                ("📋", "7-Section Analysis", "Structured reports with executive summary and recommendations", Color.FromArgb(255, 200, 150)),
                ("🛡️", "Security Assessment", "Vulnerability analysis with risk level indicators", Color.FromArgb(255, 100, 100)),
                ("📤", "Markdown Export", "Professional reports ready for documentation and sharing", Color.FromArgb(100, 255, 150))
            };

            foreach (var (emoji, title, description, color) in aiFeatures)
            {
                AppendFormattedText(rtb, $"  {emoji} ", Color.White, 12, FontStyle.Regular);
                AppendFormattedText(rtb, title, color, 12, FontStyle.Bold);
                rtb.AppendText("\n");
                AppendFormattedText(rtb, $"    {description}", Color.FromArgb(180, 180, 180), 10, FontStyle.Regular);
                rtb.AppendText("\n\n");
            }

            // Settings & Configuration
            AppendFormattedText(rtb, "⚙️ Settings & Configuration", Color.FromArgb(255, 200, 100), 16, FontStyle.Bold);
            rtb.AppendText("\n\n");

            var settings = new[]
            {
                ("🎨", "UI Modes", "Simple (basic users) or Advanced (power users with full features)", Color.FromArgb(200, 150, 255)),
                ("🧠", "AI Models", "Claude Sonnet 4, 3.5 Sonnet, 3.5 Haiku, 3 Opus", Color.FromArgb(150, 100, 255)),
                ("📡", "AI Providers", "Switch between Claude (knowledge) and Perplexity (web research)", Color.FromArgb(100, 150, 255)),
                ("🔧", "Package Sources", "winget, msstore, or all sources for comprehensive coverage", Color.FromArgb(100, 200, 255)),
                ("🔐", "API Configuration", "Store Claude and Perplexity API keys in config.json", Color.FromArgb(255, 150, 100)),
                ("📄", "Logging", "Verbose mode for detailed command output and troubleshooting", Color.FromArgb(150, 255, 150))
            };

            foreach (var (emoji, title, description, color) in settings)
            {
                AppendFormattedText(rtb, $"  {emoji} ", Color.White, 12, FontStyle.Regular);
                AppendFormattedText(rtb, title, color, 12, FontStyle.Bold);
                rtb.AppendText("\n");
                AppendFormattedText(rtb, $"    {description}", Color.FromArgb(180, 180, 180), 10, FontStyle.Regular);
                rtb.AppendText("\n\n");
            }

            // Tips & Best Practices
            AppendFormattedText(rtb, "💡 Pro Tips", Color.FromArgb(255, 200, 100), 16, FontStyle.Bold);
            rtb.AppendText("\n\n");

            var tips = new[]
            {
                "🎯 Run 'Check Updates' before using AI Research for best results",
                "✅ Use checkboxes to select multiple packages for batch operations",
                "🔍 Review AI recommendations before upgrading critical software",
                "📊 Export AI reports for documentation and team decision-making",
                "📄 Monitor logs panel for troubleshooting installation issues",
                "🔐 Configure API keys in config.json to unlock AI features",
                "⚡ Use Simple mode for basic operations, Advanced for full power",
                "🛡️ Always test upgrades in non-production environments first"
            };

            foreach (var tip in tips)
            {
                AppendFormattedText(rtb, $"  {tip}", Color.FromArgb(200, 255, 150), 11, FontStyle.Regular);
                rtb.AppendText("\n");
            }

            rtb.AppendText("\n");

            // Footer
            AppendFormattedText(rtb, new string('─', 60), Color.FromArgb(80, 80, 80), 8, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "🏢 Developed by GeekSuave Labs | Mark Relph", Color.FromArgb(150, 150, 150), 9, FontStyle.Italic);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "🧿 WingetWizard v2.0 - Making Package Management Magical!", Color.FromArgb(100, 200, 255), 10, FontStyle.Bold);

            rtb.SelectionStart = 0;
            rtb.ScrollToCaret();
        }
        
        private void ShowAbout(object sender, EventArgs e)
        {
            var about = new Form
            {
                Text = "About WingetWizard",
                Size = new(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var richTextBox = new RichTextBox
            {
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.None,
                Font = new Font("Calibri", 11F),
                Margin = new Padding(20)
            };

            FormatAboutContent(richTextBox);
            about.Controls.Add(richTextBox);
            about.ShowDialog();
        }

        private void FormatAboutContent(RichTextBox rtb)
        {
            rtb.Clear();

            // App Icon and Title
            AppendFormattedText(rtb, "🧿", Color.FromArgb(100, 200, 255), 32, FontStyle.Bold);
            rtb.AppendText("  ");
            AppendFormattedText(rtb, "WingetWizard", Color.FromArgb(100, 200, 255), 24, FontStyle.Bold);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "AI-Enhanced Windows Package Manager", Color.FromArgb(150, 200, 255), 12, FontStyle.Italic);
            rtb.AppendText("\n\n");

            // Version Info
            AppendFormattedText(rtb, "📦 Version: ", Color.FromArgb(150, 150, 150), 11, FontStyle.Regular);
            AppendFormattedText(rtb, "2.0.0", Color.FromArgb(100, 255, 100), 11, FontStyle.Bold);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "🏗️ Framework: ", Color.FromArgb(150, 150, 150), 11, FontStyle.Regular);
            AppendFormattedText(rtb, ".NET 6 Windows Forms", Color.FromArgb(100, 200, 255), 11, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "🤖 AI Powered: ", Color.FromArgb(150, 150, 150), 11, FontStyle.Regular);
            AppendFormattedText(rtb, "Claude + Perplexity", Color.FromArgb(200, 100, 255), 11, FontStyle.Bold);
            rtb.AppendText("\n\n");

            // Features Highlight
            AppendFormattedText(rtb, "✨ Key Features", Color.FromArgb(255, 200, 100), 14, FontStyle.Bold);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "• 🔄 Intelligent package management with winget integration", Color.FromArgb(200, 200, 200), 10, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "• 🤖 AI-powered upgrade recommendations and analysis", Color.FromArgb(200, 200, 200), 10, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "• 🛡️ Security assessment and vulnerability detection", Color.FromArgb(200, 200, 200), 10, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "• 📊 Professional markdown reports for enterprise use", Color.FromArgb(200, 200, 200), 10, FontStyle.Regular);
            rtb.AppendText("\n\n");

            // Copyright
            AppendFormattedText(rtb, "© 2024 ", Color.FromArgb(150, 150, 150), 10, FontStyle.Regular);
            AppendFormattedText(rtb, "GeekSuave Labs", Color.FromArgb(100, 200, 255), 10, FontStyle.Bold);
            AppendFormattedText(rtb, " | ", Color.FromArgb(150, 150, 150), 10, FontStyle.Regular);
            AppendFormattedText(rtb, "Mark Relph", Color.FromArgb(100, 200, 255), 10, FontStyle.Bold);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, "All rights reserved.", Color.FromArgb(150, 150, 150), 9, FontStyle.Italic);
            rtb.AppendText("\n\n");

            // Development Tools
            AppendFormattedText(rtb, "🛠️ Built With: ", Color.FromArgb(150, 150, 150), 10, FontStyle.Regular);
            AppendFormattedText(rtb, "Q Developer", Color.FromArgb(255, 150, 100), 10, FontStyle.Bold);
            AppendFormattedText(rtb, " • ", Color.FromArgb(150, 150, 150), 10, FontStyle.Regular);
            AppendFormattedText(rtb, "Claude AI", Color.FromArgb(200, 100, 255), 10, FontStyle.Bold);
            AppendFormattedText(rtb, " • ", Color.FromArgb(150, 150, 150), 10, FontStyle.Regular);
            AppendFormattedText(rtb, "Cursor", Color.FromArgb(100, 200, 255), 10, FontStyle.Bold);
            rtb.AppendText("\n\n");

            // Magic tagline
            AppendFormattedText(rtb, "✨ Making Package Management Magical! ✨", Color.FromArgb(255, 200, 150), 12, FontStyle.Bold | FontStyle.Italic);

            rtb.SelectionStart = 0;
            rtb.ScrollToCaret();
        }
        
        private void ShowUISettings(object sender, EventArgs e)
        {
            var settings = new Form { Text = "UI Settings", Size = new(300, 150), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, Font = new("Calibri", 11F) };
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

            }
        }
        
        private void ShowAISettings(object sender, EventArgs e)
        {
            var settings = new Form { Text = "AI Settings", Size = new(450, 300), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, Font = new("Calibri", 11F) };
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 2, Padding = new(10) };
            
            var lblProvider = new Label { Text = "AI Provider:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var cmbProvider = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cmbProvider.Items.AddRange(new[] { "Claude", "Perplexity" });
            cmbProvider.SelectedIndex = usePerplexity ? 1 : 0;
            
            var lblModel = new Label { Text = "Model:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var cmbModel = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cmbModel.Items.AddRange(new[] { "claude-sonnet-4-20250514", "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022", "claude-3-opus-20240229" });
            if (cmbModel.Items.Contains(selectedAiModel)) cmbModel.SelectedItem = selectedAiModel;
            else cmbModel.SelectedIndex = 0;
            
            var lblClaude = new Label { Text = "Claude API Key:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var txtClaude = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Text = GetStoredApiKey("AnthropicApiKey") };
            
            var lblPerplexity = new Label { Text = "Perplexity API Key:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
            var txtPerplexity = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Text = GetStoredApiKey("PerplexityApiKey") };
            
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill };
            
            panel.Controls.Add(lblProvider, 0, 0);
            panel.Controls.Add(cmbProvider, 1, 0);
            panel.Controls.Add(lblModel, 0, 1);
            panel.Controls.Add(cmbModel, 1, 1);
            panel.Controls.Add(lblClaude, 0, 2);
            panel.Controls.Add(txtClaude, 1, 2);
            panel.Controls.Add(lblPerplexity, 0, 3);
            panel.Controls.Add(txtPerplexity, 1, 3);
            panel.Controls.Add(btnOK, 0, 4);
            panel.Controls.Add(btnCancel, 1, 4);
            settings.Controls.Add(panel);
            settings.AcceptButton = btnOK;
            settings.CancelButton = btnCancel;
            
            if (settings.ShowDialog() == DialogResult.OK)
            {
                usePerplexity = cmbProvider.SelectedIndex == 1;
                selectedAiModel = cmbModel.SelectedItem?.ToString() ?? selectedAiModel;
                
                if (!string.IsNullOrWhiteSpace(txtClaude.Text))
                {
                    StoreApiKey("AnthropicApiKey", txtClaude.Text);
                    LogMessage($"Claude API key saved");
                }
                if (!string.IsNullOrWhiteSpace(txtPerplexity.Text))
                {
                    StoreApiKey("PerplexityApiKey", txtPerplexity.Text);
                    LogMessage($"Perplexity API key saved");
                }
                    
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

        private async void BtnListAll_Click(object sender, EventArgs e)
        {
            lstApps.Items.Clear();
            upgradableApps.Clear();
            ShowWelcomePanel();
            
            SpinningProgressForm spinningForm = null;
            try
            {
                spinningForm = new SpinningProgressForm("📋 Loading All Apps...");
                spinningForm.CenterOnParent(this);
                spinningForm.Show(this);
                btnListAll.Enabled = false;

            await Task.Run(() =>
            {
                var source = cmbSource.SelectedItem?.ToString() == "all" ? "" : $"--source {cmbSource.SelectedItem}";
                var command = $"winget list {source}{(chkVerbose.Checked ? " --verbose" : "")}";
                LogMessage($"Executing: {command}");
                var output = RunPowerShell(command);
                LogMessage($"Output: {SafeSubstring(output, 500)}");
                var lines = output?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
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
                    if (parts.Length >= 3)
                    {
                        var name = parts[0];
                        var id = parts[1];
                        var version = parts[2];
                        var packageSource = parts.Length > 3 ? parts[3] : "winget";
                        
                        try
                        {
                            if (!this.IsDisposed)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    try
                                    {
                                        if (!this.IsDisposed)
                                        {
                                            var item = new ListViewItem(new[] { name, id, version, "", packageSource, "", "" });
                                            lstApps.Items.Add(item);
                                            
                                            // Hide welcome panel when first item is added
                                            if (lstApps.Items.Count == 1) HideWelcomePanel();
                                        }
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        // Form is being disposed, ignore UI updates
                                    }
                                }));
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Form is being disposed, ignore UI updates
                        }
                    }
                }
            });
            
            if (lstApps.Items.Count == 0)
            {
                MessageBox.Show("No installed apps found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            }
            finally
            {
                spinningForm?.Close();
                spinningForm?.Dispose();
                btnListAll.Enabled = true;
            }
        }

        private async void BtnCheck_Click(object sender, EventArgs e)
        {
            lstApps.Items.Clear();
            upgradableApps.Clear();
            ShowWelcomePanel();
            
            SpinningProgressForm spinningForm = null;
            try
            {
                spinningForm = new SpinningProgressForm("🔄 Checking for Updates...");
                spinningForm.CenterOnParent(this);
                spinningForm.Show(this);
                btnCheck.Enabled = false;

            await Task.Run(() =>
            {
                var source = cmbSource.SelectedItem?.ToString() == "all" ? "" : $"--source {cmbSource.SelectedItem}";
                var command = $"winget upgrade {source}{(chkVerbose.Checked ? " --verbose" : "")}";
                LogMessage($"Executing: {command}");
                var output = RunPowerShell(command);
                LogMessage($"Output: {SafeSubstring(output, 500)}");
                var lines = output?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
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
                        lock (upgradableAppsLock)
                        {
                            upgradableApps.Add(app);
                        }
                        
                        try
                        {
                            if (!this.IsDisposed)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    try
                                    {
                                        if (!this.IsDisposed)
                                        {
                                            var packageSource = "winget";
                                            // Try to find actual source in remaining parts
                                            for (int i = 4; i < parts.Length; i++)
                                            {
                                                if (parts[i].ToLower().Contains("winget") || parts[i].ToLower().Contains("msstore"))
                                                {
                                                    packageSource = parts[i];
                                                    break;
                                                }
                                            }
                                            var item = new ListViewItem(new[] { app.Name, app.Id, app.Version, app.Available, packageSource, "", "" });
                                            item.Tag = app;
                                            lstApps.Items.Add(item);
                                            
                                            // Hide welcome panel when first item is added
                                            if (lstApps.Items.Count == 1) HideWelcomePanel();
                                        }
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        // Form is being disposed, ignore UI updates
                                    }
                                }));
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Form is being disposed, ignore UI updates
                        }
                    }
                }
            });
            
            if (upgradableApps.Count == 0)
            {
                MessageBox.Show("No upgradable apps found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            }
            finally
            {
                spinningForm?.Close();
                spinningForm?.Dispose();
                btnCheck.Enabled = true;
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

            SpinningProgressForm spinningForm = null;
            try
            {
                spinningForm = new SpinningProgressForm("⬆️ Upgrading Packages...");
                spinningForm.CenterOnParent(this);
                spinningForm.Show(this);
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
                    LogMessage($"{app.Name} result: {app.Status} - {SafeSubstring(result, 200)}");
                    
                    try
                    {
                        if (!this.IsDisposed)
                        {
                            this.Invoke(() => {
                                try
                                {
                                    if (!this.IsDisposed)
                                    {
                                        var item = checkedItems.FirstOrDefault(i => i.Tag == app);
                                        if (item?.SubItems.Count > 5) item.SubItems[5].Text = app.Status;
                                    }
                                }
                                catch (ObjectDisposedException)
                                {
                                    // Form is being disposed, ignore UI updates
                                }
                            });
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Form is being disposed, ignore UI updates
                    }
                }
            });

            var successCount = selectedApps.Count(a => a.Status.Contains("Success"));
            MessageBox.Show($"Completed: {successCount}/{selectedApps.Count} apps upgraded successfully.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                spinningForm?.Close();
                spinningForm?.Dispose();
                btnUpgrade.Enabled = true;
            }
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
            
            SpinningProgressForm spinningForm = null;
            try
            {
                spinningForm = new SpinningProgressForm("🚀 Upgrading All Packages...");
                spinningForm.CenterOnParent(this);
                spinningForm.Show(this);
                btnUpgradeAll.Enabled = false;
                
                await Task.Run(() => 
                {
                    var verbose = chkVerbose.Checked ? " --verbose" : "";
                    var command = $"winget upgrade --all --accept-source-agreements --accept-package-agreements --silent{verbose}";
                    LogMessage($"Executing: {command}");
                    var result = RunPowerShell(command);
                    LogMessage($"Upgrade all result: {SafeSubstring(result, 500)}");
                });
            }
            finally
            {
                spinningForm?.Close();
                spinningForm?.Dispose();
                btnUpgradeAll.Enabled = true;
            }
            MessageBox.Show("All apps upgraded.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            BtnCheck_Click(sender, e);
        }
        

        
        private async void BtnInstall_Click(object sender, EventArgs e)
        {
            var checkedItems = lstApps.CheckedItems.Cast<ListViewItem>().ToList();
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Please check apps to install.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var confirm = MessageBox.Show($"Install {checkedItems.Count} selected apps?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            
            btnInstall.Enabled = false;
            
            await Task.Run(() => 
            {
                var verbose = chkVerbose.Checked ? " --verbose" : "";
                foreach (var item in checkedItems)
                {
                    var command = $"winget install --id \"{item.SubItems[1].Text}\" --accept-source-agreements --accept-package-agreements --silent{verbose}";
                    LogMessage($"Installing: {command}");
                    var result = RunPowerShell(command);
                    LogMessage($"Install result: {SafeSubstring(result, 300)}");
                }
            });
            
            btnInstall.Enabled = true;
            MessageBox.Show($"Installation of {checkedItems.Count} apps completed.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private async void BtnRepair_Click(object sender, EventArgs e)
        {
            var checkedItems = lstApps.CheckedItems.Cast<ListViewItem>().ToList();
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Please check apps to repair.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var confirm = MessageBox.Show($"Repair {checkedItems.Count} selected packages?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            
            btnRepair.Enabled = false;
            
            await Task.Run(() =>
            {
                var verbose = chkVerbose.Checked ? " --verbose" : "";
                foreach (var item in checkedItems)
                {
                    var command = $"winget repair --id \"{item.SubItems[1].Text}\" --accept-source-agreements --accept-package-agreements --silent{verbose}";
                    LogMessage($"Repairing: {command}");
                    var result = RunPowerShell(command);
                    LogMessage($"Repair result: {SafeSubstring(result, 300)}");
                }
            });
            
            btnRepair.Enabled = true;
            MessageBox.Show($"Repair of {checkedItems.Count} apps completed.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnUninstall_Click(object sender, EventArgs e)
        {
            var checkedItems = lstApps.CheckedItems.Cast<ListViewItem>().ToList();
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Please check apps to uninstall.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var confirm = MessageBox.Show($"Uninstall {checkedItems.Count} selected packages?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            
            btnUninstall.Enabled = false;
            
            await Task.Run(() =>
            {
                var verbose = chkVerbose.Checked ? " --verbose" : "";
                foreach (var item in checkedItems)
                {
                    var command = $"winget uninstall --id \"{item.SubItems[1].Text}\" --silent{verbose}";
                    LogMessage($"Uninstalling: {command}");
                    var result = RunPowerShell(command);
                    LogMessage($"Uninstall result: {SafeSubstring(result, 300)}");
                }
            });
            
            btnUninstall.Enabled = true;
            MessageBox.Show($"Uninstallation of {checkedItems.Count} apps completed.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private async void BtnResearch_Click(object sender, EventArgs e)
        {
            var checkedItems = lstApps.CheckedItems.Cast<ListViewItem>().ToList();
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Please check apps to research.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            SpinningProgressForm spinningForm = null;
            var recommendations = new List<(UpgradableApp app, string recommendation)>();
            
            try
            {
                // Show spinning popup
                spinningForm = new SpinningProgressForm("🤖 AI Research in Progress...");
                spinningForm.CenterOnParent(this);
                spinningForm.Show(this);
                btnResearch.Enabled = false;
                
                await Task.Run(async () =>
                {
                    var processedCount = 0;
                    foreach (var item in checkedItems)
                    {
                        var app = item.Tag as UpgradableApp;
                        if (app == null)
                        {
                            app = new UpgradableApp
                            {
                                Name = item.SubItems[0].Text,
                                Id = item.SubItems[1].Text,
                                Version = item.SubItems[2].Text,
                                Available = item.SubItems[3].Text
                            };
                        }
                        
                        // Update spinning popup message
                        processedCount++;
                        var statusMessage = $"🧠 Analyzing {app.Name}... ({processedCount}/{checkedItems.Count})";
                        this.Invoke(() => spinningForm?.UpdateMessage(statusMessage));
                        
                        LogMessage($"Researching {app.Name} ({app.Version} -> {app.Available})");
                        var recommendation = await GetAIRecommendation(app);
                        app.Recommendation = recommendation;
                        recommendations.Add((app, recommendation));
                        LogMessage($"AI recommendation for {app.Name}: {recommendation}");
                        
                        try
                        {
                            if (!this.IsDisposed)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    try
                                    {
                                        if (!this.IsDisposed && item?.SubItems.Count > 6)
                                        {
                                            var lines = recommendation?.Split('\n');
                                            var summary = lines?.Length > 0 ? lines[0] : "No summary";
                                            item.SubItems[6].Text = SafeSubstring(summary, 50);
                                        }
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        // Form is being disposed, ignore UI updates
                                    }
                                }));
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Form is being disposed, ignore UI updates
                        }
                    }
                });
            }
            finally
            {
                // Close spinner before showing results
                spinningForm?.Close();
                spinningForm?.Dispose();
                btnResearch.Enabled = true;
            }
            
            // Show results popup after spinner is closed
            ShowRecommendationsPopup(recommendations);
        }
        
        private async Task<string> GetAIRecommendation(UpgradableApp app)
        {
            return usePerplexity ? await GetPerplexityRecommendation(app) : await GetClaudeRecommendation(app);
        }
        
        private async Task<string> GetClaudeRecommendation(UpgradableApp app)
        {
            var apiKey = GetStoredApiKey("AnthropicApiKey");
            if (string.IsNullOrEmpty(apiKey)) return "";
            
            var requestBody = new
            {
                model = selectedAiModel,
                max_tokens = 2500,
                messages = new[] { new { role = "user", content = CreateSoftwareResearchPrompt(app.Name, app.Id, app.Version, app.Available) } }
            };
            
            var headers = new Dictionary<string, string>
            {
                ["x-api-key"] = apiKey,
                ["anthropic-version"] = "2023-06-01"
            };
            
            return await MakeApiRequest("https://api.anthropic.com/v1/messages", requestBody, headers, 
                result => result.GetProperty("content")[0].GetProperty("text").GetString() ?? "No recommendation available",
                "Claude");
        }
        
        private async Task<string> GetPerplexityRecommendation(UpgradableApp app)
        {
            var apiKey = GetStoredApiKey("PerplexityApiKey");
            if (string.IsNullOrEmpty(apiKey)) return "";
            
            var requestBody = new
            {
                model = "sonar",
                messages = new object[]
                {
                    new { role = "system", content = "Be precise and concise. Focus on actionable software upgrade insights." },
                    new { role = "user", content = CreateSoftwareResearchPrompt(app.Name, app.Id, app.Version, app.Available) }
                },
                max_tokens = 2500,
                temperature = 0.2
            };
            
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {apiKey}"
            };
            
            return await MakeApiRequest("https://api.perplexity.ai/chat/completions", requestBody, headers,
                result => result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No recommendation available",
                "Perplexity");
        }
        
        private async Task<string> MakeApiRequest(string url, object requestBody, Dictionary<string, string> headers, 
            Func<JsonElement, string> responseParser, string providerName)
        {
            try
            {
                await httpSemaphore.WaitAsync();
                try
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    foreach (var header in headers)
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    
                    var response = await httpClient.PostAsync(url, 
                        new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                        return responseParser(result);
                    }
                    
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"{providerName} API Error {response.StatusCode}: {errorContent}");
                    return $"{providerName} API Error: {response.StatusCode}";
                }
                finally
                {
                    httpSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"{providerName} API error: {ex.Message}");
                return $"{providerName} research failed";
            }
        }
        
        private string GetStoredApiKey(string keyName)
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                LogMessage($"Looking for {keyName} in {settingsPath}");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath, Encoding.UTF8);
                    LogMessage($"Settings file content: {json}");
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    var hasKey = settings?.ContainsKey(keyName) == true;
                    var keyValue = hasKey ? settings[keyName]?.ToString() ?? "" : "";
                    LogMessage($"Key {keyName} found: {hasKey}, value length: {keyValue.Length}");
                    return keyValue;
                }
                else
                {
                    LogMessage($"Settings file does not exist: {settingsPath}");
                }
            }
            catch (Exception ex) { LogMessage($"Error loading {keyName}: {ex.Message}"); }
            return "";
        }
        
        private void StoreApiKey(string keyName, string value)
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                LogMessage($"Storing {keyName} to {settingsPath}, value length: {value.Length}");
                
                var settings = new Dictionary<string, object>
                {
                    [keyName] = value,
                    ["isAdvancedMode"] = isAdvancedMode,
                    ["selectedAiModel"] = selectedAiModel,
                    ["usePerplexity"] = usePerplexity
                };
                
                // Load existing settings to preserve other keys
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath, Encoding.UTF8);
                    var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
                    foreach (var kvp in existing)
                    {
                        if (!settings.ContainsKey(kvp.Key))
                        {
                            settings[kvp.Key] = kvp.Value;
                        }
                    }
                }
                
                var finalJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, finalJson, Encoding.UTF8);
                LogMessage($"Successfully wrote settings file with {settings.Count} keys");
            }
            catch (Exception ex) { LogMessage($"Error storing {keyName}: {ex.Message}"); }
        }
        
        private static string CreateSoftwareResearchPrompt(string softwareName, string packageId, string currentVersion, string newVersion)
        {
            return $@"# 🔍 Software Upgrade Research: {softwareName}

You are a senior software analyst providing comprehensive upgrade recommendations. Research the upgrade from **{currentVersion}** to **{newVersion}** for package `{packageId}`.

## 📋 Required Analysis Format

Provide your analysis in the following **exact markdown structure** with emojis and color indicators:

### 🎯 **Executive Summary**
> 🟢 RECOMMENDED / 🟡 CONDITIONAL / 🔴 NOT RECOMMENDED

Brief 1-2 sentence recommendation with urgency level.

### 🔄 **Version Changes**
- **Current Version**: `{currentVersion}`
- **Target Version**: `{newVersion}`
- **Update Type**: 🔵 Major / 🟡 Minor / 🟢 Patch / 🔴 Breaking
- **Release Date**: [Date if available]

### ⚡ **Key Improvements**
- 🆕 **New Features**: List major new functionality
- 🐛 **Bug Fixes**: Critical issues resolved
- 🔧 **Enhancements**: Performance and usability improvements
- 📊 **Performance**: Speed/resource impact changes

### 🔒 **Security Assessment**
- 🛡️ **Security Fixes**: List any CVE fixes or security patches
- 🚨 **Vulnerability Status**: Current security standing
- 🔐 **Risk Level**: 🟢 Low / 🟡 Medium / 🔴 High / 🟣 Critical

### ⚠️ **Compatibility & Risks**
- 💥 **Breaking Changes**: List any breaking changes
- 🔗 **Dependencies**: New requirements or conflicts
- 🖥️ **System Requirements**: Hardware/OS compatibility
- 🔄 **Migration Effort**: 🟢 None / 🟡 Minor / 🔴 Significant

### 📅 **Recommendation Timeline**
- 🚀 **Immediate** (Security/Critical)
- 📆 **Within 1 week** (Important updates)
- 🗓️ **Within 1 month** (Regular updates)
- ⏳ **When convenient** (Optional updates)

### 🎯 **Action Items**
- [ ] **Pre-upgrade**: Backup/preparation steps
- [ ] **During upgrade**: Installation considerations
- [ ] **Post-upgrade**: Verification and testing
- [ ] **Rollback plan**: If issues occur

---
💡 **Pro Tip**: Include any relevant links to release notes, documentation, or known issues.

**Important**: Use exact emoji indicators, maintain consistent formatting, and provide actionable insights. Focus on enterprise-grade decision making with clear visual hierarchy using colors and emojis.";
        }

        private string RunPowerShell(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return "Command is null or empty";
            var validCommands = new[] { "winget list", "winget upgrade", "winget install", "winget uninstall", "winget repair" };
            if (!validCommands.Any(cmd => command.TrimStart().StartsWith(cmd, StringComparison.OrdinalIgnoreCase)))
                return "Invalid command format";
            var psi = new ProcessStartInfo { FileName = "powershell.exe", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add("-Command"); psi.ArgumentList.Add(command);
            using var process = Process.Start(psi);
            return process?.StandardOutput.ReadToEnd() ?? "Process failed";
        }
        
        private void LogMessage(string message) 
        {
            if (txtLogs?.IsDisposed == false)
            {
                if (txtLogs.InvokeRequired)
                {
                    txtLogs.Invoke(() => {
                        txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                        txtLogs.ScrollToCaret();
                    });
                }
                else
                {
                    txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                    txtLogs.ScrollToCaret();
                }
            }
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
            var validRecommendations = recommendations.Where(r => !string.IsNullOrEmpty(r.recommendation)).ToList();
            if (validRecommendations.Count == 0) 
            {
                MessageBox.Show("No AI recommendations available. Please configure API keys in Settings.", "No Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var popup = new Form 
            {
                Text = "🤖 AI Research Results - WingetWizard",
                Size = new Size(1000, 700),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                MinimumSize = new Size(800, 600)
            };
            
            // Create a panel for buttons at the bottom
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };
            
            var btnExport = new Button
            {
                Text = "📤 Export as Markdown",
                Size = new Size(150, 30),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Calibri", 9F, FontStyle.Bold)
            };
            btnExport.FlatAppearance.BorderSize = 0;
            
            var btnClose = new Button
            {
                Text = "❌ Close",
                Size = new Size(100, 30),
                Location = new Point(170, 10),
                BackColor = Color.FromArgb(196, 43, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Calibri", 9F, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnClose.FlatAppearance.BorderSize = 0;
            
            buttonPanel.Controls.Add(btnExport);
            buttonPanel.Controls.Add(btnClose);
            
            var richTextBox = new RichTextBox 
            {
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Calibri", 12F),
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            
            var content = CreateMarkdownContent(validRecommendations);
            FormatRichTextContent(richTextBox, validRecommendations);
            
            // Export button click handler
            btnExport.Click += (s, e) => ExportMarkdownReport(content, validRecommendations.Count);
            
            popup.Controls.Add(richTextBox);
            popup.Controls.Add(buttonPanel);
            popup.AcceptButton = btnClose;
            popup.ShowDialog();
        }
        
        private string CreateMarkdownContent(List<(UpgradableApp app, string recommendation)> recommendations)
        {
            var content = new StringBuilder();
            
            // Header with metadata
            content.AppendLine("# 🧿 WingetWizard AI Research Report");
            content.AppendLine();
            content.AppendLine("---");
            content.AppendLine();
            content.AppendLine("## 📊 **Report Metadata**");
            content.AppendLine();
            content.AppendLine($"- **🕒 Generated**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"- **📦 Packages Analyzed**: {recommendations.Count}");
            content.AppendLine($"- **🤖 AI Provider**: {(usePerplexity ? "Perplexity Sonar" : $"Claude {selectedAiModel}")}");
            content.AppendLine($"- **⚙️ Tool**: WingetWizard v2.0 by GeekSuave Labs");
            content.AppendLine();
            content.AppendLine("---");
            content.AppendLine();
            
            // Summary section
            content.AppendLine("## 🎯 **Executive Summary**");
            content.AppendLine();
            var recommendedCount = 0;
            var conditionalCount = 0;
            var notRecommendedCount = 0;
            
            foreach (var (app, recommendation) in recommendations)
            {
                if (recommendation.Contains("🟢 RECOMMENDED") && !recommendation.Contains("🟡") && !recommendation.Contains("🔴"))
                    recommendedCount++;
                else if (recommendation.Contains("🟡 CONDITIONAL"))
                    conditionalCount++;
                else if (recommendation.Contains("🔴 NOT RECOMMENDED"))
                    notRecommendedCount++;
            }
            
            content.AppendLine($"- 🟢 **Recommended Updates**: {recommendedCount}");
            content.AppendLine($"- 🟡 **Conditional Updates**: {conditionalCount}");
            content.AppendLine($"- 🔴 **Not Recommended**: {notRecommendedCount}");
            content.AppendLine();
            content.AppendLine("---");
            content.AppendLine();
            
            // Individual package analyses
            content.AppendLine("## 📦 **Package Analysis**");
            content.AppendLine();
            
            foreach (var (app, recommendation) in recommendations)
            {
                content.AppendLine($"### 🔍 **{app.Name}**");
                content.AppendLine();
                content.AppendLine($"**📋 Package Details**");
                content.AppendLine($"- **Package ID**: `{app.Id}`");
                content.AppendLine($"- **Current Version**: `{app.Version}`");
                content.AppendLine($"- **Available Version**: `{app.Available}`");
                content.AppendLine($"- **Analysis Date**: {DateTime.Now:yyyy-MM-dd}");
                content.AppendLine();
                
                // Add the AI recommendation
                content.AppendLine("**🤖 AI Analysis**");
                content.AppendLine();
                content.AppendLine(recommendation);
                content.AppendLine();
                content.AppendLine("---");
                content.AppendLine();
            }
            
            // Footer
            content.AppendLine("## 📄 **Report Footer**");
            content.AppendLine();
            content.AppendLine("> 💡 **Disclaimer**: This report is generated by AI analysis and should be reviewed by qualified IT personnel before implementing upgrades.");
            content.AppendLine(">");
            content.AppendLine("> 🔒 **Security Note**: Always verify security updates through official channels and test in non-production environments first.");
            content.AppendLine(">");
            content.AppendLine($"> 🧿 **Generated by**: WingetWizard v2.0 - AI-Enhanced Package Management Tool");
            content.AppendLine($"> 🏢 **Developed by**: GeekSuave Labs | Mark Relph");
            content.AppendLine($"> 📅 **Report Date**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            return content.ToString();
        }
        
        private void FormatRichTextContent(RichTextBox rtb, List<(UpgradableApp app, string recommendation)> recommendations)
        {
            rtb.Clear();
            
            // Header
            AppendFormattedText(rtb, "🧿 WingetWizard AI Research Report", Color.FromArgb(100, 200, 255), 16, FontStyle.Bold);
            rtb.AppendText("\n\n");
            
            // Metadata section
            AppendFormattedText(rtb, "📊 Report Metadata", Color.FromArgb(255, 200, 100), 14, FontStyle.Bold);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, $"🕒 Generated: ", Color.FromArgb(150, 150, 150), 10, FontStyle.Regular);
            AppendFormattedText(rtb, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}", Color.White, 10, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, $"📦 Packages: ", Color.FromArgb(150, 150, 150), 10, FontStyle.Regular);
            AppendFormattedText(rtb, $"{recommendations.Count}", Color.White, 10, FontStyle.Regular);
            rtb.AppendText("\n");
            AppendFormattedText(rtb, $"🤖 AI Provider: ", Color.FromArgb(150, 150, 150), 10, FontStyle.Regular);
            AppendFormattedText(rtb, $"{(usePerplexity ? "Perplexity Sonar" : $"Claude {selectedAiModel}")}", Color.White, 10, FontStyle.Regular);
            rtb.AppendText("\n\n");
            
            // Summary counts
            AppendFormattedText(rtb, "🎯 Executive Summary", Color.FromArgb(255, 200, 100), 14, FontStyle.Bold);
            rtb.AppendText("\n");
            
            var recommendedCount = 0;
            var conditionalCount = 0;
            var notRecommendedCount = 0;
            
            foreach (var (app, recommendation) in recommendations)
            {
                if (recommendation.Contains("🟢 RECOMMENDED") && !recommendation.Contains("🟡") && !recommendation.Contains("🔴"))
                    recommendedCount++;
                else if (recommendation.Contains("🟡 CONDITIONAL"))
                    conditionalCount++;
                else if (recommendation.Contains("🔴 NOT RECOMMENDED"))
                    notRecommendedCount++;
            }
            
            AppendFormattedText(rtb, "🟢 Recommended: ", Color.FromArgb(100, 255, 100), 10, FontStyle.Bold);
            AppendFormattedText(rtb, $"{recommendedCount}", Color.White, 10, FontStyle.Regular);
            rtb.AppendText("  ");
            AppendFormattedText(rtb, "🟡 Conditional: ", Color.FromArgb(255, 255, 100), 10, FontStyle.Bold);
            AppendFormattedText(rtb, $"{conditionalCount}", Color.White, 10, FontStyle.Regular);
            rtb.AppendText("  ");
            AppendFormattedText(rtb, "🔴 Not Recommended: ", Color.FromArgb(255, 100, 100), 10, FontStyle.Bold);
            AppendFormattedText(rtb, $"{notRecommendedCount}", Color.White, 10, FontStyle.Regular);
            rtb.AppendText("\n\n");
            
            // Individual package analyses
            AppendFormattedText(rtb, "📦 Package Analysis", Color.FromArgb(255, 200, 100), 14, FontStyle.Bold);
            rtb.AppendText("\n\n");
            
            foreach (var (app, recommendation) in recommendations)
            {
                // Package name header
                AppendFormattedText(rtb, $"🔍 {app.Name}", Color.FromArgb(150, 200, 255), 13, FontStyle.Bold);
                rtb.AppendText("\n");
                
                // Package details
                AppendFormattedText(rtb, "Package ID: ", Color.FromArgb(150, 150, 150), 9, FontStyle.Regular);
                AppendFormattedText(rtb, $"{app.Id}", Color.FromArgb(200, 200, 200), 9, FontStyle.Regular);
                rtb.AppendText("\n");
                AppendFormattedText(rtb, "Upgrade: ", Color.FromArgb(150, 150, 150), 9, FontStyle.Regular);
                AppendFormattedText(rtb, $"{app.Version}", Color.FromArgb(255, 150, 150), 9, FontStyle.Bold);
                AppendFormattedText(rtb, " → ", Color.White, 9, FontStyle.Regular);
                AppendFormattedText(rtb, $"{app.Available}", Color.FromArgb(150, 255, 150), 9, FontStyle.Bold);
                rtb.AppendText("\n\n");
                
                // AI recommendation with better formatting
                AppendFormattedText(rtb, "🤖 AI Analysis:", Color.FromArgb(200, 150, 255), 11, FontStyle.Bold);
                rtb.AppendText("\n");
                FormatAIRecommendation(rtb, recommendation);
                rtb.AppendText("\n");
                
                // Separator
                AppendFormattedText(rtb, new string('─', 60), Color.FromArgb(80, 80, 80), 8, FontStyle.Regular);
                rtb.AppendText("\n\n");
            }
            
            rtb.SelectionStart = 0;
            rtb.ScrollToCaret();
        }
        
        private void AppendFormattedText(RichTextBox rtb, string text, Color color, float fontSize, FontStyle style)
        {
            int start = rtb.TextLength;
            rtb.AppendText(text);
            rtb.Select(start, text.Length);
            rtb.SelectionColor = color;
            rtb.SelectionFont = new Font("Calibri", fontSize, style);
            rtb.Select(rtb.TextLength, 0);
        }
        
        private void FormatAIRecommendation(RichTextBox rtb, string recommendation)
        {
            var lines = recommendation.Split('\n');
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    rtb.AppendText("\n");
                    continue;
                }
                
                // Format headers (lines starting with ###, ##, #)
                if (line.TrimStart().StartsWith("### "))
                {
                    AppendFormattedText(rtb, line, Color.FromArgb(150, 200, 255), 11, FontStyle.Bold);
                }
                else if (line.TrimStart().StartsWith("## "))
                {
                    AppendFormattedText(rtb, line, Color.FromArgb(255, 200, 100), 12, FontStyle.Bold);
                }
                else if (line.TrimStart().StartsWith("# "))
                {
                    AppendFormattedText(rtb, line, Color.FromArgb(100, 200, 255), 13, FontStyle.Bold);
                }
                // Format bullet points
                else if (line.TrimStart().StartsWith("- "))
                {
                    var trimmed = line.TrimStart();
                    var indent = line.Length - trimmed.Length;
                    rtb.AppendText(new string(' ', indent));
                    
                    // Color code based on emoji indicators
                    if (trimmed.Contains("🟢"))
                        AppendFormattedText(rtb, trimmed, Color.FromArgb(100, 255, 100), 9, FontStyle.Regular);
                    else if (trimmed.Contains("🟡"))
                        AppendFormattedText(rtb, trimmed, Color.FromArgb(255, 255, 100), 9, FontStyle.Regular);
                    else if (trimmed.Contains("🔴"))
                        AppendFormattedText(rtb, trimmed, Color.FromArgb(255, 100, 100), 9, FontStyle.Regular);
                    else if (trimmed.Contains("🟣"))
                        AppendFormattedText(rtb, trimmed, Color.FromArgb(200, 100, 255), 9, FontStyle.Regular);
                    else
                        AppendFormattedText(rtb, trimmed, Color.FromArgb(200, 200, 200), 9, FontStyle.Regular);
                }
                // Format blockquotes
                else if (line.TrimStart().StartsWith("> "))
                {
                    AppendFormattedText(rtb, line, Color.FromArgb(150, 255, 150), 9, FontStyle.Italic);
                }
                // Format bold text indicators
                else if (line.Contains("**") && line.Count(c => c == '*') >= 4)
                {
                    FormatBoldText(rtb, line);
                }
                // Regular text
                else
                {
                    AppendFormattedText(rtb, line, Color.FromArgb(220, 220, 220), 9, FontStyle.Regular);
                }
                
                rtb.AppendText("\n");
            }
        }
        
        private void FormatBoldText(RichTextBox rtb, string line)
        {
            var parts = line.Split(new[] { "**" }, StringSplitOptions.None);
            bool isBold = false;
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                
                if (isBold)
                    AppendFormattedText(rtb, part, Color.White, 9, FontStyle.Bold);
                else
                    AppendFormattedText(rtb, part, Color.FromArgb(220, 220, 220), 9, FontStyle.Regular);
                
                isBold = !isBold;
            }
        }

        private void ExportMarkdownReport(string content, int packageCount)
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                    DefaultExt = "md",
                    FileName = $"WingetWizard_AI_Research_{DateTime.Now:yyyyMMdd_HHmmss}_{packageCount}packages.md"
                };
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, content, Encoding.UTF8);
                    MessageBox.Show($"✅ AI Research report exported successfully!\n\nFile: {saveDialog.FileName}\nSize: {content.Length:N0} characters", 
                                  "📤 Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Ask if user wants to open the file
                    if (MessageBox.Show("🔍 Would you like to open the exported file?", "Open File", 
                                      MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Failed to open exported file: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Export error: {ex.Message}");
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing) { httpClient?.Dispose(); httpSemaphore?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
