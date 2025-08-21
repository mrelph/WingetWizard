using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UpgradeApp.Models;
using UpgradeApp.Services;
using UpgradeApp.UI;
using UpgradeApp.Utils;

namespace UpgradeApp
{
    /// <summary>
    /// Main application entry point for WingetWizard
    /// Initializes Windows Forms with modern visual styles
    /// </summary>
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

    /// <summary>
    /// Main application form featuring Claude AI-inspired interface design.
    /// Combines modern Windows Forms UI with sophisticated Claude-style aesthetics,
    /// enhanced AI integration, and comprehensive package management functionality.
    /// 
    /// Key Features:
    /// - Claude-inspired color palette and typography
    /// - Personalized welcome screens with time-based greetings
    /// - Spinning progress indicators with animated WingetWizard logo
    /// - Enhanced AI prompting with structured 7-section analysis
    /// - Rich text rendering with color-coded recommendations
    /// - Professional markdown export with metadata and executive summaries
    /// - Thread-safe operations with service-based architecture
    /// </summary>
    public class MainForm : Form, IDisposable
    {
        // Windows API for dark mode title bar
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        // UI Controls - Modern button layout with Claude-inspired card design
        private Button btnCheck = null!;
        private Button btnUpgrade = null!;
        private Button btnUpgradeAll = null!;
        private Button btnInstall = null!;
        private Button btnUninstall = null!;
        private Button btnResearch = null!;
        private Button btnLogs = null!;
        private Button btnExport = null!;
        private Button btnHelp = null!;
        private Button btnSettings = null!;
        private Button btnListAll = null!;
        private Button btnRepair = null!;
        private TextBox txtLogs = null!;          // Logging output with green terminal styling
        private ListView lstApps = null!;         // Package list with enhanced visualization
        private ComboBox cmbSource = null!;       // Source selection (winget, msstore, all)
        
        // In-UI progress indicator
        private ProgressBar progressBar = null!;
        private Label statusLabel = null!;

        private SplitContainer splitter = null!;  // Resizable layout with hidden-by-default logs
        private ToolTip buttonToolTips = null!;   // Tooltips for buttons when window is scaled down
        
        // Service layer - Business logic separated from UI
        private readonly PackageService _packageService;
        private AIService _aiService;
        private readonly ReportService _reportService;
        private readonly SettingsService _settingsService;
        
        // Thread-safe data management
        private readonly List<UpgradableApp> upgradableApps = new();  // Package inventory
        private readonly object upgradableAppsLock = new();           // Thread synchronization
        
        // Configuration settings
        private bool isAdvancedMode = true;                            // UI complexity mode
        private string selectedAiModel = "claude-sonnet-4-20250514";   // Claude model selection
        private bool verboseLogging = false;                           // Verbose logging setting
        private bool isDarkMode = true;                                // OS theme detection

        /// <summary>
        /// Creates modern typography with intelligent font fallback system.
        /// Prioritizes Calibri for Claude-inspired aesthetics, with Segoe UI and system fallbacks.
        /// Ensures consistent, readable typography across different Windows environments.
        /// </summary>
        /// <param name="size">Font size in points</param>
        /// <param name="style">Font style (Regular, Bold, Italic, etc.)</param>
        /// <returns>Font instance with best available modern typeface</returns>
        private static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
        {
            try
            {
                return new Font("Calibri", size, style);  // Primary: Modern Calibri (Claude-inspired)
            }
            catch
            {
                try
                {
                    return new Font("Segoe UI", size, style);  // Secondary: Segoe UI (Windows standard)
                }
                catch
                {
                    return new Font(FontFamily.GenericSansSerif, size, style);  // Fallback: System default
                }
            }
        }

        /// <summary>
        /// Creates the Claude-inspired welcome panel with personalized greeting and action cards.
        /// Features time-based greetings, sophisticated color scheme, and interactive action suggestions.
        /// </summary>
        /// <returns>A fully configured welcome panel with greeting and action cards</returns>
        private Panel CreateWelcomePanel()
        {
            var welcomePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = GetThemeColor(Color.FromArgb(15, 15, 15), Color.White),
                Visible = true
            };

            // Get time-based greeting for personalized experience
            var hour = DateTime.Now.Hour;
            var greeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
            var userName = Environment.UserName;

            // Main greeting label with WingetWizard logo and personalized message
            var greetingLabel = new Label
            {
                Text = $"🧿 {greeting}, {userName}",
                Font = CreateFont(28F, FontStyle.Bold),
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Subtitle with CLI-style helpful tone
            var subtitleLabel = new Label
            {
                Text = "Ready to manage your packages? Choose an action below:",
                Font = CreateFont(14F, FontStyle.Regular),
                ForeColor = GetThemeColor(Color.FromArgb(180, 180, 180), Color.FromArgb(100, 100, 100)),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Action suggestions panel - Modern card-based layout
            var actionsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(20)
            };

            var actionCards = new[]
            {
                ("🔄 Check Updates", "Scan for available package updates", "Start here to see what's new", Color.FromArgb(55, 125, 255)),
                ("🤖 AI Research", "Get intelligent upgrade recommendations", "AI-powered analysis and insights", Color.FromArgb(147, 51, 234)),
                ("📋 List All Apps", "View your complete software inventory", "See everything installed", Color.FromArgb(107, 114, 128)),
                ("📤 Export", "Save package information and reports", "Backup and share your data", Color.FromArgb(251, 146, 60)),
                ("🚀 Quick Start", "Begin with recommended actions", "Let AI guide your journey", Color.FromArgb(34, 197, 94))
            };

            foreach (var (title, description, subtitle, color) in actionCards)
            {
                var card = CreateModernCard(title, description, subtitle, color);
                actionsPanel.Controls.Add(card);
            }

            // Add a fun CLI-style status bar
            var statusBar = new Panel
            {
                Height = 30,
                Dock = DockStyle.Bottom,
                BackColor = GetThemeColor(Color.FromArgb(25, 25, 25), Color.FromArgb(240, 240, 240))
            };

            var statusLabel = new Label
            {
                Text = $"Ready • {DateTime.Now:HH:mm:ss} • WingetWizard v2.1",
                Font = CreateFont(10F, FontStyle.Regular),
                ForeColor = GetThemeColor(Color.FromArgb(120, 120, 120), Color.FromArgb(100, 100, 100)),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 0, 0, 0)
            };

