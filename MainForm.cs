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
using WingetWizard.Models;
using WingetWizard.Services;
using WingetWizard.Utils;

namespace WingetWizard
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
    /// - In-UI progress bar with real-time status updates
    /// - Enhanced AI prompting with comprehensive upgrade analysis
    /// - Rich text rendering with color-coded recommendations
    /// - Professional markdown export with metadata and executive summaries
    /// - Thread-safe operations with service-based architecture
    /// </summary>
    public class MainForm : Form, IDisposable
    {
        // Windows API for dark mode title bar
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        
        // Windows 10 version-specific dark mode attributes for compatibility
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19; // Windows 10 before 20H1
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Windows 10 20H1 and later
        
        // Application constants
        private const string APP_VERSION = "v2.4";
        private const int STATUS_COLUMN_INDEX = 5;
        private static readonly Color PRIMARY_BLUE = Color.FromArgb(59, 130, 246);
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
        private Button btnSearchInstall = null!;
        private TextBox txtLogs = null!;          // Logging output with green terminal styling
        private ListView lstApps = null!;         // Package list with enhanced visualization
        private ComboBox cmbSource = null!;       // Source selection (winget, msstore, all)
        
        // In-UI progress indicator
        private ProgressBar progressBar = null!;
        private Label statusLabel = null!;
        private Label versionLabel = null!;

        private SplitContainer splitter = null!;  // Resizable layout with hidden-by-default logs
        private ToolTip buttonToolTips = null!;   // Tooltips for buttons when window is scaled down
        
        // Service layer - Business logic separated from UI
        private readonly PackageService _packageService;
        private AIService _aiService;
        private readonly ReportService _reportService;
        private readonly SettingsService _settingsService;
        private readonly SecureSettingsService _secureSettingsService;
        private readonly HealthCheckService _healthCheckService;
        private readonly ConfigurationValidationService _configValidationService;
        private readonly PerformanceMetricsService _performanceMetricsService;
        
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

            // WingetWizard logo image
            var logoImage = new PictureBox
            {
                Size = new Size(80, 80),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent
            };
            
            // Load the logo image
            try
            {
                // Try to load from file first
                var logoPath = Path.Combine(Application.StartupPath, "WinGetLogo.png");
                if (File.Exists(logoPath))
                {
                    logoImage.Image = Image.FromFile(logoPath);
                }
                else
                {
                    // Try to load from embedded resources
                    using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("WingetWizard.WinGetLogo.png");
                    if (stream != null)
                    {
                        logoImage.Image = Image.FromStream(stream);
                    }
                    else
                    {
                        // Fallback: create a themed logo if resource not found
                        var bmp = new Bitmap(80, 80);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            // Create a nice gradient background
                            var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                                new Rectangle(0, 0, 80, 80),
                                Color.FromArgb(100, 200, 255),
                                Color.FromArgb(59, 130, 246),
                                45f);
                            g.FillEllipse(brush, 10, 10, 60, 60);
                            g.DrawString("🧿", CreateFont(28F), Brushes.White, new PointF(18, 18));
                            brush.Dispose();
                        }
                        logoImage.Image = bmp;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logo loading failed: {ex.Message}");
                // Create a simple fallback logo
                var bmp = new Bitmap(80, 80);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.FillEllipse(new SolidBrush(GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(59, 130, 246))), 10, 10, 60, 60);
                    g.DrawString("W", CreateFont(32F, FontStyle.Bold), Brushes.White, new PointF(28, 20));
                }
                logoImage.Image = bmp;
            }

            // Main greeting label with personalized message
            var greetingLabel = new Label
            {
                Text = $"{greeting}, {userName}",
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
                Text = $"Ready • {DateTime.Now:HH:mm:ss} • WingetWizard {APP_VERSION}",
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

            logoImage.Location = new Point(0, 0);
            greetingLabel.Location = new Point(0, 90);
            subtitleLabel.Location = new Point(0, 130);
            actionsPanel.Location = new Point(0, 160);

            centerPanel.Controls.Add(logoImage);
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
            _secureSettingsService = new SecureSettingsService();
            _packageService = new PackageService();
            _reportService = new ReportService(Path.Combine(Application.StartupPath, "AI_Reports"));
            _healthCheckService = new HealthCheckService(_settingsService, _secureSettingsService);
            _configValidationService = new ConfigurationValidationService(_settingsService, _secureSettingsService);
            _performanceMetricsService = new PerformanceMetricsService();
            
            // Load settings
            LoadSettings();
            
            // Initialize AI service with current settings from secure storage
            var (accessKeyId, secretAccessKey, region, _) = _secureSettingsService.GetBedrockCredentials();
            var primaryLLMProvider = _secureSettingsService.GetApiKey("PrimaryLLMProvider") ?? "Anthropic (Claude Direct)";
            var isAnthropicPrimary = primaryLLMProvider == "Anthropic (Claude Direct)";
            var primaryProvider = isAnthropicPrimary ? "Claude" : "Bedrock";
            
            _aiService = new AIService(
                _secureSettingsService.GetApiKey("AnthropicApiKey") ?? "",
                _secureSettingsService.GetApiKey("PerplexityApiKey") ?? "",
                selectedAiModel,
                true, // Always use two-stage process
                primaryProvider, // Use selected primary provider
                accessKeyId,
                secretAccessKey,
                region
            );
            
            System.Diagnostics.Debug.WriteLine($"AI service initialized with primary provider: {primaryProvider}");
            
            InitializeComponent();
            
            // Start performance metrics collection timer (every 30 seconds)
            var metricsTimer = new System.Windows.Forms.Timer
            {
                Interval = 30000, // 30 seconds
                Enabled = true
            };
            metricsTimer.Tick += (s, e) => _performanceMetricsService.CollectSystemMetrics();
            metricsTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "WingetWizard - AI-Enhanced Package Manager";
            this.Size = new Size(1000, 700); // Increased size for better modern feel
            this.MinimumSize = new Size(900, 600);
            this.Font = CreateFont(11F); // Modern system font with fallback
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
                Font = CreateFont(18F, FontStyle.Bold),
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(25, 0, 0, 0)
            };

            var versionLabel = new Label
            {
                Text = "AI-Enhanced Package Manager",
                Font = CreateFont(10F, FontStyle.Regular),
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
                Font = CreateFont(10F, FontStyle.Regular),
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(25, 5, 0, 0)
            };
            
            progressPanel.Controls.Add(progressBar);
            progressPanel.Controls.Add(statusLabel);
            progressPanel.Tag = "progress";
            
            // Version label in top-right corner
            versionLabel = new Label
            {
                Text = APP_VERSION,
                Font = CreateFont(10F, FontStyle.Regular),
                ForeColor = GetThemeColor(Color.FromArgb(120, 120, 120), Color.FromArgb(100, 100, 100)),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };

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
            
            btnSearchInstall = CreateButton("🔍 Search & Install", Color.FromArgb(147, 51, 234), "Search for new packages and install them");
            
            cmbSource = new() { 
                DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(3),
                BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = CreateFont(11F)
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
            topPanel.Controls.Add(btnSearchInstall, 3, 1);
            topPanel.Controls.Add(cmbSource, 4, 1);
            
            splitter = new SplitContainer { 
                Dock = DockStyle.Fill, Orientation = Orientation.Vertical, 
                Margin = new Padding(25, 15, 25, 25), BackColor = Color.FromArgb(25, 25, 25),
                SplitterWidth = 8, Panel1MinSize = 200, Panel2MinSize = 100,
                Panel2Collapsed = true
            };
            
            lstApps = new() { 
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false, 
                CheckBoxes = true, MultiSelect = true, BackColor = GetThemeColor(Color.FromArgb(15, 15, 15), Color.White),
                ForeColor = GetThemeColor(Color.FromArgb(230, 230, 230), Color.Black), Font = CreateFont(11F), BorderStyle = BorderStyle.None
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
            this.Controls.Add(versionLabel);
            
            var handlers = new (Button btn, EventHandler handler)[] {
                (btnCheck, BtnCheck_Click), (btnUpgrade, BtnUpgrade_Click), (btnUpgradeAll, BtnUpgradeAll_Click),
                (btnListAll, BtnListAll_Click), (btnInstall, BtnInstall_Click), (btnUninstall, BtnUninstall_Click),
                (btnRepair, BtnRepair_Click), (btnResearch, BtnResearch_Click), (btnLogs, BtnLogs_Click), 
                (btnExport, ExportUpgradeList), (btnHelp, ShowHelpMenu), (btnSettings, ShowSettingsMenu),
                (btnSearchInstall, BtnSearchInstall_Click)
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
                verboseLogging = false;
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
            var operationId = _performanceMetricsService.StartOperation("CheckForUpdates");
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
                
                _performanceMetricsService.EndOperation(operationId, true);
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking updates: {ex.Message}");
                MessageBox.Show($"Failed to check updates: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _performanceMetricsService.EndOperation(operationId, false);
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

            var operationId = _performanceMetricsService.StartOperation("UpgradePackages");
            try
            {
                ShowProgress($"Upgrading {selectedItems.Count} packages...");
                LogMessage($"Upgrading {selectedItems.Count} selected packages...");
                var successCount = 0;
                var failCount = 0;
                
                foreach (ListViewItem item in selectedItems)
                {
                    var packageId = item.SubItems[1].Text; // ID column
                    UpdateProgress($"Upgrading {item.SubItems[0].Text}...");
                    var (success, message) = await _packageService.UpgradePackageAsync(packageId, verboseLogging);
                    
                    if (success)
                    {
                        item.SubItems[5].Text = "✅ Upgraded"; // Status column
                        LogMessage($"Successfully upgraded {item.SubItems[0].Text}");
                        successCount++;
                    }
                    else
                    {
                        item.SubItems[5].Text = "❌ Failed"; // Status column
                        LogMessage($"Failed to upgrade {item.SubItems[0].Text}: {message}");
                        failCount++;
                    }
                }
                
                var overallSuccess = failCount == 0;
                _performanceMetricsService.EndOperation(operationId, overallSuccess);
                
                // Record individual package upgrade metrics
                if (successCount > 0)
                {
                    _performanceMetricsService.AddMetric("PackagesUpgradedSuccessfully", successCount);
                }
                if (failCount > 0)
                {
                    _performanceMetricsService.AddMetric("PackagesUpgradeFailed", failCount);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during upgrade: {ex.Message}");
                MessageBox.Show($"Upgrade failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            finally
            {
                HideProgress();
            }
        }

        private async void BtnUpgradeAll_Click(object? sender, EventArgs e)
        {
            var operationId = _performanceMetricsService.StartOperation("UpgradeAllPackages");
            try
            {
                ShowProgress("Upgrading all available packages...");
                LogMessage("Upgrading all available packages...");
                var (success, message) = await _packageService.UpgradeAllPackagesAsync(verboseLogging);
                
                if (success)
                {
                    LogMessage("All packages upgraded successfully");
                    MessageBox.Show("All packages have been upgraded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _performanceMetricsService.EndOperation(operationId, true);
                }
                else
                {
                    LogMessage($"Upgrade all failed: {message}");
                    MessageBox.Show($"Upgrade failed: {message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _performanceMetricsService.EndOperation(operationId, false);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during bulk upgrade: {ex.Message}");
                MessageBox.Show($"Bulk upgrade failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            finally
            {
                HideProgress();
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
            
            // Add confirmation dialog before uninstalling
            var result = MessageBox.Show(
                $"Are you sure you want to uninstall {selectedItems.Count} selected package(s)?\n\nThis action cannot be undone.",
                "Confirm Uninstall",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
                
            if (result != DialogResult.Yes)
                return;

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
                ShowProgress($"Repairing {selectedItems.Count} packages...");
                LogMessage($"Repairing {selectedItems.Count} selected packages...");
                foreach (ListViewItem item in selectedItems)
                {
                    var packageId = item.SubItems[1].Text; // ID column
                    UpdateProgress($"Repairing {item.SubItems[0].Text}...");
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
            finally
            {
                HideProgress();
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
            settingsMenu.Items.Add("🏥 Health Check", null, (s, args) => ShowHealthCheck());
            settingsMenu.Items.Add("⚙️ Config Validation", null, (s, args) => ShowConfigValidation());
            settingsMenu.Items.Add("📊 Performance Metrics", null, (s, args) => ShowPerformanceMetrics());
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
                    
                    if (columnIndex == STATUS_COLUMN_INDEX)
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
            
            // Position version label in top-right corner
            if (versionLabel != null)
            {
                versionLabel.Location = new Point(this.Width - versionLabel.Width - 15, 10);
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
                try
                {
                    int useImmersiveDarkMode = enable ? 1 : 0;
                    
                    // Try Windows 10 version 2004 and later
                    if (DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
                    {
                        // Fallback for older Windows 10 versions
                        DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Failed to set dark mode chrome: {ex.Message}");
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
                try
                {
                    int useImmersiveDarkMode = isDarkMode ? 1 : 0;
                    if (DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
                    {
                        DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Failed to set dark mode chrome for dialog: {ex.Message}");
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
                Font = CreateFont(9F, FontStyle.Bold),
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
                Text = $@"🧿 WingetWizard {APP_VERSION}

AI-Enhanced Windows Package Manager with Search & Discovery

Key Features:
• 🔍 Professional package search and installation
• Native OS theme integration (dark/light mode)
• Dark mode window chrome (title bar, buttons)
• Two-stage AI analysis (Perplexity + Claude)
• Comprehensive application and upgrade analysis
• Professional reporting with full context
• Thread-safe service-based architecture
• In-UI progress tracking (no popup windows)

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
                Font = CreateFont(11F),
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
3. 🔍 Use 'Search & Install' to find and install new software
4. Select packages and use 'AI Research' for comprehensive analysis
5. Use 'Upgrade Selected' or 'Upgrade All' to update packages

🔍 Package Search & Installation:
• Click '🔍 Search & Install' to open the search dialog
• Enter package names (e.g., 'vscode', 'chrome', 'python')
• Press Enter or click 'Search' to find packages
• Use checkboxes to select packages for installation
• Click 'Install Selected' to install chosen packages

Popular Search Terms:
• Development: vscode, git, python, nodejs, docker
• Browsers: chrome, firefox, edge, brave
• Media: vlc, spotify, discord, zoom  
• Utilities: 7zip, notepad++, winrar, putty

Search Tips:
• Use simple terms: 'vscode' works better than full names
• Try variations: 'chrome', 'google chrome', or 'chromium'
• Results show source information (winget, msstore)
• Use 'Select All' for quick selection of all results

Key Features:
• Professional search interface for package discovery and installation
• Native OS theme integration (automatic dark/light mode)
• Dark mode window chrome (title bar, minimize/maximize/close)
• Configurable primary/fallback LLM providers (Anthropic Claude or AWS Bedrock)
• Two-stage AI analysis (Perplexity + Primary LLM)
• Comprehensive application information and AI-generated reports
• Individual package reports with full context
• Auto-sizing columns and responsive design
• Multiple package sources (winget, msstore, combined)

AI Configuration:
• Choose between Anthropic Claude Direct API or AWS Bedrock as primary LLM
• Automatic fallback to secondary provider if primary fails
• Support for both Bedrock API keys and full AWS credentials
• Perplexity provides research data for all AI operations
• Required fields are highlighted based on your primary LLM selection

Theme Integration:
• Automatically detects Windows dark/light mode preference
• All UI elements adapt to your OS theme settings
• Native window chrome matches system appearance
• Consistent theming across all dialogs and controls

AI Research Process:
• Perplexity researches application details and changes
• Primary LLM formats professional upgrade reports
• Fallback to secondary LLM if primary fails
• Includes application overview, security analysis, and recommendations
• Saves individual reports for each package
• Click '📄 View Report' in Status column to open reports

Progress Tracking:
• In-UI progress bar with theme-aware colors
• Real-time status updates with proper contrast
• Clean, unobtrusive progress indication (no modal popups)

Tips:
• The app automatically matches your Windows theme preference
• Configure your primary LLM provider first, then add required credentials
• Use Bedrock API keys for simpler Bedrock authentication
• Use verbose logging for detailed operation information
• Export package lists and AI reports for backup
• Review AI recommendations before upgrading critical software",
                ReadOnly = true,
                Font = CreateFont(11F),
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
• Watch the in-UI progress bar for status
• No keyboard shortcuts needed - fully automated",
                ReadOnly = true,
                Font = CreateFont(11F),
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
                Size = new Size(600, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                AutoScroll = true
            };
            ApplyThemeToForm(aiForm);
            
            int yPos = 20;
            
            // Claude Direct API Section
            var claudeLabel = new Label { Text = "Claude Direct API", Font = new Font("Calibri", 10, FontStyle.Bold), Location = new Point(20, yPos), AutoSize = true };
            ApplyThemeToControl(claudeLabel);
            yPos += 25;
            
            // Primary LLM Selection
            var primaryLLMLabel = new Label { Text = "Primary LLM Provider:", Location = new Point(20, yPos) };
            ApplyThemeToControl(primaryLLMLabel);
            yPos += 20;
            var primaryLLMCombo = new ComboBox 
            { 
                Location = new Point(20, yPos), 
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ApplyThemeToControl(primaryLLMCombo);
            primaryLLMCombo.Items.AddRange(new[] { "Anthropic (Claude Direct)", "AWS Bedrock" });
            
            // Load current primary LLM setting
            var currentPrimaryLLM = _secureSettingsService.GetApiKey("PrimaryLLMProvider") ?? "Anthropic (Claude Direct)";
            primaryLLMCombo.SelectedItem = currentPrimaryLLM;
            
            // Add tooltip explaining primary/fallback
            buttonToolTips?.SetToolTip(primaryLLMCombo, 
                "Select your primary LLM provider. The other provider will serve as a fallback if the primary fails. " +
                "Required fields will be highlighted in red based on your selection.");
            
            yPos += 35;
            

            
            var claudeKeyLabel = new Label { Text = "Claude API Key:", Location = new Point(20, yPos) };
            ApplyThemeToControl(claudeKeyLabel);
            yPos += 20;
            var claudeApiKey = _secureSettingsService.GetApiKey("AnthropicApiKey") ?? "";
            System.Diagnostics.Debug.WriteLine($"Loaded Claude API key: {(!string.IsNullOrEmpty(claudeApiKey) ? "***PRESENT***" : "***EMPTY***")}");
            var claudeKeyBox = new TextBox 
            { 
                Location = new Point(20, yPos), 
                Width = 500,
                Text = claudeApiKey,
                UseSystemPasswordChar = true
            };
            ApplyThemeToControl(claudeKeyBox);
            yPos += 35;
            
            // AWS Bedrock Section
            var bedrockLabel = new Label { Text = "AWS Bedrock (Fallback)", Font = new Font("Calibri", 10, FontStyle.Bold), Location = new Point(20, yPos), AutoSize = true };
            ApplyThemeToControl(bedrockLabel);
            yPos += 25;
            
            // Bedrock API Key (new simpler option)
            var bedrockApiKeyLabel = new Label { Text = "Bedrock API Key (Recommended):", Location = new Point(20, yPos) };
            ApplyThemeToControl(bedrockApiKeyLabel);
            yPos += 20;
            var bedrockApiKey = _secureSettingsService.GetApiKey("BedrockApiKey") ?? "";
            var bedrockApiKeyBox = new TextBox 
            { 
                Location = new Point(20, yPos), 
                Width = 500,
                Text = bedrockApiKey,
                UseSystemPasswordChar = true
            };
            ApplyThemeToControl(bedrockApiKeyBox);
            yPos += 35;
            
            var bedrockApiKeyInfo = new Label 
            { 
                Text = "💡 Tip: Get your Bedrock API key from the AWS Console → Bedrock → API Keys. This is simpler than full AWS credentials.", 
                Location = new Point(20, yPos),
                Width = 500,
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                AutoSize = false,
                Height = 30
            };
            ApplyThemeToControl(bedrockApiKeyInfo);
            yPos += 40;
            
            var bedrockCredsLabel = new Label { Text = "Or use full AWS credentials:", Location = new Point(20, yPos) };
            ApplyThemeToControl(bedrockCredsLabel);
            yPos += 20;
            
            var awsKeyLabel = new Label { Text = "AWS Access Key ID:", Location = new Point(20, yPos) };
            ApplyThemeToControl(awsKeyLabel);
            yPos += 20;
            var awsAccessKey = _secureSettingsService.GetApiKey("aws_access_key_id") ?? "";
            System.Diagnostics.Debug.WriteLine($"Loaded AWS Access Key: {(!string.IsNullOrEmpty(awsAccessKey) ? "***PRESENT***" : "***EMPTY***")}");
            var awsKeyBox = new TextBox 
            { 
                Location = new Point(20, yPos), 
                Width = 500,
                Text = awsAccessKey,
                UseSystemPasswordChar = true
            };
            ApplyThemeToControl(awsKeyBox);
            yPos += 35;
            
            var awsSecretLabel = new Label { Text = "AWS Secret Access Key:", Location = new Point(20, yPos) };
            ApplyThemeToControl(awsSecretLabel);
            yPos += 20;
            var awsSecretKey = _secureSettingsService.GetApiKey("aws_secret_access_key") ?? "";
            System.Diagnostics.Debug.WriteLine($"Loaded AWS Secret Key: {(!string.IsNullOrEmpty(awsSecretKey) ? "***PRESENT***" : "***EMPTY***")}");
            var awsSecretBox = new TextBox 
            { 
                Location = new Point(20, yPos), 
                Width = 500,
                Text = awsSecretKey,
                UseSystemPasswordChar = true
            };
            ApplyThemeToControl(awsSecretBox);
            yPos += 35;
            
            var awsRegionLabel = new Label { Text = "AWS Region:", Location = new Point(20, yPos) };
            ApplyThemeToControl(awsRegionLabel);
            yPos += 20;
            var awsRegionBox = new ComboBox 
            { 
                Location = new Point(20, yPos), 
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ApplyThemeToControl(awsRegionBox);
            awsRegionBox.Items.AddRange(new[] { "us-east-1", "us-west-2", "eu-west-1", "eu-central-1", "ap-southeast-1", "ap-northeast-1" });
            awsRegionBox.SelectedItem = _secureSettingsService.GetApiKey("aws_region") ?? AppConstants.DEFAULT_AWS_REGION;
            yPos += 35;
            
            // Perplexity Section
            var perplexityLabel = new Label { Text = "Perplexity (Research)", Font = new Font("Calibri", 10, FontStyle.Bold), Location = new Point(20, yPos), AutoSize = true };
            ApplyThemeToControl(perplexityLabel);
            yPos += 25;
            
            var perplexityKeyLabel = new Label { Text = "Perplexity API Key:", Location = new Point(20, yPos) };
            ApplyThemeToControl(perplexityKeyLabel);
            yPos += 20;
            var perplexityKeyBox = new TextBox 
            { 
                Location = new Point(20, yPos), 
                Width = 500,
                Text = _secureSettingsService.GetApiKey("PerplexityApiKey") ?? "",
                UseSystemPasswordChar = true
            };
            ApplyThemeToControl(perplexityKeyBox);
            yPos += 35;
            
            // Bedrock Model Selection
            var bedrockModelLabel = new Label { Text = "Bedrock Model:", Location = new Point(20, yPos) };
            ApplyThemeToControl(bedrockModelLabel);
            yPos += 20;
            var bedrockModelCombo = new ComboBox 
            { 
                Location = new Point(20, yPos), 
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ApplyThemeToControl(bedrockModelCombo);
            
            var refreshModelsButton = new Button 
            { 
                Text = "🔄", 
                Location = new Point(380, yPos),
                Size = new Size(30, 23),
                BackColor = GetThemeColor(Color.FromArgb(34, 197, 94), Color.FromArgb(21, 128, 61)),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = CreateFont(12F)
            };
            buttonToolTips?.SetToolTip(refreshModelsButton, "Refresh available Bedrock models");
            
            var testBedrockButton = new Button 
            { 
                Text = "🔍", 
                Location = new Point(415, yPos),
                Size = new Size(30, 23),
                BackColor = GetThemeColor(Color.FromArgb(59, 130, 246), Color.FromArgb(37, 99, 235)),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = CreateFont(12F)
            };
            buttonToolTips?.SetToolTip(testBedrockButton, "Test Bedrock connection");
            
            // Update section labels based on primary LLM selection
            void UpdateSectionLabels()
            {
                var isAnthropicPrimary = primaryLLMCombo.SelectedItem?.ToString() == "Anthropic (Claude Direct)";
                claudeLabel.Text = isAnthropicPrimary ? "Claude Direct API (Primary)" : "Claude Direct API (Fallback)";
                bedrockLabel.Text = isAnthropicPrimary ? "AWS Bedrock (Fallback)" : "AWS Bedrock (Primary)";
                
                // Update colors to indicate primary vs fallback
                claudeLabel.ForeColor = isAnthropicPrimary ? 
                    GetThemeColor(Color.FromArgb(34, 197, 94), Color.FromArgb(21, 128, 61)) : // Green for primary
                    GetThemeColor(Color.FromArgb(107, 114, 128), Color.FromArgb(75, 85, 99)); // Gray for fallback
                
                bedrockLabel.ForeColor = isAnthropicPrimary ? 
                    GetThemeColor(Color.FromArgb(107, 114, 128), Color.FromArgb(75, 85, 99)) : // Gray for fallback
                    GetThemeColor(Color.FromArgb(34, 197, 94), Color.FromArgb(21, 128, 61)); // Green for primary
                
                // Update required field indicators
                var primaryColor = GetThemeColor(Color.FromArgb(239, 68, 68), Color.FromArgb(220, 38, 38)); // Red for required
                var optionalColor = GetThemeColor(Color.FromArgb(107, 114, 128), Color.FromArgb(75, 85, 99)); // Gray for optional
                
                // Claude API Key - required if primary, optional if fallback
                claudeKeyLabel.ForeColor = isAnthropicPrimary ? primaryColor : optionalColor;
                claudeKeyLabel.Text = isAnthropicPrimary ? "Claude API Key (Required):" : "Claude API Key (Optional):";
                
                // Bedrock credentials - required if primary, optional if fallback
                var bedrockRequired = !isAnthropicPrimary;
                bedrockApiKeyLabel.ForeColor = bedrockRequired ? primaryColor : optionalColor;
                bedrockApiKeyLabel.Text = bedrockRequired ? "Bedrock API Key (Required):" : "Bedrock API Key (Optional):";
                
                awsKeyLabel.ForeColor = bedrockRequired ? primaryColor : optionalColor;
                awsKeyLabel.Text = bedrockRequired ? "AWS Access Key ID (Required):" : "AWS Access Key ID (Optional):";
                
                awsSecretLabel.ForeColor = bedrockRequired ? primaryColor : optionalColor;
                awsSecretLabel.Text = bedrockRequired ? "AWS Secret Access Key (Required):" : "AWS Secret Access Key (Optional):";
            }
            
            // Set initial labels
            UpdateSectionLabels();
            
            // Update labels when selection changes
            primaryLLMCombo.SelectedIndexChanged += (s, e) => UpdateSectionLabels();
            
            // Load current Bedrock model selection
            var currentBedrockModel = _secureSettingsService.GetApiKey("bedrock_model") ?? "";
            
            // Auto-load models function
            async Task LoadBedrockModels()
            {
                // Check if we have either a Bedrock API key or AWS credentials
                var hasBedrockApiKey = !string.IsNullOrEmpty(bedrockApiKeyBox.Text);
                var hasAwsCredentials = !string.IsNullOrEmpty(awsKeyBox.Text) && !string.IsNullOrEmpty(awsSecretBox.Text);
                
                if (!hasBedrockApiKey && !hasAwsCredentials)
                {
                    bedrockModelCombo.Items.Clear();
                    bedrockModelCombo.Items.Add("Enter Bedrock API Key or AWS credentials to load models");
                    bedrockModelCombo.SelectedIndex = 0;
                    bedrockModelCombo.Enabled = false;
                    return;
                }
                
                try
                {
                    if (bedrockModelCombo.InvokeRequired)
                    {
                        bedrockModelCombo.Invoke(() =>
                        {
                            bedrockModelCombo.Items.Clear();
                            bedrockModelCombo.Items.Add("Loading models...");
                            bedrockModelCombo.SelectedIndex = 0;
                            bedrockModelCombo.Enabled = false;
                        });
                    }
                    else
                    {
                        bedrockModelCombo.Items.Clear();
                        bedrockModelCombo.Items.Add("Loading models...");
                        bedrockModelCombo.SelectedIndex = 0;
                        bedrockModelCombo.Enabled = false;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Loading Bedrock models for region: {awsRegionBox.SelectedItem}");
                    
                    BedrockModelDiscoveryService modelDiscovery;
                    
                    if (hasBedrockApiKey)
                    {
                        // Use Bedrock API key authentication
                        modelDiscovery = new BedrockModelDiscoveryService(
                            bedrockApiKeyBox.Text,
                            awsRegionBox.SelectedItem?.ToString() ?? AppConstants.DEFAULT_AWS_REGION
                        );
                        System.Diagnostics.Debug.WriteLine("Using Bedrock API key authentication");
                    }
                    else
                    {
                        // Use AWS credentials authentication
                        modelDiscovery = new BedrockModelDiscoveryService(
                            awsKeyBox.Text,
                            awsSecretBox.Text,
                            awsRegionBox.SelectedItem?.ToString() ?? AppConstants.DEFAULT_AWS_REGION
                        );
                        System.Diagnostics.Debug.WriteLine("Using AWS credentials authentication");
                    }
                    
                    // Test connection first
                    System.Diagnostics.Debug.WriteLine("Testing Bedrock connection...");
                    var availableModels = await modelDiscovery.GetTextModelsAsync(forceRefresh: true);
                    
                    System.Diagnostics.Debug.WriteLine($"Discovered {availableModels.Count} Bedrock models");
                    
                    if (!availableModels.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("No models discovered, checking if it's a connection issue...");
                        
                        // Try to get any models to see if it's a filtering issue
                        var allModels = await modelDiscovery.GetAvailableModelsAsync(forceRefresh: true);
                        System.Diagnostics.Debug.WriteLine($"Total models available (including non-text): {allModels.Count}");
                        
                        if (!allModels.Any())
                        {
                            throw new InvalidOperationException("No Bedrock models available in this region. Please check your credentials and region selection.");
                        }
                        else
                        {
                            // Use all models if text filtering is too restrictive
                            availableModels = allModels;
                        }
                    }
                    
                    var modelItems = availableModels.Select(m => 
                        $"{m.ModelName} ({m.ProviderName}) - {m.ModelId}"
                    ).OrderBy(x => x).ToArray();
                    
                    if (bedrockModelCombo.InvokeRequired)
                    {
                        bedrockModelCombo.Invoke(() =>
                        {
                            bedrockModelCombo.Items.Clear();
                            
                            if (availableModels.Any())
                            {
                                bedrockModelCombo.Enabled = true;
                                bedrockModelCombo.Items.AddRange(modelItems);
                                
                                if (!string.IsNullOrEmpty(currentBedrockModel))
                                {
                                    var matchingItem = modelItems.FirstOrDefault(item => item.Contains(currentBedrockModel));
                                    bedrockModelCombo.SelectedItem = matchingItem ?? modelItems.FirstOrDefault();
                                }
                                else
                                {
                                    bedrockModelCombo.SelectedIndex = 0;
                                }
                            }
                            else
                            {
                                bedrockModelCombo.Items.Add("No models available in this region");
                                bedrockModelCombo.SelectedIndex = 0;
                                bedrockModelCombo.Enabled = false;
                            }
                        });
                    }
                    else
                    {
                        bedrockModelCombo.Items.Clear();
                        
                        if (availableModels.Any())
                        {
                            bedrockModelCombo.Enabled = true;
                            bedrockModelCombo.Items.AddRange(modelItems);
                            
                            if (!string.IsNullOrEmpty(currentBedrockModel))
                            {
                                var matchingItem = modelItems.FirstOrDefault(item => item.Contains(currentBedrockModel));
                                bedrockModelCombo.SelectedItem = matchingItem ?? modelItems.FirstOrDefault();
                            }
                            else
                            {
                                bedrockModelCombo.SelectedIndex = 0;
                            }
                        }
                        else
                        {
                            bedrockModelCombo.Items.Add("No models available in this region");
                            bedrockModelCombo.SelectedIndex = 0;
                            bedrockModelCombo.Enabled = false;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Added {modelItems.Length} models to dropdown");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load Bedrock models: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" (Inner: {ex.InnerException.Message})";
                    }
                    
                    if (bedrockModelCombo.InvokeRequired)
                    {
                        bedrockModelCombo.Invoke(() =>
                        {
                            bedrockModelCombo.Items.Clear();
                            bedrockModelCombo.Items.Add($"Failed to load: {errorMessage}");
                            bedrockModelCombo.SelectedIndex = 0;
                            bedrockModelCombo.Enabled = false;
                        });
                    }
                    else
                    {
                        bedrockModelCombo.Items.Clear();
                        bedrockModelCombo.Items.Add($"Failed to load: {errorMessage}");
                        bedrockModelCombo.SelectedIndex = 0;
                        bedrockModelCombo.Enabled = false;
                    }
                }
            }
            
            // Auto-load on credentials change (with debouncing)
            System.Windows.Forms.Timer? debounceTimer = null;
            
            void ScheduleModelLoad()
            {
                debounceTimer?.Stop();
                debounceTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                debounceTimer.Tick += (s, e) =>
                {
                    debounceTimer.Stop();
                    #pragma warning disable CS4014 // Intentionally fire-and-forget
                Task.Run(async () =>
                {
                    try
                    {
                        await LoadBedrockModels();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in scheduled model load: {ex.Message}");
                    }
                });
#pragma warning restore CS4014
                };
                debounceTimer.Start();
            }
            
            bedrockApiKeyBox.TextChanged += (s, e) => ScheduleModelLoad();
            awsKeyBox.TextChanged += (s, e) => ScheduleModelLoad();
            awsSecretBox.TextChanged += (s, e) => ScheduleModelLoad();
            awsRegionBox.SelectedIndexChanged += (s, e) => ScheduleModelLoad();
            
            // Manual refresh button
            refreshModelsButton.Click += async (s, e) => 
            {
                refreshModelsButton.Enabled = false;
                refreshModelsButton.Text = "⏳";
                try
                {
                    await LoadBedrockModels();
                }
                finally
                {
                    refreshModelsButton.Enabled = true;
                    refreshModelsButton.Text = "🔄";
                }
            };
            
            // Test Bedrock connection button
            testBedrockButton.Click += async (s, e) =>
            {
                testBedrockButton.Enabled = false;
                testBedrockButton.Text = "⏳";
                
                try
                {
                    // Check if we have either a Bedrock API key or AWS credentials
                    var hasBedrockApiKey = !string.IsNullOrEmpty(bedrockApiKeyBox.Text);
                    var hasAwsCredentials = !string.IsNullOrEmpty(awsKeyBox.Text) && !string.IsNullOrEmpty(awsSecretBox.Text);
                    
                    if (!hasBedrockApiKey && !hasAwsCredentials)
                    {
                        MessageBox.Show("Please enter either a Bedrock API Key or AWS credentials first.", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    var region = awsRegionBox.SelectedItem?.ToString() ?? AppConstants.DEFAULT_AWS_REGION;
                    BedrockModelDiscoveryService modelDiscovery;
                    
                    if (hasBedrockApiKey)
                    {
                        modelDiscovery = new BedrockModelDiscoveryService(
                            bedrockApiKeyBox.Text,
                            region
                        );
                        System.Diagnostics.Debug.WriteLine("Testing connection with Bedrock API key");
                    }
                    else
                    {
                        modelDiscovery = new BedrockModelDiscoveryService(
                            awsKeyBox.Text,
                            awsSecretBox.Text,
                            region
                        );
                        System.Diagnostics.Debug.WriteLine("Testing connection with AWS credentials");
                    }
                    
                    // Test basic connection first
                    var connectionTest = await modelDiscovery.TestConnectionAsync();
                    if (!connectionTest)
                    {
                        var authMethod = hasBedrockApiKey ? "Bedrock API Key" : "AWS credentials";
                        MessageBox.Show($"❌ Bedrock connection failed!\n\nRegion: {region}\nAuth Method: {authMethod}\n\nUnable to establish connection to Bedrock service.\n\nPlease check:\n• Your credentials are correct\n• Region selection is valid\n• Network connectivity\n• API permissions", 
                            "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    // Test with a simple model availability check
                    var testResult = await modelDiscovery.IsModelAvailableAsync(AppConstants.BEDROCK_CLAUDE_35_SONNET_V2);
                    
                    if (testResult)
                    {
                        var authMethod = hasBedrockApiKey ? "Bedrock API Key" : "AWS credentials";
                        MessageBox.Show($"✅ Bedrock connection successful!\n\nRegion: {region}\nAuth Method: {authMethod}\nTest Model: {AppConstants.BEDROCK_CLAUDE_35_SONNET_V2}\n\nYou can now refresh the model list.", 
                            "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Try to get any models to see what's available
                        var availableModels = await modelDiscovery.GetAvailableModelsAsync(forceRefresh: true);
                        
                        if (availableModels.Any())
                        {
                            var modelNames = string.Join("\n• ", availableModels.Take(5).Select(m => $"{m.ModelName} ({m.ProviderName})"));
                            var moreText = availableModels.Count > 5 ? $"\n\n... and {availableModels.Count - 5} more models" : "";
                            var authMethod = hasBedrockApiKey ? "Bedrock API Key" : "AWS credentials";
                            
                            MessageBox.Show($"⚠️ Bedrock connection successful, but the test model was not found.\n\nRegion: {region}\nAuth Method: {authMethod}\n\nAvailable models in this region:\n• {modelNames}{moreText}\n\nThis is normal - different regions have different model availability. You can now refresh the model list to see all available models.", 
                                "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            var authMethod = hasBedrockApiKey ? "Bedrock API Key" : "AWS credentials";
                            MessageBox.Show($"⚠️ Bedrock connection test completed, but no models were found.\n\nRegion: {region}\nAuth Method: {authMethod}\n\nThis might indicate:\n• No models available in this region\n• API permissions issues\n• Region-specific model availability\n\nTry a different region or check your permissions.", 
                                "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                    }
                    
                    MessageBox.Show($"❌ Bedrock connection failed!\n\nError: {errorMessage}\n\nPlease check:\n• Your credentials are correct\n• Region selection is valid\n• API permissions include Bedrock access\n• Network connectivity", 
                        "Connection Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    testBedrockButton.Enabled = true;
                    testBedrockButton.Text = "🔍";
                }
            };
            
            // Initial load if credentials exist
            var hasBedrockApiKey = !string.IsNullOrEmpty(bedrockApiKey);
            var hasAwsCredentials = !string.IsNullOrEmpty(awsAccessKey) && !string.IsNullOrEmpty(awsSecretKey);
            
            if (hasBedrockApiKey || hasAwsCredentials)
            {
#pragma warning disable CS4014 // Intentionally fire-and-forget
                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    aiForm.Invoke(async () => await LoadBedrockModels());
                });
#pragma warning restore CS4014
            }
            else
            {
                bedrockModelCombo.Items.Add("Enter Bedrock API Key or AWS credentials to load models");
                bedrockModelCombo.SelectedIndex = 0;
                bedrockModelCombo.Enabled = false;
            }
            
            yPos += 35;
            
            // Claude Model Selection  
            var claudeModelLabel = new Label { Text = "Claude Direct API Model:", Location = new Point(20, yPos) };
            ApplyThemeToControl(claudeModelLabel);
            yPos += 20;
            var claudeModelCombo = new ComboBox 
            { 
                Location = new Point(20, yPos), 
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ApplyThemeToControl(claudeModelCombo);
            claudeModelCombo.Items.AddRange(new[] { "claude-sonnet-4-20250514", "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20240307" });
            claudeModelCombo.SelectedItem = selectedAiModel;
            yPos += 35;
            
            // Info Section
            var infoLabel = new Label 
            { 
                Text = "Multi-provider AI: Claude Direct → AWS Bedrock fallback. Perplexity provides research data.", 
                Location = new Point(20, yPos),
                Width = 500,
                ForeColor = GetThemeColor(Color.FromArgb(180, 180, 180), Color.FromArgb(100, 100, 100)),
                AutoSize = false,
                Height = 40
            };
            ApplyThemeToControl(infoLabel);
            
            // Update info label based on primary LLM selection
            void UpdateInfoLabel()
            {
                var isAnthropicPrimary = primaryLLMCombo.SelectedItem?.ToString() == "Anthropic (Claude Direct)";
                if (isAnthropicPrimary)
                {
                    infoLabel.Text = "Multi-provider AI: Claude Direct (Primary) → AWS Bedrock (Fallback). Perplexity provides research data.";
                }
                else
                {
                    infoLabel.Text = "Multi-provider AI: AWS Bedrock (Primary) → Claude Direct (Fallback). Perplexity provides research data.";
                }
            }
            
            // Set initial info label
            UpdateInfoLabel();
            
            // Update info label when selection changes
            primaryLLMCombo.SelectedIndexChanged += (s, e) => UpdateInfoLabel();
            
            yPos += 50;
            
            // Buttons
            var testButton = new Button 
            { 
                Text = "Test Connection", 
                Location = new Point(20, yPos),
                Size = new Size(120, 30),
                BackColor = GetThemeColor(Color.FromArgb(34, 197, 94), Color.FromArgb(21, 128, 61)),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            var saveButton = new Button 
            { 
                Text = "Save Settings", 
                Location = new Point(150, yPos),
                Size = new Size(120, 30),
                BackColor = PRIMARY_BLUE,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            testButton.Click += (s, e) =>
            {
                testButton.Enabled = false;
                testButton.Text = "Testing...";
                
                try
                {
                    // Quick validation test
                    var hasClaudeKey = !string.IsNullOrEmpty(claudeKeyBox.Text);
                    var hasBedrockKeys = !string.IsNullOrEmpty(awsKeyBox.Text) && !string.IsNullOrEmpty(awsSecretBox.Text);
                    
                    if (hasClaudeKey || hasBedrockKeys)
                    {
                        MessageBox.Show("API keys configured successfully!", "Test Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Please configure at least Claude or AWS Bedrock credentials.", "Test Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                finally
                {
                    testButton.Enabled = true;
                    testButton.Text = "Test Connection";
                }
            };
            
            saveButton.Click += (s, e) =>
            {
                try
                {
                    // Save primary LLM selection
                    var selectedPrimaryLLM = primaryLLMCombo.SelectedItem?.ToString() ?? "Anthropic (Claude Direct)";
                    var isAnthropicPrimary = selectedPrimaryLLM == "Anthropic (Claude Direct)";
                    
                    // Validate required fields based on primary LLM selection
                    var validationErrors = new List<string>();
                    
                    if (isAnthropicPrimary)
                    {
                        // Anthropic is primary - Claude API key is required
                        if (string.IsNullOrEmpty(claudeKeyBox.Text))
                        {
                            validationErrors.Add("Claude API Key is required when Anthropic is the primary LLM provider.");
                        }
                    }
                    else
                    {
                        // Bedrock is primary - either Bedrock API key or AWS credentials are required
                        var hasBedrockApiKey = !string.IsNullOrEmpty(bedrockApiKeyBox.Text);
                        var hasAwsCredentials = !string.IsNullOrEmpty(awsKeyBox.Text) && !string.IsNullOrEmpty(awsSecretBox.Text);
                        
                        if (!hasBedrockApiKey && !hasAwsCredentials)
                        {
                            validationErrors.Add("Either Bedrock API Key or AWS credentials are required when Bedrock is the primary LLM provider.");
                        }
                    }
                    
                    if (validationErrors.Any())
                    {
                        var errorMessage = "Please fix the following validation errors:\n\n" + string.Join("\n", validationErrors);
                        MessageBox.Show(errorMessage, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    _secureSettingsService.SaveApiKey("PrimaryLLMProvider", selectedPrimaryLLM);
                    System.Diagnostics.Debug.WriteLine($"Saved primary LLM provider: {selectedPrimaryLLM}");
                    
                    // Save all credentials securely with debug output
                    if (!string.IsNullOrEmpty(claudeKeyBox.Text))
                    {
                        _secureSettingsService.SaveApiKey("AnthropicApiKey", claudeKeyBox.Text);
                        System.Diagnostics.Debug.WriteLine("Saved Claude API key");
                    }
                    
                    // Save Bedrock API key if provided
                    if (!string.IsNullOrEmpty(bedrockApiKeyBox.Text))
                    {
                        _secureSettingsService.SaveApiKey("BedrockApiKey", bedrockApiKeyBox.Text);
                        System.Diagnostics.Debug.WriteLine("Saved Bedrock API key");
                    }
                    
                    // Save AWS credentials if provided
                    if (!string.IsNullOrEmpty(awsKeyBox.Text) && !string.IsNullOrEmpty(awsSecretBox.Text))
                    {
                        var selectedBedrockModelDisplay = bedrockModelCombo.SelectedItem?.ToString() ?? "";
                        var selectedBedrockModel = AppConstants.BEDROCK_CLAUDE_35_SONNET_V2; // fallback
                        
                        // Extract model ID from display string (format: "Name (Provider) - ModelID")
                        if (!string.IsNullOrEmpty(selectedBedrockModelDisplay) && selectedBedrockModelDisplay.Contains(" - "))
                        {
                            var parts = selectedBedrockModelDisplay.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                selectedBedrockModel = parts[1]; // The model ID is after the " - "
                            }
                        }
                        
                        _secureSettingsService.SaveBedrockCredentials(
                            awsKeyBox.Text,
                            awsSecretBox.Text, 
                            awsRegionBox.SelectedItem?.ToString() ?? AppConstants.DEFAULT_AWS_REGION,
                            selectedBedrockModel
                        );
                        System.Diagnostics.Debug.WriteLine($"Saved Bedrock credentials with model: {selectedBedrockModel}");
                    }
                    
                    if (!string.IsNullOrEmpty(perplexityKeyBox.Text))
                    {
                        _secureSettingsService.SaveApiKey("PerplexityApiKey", perplexityKeyBox.Text);
                        System.Diagnostics.Debug.WriteLine("Saved Perplexity API key");
                    }
                    
                    selectedAiModel = claudeModelCombo.SelectedItem?.ToString() ?? "claude-sonnet-4-20250514";
                    SaveSettings();
                    
                    // Recreate AI service with new credentials and primary LLM selection
                    _aiService?.Dispose();
                    var (newAccessKeyId, newSecretAccessKey, newRegion, _) = _secureSettingsService.GetBedrockCredentials();
                    
                    // Determine which provider to use as primary based on selection
                    var primaryProvider = isAnthropicPrimary ? "Claude" : "Bedrock";
                    
                    _aiService = new AIService(
                        _secureSettingsService.GetApiKey("AnthropicApiKey") ?? "",
                        _secureSettingsService.GetApiKey("PerplexityApiKey") ?? "",
                        selectedAiModel,
                        true,
                        primaryProvider, // Use selected primary provider
                        newAccessKeyId,
                        newSecretAccessKey,
                        newRegion
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"AI service recreated with primary provider: {primaryProvider}");
                    MessageBox.Show("AI settings saved successfully!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    aiForm.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving AI settings: {ex.Message}");
                    MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            
            aiForm.Controls.AddRange(new Control[] { 
                primaryLLMLabel, primaryLLMCombo,
                claudeLabel, claudeKeyLabel, claudeKeyBox,
                bedrockLabel, bedrockApiKeyLabel, bedrockApiKeyBox, bedrockApiKeyInfo, bedrockCredsLabel, awsKeyLabel, awsKeyBox, awsSecretLabel, awsSecretBox, awsRegionLabel, awsRegionBox,
                bedrockModelLabel, bedrockModelCombo, refreshModelsButton, testBedrockButton,
                perplexityLabel, perplexityKeyLabel, perplexityKeyBox,
                claudeModelLabel, claudeModelCombo, infoLabel, testButton, saveButton
            });
            
            System.Diagnostics.Debug.WriteLine("AI Settings form initialized with dynamic Bedrock model loading");
            
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
                BackColor = PRIMARY_BLUE,
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
                BackColor = PRIMARY_BLUE,
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

        private void ShowHealthCheck()
        {
            var healthForm = new Form
            {
                Text = "System Health Check",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(healthForm);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(20)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            // Header with status indicator
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            var statusLabel = new Label
            {
                Text = "🔄 Running health check...",
                Font = CreateFont(14F, FontStyle.Bold),
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            var quickCheckBtn = new Button
            {
                Text = "Quick Check",
                BackColor = PRIMARY_BLUE,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 100,
                Height = 30
            };

            var fullCheckBtn = new Button
            {
                Text = "Full Check",
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 100,
                Height = 30,
                Margin = new Padding(0, 0, 10, 0)
            };

            headerPanel.Controls.Add(statusLabel);
            headerPanel.Controls.Add(quickCheckBtn);
            headerPanel.Controls.Add(fullCheckBtn);

            // Results display
            var resultsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = CreateFont(10F),
                BorderStyle = BorderStyle.None,
                Text = "Click 'Quick Check' for basic health status or 'Full Check' for comprehensive analysis."
            };
            ApplyThemeToControl(resultsBox);

            // Close button
            var closeButton = new Button
            {
                Text = "Close",
                BackColor = Color.FromArgb(107, 114, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 35),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            closeButton.Click += (s, e) => healthForm.Close();

            // Event handlers for health check buttons
            quickCheckBtn.Click += async (s, e) =>
            {
                try
                {
                    statusLabel.Text = "🔄 Running quick health check...";
                    statusLabel.ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215));
                    quickCheckBtn.Enabled = false;
                    fullCheckBtn.Enabled = false;
                    
                    var result = await _healthCheckService.PerformQuickHealthCheckAsync();
                    DisplayHealthResult(result, resultsBox, statusLabel);
                }
                catch (Exception ex)
                {
                    statusLabel.Text = "❌ Health check failed";
                    statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                    resultsBox.Text = $"Health check failed with error:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}";
                }
                finally
                {
                    quickCheckBtn.Enabled = true;
                    fullCheckBtn.Enabled = true;
                }
            };

            fullCheckBtn.Click += async (s, e) =>
            {
                try
                {
                    statusLabel.Text = "🔄 Running comprehensive health check...";
                    statusLabel.ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215));
                    quickCheckBtn.Enabled = false;
                    fullCheckBtn.Enabled = false;
                    
                    var result = await _healthCheckService.PerformHealthCheckAsync();
                    DisplayHealthResult(result, resultsBox, statusLabel);
                }
                catch (Exception ex)
                {
                    statusLabel.Text = "❌ Health check failed";
                    statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                    resultsBox.Text = $"Health check failed with error:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}";
                }
                finally
                {
                    quickCheckBtn.Enabled = true;
                    fullCheckBtn.Enabled = true;
                }
            };

            mainPanel.Controls.Add(headerPanel, 0, 0);
            mainPanel.Controls.Add(resultsBox, 0, 1);
            mainPanel.Controls.Add(closeButton, 0, 2);

            healthForm.Controls.Add(mainPanel);
            healthForm.ShowDialog(this);
        }

        private void DisplayHealthResult(HealthCheckResult result, RichTextBox resultsBox, Label statusLabel)
        {
            // Update status label
            if (result.IsHealthy)
            {
                statusLabel.Text = "✅ System is healthy";
                statusLabel.ForeColor = Color.FromArgb(34, 197, 94);
            }
            else
            {
                statusLabel.Text = $"⚠️ {result.Issues.Count} issue(s) found";
                statusLabel.ForeColor = Color.FromArgb(245, 158, 11);
            }

            // Format and display detailed results
            var summary = result.GetSummary();
            resultsBox.Text = summary;

            // Add color formatting for better readability
            resultsBox.SelectAll();
            resultsBox.SelectionColor = GetThemeColor(Color.White, Color.Black);
            resultsBox.DeselectAll();

            // Highlight critical issues in red
            foreach (var issue in result.Issues)
            {
                var startIndex = resultsBox.Text.IndexOf($"❌ {issue}");
                if (startIndex >= 0)
                {
                    resultsBox.Select(startIndex, issue.Length + 2);
                    resultsBox.SelectionColor = Color.FromArgb(239, 68, 68);
                }
            }

            // Highlight warnings in orange
            foreach (var warning in result.Warnings)
            {
                var startIndex = resultsBox.Text.IndexOf($"⚠️ {warning}");
                if (startIndex >= 0)
                {
                    resultsBox.Select(startIndex, warning.Length + 2);
                    resultsBox.SelectionColor = Color.FromArgb(245, 158, 11);
                }
            }

            // Highlight metrics in blue
            foreach (var metric in result.Metrics)
            {
                var metricText = $"📊 {metric.Key}: {metric.Value}";
                var startIndex = resultsBox.Text.IndexOf(metricText);
                if (startIndex >= 0)
                {
                    resultsBox.Select(startIndex, metricText.Length);
                    resultsBox.SelectionColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215));
                }
            }

            resultsBox.DeselectAll();
            resultsBox.ScrollToCaret();
        }

        private void ShowConfigValidation()
        {
            var configForm = new Form
            {
                Text = "Configuration Validation",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(configForm);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(20)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            // Header with validation button
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            var statusLabel = new Label
            {
                Text = "Click 'Validate Configuration' to check all settings and dependencies",
                Font = CreateFont(14F, FontStyle.Bold),
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            var validateBtn = new Button
            {
                Text = "Validate Configuration",
                BackColor = PRIMARY_BLUE,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 150,
                Height = 30
            };

            headerPanel.Controls.Add(statusLabel);
            headerPanel.Controls.Add(validateBtn);

            // Results display
            var resultsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = CreateFont(10F),
                BorderStyle = BorderStyle.None,
                Text = "Configuration validation will check:\n\n• Core settings and API keys\n• File paths and permissions\n• Application dependencies\n• Security settings\n• Performance configurations"
            };
            ApplyThemeToControl(resultsBox);

            // Close button
            var closeButton = new Button
            {
                Text = "Close",
                BackColor = Color.FromArgb(107, 114, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 35),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            closeButton.Click += (s, e) => configForm.Close();

            // Event handler for validation button
            validateBtn.Click += (s, e) =>
            {
                try
                {
                    statusLabel.Text = "🔄 Validating configuration...";
                    statusLabel.ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215));
                    validateBtn.Enabled = false;
                    
                    var result = _configValidationService.ValidateConfiguration();
                    DisplayConfigValidationResult(result, resultsBox, statusLabel);
                }
                catch (Exception ex)
                {
                    statusLabel.Text = "❌ Configuration validation failed";
                    statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                    resultsBox.Text = $"Configuration validation failed with error:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}";
                }
                finally
                {
                    validateBtn.Enabled = true;
                }
            };

            mainPanel.Controls.Add(headerPanel, 0, 0);
            mainPanel.Controls.Add(resultsBox, 0, 1);
            mainPanel.Controls.Add(closeButton, 0, 2);

            configForm.Controls.Add(mainPanel);
            configForm.ShowDialog(this);
        }

        private void DisplayConfigValidationResult(ConfigurationValidationResult result, RichTextBox resultsBox, Label statusLabel)
        {
            // Update status label
            if (result.IsValid)
            {
                statusLabel.Text = "✅ Configuration is valid";
                statusLabel.ForeColor = Color.FromArgb(34, 197, 94);
            }
            else
            {
                statusLabel.Text = $"❌ {result.Errors.Count} critical error(s) found";
                statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
            }

            // Display validation report
            var report = _configValidationService.GetValidationReport();
            resultsBox.Text = report;

            // Add color formatting
            resultsBox.SelectAll();
            resultsBox.SelectionColor = GetThemeColor(Color.White, Color.Black);
            resultsBox.DeselectAll();

            // Highlight errors in red
            foreach (var error in result.Errors)
            {
                var startIndex = resultsBox.Text.IndexOf($"❌ {error}");
                if (startIndex >= 0)
                {
                    resultsBox.Select(startIndex, error.Length + 2);
                    resultsBox.SelectionColor = Color.FromArgb(239, 68, 68);
                }
            }

            // Highlight warnings in orange
            foreach (var warning in result.Warnings)
            {
                var startIndex = resultsBox.Text.IndexOf($"⚠️ {warning}");
                if (startIndex >= 0)
                {
                    resultsBox.Select(startIndex, warning.Length + 2);
                    resultsBox.SelectionColor = Color.FromArgb(245, 158, 11);
                }
            }

            resultsBox.DeselectAll();
            resultsBox.ScrollToCaret();
        }

        private void ShowPerformanceMetrics()
        {
            var perfForm = new Form
            {
                Text = "Performance Metrics",
                Size = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            ApplyThemeToForm(perfForm);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(20)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            // Header with refresh button
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            var statusLabel = new Label
            {
                Text = "Real-time performance metrics and operation statistics",
                Font = CreateFont(14F, FontStyle.Bold),
                ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215)),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            var refreshBtn = new Button
            {
                Text = "Refresh Metrics",
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 120,
                Height = 30
            };

            headerPanel.Controls.Add(statusLabel);
            headerPanel.Controls.Add(refreshBtn);

            // Results display
            var resultsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = CreateFont(10F),
                BorderStyle = BorderStyle.None,
                Text = "Performance metrics will be displayed here. Click 'Refresh Metrics' to collect current data."
            };
            ApplyThemeToControl(resultsBox);

            // Close button
            var closeButton = new Button
            {
                Text = "Close",
                BackColor = Color.FromArgb(107, 114, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 35),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            closeButton.Click += (s, e) => perfForm.Close();

            // Event handler for refresh button
            refreshBtn.Click += (s, e) =>
            {
                try
                {
                    statusLabel.Text = "🔄 Collecting performance metrics...";
                    statusLabel.ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215));
                    refreshBtn.Enabled = false;
                    
                    // Collect current metrics
                    _performanceMetricsService.CollectSystemMetrics();
                    
                    // Generate and display report
                    var report = _performanceMetricsService.GeneratePerformanceReport();
                    resultsBox.Text = report;
                    
                    statusLabel.Text = "✅ Performance metrics updated";
                    statusLabel.ForeColor = Color.FromArgb(34, 197, 94);
                }
                catch (Exception ex)
                {
                    statusLabel.Text = "❌ Failed to collect metrics";
                    statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                    resultsBox.Text = $"Failed to collect performance metrics:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}";
                }
                finally
                {
                    refreshBtn.Enabled = true;
                }
            };

            mainPanel.Controls.Add(headerPanel, 0, 0);
            mainPanel.Controls.Add(resultsBox, 0, 1);
            mainPanel.Controls.Add(closeButton, 0, 2);

            perfForm.Controls.Add(mainPanel);
            perfForm.ShowDialog(this);
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
                _healthCheckService?.Dispose();
                _performanceMetricsService?.Dispose();
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

        /// <summary>
        /// Handles the search and install button click
        /// Opens a new dialog for package search and installation
        /// </summary>
        private void BtnSearchInstall_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Search button clicked - opening search dialog");
                ShowSearchInstallDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening search dialog: {ex.Message}");
                MessageBox.Show($"Error opening search dialog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows the search and install dialog
        /// </summary>
        private void ShowSearchInstallDialog()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ShowSearchInstallDialog called - creating form");
                
                var searchForm = new Form
                {
                    Text = "🔍 Search & Install Packages",
                    Size = new Size(900, 650),
                    MinimumSize = new Size(700, 400),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    MaximizeBox = true,
                    MinimizeBox = false
                };
                
                ApplyThemeToForm(searchForm);
                
                // Create simplified main layout
                var mainPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(15),
                    RowStyles = 
                    {
                        new RowStyle(SizeType.Absolute, 60),   // Search controls
                        new RowStyle(SizeType.Percent, 100),   // Results list
                        new RowStyle(SizeType.Absolute, 50)    // Action buttons
                    }
                };
            
            // Simplified search controls
            var searchPanel = new Panel { Dock = DockStyle.Fill };
            
            var searchBox = new TextBox
            {
                Size = new Size(500, 30),
                Font = CreateFont(12F),
                Location = new Point(0, 15),
                PlaceholderText = "Search for packages... (e.g., vscode, chrome, git, python)"
            };
            
            var searchButton = new Button
            {
                Text = "🔍 Search",
                Size = new Size(120, 30),
                Font = CreateFont(12F),
                Location = new Point(520, 15),
                BackColor = GetThemeColor(Color.FromArgb(59, 130, 246), Color.FromArgb(59, 130, 246)),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            var resultsCountLabel = new Label
            {
                Text = "Enter a search term to find packages",
                Font = CreateFont(10F),
                ForeColor = GetThemeColor(Color.FromArgb(150, 150, 150), Color.FromArgb(100, 100, 100)),
                AutoSize = true,
                Location = new Point(650, 22)
            };
            
            // Add a status bar below the results for better user feedback
            var statusBar = new Panel
            {
                Height = 25,
                Dock = DockStyle.Bottom,
                BackColor = GetThemeColor(Color.FromArgb(30, 30, 30), Color.FromArgb(245, 245, 245))
            };
            
            var statusLabel = new Label
            {
                Text = "Ready to search",
                Font = CreateFont(8F),
                ForeColor = GetThemeColor(Color.FromArgb(156, 163, 175), Color.FromArgb(107, 114, 128)),
                AutoSize = true,
                Location = new Point(10, 5)
            };
            
            statusBar.Controls.Add(statusLabel);
            
            searchPanel.Controls.AddRange(new Control[] { searchBox, searchButton, resultsCountLabel });
            
            // Enhanced results list with main app styling
            var resultsList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false, // Match main app (no grid lines)
                CheckBoxes = true,
                MultiSelect = true,
                Font = CreateFont(10F),
                BackColor = GetThemeColor(Color.FromArgb(15, 15, 15), Color.White), // Match main app background
                ForeColor = GetThemeColor(Color.FromArgb(230, 230, 230), Color.Black),
                BorderStyle = BorderStyle.None, // Match main app
                HeaderStyle = ColumnHeaderStyle.Nonclickable // Match main app
            };
            
            // Columns matching main app style (optimized for search)
            string[] searchColumns = { "Name:320", "ID:220", "Version:120", "Source:90" };
            foreach (var col in searchColumns) 
            { 
                var parts = col.Split(':'); 
                var column = new ColumnHeader { Text = parts[0], Width = int.Parse(parts[1]) };
                resultsList.Columns.Add(column);
            }
            
            // Add resize handler to auto-adjust Name column
            searchForm.Resize += (s, e) =>
            {
                if (resultsList.Columns.Count > 0)
                {
                    // Calculate available width for Name column (total width - other columns - padding)
                    var otherColumnsWidth = resultsList.Columns[1].Width + resultsList.Columns[2].Width + resultsList.Columns[3].Width;
                    var availableWidth = resultsList.ClientSize.Width - otherColumnsWidth - 40; // 40px padding
                    resultsList.Columns[0].Width = Math.Max(200, availableWidth); // Minimum 200px for Name column
                }
            };
            
            // Apply theme to match main app styling
            ApplyThemeToControl(resultsList);
            
            // Simplified action buttons
            var actionPanel = new Panel { Dock = DockStyle.Fill };
            
            var installButton = new Button
            {
                Text = "📦 Install Selected",
                Size = new Size(150, 35),
                Font = CreateFont(11F),
                Location = new Point(0, 10),
                BackColor = GetThemeColor(Color.FromArgb(34, 197, 94), Color.FromArgb(34, 197, 94)),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            
            var selectAllButton = new Button
            {
                Text = "Select All",
                Size = new Size(100, 35),
                Font = CreateFont(11F),
                Location = new Point(170, 10),
                BackColor = GetThemeColor(Color.FromArgb(59, 130, 246), Color.FromArgb(59, 130, 246)),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            var deselectAllButton = new Button
            {
                Text = "Deselect All",
                Size = new Size(100, 35),
                Font = CreateFont(11F),
                Location = new Point(280, 10),
                BackColor = GetThemeColor(Color.FromArgb(107, 114, 128), Color.FromArgb(107, 114, 128)),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            actionPanel.Controls.AddRange(new Control[] { installButton, selectAllButton, deselectAllButton });
            
            // Add panels to simplified layout
            mainPanel.Controls.Add(searchPanel, 0, 0);
            mainPanel.Controls.Add(resultsList, 0, 1);
            mainPanel.Controls.Add(actionPanel, 0, 2);
            
            // Add status bar to the form for better user feedback
            searchForm.Controls.Add(statusBar);
            
            // Event handlers with improved functionality
            searchButton.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(searchBox.Text.Trim()))
                {
                    MessageBox.Show("Please enter a search term.", "Search Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    searchBox.Focus();
                    return;
                }
                
                searchButton.Enabled = false;
                searchButton.Text = "🔍 Searching...";
                resultsCountLabel.Text = "Searching...";
                statusLabel.Text = $"Searching for '{searchBox.Text.Trim()}'...";
                
                try
                {
                    var searchTerm = searchBox.Text.Trim();
                    var results = await _packageService.SearchPackagesAsync(searchTerm, null, 100, false, verboseLogging);
                    
                    PopulateSearchResults(resultsList, results);
                    resultsCountLabel.Text = $"Found {results.Count} package(s)";
                    installButton.Enabled = results.Count > 0;
                    
                    if (results.Count == 0)
                    {
                        resultsCountLabel.Text = "No packages found. Try a different search term.";
                        statusLabel.Text = "No packages found. Try a different search term.";
                    }
                    else
                    {
                        statusLabel.Text = $"Search completed. Found {results.Count} package(s).";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Search failed: {ex.Message}\n\nTry checking your internet connection and winget installation.", 
                        "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    resultsCountLabel.Text = "Search failed";
                    statusLabel.Text = $"Search failed: {ex.Message}";
                }
                finally
                {
                    searchButton.Enabled = true;
                    searchButton.Text = "🔍 Search";
                }
            };
            
            // Enter key in search box triggers search
            searchBox.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    searchButton.PerformClick();
                }
            };
            
            selectAllButton.Click += (s, e) =>
            {
                foreach (ListViewItem item in resultsList.Items)
                {
                    item.Checked = true;
                }
                UpdateInstallButtonState();
            };
            
            deselectAllButton.Click += (s, e) =>
            {
                foreach (ListViewItem item in resultsList.Items)
                {
                    item.Checked = false;
                }
                UpdateInstallButtonState();
            };
            
            installButton.Click += async (s, e) =>
            {
                var selectedPackages = new List<string>();
                foreach (ListViewItem item in resultsList.Items)
                {
                    if (item.Checked)
                    {
                        selectedPackages.Add(item.SubItems[1].Text); // ID column
                    }
                }
                
                if (selectedPackages.Count == 0)
                {
                    MessageBox.Show("Please select packages to install.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                var result = MessageBox.Show(
                    $"Install {selectedPackages.Count} selected package(s)?\n\nThis may take several minutes depending on package sizes.",
                    "Confirm Installation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    installButton.Enabled = false;
                    installButton.Text = "📦 Installing...";
                    
                    try
                    {
                        var installResult = await _packageService.InstallMultiplePackagesAsync(selectedPackages, verboseLogging);
                        if (installResult.Success)
                        {
                            MessageBox.Show($"Installation completed successfully!\n\nInstalled {selectedPackages.Count} package(s).", 
                                "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            // Refresh the list to show updated status
                            searchButton.PerformClick();
                        }
                        else
                        {
                            MessageBox.Show($"Installation failed: {installResult.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Installation failed: {ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        installButton.Enabled = true;
                        installButton.Text = "📦 Install Selected";
                    }
                }
            };
            
            
            // Helper function to update install button state
            void UpdateInstallButtonState()
            {
                var hasSelection = resultsList.Items.Cast<ListViewItem>().Any(item => item.Checked);
                installButton.Enabled = hasSelection;
            }
            
            // Update install button state when checkboxes change
            resultsList.ItemChecked += (s, e) => UpdateInstallButtonState();
            
            // Double-click to view package details
            resultsList.DoubleClick += (s, e) =>
            {
                if (resultsList.SelectedItems.Count > 0)
                {
                    var selectedItem = resultsList.SelectedItems[0];
                    var packageId = selectedItem.SubItems[1].Text;
                    ShowPackageDetails(packageId);
                }
            };
            
            searchForm.Controls.Add(mainPanel);
            System.Diagnostics.Debug.WriteLine("Search dialog form created successfully - showing dialog");
            searchForm.ShowDialog(this);
            System.Diagnostics.Debug.WriteLine("Search dialog closed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ShowSearchInstallDialog: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            MessageBox.Show($"Error creating search dialog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
        
        /// <summary>
        /// Populates the search results list view
        /// </summary>
        private void PopulateSearchResults(ListView listView, List<PackageSearchResult> results)
        {
            System.Diagnostics.Debug.WriteLine("=== POPULATE SEARCH RESULTS DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"Received {results.Count} results to display");
            
            listView.Items.Clear();
            
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                System.Diagnostics.Debug.WriteLine($"Processing result {i}: Name='{result.Name}', ID='{result.Id}', Version='{result.Version}', Source='{result.Source}'");
                
                var item = new ListViewItem(result.Name);
                item.SubItems.Add(result.Id);
                item.SubItems.Add(result.Version);
                item.SubItems.Add(result.Source);
                
                // Store the package result in the item's tag for reference
                item.Tag = result;
                
                listView.Items.Add(item);
                System.Diagnostics.Debug.WriteLine($"Added ListView item {i}: {item.Text} with color {item.BackColor}");
            }
            
            System.Diagnostics.Debug.WriteLine($"ListView now contains {listView.Items.Count} items");
            System.Diagnostics.Debug.WriteLine($"ListView columns: {listView.Columns.Count}");
            foreach (ColumnHeader col in listView.Columns)
            {
                System.Diagnostics.Debug.WriteLine($"Column: {col.Text}, Width: {col.Width}");
            }
            
            // Force refresh to show the new items and colors
            listView.Refresh();
            
            // Log completion for debugging
            if (results.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No search results to display");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Successfully populated {results.Count} search results");
            }
        }
        
        /// <summary>
        /// Filters search results based on user input
        /// </summary>
        private List<ListViewItem> FilterSearchResults(ListView listView, string filterText, string filterType)
        {
            var filteredItems = new List<ListViewItem>();
            var filterLower = filterText.ToLowerInvariant();
            
            foreach (ListViewItem item in listView.Items)
            {
                bool shouldInclude = false;
                
                switch (filterType.ToLowerInvariant())
                {
                    case "name":
                        shouldInclude = item.SubItems[0].Text.ToLowerInvariant().Contains(filterLower);
                        break;
                    case "id":
                        shouldInclude = item.SubItems[1].Text.ToLowerInvariant().Contains(filterLower);
                        break;
                    case "publisher":
                        shouldInclude = item.SubItems[3].Text.ToLowerInvariant().Contains(filterLower);
                        break;
                    case "tags":
                        // For tags, we'd need to store them in the tag property or add a tags column
                        shouldInclude = item.SubItems[0].Text.ToLowerInvariant().Contains(filterLower) ||
                                      item.SubItems[1].Text.ToLowerInvariant().Contains(filterLower);
                        break;
                    default:
                        shouldInclude = true;
                        break;
                }
                
                if (shouldInclude)
                {
                    filteredItems.Add(item);
                }
            }
            
            return filteredItems;
        }
        
        /// <summary>
        /// Updates the ListView with filtered results
        /// </summary>
        private void UpdateFilteredResults(ListView listView, List<ListViewItem> filteredItems)
        {
            listView.Items.Clear();
            foreach (var item in filteredItems)
            {
                listView.Items.Add(item);
            }
        }
        
        /// <summary>
        /// Sorts search results based on user selection
        /// </summary>
        private List<ListViewItem> SortSearchResults(ListView listView, string sortBy, bool ascending)
        {
            var items = listView.Items.Cast<ListViewItem>().ToList();
            
            switch (sortBy.ToLowerInvariant())
            {
                case "name":
                    return ascending ? 
                        items.OrderBy(item => item.SubItems[0].Text).ToList() : 
                        items.OrderByDescending(item => item.SubItems[0].Text).ToList();
                case "version":
                    return ascending ? 
                        items.OrderBy(item => item.SubItems[2].Text).ToList() : 
                        items.OrderByDescending(item => item.SubItems[2].Text).ToList();
                case "publisher":
                    return ascending ? 
                        items.OrderBy(item => item.SubItems[3].Text).ToList() : 
                        items.OrderByDescending(item => item.SubItems[3].Text).ToList();
                default:
                    return ascending ? 
                        items.OrderBy(item => item.SubItems[0].Text).ToList() : 
                        items.OrderByDescending(item => item.SubItems[0].Text).ToList();
            }
        }
        
        /// <summary>
        /// Shows detailed information about a selected package
        /// </summary>
        private async void ShowPackageDetails(string packageId)
        {
            try
            {
                var details = await _packageService.GetPackageDetailsAsync(packageId, verboseLogging);
                if (details != null)
                {
                    var detailsForm = new Form
                    {
                        Text = $"📦 Package Details: {details.Name}",
                        Size = new Size(600, 500),
                        StartPosition = FormStartPosition.CenterParent,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false
                    };
                    
                    ApplyThemeToForm(detailsForm);
                    
                    var detailsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
                    var detailsText = new TextBox
                    {
                        Multiline = true,
                        ReadOnly = true,
                        ScrollBars = ScrollBars.Vertical,
                        Dock = DockStyle.Fill,
                        Font = CreateFont(10F),
                        Text = $"Name: {details.Name}\n" +
                               $"ID: {details.Id}\n" +
                               $"Version: {details.Version}\n" +
                               $"Publisher: {details.Publisher}\n" +
                               $"Description: {details.Description}\n" +
                               $"Homepage: {details.Homepage}\n" +
                               $"License: {details.License}\n" +
                               $"Tags: {details.Tags}\n" +
                               $"Source: {details.Source}"
                    };
                    
                    detailsPanel.Controls.Add(detailsText);
                    detailsForm.Controls.Add(detailsPanel);
                    detailsForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to get package details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