            statusBar.Controls.Add(statusLabel);

            // Position everything centered
            var centerPanel = new Panel
            {
                AutoSize = true,
                Anchor = AnchorStyles.None
            };

            greetingLabel.Location = new Point(0, 0);
            subtitleLabel.Location = new Point(0, 50);
            actionsPanel.Location = new Point(0, 100);

            centerPanel.Controls.Add(greetingLabel);
            centerPanel.Controls.Add(subtitleLabel);
            centerPanel.Controls.Add(actionsPanel);

            // Center the content
            centerPanel.Location = new Point(
                (welcomePanel.Width - centerPanel.Width) / 2,
                (welcomePanel.Height - centerPanel.Height) / 2 - 50
            );

            welcomePanel.Controls.Add(centerPanel);
            welcomePanel.Controls.Add(statusBar);

            // Handle resize to keep content centered
            welcomePanel.Resize += (s, e) =>
            {
                centerPanel.Location = new Point(
                    (welcomePanel.Width - centerPanel.Width) / 2,
                    (welcomePanel.Height - centerPanel.Height) / 2 - 50
                );
            };

            return welcomePanel;
        }

        private Panel CreateModernCard(string title, string description, string subtitle, Color accentColor)
        {
            var card = new Panel
            {
                Width = 200,
                Height = 120,
                BackColor = GetThemeColor(Color.FromArgb(25, 25, 25), Color.FromArgb(250, 250, 250)),
                Margin = new Padding(10),
                Cursor = Cursors.Hand,
                Tag = title // Store the action for potential click handling
            };

            // Add subtle border
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(40, 40, 40), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // Title with accent color
            var cardTitle = new Label
            {
                Text = title,
                Font = CreateFont(12F, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(15, 15),
                AutoSize = true
            };

            // Main description
            var cardDesc = new Label
            {
                Text = description,
                Font = CreateFont(10F, FontStyle.Regular),
                ForeColor = GetThemeColor(Color.FromArgb(200, 200, 200), Color.FromArgb(80, 80, 80)),
                Location = new Point(15, 40),
                Size = new Size(170, 30)
            };

            // Subtitle in smaller font
            var cardSubtitle = new Label
            {
                Text = subtitle,
                Font = CreateFont(9F, FontStyle.Italic),
                ForeColor = GetThemeColor(Color.FromArgb(140, 140, 140), Color.FromArgb(120, 120, 120)),
                Location = new Point(15, 75),
                Size = new Size(170, 20)
            };

            // Add hover effect
            var originalCardColor = GetThemeColor(Color.FromArgb(25, 25, 25), Color.FromArgb(250, 250, 250));
            var hoverCardColor = GetThemeColor(Color.FromArgb(35, 35, 35), Color.FromArgb(240, 240, 240));
            
            card.MouseEnter += (s, e) =>
            {
                card.BackColor = hoverCardColor;
                cardTitle.ForeColor = Color.FromArgb(
                    Math.Min(255, accentColor.R + 30),
                    Math.Min(255, accentColor.G + 30),
                    Math.Min(255, accentColor.B + 30));
            };

            card.MouseLeave += (s, e) =>
            {
                card.BackColor = originalCardColor;
                cardTitle.ForeColor = accentColor;
            };

            card.Controls.Add(cardTitle);
            card.Controls.Add(cardDesc);
            card.Controls.Add(cardSubtitle);

            return card;
        }

        private void HideWelcomePanel()
        {
            var welcomePanel = splitter?.Panel1?.Controls?.OfType<Panel>()?.FirstOrDefault(p => p.Tag?.ToString() == "welcome");
            if (welcomePanel != null)
                welcomePanel.Visible = false;
        }

        private void ShowWelcomePanel()
        {
            var welcomePanel = splitter?.Panel1?.Controls?.OfType<Panel>()?.FirstOrDefault(p => p.Tag?.ToString() == "welcome");
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

        /// <summary>
        /// Initializes the main WingetWizard form with Claude AI-inspired interface.
        /// Sets up comprehensive package management UI with modern aesthetics and enhanced functionality.
        /// </summary>
        public MainForm()
        {
            // Initialize services
            _settingsService = new SettingsService();
            _packageService = new PackageService();
            _reportService = new ReportService(Path.Combine(Application.StartupPath, "AI_Reports"));
            
            // Load settings
            LoadSettings();
            
            // Initialize AI service with current settings
            _aiService = new AIService(
                _settingsService.GetApiKey("AnthropicApiKey"),
                _settingsService.GetApiKey("PerplexityApiKey"),
                selectedAiModel,
                true // Always use two-stage process
            );
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "WingetWizard - AI-Enhanced Package Manager";
            this.Size = new Size(1000, 700); // Increased size for better modern feel
            this.MinimumSize = new Size(900, 600);
            this.Font = new Font("Segoe UI", 11F); // Modern system font
            this.StartPosition = FormStartPosition.CenterScreen;
            try { this.Icon = new Icon("Logo.ico"); } 
            catch (Exception ex) { LogMessage($"Icon load failed: {ex.Message}"); }
            
            // Initialize tooltips for better usability when window is scaled down
            buttonToolTips = new ToolTip()
            {
                AutoPopDelay = 5000,    // Show for 5 seconds
                InitialDelay = 1000,    // Wait 1 second before showing
                ReshowDelay = 500,      // Quick reshow when moving between controls
                ShowAlways = true       // Show even when form is not active
            };
            ApplySystemTheme();

            // Modern header with app title and version
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = GetThemeColor(Color.FromArgb(15, 15, 15), Color.FromArgb(240, 240, 240))
            };

            var headerLabel = new Label
            {
                Text = "🧿 WingetWizard",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(25, 0, 0, 0)
            };

            var versionLabel = new Label
            {
                Text = "AI-Enhanced Package Manager",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = GetThemeColor(Color.FromArgb(140, 140, 140), Color.FromArgb(100, 100, 100)),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 0, 25, 0),
                AutoSize = false,
                Width = 250
            };

            headerPanel.Controls.Add(headerLabel);
            headerPanel.Controls.Add(versionLabel);
            
            // Add progress indicator panel
            var progressPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = GetThemeColor(Color.FromArgb(20, 20, 20), Color.FromArgb(235, 235, 235)),
                Visible = false
            };
            
            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Height = 4,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(100, 200, 255)
            };
            
            statusLabel = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(25, 5, 0, 0)
            };
            
            progressPanel.Controls.Add(progressBar);
            progressPanel.Controls.Add(statusLabel);
            progressPanel.Tag = "progress";

            var topPanel = new TableLayoutPanel { 
                Dock = DockStyle.Top, Height = 140, ColumnCount = 9, RowCount = 2, 
                Padding = new Padding(25), BackColor = GetThemeColor(Color.FromArgb(20, 20, 20), Color.FromArgb(245, 245, 245))
            };
            float[] colWidths = { 11F, 11F, 11F, 11F, 11F, 12F, 11F, 11F, 11F };
            float[] rowHeights = { 55F, 55F };
            for (int i = 0; i < 9; i++) topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colWidths[i]));
            for (int i = 0; i < 2; i++) topPanel.RowStyles.Add(new RowStyle(SizeType.Percent, rowHeights[i]));
            
            // Modern vibrant color palette inspired by contemporary apps
            var primaryBlue = Color.FromArgb(59, 130, 246);      // Modern blue
            var successGreen = Color.FromArgb(34, 197, 94);      // Vibrant green
            var accentOrange = Color.FromArgb(249, 115, 22);     // Modern orange
            var warningAmber = Color.FromArgb(245, 158, 11);     // Warm amber
            var purpleAI = Color.FromArgb(147, 51, 234);         // AI purple
            var neutralGray = Color.FromArgb(107, 114, 128);     // Sophisticated gray
            var darkBlue = Color.FromArgb(30, 58, 138);          // Deep blue
            var darkGreen = Color.FromArgb(20, 83, 45);          // Forest green
            var crimsonRed = Color.FromArgb(239, 68, 68);        // Modern red

            (btnCheck, btnUpgrade, btnUpgradeAll, btnListAll, btnResearch, btnLogs, btnExport, btnHelp, btnSettings) = 
                (CreateButton("🔄 Check Updates", primaryBlue, "Check for available package updates"),
                 CreateButton("⬆️ Upgrade Selected", successGreen, "Upgrade only the selected packages"),
                 CreateButton("🚀 Upgrade All", darkGreen, "Upgrade all available packages at once"),
                 CreateButton("📋 List All Apps", neutralGray, "Show all installed applications"),
                 CreateButton("🤖 AI Research", purpleAI, "Get AI-powered package recommendations"),
                 CreateButton("📄 Show Logs", Color.FromArgb(75, 85, 99), "Toggle log output visibility"), 
                 CreateButton("📤 Export", accentOrange, "Export package list to file"),
                 CreateButton("❓ Help", darkBlue, "Show help menu and about information"), 
                 CreateButton("⚙️ Settings", Color.FromArgb(55, 65, 81), "Configure application settings"));
            
            (btnInstall, btnUninstall, btnRepair) = (
                CreateButton("📦 Install Selected", successGreen, "Install the selected packages"),
                CreateButton("🗑️ Uninstall Selected", crimsonRed, "Uninstall the selected packages"),
                CreateButton("🔧 Repair Selected", warningAmber, "Repair the selected packages"));
            
            cmbSource = new() { 
                DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(3),
                BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F)
            };
            cmbSource.Items.AddRange(new[] { "winget", "msstore", "all" });
            cmbSource.SelectedIndex = 0;
            buttonToolTips.SetToolTip(cmbSource, "Select package source: winget, Microsoft Store, or all sources");
            
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
            topPanel.Controls.Add(cmbSource, 3, 1);
            
            splitter = new SplitContainer { 
                Dock = DockStyle.Fill, Orientation = Orientation.Vertical, 
                Margin = new Padding(25, 15, 25, 25), BackColor = Color.FromArgb(25, 25, 25),
                SplitterWidth = 8, Panel1MinSize = 200, Panel2MinSize = 100,
                Panel2Collapsed = true
            };
            
            lstApps = new() { 
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false, 
                CheckBoxes = true, MultiSelect = true, BackColor = GetThemeColor(Color.FromArgb(15, 15, 15), Color.White),
                ForeColor = GetThemeColor(Color.FromArgb(230, 230, 230), Color.Black), Font = new Font("Segoe UI", 11F), BorderStyle = BorderStyle.None
            };
            
            // Add click handler for opening AI reports from status column
            lstApps.MouseClick += LstApps_MouseClick;
            
            txtLogs = new() { 
                Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, 
                Font = new Font("Consolas", 11F), BackColor = GetThemeColor(Color.FromArgb(12, 12, 12), Color.White), 
                ForeColor = GetThemeColor(Color.FromArgb(34, 197, 94), Color.FromArgb(0, 120, 0)), Text = "=== WingetWizard Logs ===\n",
                BorderStyle = BorderStyle.None
            };
            
            // Create welcome overlay for when no packages are loaded
            var welcomePanel = CreateWelcomePanel();
            welcomePanel.Tag = "welcome";
            
            splitter.Panel1.Controls.Add(lstApps);
            splitter.Panel1.Controls.Add(welcomePanel);
            splitter.Panel2.Controls.Add(txtLogs);
            string[] columns = { "Name:250", "ID:200", "Current Version:120", "Available Version:120", "Source:80", "Status:100", "AI Recommendation:200" };
            foreach (var col in columns) { 
                var parts = col.Split(':'); 
                var column = new ColumnHeader { Text = parts[0], Width = int.Parse(parts[1]) };
                lstApps.Columns.Add(column);
            }
            
            // Modern ListView styling
            lstApps.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstApps.BackColor = GetThemeColor(Color.FromArgb(15, 15, 15), Color.White);
            lstApps.ForeColor = GetThemeColor(Color.FromArgb(230, 230, 230), Color.Black);
            lstApps.GridLines = false;
            lstApps.FullRowSelect = true;
            lstApps.View = View.Details;
            lstApps.CheckBoxes = true;
            lstApps.MultiSelect = true;
            
            this.Controls.Add(splitter);
            this.Controls.Add(topPanel);
            this.Controls.Add(progressPanel);
            this.Controls.Add(headerPanel);
            
            var handlers = new (Button btn, EventHandler handler)[] {
                (btnCheck, BtnCheck_Click), (btnUpgrade, BtnUpgrade_Click), (btnUpgradeAll, BtnUpgradeAll_Click),
                (btnListAll, BtnListAll_Click), (btnInstall, BtnInstall_Click), (btnUninstall, BtnUninstall_Click),
                (btnRepair, BtnRepair_Click), (btnResearch, BtnResearch_Click), (btnLogs, BtnLogs_Click), 
                (btnExport, ExportUpgradeList), (btnHelp, ShowHelpMenu), (btnSettings, ShowSettingsMenu)
            };
            foreach (var (btn, handler) in handlers) btn.Click += handler;
            this.Resize += MainForm_Resize;
            this.HandleCreated += (s, e) => EnableDarkModeChrome(isDarkMode);
            UpdateUIMode();
        }

        // Continue with the rest of the methods...
        // (This is a partial implementation - the full file would continue with all the button click handlers
        // and other methods, but now using the service classes instead of embedded business logic)

        private void LoadSettings()
        {
            try
            {
                isAdvancedMode = _settingsService.GetSetting("isAdvancedMode", true);
                selectedAiModel = _settingsService.GetSetting("selectedAiModel", "claude-sonnet-4-20250514");
                verboseLogging = _settingsService.GetSetting("verboseLogging", false);
            }
            catch (Exception ex)
            {
                LogMessage($"Settings load error: {ex.Message}");
                // Fall back to default settings
                isAdvancedMode = true;
                selectedAiModel = "claude-sonnet-4-20250514";
            }
        }

        private void SaveSettings()
        {
            try
            {
                _settingsService.SetSetting("isAdvancedMode", isAdvancedMode);
                _settingsService.SetSetting("selectedAiModel", selectedAiModel);
                _settingsService.SetSetting("verboseLogging", verboseLogging);
                _settingsService.SaveSettings();
                LogMessage($"Settings saved with {_settingsService.GetAllSettings().Count} keys");
            }
            catch (Exception ex)
            {
                LogMessage($"Settings save error: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Button click handlers using service classes
        private async void BtnCheck_Click(object? sender, EventArgs e)
        {
            try
            {
                ShowProgress("Checking for available updates...");
                LogMessage("Checking for available updates...");
                var source = cmbSource.SelectedItem?.ToString() ?? "winget";
                var apps = await _packageService.CheckForUpdatesAsync(source, verboseLogging);
                
                lock (upgradableAppsLock)
                {
                    upgradableApps.Clear();
                    upgradableApps.AddRange(apps);
                }
                
                UpdatePackageList();
                LogMessage($"Found {apps.Count} packages with available updates");
                HideWelcomePanel();
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking updates: {ex.Message}");
                MessageBox.Show($"Failed to check updates: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
            }
        }

        private async void BtnUpgrade_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select packages to upgrade", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ShowProgress($"Upgrading {selectedItems.Count} packages...");
                LogMessage($"Upgrading {selectedItems.Count} selected packages...");
                foreach (ListViewItem item in selectedItems)
                {
                    var packageId = item.SubItems[1].Text; // ID column
                    UpdateProgress($"Upgrading {item.SubItems[0].Text}...");
                    var (success, message) = await _packageService.UpgradePackageAsync(packageId, verboseLogging);
                    
                    if (success)
                    {
                        item.SubItems[5].Text = "✅ Upgraded"; // Status column
                        LogMessage($"Successfully upgraded {item.SubItems[0].Text}");
                    }
                    else
                    {
                        item.SubItems[5].Text = "❌ Failed"; // Status column
                        LogMessage($"Failed to upgrade {item.SubItems[0].Text}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during upgrade: {ex.Message}");
                MessageBox.Show($"Upgrade failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
            }
        }

        private async void BtnUpgradeAll_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Upgrading all available packages...");
                var (success, message) = await _packageService.UpgradeAllPackagesAsync(verboseLogging);
                
                if (success)
                {
                    LogMessage("All packages upgraded successfully");
                    MessageBox.Show("All packages have been upgraded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage($"Upgrade all failed: {message}");
                    MessageBox.Show($"Upgrade failed: {message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during bulk upgrade: {ex.Message}");
                MessageBox.Show($"Bulk upgrade failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnListAll_Click(object? sender, EventArgs e)
        {
            try
            {
                ShowProgress("Listing all installed applications...");
                LogMessage("Listing all installed applications...");
                var source = cmbSource.SelectedItem?.ToString() ?? "winget";
                var apps = await _packageService.ListAllAppsAsync(source, verboseLogging);
                
                lock (upgradableAppsLock)
                {
                    upgradableApps.Clear();
                    upgradableApps.AddRange(apps);
                }
                
                UpdatePackageList();
                LogMessage($"Found {apps.Count} installed applications");
                HideWelcomePanel();
            }
            catch (Exception ex)
            {
                LogMessage($"Error listing applications: {ex.Message}");
                MessageBox.Show($"Failed to list applications: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
            }
        }

        private async void BtnInstall_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select packages to install", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                LogMessage($"Installing {selectedItems.Count} selected packages...");
                foreach (ListViewItem item in selectedItems)
                {
                    var packageId = item.SubItems[1].Text; // ID column
                    var (success, message) = await _packageService.InstallPackageAsync(packageId, verboseLogging);
                    
                    if (success)
                    {
                        item.SubItems[5].Text = "✅ Installed"; // Status column
                        LogMessage($"Successfully installed {item.SubItems[0].Text}");
                    }
                    else
                    {
                        item.SubItems[5].Text = "❌ Failed"; // Status column
                        LogMessage($"Failed to install {item.SubItems[0].Text}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during installation: {ex.Message}");
                MessageBox.Show($"Installation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnUninstall_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select packages to uninstall", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                LogMessage($"Uninstalling {selectedItems.Count} selected packages...");
                foreach (ListViewItem item in selectedItems)
                {
                    var packageId = item.SubItems[1].Text; // ID column
                    var (success, message) = await _packageService.UninstallPackageAsync(packageId, verboseLogging);
                    
                    if (success)
                    {
                        item.SubItems[5].Text = "✅ Uninstalled"; // Status column
                        LogMessage($"Successfully uninstalled {item.SubItems[0].Text}");
                    }
                    else
                    {
                        item.SubItems[5].Text = "❌ Failed"; // Status column
                        LogMessage($"Failed to uninstall {item.SubItems[0].Text}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during uninstallation: {ex.Message}");
                MessageBox.Show($"Uninstallation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnRepair_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select packages to repair", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                LogMessage($"Repairing {selectedItems.Count} selected packages...");
                foreach (ListViewItem item in selectedItems)
                {
                    var packageId = item.SubItems[1].Text; // ID column
                    var (success, message) = await _packageService.RepairPackageAsync(packageId, verboseLogging);
                    
                    if (success)
                    {
                        item.SubItems[5].Text = "✅ Repaired"; // Status column
                        LogMessage($"Successfully repaired {item.SubItems[0].Text}");
                    }
                    else
                    {
                        item.SubItems[5].Text = "❌ Failed"; // Status column
                        LogMessage($"Failed to repair {item.SubItems[0].Text}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during repair: {ex.Message}");
                MessageBox.Show($"Repair failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnResearch_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select packages for AI research", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ShowProgress("Starting AI research...");
                LogMessage($"Starting AI research for {selectedItems.Count} packages...");
                var recommendations = new List<(UpgradableApp app, string recommendation)>();
                
                foreach (ListViewItem item in selectedItems)
                {
                    var app = new UpgradableApp
                    {
                        Name = item.SubItems[0].Text,
                        Id = item.SubItems[1].Text,
                        Version = item.SubItems[2].Text,
                        Available = item.SubItems[3].Text
                    };
                    
                    UpdateProgress($"Researching {app.Name}...");
                    LogMessage($"Researching {app.Name}...");
                    var recommendation = await _aiService.GetAIRecommendationAsync(app);
                    recommendations.Add((app, recommendation));
                    
                    // Update the AI Recommendation column
                    item.SubItems[6].Text = SafeSubstring(recommendation, 50);
                }
                
                UpdateProgress("Saving reports...");
                // Save individual reports
                var markdownContent = _reportService.CreateMarkdownContent(recommendations, true, selectedAiModel);
                var reportsSaved = _reportService.SaveIndividualPackageReports(markdownContent);
                
                LogMessage($"AI research complete! {reportsSaved} individual reports saved.");
                MessageBox.Show($"AI research complete!\n\n{reportsSaved} individual reports have been saved to the AI_Reports folder.", 
                    "Research Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Update status columns with report links
                UpdateStatusColumnsWithReportLinks();
            }
            catch (Exception ex)
            {
                LogMessage($"Error during AI research: {ex.Message}");
                MessageBox.Show($"AI research failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
            }
        }

        private void BtnLogs_Click(object? sender, EventArgs e)
        {
            splitter.Panel2Collapsed = !splitter.Panel2Collapsed;
            btnLogs.Text = splitter.Panel2Collapsed ? "📄 Show Logs" : "📄 Hide Logs";
        }

        private void ExportUpgradeList(object? sender, EventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"WingetWizard_Export_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    lock (upgradableAppsLock)
                    {
                        var content = _packageService.ExportPackageList(upgradableApps);
                        File.WriteAllText(saveDialog.FileName, content, Encoding.UTF8);
                    }
                    
                    LogMessage($"Package list exported to {saveDialog.FileName}");
                    MessageBox.Show($"Package list exported successfully to:\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Export failed: {ex.Message}");
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowHelpMenu(object? sender, EventArgs e)
        {
            var helpMenu = new ContextMenuStrip();
            helpMenu.Items.Add("About WingetWizard", null, (s, args) => ShowAbout());
            helpMenu.Items.Add("Help Documentation", null, (s, args) => ShowHelp());
            helpMenu.Items.Add("Keyboard Shortcuts", null, (s, args) => ShowKeyboardShortcuts());
            
            helpMenu.BackColor = GetThemeColor(Color.FromArgb(25, 25, 25), Color.White);
            helpMenu.ForeColor = GetThemeColor(Color.White, Color.Black);
            helpMenu.Renderer = new ModernMenuRenderer(isDarkMode);
            
            helpMenu.Show(btnHelp, new Point(0, btnHelp.Height));
        }

        private void ShowSettingsMenu(object? sender, EventArgs e)
        {
            var settingsMenu = new ContextMenuStrip();
            settingsMenu.Items.Add("AI Settings", null, (s, args) => ShowAISettings());
            settingsMenu.Items.Add("UI Settings", null, (s, args) => ShowUISettings());
            settingsMenu.Items.Add("Logging Settings", null, (s, args) => ShowLoggingSettings());
            settingsMenu.Items.Add("-");
            settingsMenu.Items.Add("Reset API Keys", null, (s, args) => ResetApiKeys());
            
            settingsMenu.BackColor = GetThemeColor(Color.FromArgb(25, 25, 25), Color.White);
            settingsMenu.ForeColor = GetThemeColor(Color.White, Color.Black);
            settingsMenu.Renderer = new ModernMenuRenderer(isDarkMode);
            
            settingsMenu.Show(btnSettings, new Point(0, btnSettings.Height));
        }

        private void LstApps_MouseClick(object? sender, MouseEventArgs e)
        {
            var hitItem = lstApps.GetItemAt(e.X, e.Y);
            if (hitItem != null)
            {
                var hitSubItem = hitItem.GetSubItemAt(e.X, e.Y);
                if (hitSubItem != null)
                {
                    // Find which column was clicked by checking the X position
                    var columnIndex = -1;
                    var x = e.X;
                    var totalX = 0;
                    
                    for (int i = 0; i < lstApps.Columns.Count; i++)
                    {
                        totalX += lstApps.Columns[i].Width;
                        if (x <= totalX)
                        {
                            columnIndex = i;
                            break;
                        }
                    }
                    
                    if (columnIndex >= 0 && lstApps.Columns[columnIndex].Text == "Status")
                    {
                        var packageName = hitItem.SubItems[0].Text;
                        var reportPath = _reportService.GetReportPath(packageName);
                        
                        if (!string.IsNullOrEmpty(reportPath) && File.Exists(reportPath))
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = reportPath,
                                    UseShellExecute = true
                                });
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Failed to open report: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (lstApps?.Columns?.Count > 0)
            {
                var totalWidth = lstApps.ClientSize.Width - 20; // Account for scrollbar and padding
                var columns = lstApps.Columns;
                
                // Proportional column widths
                var totalPercentage = 100.0;
                var widths = new[] { 25.0, 20.0, 12.0, 12.0, 8.0, 10.0, 13.0 }; // Percentages for each column
                
                for (int i = 0; i < columns.Count && i < widths.Length; i++)
                {
                    columns[i].Width = (int)(totalWidth * widths[i] / totalPercentage);
                }
            }
        }

        private void UpdateUIMode()
        {
            // Update UI based on advanced mode setting
            if (isAdvancedMode)
            {
                btnRepair.Visible = true;
                btnInstall.Visible = true;
                btnUninstall.Visible = true;
            }
            else
            {
                btnRepair.Visible = false;
                btnInstall.Visible = false;
                btnUninstall.Visible = false;
            }
        }

        private void ApplySystemTheme()
        {
            try
            {
                // Check if system is in dark mode
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                isDarkMode = value == null || (int)value == 0; // Default to dark if can't read
            }
            catch
            {
                isDarkMode = true; // Default to dark mode
            }
            
            // Apply dark mode to window chrome
            EnableDarkModeChrome(isDarkMode);
        }
        
        private void EnableDarkModeChrome(bool enable)
        {
            if (this.Handle != IntPtr.Zero)
            {
                int useImmersiveDarkMode = enable ? 1 : 0;
                
                // Try Windows 10 version 2004 and later
                if (DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
                {
                    // Fallback for older Windows 10 versions
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
                }
            }
        }
        
        private Color GetThemeColor(Color darkColor, Color lightColor)
        {
            return isDarkMode ? darkColor : lightColor;
        }
        
        private void ApplyThemeToForm(Form form)
        {
            form.BackColor = GetThemeColor(Color.FromArgb(25, 25, 25), Color.White);
            form.ForeColor = GetThemeColor(Color.White, Color.Black);
            
            // Apply dark mode chrome to dialog forms
            if (form.Handle != IntPtr.Zero)
            {
                int useImmersiveDarkMode = isDarkMode ? 1 : 0;
                if (DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
                }
            }
        }
        
        private void ApplyThemeToControl(Control control)
        {
            if (control is TextBox textBox)
            {
                textBox.BackColor = GetThemeColor(Color.FromArgb(40, 40, 40), Color.White);
                textBox.ForeColor = GetThemeColor(Color.White, Color.Black);
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = GetThemeColor(Color.FromArgb(40, 40, 40), Color.White);
                comboBox.ForeColor = GetThemeColor(Color.White, Color.Black);
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.ForeColor = GetThemeColor(Color.White, Color.Black);
            }
            else if (control is Label label)
            {
                label.ForeColor = GetThemeColor(Color.White, Color.Black);
            }
            else if (control is RichTextBox richTextBox)
            {
                richTextBox.BackColor = GetThemeColor(Color.FromArgb(25, 25, 25), Color.White);
                richTextBox.ForeColor = GetThemeColor(Color.White, Color.Black);
            }
        }

        private Button CreateButton(string text, Color backColor, string? tooltip = null)
        {
            var button = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            
            // Add hover effects
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = Color.FromArgb(
                    Math.Min(255, backColor.R + 30),
                    Math.Min(255, backColor.G + 30),
                    Math.Min(255, backColor.B + 30));
            };
            
            button.MouseLeave += (s, e) =>
            {
                button.BackColor = backColor;
            };
            
            // Set tooltip if provided
            if (!string.IsNullOrEmpty(tooltip))
            {
                buttonToolTips?.SetToolTip(button, tooltip);
            }
            
            return button;
        }

        private void LogMessage(string message)
        {
            if (txtLogs != null && !this.IsDisposed)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => LogMessage(message)));
                    return;
                }
                
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                txtLogs.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
                txtLogs.ScrollToCaret();
            }
        }

        // Progress indicator methods
        private void ShowProgress(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowProgress(message)));
                return;
            }
            
            var progressPanel = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "progress");
            if (progressPanel != null)
            {
                progressPanel.Visible = true;
                statusLabel.Text = message;
            }
        }
        
        private void UpdateProgress(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(message)));
                return;
            }
            
            statusLabel.Text = message;
        }
        
        private void HideProgress()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(HideProgress));
                return;
            }
            
            var progressPanel = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "progress");
            if (progressPanel != null)
            {
                progressPanel.Visible = false;
                statusLabel.Text = "Ready";
            }
        }

        // Helper methods for UI updates
        private void UpdatePackageList()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdatePackageList));
                return;
            }
            
            lstApps.Items.Clear();
            lock (upgradableAppsLock)
            {
                foreach (var app in upgradableApps)
                {
                    var item = new ListViewItem(app.Name);
                    item.SubItems.Add(app.Id);
                    item.SubItems.Add(app.Version);
                    item.SubItems.Add(app.Available);
                    item.SubItems.Add(cmbSource.SelectedItem?.ToString() ?? "winget");
                    item.SubItems.Add(app.Status);
                    item.SubItems.Add(SafeSubstring(app.Recommendation, 50));
                    
                    lstApps.Items.Add(item);
                }
            }
        }

        private void UpdateStatusColumnsWithReportLinks()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateStatusColumnsWithReportLinks));
                return;
            }
            
            foreach (ListViewItem item in lstApps.Items)
            {
                var packageName = item.SubItems[0].Text;
                if (_reportService.HasReport(packageName))
                {
                    item.SubItems[5].Text = "📄 View Report";
                    item.SubItems[5].Tag = _reportService.GetReportPath(packageName);
                }
            }
        }

        // Dialog methods
        private void ShowAbout()
        {
            var aboutForm = new Form
            {
                Text = "About WingetWizard",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(aboutForm);
            
            var aboutText = new RichTextBox
            {
                Text = @"🧿 WingetWizard v2.1

AI-Enhanced Windows Package Manager

Key Features:
• Native OS theme integration (dark/light mode)
• Dark mode window chrome (title bar, buttons)
• Two-stage AI analysis (Perplexity + Claude)
• Comprehensive application and upgrade analysis
• Professional reporting with full context
• Thread-safe service-based architecture
• Theme-aware progress tracking (no popup windows)

AI Capabilities:
• Complete application overview and analysis
• Security vulnerability assessment
• Developer reputation and trust analysis
• Upgrade impact and compatibility review
• Professional markdown report generation

Theme Integration:
• Automatic Windows dark/light mode detection
• Native window chrome theming via Windows API
• Complete UI adaptation to OS preferences

Developed by: Mark Relph
Company: GeekSuave Labs

Built with .NET 6, Windows Forms, and native OS integration",
                ReadOnly = true,
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None
            };
            ApplyThemeToControl(aboutText);
            
            aboutForm.Controls.Add(aboutText);
            aboutForm.ShowDialog(this);
        }

        private void ShowHelp()
        {
            var helpForm = new Form
            {
                Text = "WingetWizard Help",
                Size = new Size(700, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(helpForm);
            
            var helpText = new RichTextBox
            {
                Text = @"🧿 WingetWizard Help

Getting Started:
1. Use 'List All Apps' to see installed packages
2. Use 'Check Updates' to find available upgrades
3. Select packages and use 'AI Research' for comprehensive analysis
4. Use 'Upgrade Selected' or 'Upgrade All' to update packages

Key Features:
• Native OS theme integration (automatic dark/light mode)
• Dark mode window chrome (title bar, minimize/maximize/close)
• Two-stage AI analysis (Perplexity + Claude)
• Comprehensive application information
• Individual package reports with full context
• Auto-sizing columns and responsive design
• Multiple package sources (winget, msstore)

Theme Integration:
• Automatically detects Windows dark/light mode preference
• All UI elements adapt to your OS theme settings
• Native window chrome matches system appearance
• Consistent theming across all dialogs and controls

AI Research Process:
• Perplexity researches application details and changes
• Claude formats professional upgrade reports
• Includes application overview, security analysis, and recommendations
• Saves individual reports for each package
• Click '📄 View Report' in Status column to open reports

Progress Tracking:
• Theme-aware progress bar (no popup windows)
• Real-time status updates with proper contrast
• Clean, unobtrusive progress indication

Tips:
• The app automatically matches your Windows theme preference
• Configure both Claude and Perplexity API keys for best results
• Use verbose logging for detailed operation information
• Export package lists and AI reports for backup
• Review AI recommendations before upgrading critical software",
                ReadOnly = true,
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None
            };
            ApplyThemeToControl(helpText);
            
            helpForm.Controls.Add(helpText);
            helpForm.ShowDialog(this);
        }

        private void ShowKeyboardShortcuts()
        {
            var shortcutsForm = new Form
            {
                Text = "Keyboard Shortcuts",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(shortcutsForm);
            
            var shortcutsText = new RichTextBox
            {
                Text = @"⌨️ Keyboard Shortcuts

Package Management:
Ctrl+U: Check for updates
Ctrl+L: List all applications
Ctrl+R: Start comprehensive AI research
Ctrl+G: Upgrade selected packages
Ctrl+Shift+G: Upgrade all packages

Interface:
Ctrl+A: Select all packages
Ctrl+D: Deselect all packages
Ctrl+E: Export package list and reports
Ctrl+H: Show help menu
Ctrl+S: Show settings menu
F5: Refresh package list
Esc: Close dialogs

Theme Integration:
• App automatically detects your Windows theme
• Dark/light mode switches instantly with OS settings
• Window chrome matches your system appearance

Progress Tracking:
• Watch the theme-aware progress bar for status
• No keyboard shortcuts needed - fully automated",
                ReadOnly = true,
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None
            };
            ApplyThemeToControl(shortcutsText);
            
            shortcutsForm.Controls.Add(shortcutsText);
            shortcutsForm.ShowDialog(this);
        }

        private void ShowAISettings()
        {
            var aiForm = new Form
            {
                Text = "AI Settings",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(aiForm);
            
            var claudeKeyLabel = new Label { Text = "Claude API Key:", Location = new Point(20, 20) };
            var claudeKeyBox = new TextBox 
            { 
                Location = new Point(20, 45), 
                Width = 400,
                Text = _settingsService.GetApiKey("AnthropicApiKey")
            };
            ApplyThemeToControl(claudeKeyBox);
            
            var perplexityKeyLabel = new Label { Text = "Perplexity API Key:", Location = new Point(20, 80) };
            ApplyThemeToControl(perplexityKeyLabel);
            var perplexityKeyBox = new TextBox 
            { 
                Location = new Point(20, 105), 
                Width = 400,
                Text = _settingsService.GetApiKey("PerplexityApiKey")
            };
            ApplyThemeToControl(perplexityKeyBox);
            
            var modelLabel = new Label { Text = "Claude Model:", Location = new Point(20, 140) };
            ApplyThemeToControl(modelLabel);
            var modelCombo = new ComboBox 
            { 
                Location = new Point(20, 165), 
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ApplyThemeToControl(modelCombo);
            modelCombo.Items.AddRange(new[] { "claude-sonnet-4-20250514", "claude-3-5-sonnet-20241022", "claude-3-haiku-20240307" });
            modelCombo.SelectedItem = selectedAiModel;
            
            var infoLabel = new Label 
            { 
                Text = "Two-stage AI: Perplexity researches application details, Claude formats professional reports", 
                Location = new Point(20, 200),
                ForeColor = GetThemeColor(Color.FromArgb(180, 180, 180), Color.FromArgb(100, 100, 100)),
                AutoSize = true
            };
            
            var saveButton = new Button 
            { 
                Text = "Save Settings", 
                Location = new Point(20, 240),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            saveButton.Click += (s, e) =>
            {
                _settingsService.StoreApiKey("AnthropicApiKey", claudeKeyBox.Text);
                _settingsService.StoreApiKey("PerplexityApiKey", perplexityKeyBox.Text);
                selectedAiModel = modelCombo.SelectedItem?.ToString() ?? "claude-sonnet-4-20250514";
                SaveSettings();
                
                // Recreate AI service with new keys
                _aiService?.Dispose();
                _aiService = new AIService(
                    _settingsService.GetApiKey("AnthropicApiKey"),
                    _settingsService.GetApiKey("PerplexityApiKey"),
                    selectedAiModel,
                    true
                );
                
                aiForm.Close();
            };
            
            aiForm.Controls.AddRange(new Control[] { 
                claudeKeyLabel, claudeKeyBox, perplexityKeyLabel, perplexityKeyBox, 
                modelLabel, modelCombo, infoLabel, saveButton 
            });
            
            aiForm.ShowDialog(this);
        }

        private void ShowUISettings()
        {
            var uiForm = new Form
            {
                Text = "UI Settings",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(uiForm);
            
            var advancedModeCheck = new CheckBox 
            { 
                Text = "Advanced Mode (show all buttons)", 
                Location = new Point(20, 20),
                Checked = isAdvancedMode
            };
            ApplyThemeToControl(advancedModeCheck);
            
            var saveButton = new Button 
            { 
                Text = "Save Settings", 
                Location = new Point(20, 60),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            saveButton.Click += (s, e) =>
            {
                isAdvancedMode = advancedModeCheck.Checked;
                SaveSettings();
                UpdateUIMode();
                uiForm.Close();
            };
            
            uiForm.Controls.AddRange(new Control[] { advancedModeCheck, saveButton });
            uiForm.ShowDialog(this);
        }

        private void ShowLoggingSettings()
        {
            var loggingForm = new Form
            {
                Text = "Logging Settings",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(loggingForm);
            
            var verboseCheck = new CheckBox 
            { 
                Text = "Enable verbose logging", 
                Location = new Point(20, 20),
                Checked = verboseLogging
            };
            ApplyThemeToControl(verboseCheck);
            
            var saveButton = new Button 
            { 
                Text = "Save Settings", 
                Location = new Point(20, 60),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            saveButton.Click += (s, e) =>
            {
                verboseLogging = verboseCheck.Checked;
                SaveSettings();
                loggingForm.Close();
            };
            
            loggingForm.Controls.AddRange(new Control[] { verboseCheck, saveButton });
            loggingForm.ShowDialog(this);
        }

        private void ResetApiKeys()
        {
            var result = MessageBox.Show(
                "This will remove all stored API keys. Are you sure?",
                "Reset API Keys",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
                
            if (result == DialogResult.Yes)
            {
                _settingsService.ResetApiKeys();
                SaveSettings();
                MessageBox.Show("API keys have been reset.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) 
            { 
                buttonToolTips?.Dispose();
                _aiService?.Dispose();
            }
            base.Dispose(disposing);
        }

        // Modern menu renderer for contemporary styling
        private class ModernMenuRenderer : ToolStripProfessionalRenderer
        {
            public ModernMenuRenderer(bool isDarkMode = true) : base(new ModernColorTable(isDarkMode)) { }
        }

        private class ModernColorTable : ProfessionalColorTable
        {
            private readonly bool _isDarkMode;
            
            public ModernColorTable(bool isDarkMode)
            {
                _isDarkMode = isDarkMode;
            }
            
            public override Color MenuItemSelected => Color.FromArgb(59, 130, 246);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(59, 130, 246);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(59, 130, 246);
            public override Color MenuItemBorder => _isDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(200, 200, 200);
            public override Color MenuBorder => _isDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(200, 200, 200);
            public override Color MenuItemPressedGradientBegin => _isDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(230, 230, 230);
            public override Color MenuItemPressedGradientEnd => _isDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(230, 230, 230);
            public override Color ToolStripDropDownBackground => _isDarkMode ? Color.FromArgb(25, 25, 25) : Color.White;
        }
    }
}
