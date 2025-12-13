using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error starting application:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "WingetWizard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        
        // Cool minimalist color palette - refined and sophisticated
        private static readonly Color PRIMARY_BLUE = Color.FromArgb(59, 130, 246);      // Sky blue
        private static readonly Color ACCENT_BLUE = Color.FromArgb(99, 102, 241);      // Indigo accent
        private static readonly Color SUCCESS_GREEN = Color.FromArgb(16, 185, 129);     // Emerald
        private static readonly Color WARNING_AMBER = Color.FromArgb(245, 158, 11);    // Amber
        private static readonly Color ERROR_RED = Color.FromArgb(239, 68, 68);         // Red
        private static readonly Color NEUTRAL_GRAY = Color.FromArgb(107, 114, 128);    // Gray
        private static readonly Color PURPLE_AI = Color.FromArgb(139, 92, 246);       // Purple
        private static readonly Color ORANGE_ACCENT = Color.FromArgb(249, 115, 22);  // Orange
        
        // Background colors - softer, more refined
        private static readonly Color BG_PRIMARY = Color.FromArgb(255, 255, 255);      // Pure white (light mode)
        private static readonly Color BG_SECONDARY = Color.FromArgb(250, 250, 250);    // Off-white
        private static readonly Color BG_TERTIARY = Color.FromArgb(245, 245, 245);    // Light gray
        private static readonly Color BG_DARK_PRIMARY = Color.FromArgb(13, 13, 13);    // Deep black (dark mode)
        private static readonly Color BG_DARK_SECONDARY = Color.FromArgb(20, 20, 20); // Charcoal
        private static readonly Color BG_DARK_TERTIARY = Color.FromArgb(28, 28, 28);  // Dark gray
        
        // Text colors - improved contrast and hierarchy
        private static readonly Color TEXT_PRIMARY = Color.FromArgb(15, 15, 15);      // Near black
        private static readonly Color TEXT_SECONDARY = Color.FromArgb(100, 100, 100);  // Medium gray
        private static readonly Color TEXT_TERTIARY = Color.FromArgb(150, 150, 150);  // Light gray
        private static readonly Color TEXT_DARK_PRIMARY = Color.FromArgb(250, 250, 250); // Off-white
        private static readonly Color TEXT_DARK_SECONDARY = Color.FromArgb(180, 180, 180); // Light gray
        private static readonly Color TEXT_DARK_TERTIARY = Color.FromArgb(130, 130, 130);  // Medium gray
        
        // Border colors - more subtle
        private static readonly Color BORDER_LIGHT = Color.FromArgb(235, 235, 235);     // Very light gray
        private static readonly Color BORDER_DARK = Color.FromArgb(40, 40, 40);         // Dark gray
        
        // Shadow colors for elevation
        private static readonly Color SHADOW_LIGHT = Color.FromArgb(0, 0, 0, 8);      // Subtle shadow
        private static readonly Color SHADOW_DARK = Color.FromArgb(0, 0, 0, 30);       // Dark shadow
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
        
        // Cancellation support
        private CancellationTokenSource? _currentOperationCancellation;
        private Button? _cancelButton;
        private DateTime? _operationStartTime;
        private int _operationCurrent = 0;
        private int _operationTotal = 0;
        
        // Retry support
        private List<string> _failedPackageIds = new();
        private string _lastOperationType = "";
        
        // Progress persistence
        private readonly string _progressStatePath = Path.Combine(Application.StartupPath, "operation_state.json");
        
        // Operation history
        private readonly string _operationHistoryPath = Path.Combine(Application.StartupPath, "operation_history.json");
        private readonly List<OperationHistoryEntry> _operationHistory = new();
        
        // Sidebar navigation
        private Panel? _sidebarPanel;
        private bool _sidebarCollapsed = false;
        private const int SIDEBAR_WIDTH_EXPANDED = 240;
        private const int SIDEBAR_WIDTH_COLLAPSED = 60;

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
                BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY),
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

            // Main greeting label with personalized message - refined typography
            var greetingLabel = new Label
            {
                Text = $"{greeting}, {userName}",
                Font = CreateFont(32F, FontStyle.Bold),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Subtitle with refined helpful tone
            var subtitleLabel = new Label
            {
                Text = "Ready to manage your packages? Choose an action below:",
                Font = CreateFont(15F, FontStyle.Regular),
                ForeColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY),
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
                ("Check Updates", "Scan for available package updates", "Start here to see what's new", PRIMARY_BLUE),
                ("AI Research", "Get intelligent upgrade recommendations", "AI-powered analysis and insights", PURPLE_AI),
                ("List All Apps", "View your complete software inventory", "See everything installed", NEUTRAL_GRAY),
                ("Export", "Save package information and reports", "Backup and share your data", ORANGE_ACCENT),
                ("Quick Start", "Begin with recommended actions", "Let AI guide your journey", SUCCESS_GREEN)
            };

            foreach (var (title, description, subtitle, color) in actionCards)
            {
                var card = CreateModernCard(title, description, subtitle, color);
                actionsPanel.Controls.Add(card);
            }

            // Modern status bar with refined design
            var statusBar = new Panel
            {
                Height = 36,
                Dock = DockStyle.Bottom,
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                Padding = new Padding(0, 0, 0, 1)
            };
            
            // Add subtle top border
            statusBar.Paint += (s, e) =>
            {
                var borderColor = GetThemeColor(BORDER_DARK, BORDER_LIGHT);
                using var pen = new Pen(borderColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, statusBar.Width, 0);
            };

            var statusLabel = new Label
            {
                Text = $"Ready • {DateTime.Now:HH:mm:ss} • WingetWizard {APP_VERSION}",
                Font = CreateFont(11F, FontStyle.Regular),
                ForeColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 0, 0, 0)
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
            var isDark = isDarkMode;
            var card = new Panel
            {
                Width = 220,
                Height = 140,
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                Margin = new Padding(12),
                Cursor = Cursors.Hand,
                Tag = title, // Store the action for potential click handling
                Padding = new Padding(20, 20, 20, 20)
            };

            // Track hover state for border color
            var isHovered = false;
            
            // Draw border with accent color on hover (single Paint handler)
            card.Paint += (s, e) =>
            {
                var borderColor = isHovered ? accentColor : GetThemeColor(BORDER_DARK, BORDER_LIGHT);
                using var pen = new Pen(borderColor, isHovered ? 2 : 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // Title with accent color - refined typography
            var cardTitle = new Label
            {
                Text = title,
                Font = CreateFont(14F, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 20),
                AutoSize = true
            };

            // Main description
            var cardDesc = new Label
            {
                Text = description,
                Font = CreateFont(11F, FontStyle.Regular),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                Location = new Point(20, 48),
                Size = new Size(180, 36),
                AutoEllipsis = true
            };

            // Subtitle in smaller font
            var cardSubtitle = new Label
            {
                Text = subtitle,
                Font = CreateFont(10F, FontStyle.Regular),
                ForeColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY),
                Location = new Point(20, 88),
                Size = new Size(180, 20),
                AutoEllipsis = true
            };

            // Modern hover effect with smooth transition
            var originalCardColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY);
            var hoverCardColor = GetThemeColor(BG_DARK_TERTIARY, BG_TERTIARY);
            
            card.MouseEnter += (s, e) =>
            {
                isHovered = true;
                card.BackColor = hoverCardColor;
                cardTitle.ForeColor = accentColor;
                card.Invalidate();
            };

            card.MouseLeave += (s, e) =>
            {
                isHovered = false;
                card.BackColor = originalCardColor;
                cardTitle.ForeColor = accentColor;
                card.Invalidate();
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

        // Helper method to create rounded rectangle path for cool minimalist buttons
        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            var diameter = radius * 2;
            
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }

        /// <summary>
        /// Creates the modern sidebar navigation panel
        /// </summary>
        private Panel CreateSidebar()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = SIDEBAR_WIDTH_EXPANDED,
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                Padding = new Padding(0)
            };
            
            // Add right border
            sidebar.Paint += (s, e) =>
            {
                var borderColor = GetThemeColor(BORDER_DARK, BORDER_LIGHT);
                using var pen = new Pen(borderColor, 1);
                e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };
            
            // Scrollable container for sidebar content
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            
            // Sidebar header with collapse button
            var sidebarHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = GetThemeColor(BG_DARK_TERTIARY, BG_TERTIARY),
                Padding = new Padding(12, 12, 12, 12)
            };
            
            var collapseButton = new Button
            {
                Text = "☰",
                Font = CreateFont(16F),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                BackColor = GetThemeColor(BG_DARK_TERTIARY, BG_TERTIARY), // Use theme color instead of transparent
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Location = new Point(12, 7),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            collapseButton.FlatAppearance.BorderSize = 0;
            collapseButton.FlatAppearance.BorderColor = collapseButton.BackColor;
            var originalCollapseBackColor = collapseButton.BackColor;
            collapseButton.Click += (s, e) => ToggleSidebar();
            collapseButton.MouseEnter += (s, e) => collapseButton.BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY);
            collapseButton.MouseLeave += (s, e) => collapseButton.BackColor = originalCollapseBackColor;
            
            sidebarHeader.Controls.Add(collapseButton);
            
            // Content panel for action groups
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.Transparent,
                AutoScroll = false
            };
            
            // Create action groups with buttons
            var primaryGroup = CreateActionGroup("Primary", new[] { btnCheck, btnListAll, btnSearchInstall });
            var packageGroup = CreateActionGroup("Package Management", new[] { btnUpgrade, btnUpgradeAll, btnInstall, btnUninstall, btnRepair });
            var aiGroup = CreateActionGroup("AI Features", new[] { btnResearch });
            var toolsGroup = CreateActionGroup("Tools", new[] { btnExport, btnLogs });
            var settingsGroup = CreateActionGroup("Settings", new[] { btnSettings, btnHelp });
            
            // Source selector panel
            var sourcePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(12, 12, 12, 8),
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            
            var sourceLabel = new Label
            {
                Text = "Source:",
                Font = CreateFont(9F, FontStyle.Regular),
                ForeColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY),
                Location = new Point(12, 0),
                AutoSize = true,
                Height = 20,
                BackColor = Color.Transparent
            };
            ApplyThemeToControl(sourceLabel);
            
            cmbSource.Dock = DockStyle.Bottom;
            cmbSource.Height = 32;
            cmbSource.Margin = new Padding(0);
            
            sourcePanel.Controls.Add(sourceLabel);
            sourcePanel.Controls.Add(cmbSource);
            
            // Add groups to content panel (bottom to top for proper docking with DockStyle.Top)
            // Order matters: last added appears at top, first added at bottom
            contentPanel.Controls.Add(sourcePanel);
            contentPanel.Controls.Add(primaryGroup);
            contentPanel.Controls.Add(packageGroup);
            contentPanel.Controls.Add(aiGroup);
            contentPanel.Controls.Add(toolsGroup);
            contentPanel.Controls.Add(settingsGroup);
            
            scrollPanel.Controls.Add(contentPanel);
            sidebar.Controls.Add(scrollPanel);
            sidebar.Controls.Add(sidebarHeader);
            
            return sidebar;
        }

        /// <summary>
        /// Creates an action group with header and buttons
        /// </summary>
        private Panel CreateActionGroup(string title, Button[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
            {
                buttons = new Button[0];
            }
            
            // Calculate total height: header (28px) + buttons (36px each + 8px margin = 44px each) + bottom padding (16px)
            var headerHeight = 28;
            var buttonHeight = 36;
            var buttonMargin = 8; // 4px top + 4px bottom
            var totalButtonHeight = buttons.Length * (buttonHeight + buttonMargin);
            var bottomPadding = 16;
            var totalHeight = headerHeight + totalButtonHeight + bottomPadding;
            
            var group = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = totalHeight,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            
            // Group header
            var header = new Label
            {
                Text = title.ToUpper(),
                Font = CreateFont(9F, FontStyle.Bold),
                ForeColor = GetThemeColor(TEXT_DARK_TERTIARY, TEXT_TERTIARY),
                Location = new Point(12, 8),
                AutoSize = true,
                Height = 20,
                BackColor = Color.Transparent
            };
            ApplyThemeToControl(header);
            
            // Buttons container - positioned below header using TableLayoutPanel for better control
            var buttonsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, headerHeight),
                Height = totalButtonHeight,
                BackColor = Color.Transparent,
                AutoSize = false,
                Padding = new Padding(0)
            };
            
            // Add buttons in reverse order (since they dock to Top, last one added appears at top)
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                buttons[i].Dock = DockStyle.Top;
                buttons[i].Height = buttonHeight;
                buttons[i].Margin = new Padding(8, 4, 8, 4);
                buttons[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                buttonsPanel.Controls.Add(buttons[i]);
            }
            
            // Create a container panel for header and buttons
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = headerHeight,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(header);
            
            group.Controls.Add(buttonsPanel);
            group.Controls.Add(headerPanel);
            
            return group;
        }

        /// <summary>
        /// Creates a sidebar-styled button with icon and text
        /// </summary>
        private Button CreateSidebarButton(string text, string icon, Color accentColor, string? tooltip = null)
        {
            var isDark = isDarkMode;
            var button = new Button
            {
                Text = _sidebarCollapsed ? icon : $"{icon} {text}",
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                FlatStyle = FlatStyle.Flat,
                Font = CreateFont(10.5F, FontStyle.Regular),
                TextAlign = _sidebarCollapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
                Padding = _sidebarCollapsed ? new Padding(0) : new Padding(12, 0, 12, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.BorderColor = button.BackColor;
            
            var originalBackColor = button.BackColor;
            var originalForeColor = button.ForeColor;
            
            // Hover effect
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = GetThemeColor(BG_DARK_TERTIARY, BG_TERTIARY);
                button.ForeColor = accentColor;
            };
            
            button.MouseLeave += (s, e) =>
            {
                button.BackColor = originalBackColor;
                button.ForeColor = originalForeColor;
            };
            
            // Pressed state
            button.MouseDown += (s, e) =>
            {
                button.BackColor = accentColor;
                button.ForeColor = Color.White;
            };
            
            button.MouseUp += (s, e) =>
            {
                button.BackColor = GetThemeColor(BG_DARK_TERTIARY, BG_TERTIARY);
                button.ForeColor = accentColor;
            };
            
            if (!string.IsNullOrEmpty(tooltip))
            {
                buttonToolTips?.SetToolTip(button, tooltip);
            }
            
            return button;
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
            
            // Load operation history and progress state (with error handling)
            try
            {
                LoadOperationHistory();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load operation history: {ex.Message}");
            }
            
            try
            {
                LoadProgressState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load progress state: {ex.Message}");
            }
            
            InitializeComponent();
            
            // Start performance metrics collection timer (every 30 seconds)
            var metricsTimer = new System.Windows.Forms.Timer
            {
                Interval = 30000, // 30 seconds
                Enabled = true
            };
            metricsTimer.Tick += (s, e) => _performanceMetricsService.CollectSystemMetrics();
            metricsTimer.Start();
            
            // Handle form closing to save state
            this.FormClosing += MainForm_FormClosing;
        }
        
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveProgressState();
            SaveOperationHistory();
        }

        private void InitializeComponent()
        {
            this.Text = "WingetWizard - AI-Enhanced Package Manager";
            this.Size = new Size(1200, 800); // Larger size for modern, spacious feel
            this.MinimumSize = new Size(1000, 700);
            this.Font = CreateFont(11F); // Modern system font with fallback
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY);
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

            // Modern header with cool minimalist design
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY),
                Padding = new Padding(0, 0, 0, 0)
            };
            
            // Add subtle bottom border with gradient effect
            headerPanel.Paint += (s, e) =>
            {
                var borderColor = GetThemeColor(BORDER_DARK, BORDER_LIGHT);
                using var pen = new Pen(borderColor, 1);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            var headerLabel = new Label
            {
                Text = "WingetWizard",
                Font = CreateFont(24F, FontStyle.Bold),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(40, 0, 0, 0)
            };

            var subtitleLabel = new Label
            {
                Text = "AI-Enhanced Package Manager",
                Font = CreateFont(11.5F, FontStyle.Regular),
                ForeColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 0, 40, 0),
                AutoSize = false,
                Width = 300
            };

            headerPanel.Controls.Add(headerLabel);
            headerPanel.Controls.Add(subtitleLabel);
            
            // Modern progress indicator panel
            var progressPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                Visible = false,
                Padding = new Padding(0, 0, 0, 1)
            };
            
            // Add subtle bottom border
            progressPanel.Paint += (s, e) =>
            {
                var borderColor = GetThemeColor(BORDER_DARK, BORDER_LIGHT);
                using var pen = new Pen(borderColor, 1);
                e.Graphics.DrawLine(pen, 0, progressPanel.Height - 1, progressPanel.Width, progressPanel.Height - 1);
            };
            
            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Height = 3,
                Dock = DockStyle.Top,
                BackColor = GetThemeColor(BG_DARK_TERTIARY, BG_TERTIARY),
                ForeColor = PRIMARY_BLUE
            };
            
            statusLabel = new Label
            {
                Text = "Ready",
                Font = CreateFont(11F, FontStyle.Regular),
                ForeColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(32, 8, 0, 0)
            };
            
            // Modern cancel button for long-running operations
            _cancelButton = new Button
            {
                Text = "Cancel",
                Font = CreateFont(10F, FontStyle.Regular),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 90,
                Height = 28,
                Margin = new Padding(8, 6, 32, 6),
                Padding = new Padding(12, 6, 12, 6),
                Visible = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            
            // Set FlatAppearance after button is created
            _cancelButton.FlatAppearance.BorderSize = 1;
            _cancelButton.FlatAppearance.BorderColor = GetThemeColor(BORDER_DARK, BORDER_LIGHT);
            
            var originalCancelBackColor = _cancelButton.BackColor;
            var originalCancelForeColor = _cancelButton.ForeColor;
            var originalCancelBorderColor = _cancelButton.FlatAppearance.BorderColor;
            
            // Hover effect for cancel button
            _cancelButton.MouseEnter += (s, e) =>
            {
                _cancelButton.BackColor = ERROR_RED;
                _cancelButton.ForeColor = Color.White;
                _cancelButton.FlatAppearance.BorderColor = ERROR_RED;
            };
            
            _cancelButton.MouseLeave += (s, e) =>
            {
                _cancelButton.BackColor = originalCancelBackColor;
                _cancelButton.ForeColor = originalCancelForeColor;
                _cancelButton.FlatAppearance.BorderColor = originalCancelBorderColor;
            };
            _cancelButton.Click += (s, e) =>
            {
                _currentOperationCancellation?.Cancel();
                LogMessage("Operation cancelled by user");
            };
            
            progressPanel.Controls.Add(progressBar);
            progressPanel.Controls.Add(statusLabel);
            progressPanel.Controls.Add(_cancelButton);
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

            // Create buttons for sidebar first
            (btnCheck, btnUpgrade, btnUpgradeAll, btnListAll, btnResearch, btnLogs, btnExport, btnHelp, btnSettings) = 
                (CreateSidebarButton("Check Updates", "🔄", PRIMARY_BLUE, "Check for available package updates"),
                 CreateSidebarButton("Upgrade Selected", "⬆️", SUCCESS_GREEN, "Upgrade only the selected packages"),
                 CreateSidebarButton("Upgrade All", "🚀", SUCCESS_GREEN, "Upgrade all available packages at once"),
                 CreateSidebarButton("List All Apps", "📋", NEUTRAL_GRAY, "Show all installed applications"),
                 CreateSidebarButton("AI Research", "🤖", PURPLE_AI, "Get AI-powered package recommendations"),
                 CreateSidebarButton("Show Logs", "📄", NEUTRAL_GRAY, "Toggle log output visibility"), 
                 CreateSidebarButton("Export", "💾", ORANGE_ACCENT, "Export package list to file"),
                 CreateSidebarButton("Help", "❓", ACCENT_BLUE, "Show help menu and about information"), 
                 CreateSidebarButton("Settings", "⚙️", NEUTRAL_GRAY, "Configure application settings"));
            
            (btnInstall, btnUninstall, btnRepair) = (
                CreateSidebarButton("Install Selected", "📦", SUCCESS_GREEN, "Install the selected packages"),
                CreateSidebarButton("Uninstall Selected", "🗑️", ERROR_RED, "Uninstall the selected packages"),
                CreateSidebarButton("Repair Selected", "🔧", WARNING_AMBER, "Repair the selected packages"));
            
            btnSearchInstall = CreateSidebarButton("Search & Install", "🔍", PURPLE_AI, "Search for new packages and install them");
            
            cmbSource = new() { 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Margin = new Padding(12, 4, 12, 4),
                BackColor = GetThemeColor(BG_DARK_TERTIARY, BG_TERTIARY), 
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY), 
                FlatStyle = FlatStyle.Flat,
                Font = CreateFont(10F),
                Height = 32
            };
            cmbSource.Items.AddRange(new[] { "winget", "msstore", "all" });
            cmbSource.SelectedIndex = 0;
            buttonToolTips.SetToolTip(cmbSource, "Select package source: winget, Microsoft Store, or all sources");
            
            // Create modern sidebar navigation with buttons
            _sidebarPanel = CreateSidebar();
            
            splitter = new SplitContainer { 
                Dock = DockStyle.Fill, Orientation = Orientation.Vertical, 
                Margin = new Padding(24, 24, 24, 24), 
                BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY),
                SplitterWidth = 1, Panel1MinSize = 200, Panel2MinSize = 100,
                Panel2Collapsed = true
            };
            
            // Style the splitter divider with subtle styling
            splitter.Paint += (s, e) =>
            {
                var borderColor = GetThemeColor(BORDER_DARK, BORDER_LIGHT);
                using var pen = new Pen(borderColor, 1);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawLine(pen, splitter.SplitterDistance, 0, splitter.SplitterDistance, splitter.Height);
            };
            
            lstApps = new() { 
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = false, 
                CheckBoxes = true, MultiSelect = true, 
                BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY), 
                Font = CreateFont(11F), 
                BorderStyle = BorderStyle.None,
                OwnerDraw = false, // Use standard drawing for stability
                Padding = new Padding(0, 8, 0, 8)
            };
            
            // Add click handler for opening AI reports from status column
            lstApps.MouseClick += LstApps_MouseClick;
            
            txtLogs = new() { 
                Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, 
                Font = new Font("Consolas", 10F), 
                BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY), 
                ForeColor = GetThemeColor(Color.FromArgb(16, 185, 129), Color.FromArgb(5, 150, 105)), 
                Text = "=== WingetWizard Logs ===\n",
                BorderStyle = BorderStyle.None,
                Padding = new Padding(16, 16, 16, 16)
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
            
            // Modern ListView styling with refined appearance
            lstApps.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstApps.BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY);
            lstApps.ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
            lstApps.GridLines = false;
            lstApps.FullRowSelect = true;
            lstApps.View = View.Details;
            lstApps.CheckBoxes = true;
            lstApps.MultiSelect = true;
            lstApps.HideSelection = false;
            
            // Custom draw for alternating row colors (modern apps style)
            lstApps.OwnerDraw = true;
            lstApps.DrawItem += (s, e) =>
            {
                e.DrawDefault = true;
            };
            
            lstApps.DrawSubItem += (s, e) =>
            {
                if (e.ItemIndex % 2 == 0 && e.ItemIndex >= 0)
                {
                    var rowColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY);
                    using var brush = new SolidBrush(rowColor);
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
                e.DrawDefault = true;
            };
            
            // Add controls in proper z-order (back to front)
            this.Controls.Add(splitter);
            this.Controls.Add(_sidebarPanel);
            this.Controls.Add(progressPanel);
            this.Controls.Add(headerPanel);
            this.Controls.Add(versionLabel);
            
            // Load and apply sidebar state after initialization
            var sidebarCollapsed = _settingsService.GetSetting<bool>("SidebarCollapsed", false);
            if (sidebarCollapsed && _sidebarPanel != null)
            {
                _sidebarCollapsed = true;
                _sidebarPanel.Width = SIDEBAR_WIDTH_COLLAPSED;
                UpdateSidebarButtons();
            }
            
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
                _settingsService.SetSetting("SidebarCollapsed", _sidebarCollapsed.ToString());
                _settingsService.SaveSettings();
                LogMessage($"Settings saved with {_settingsService.GetAllSettings().Count} keys");
            }
            catch (Exception ex)
            {
                LogMessage($"Settings save error: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        /// <summary>
        /// Updates all sidebar buttons based on collapse state
        /// </summary>
        private void UpdateSidebarButtons()
        {
            UpdateButtonText(btnCheck, "🔄", "Check Updates");
            UpdateButtonText(btnListAll, "📋", "List All Apps");
            UpdateButtonText(btnSearchInstall, "🔍", "Search & Install");
            UpdateButtonText(btnUpgrade, "⬆️", "Upgrade Selected");
            UpdateButtonText(btnUpgradeAll, "🚀", "Upgrade All");
            UpdateButtonText(btnInstall, "📦", "Install Selected");
            UpdateButtonText(btnUninstall, "🗑️", "Uninstall Selected");
            UpdateButtonText(btnRepair, "🔧", "Repair Selected");
            UpdateButtonText(btnResearch, "🤖", "AI Research");
            UpdateButtonText(btnExport, "💾", "Export");
            UpdateButtonText(btnLogs, "📄", "Show Logs");
            UpdateButtonText(btnSettings, "⚙️", "Settings");
            UpdateButtonText(btnHelp, "❓", "Help");
        }
        
        /// <summary>
        /// Updates a button's text based on sidebar state
        /// </summary>
        private void UpdateButtonText(Button button, string icon, string text)
        {
            if (button == null) return;
            button.Text = _sidebarCollapsed ? icon : $"{icon} {text}";
            button.TextAlign = _sidebarCollapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
            button.Padding = _sidebarCollapsed ? new Padding(0) : new Padding(12, 0, 12, 0);
        }
        
        /// <summary>
        /// Toggles sidebar collapse/expand state
        /// </summary>
        private void ToggleSidebar()
        {
            if (_sidebarPanel == null) return;
            
            _sidebarCollapsed = !_sidebarCollapsed;
            
            // Animate width change
            var targetWidth = _sidebarCollapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH_EXPANDED;
            var timer = new System.Windows.Forms.Timer { Interval = 10 };
            var startWidth = _sidebarPanel.Width;
            var step = (targetWidth - startWidth) / 20.0f;
            var current = 0;
            
            timer.Tick += (s, e) =>
            {
                current++;
                if (current >= 20)
                {
                    _sidebarPanel.Width = targetWidth;
                    timer.Stop();
                    timer.Dispose();
                    UpdateSidebarButtons();
                    SaveSettings();
                    return;
                }
                
                _sidebarPanel.Width = (int)(startWidth + step * current);
            };
            
            timer.Start();
        }

        // Button click handlers using service classes
        private async void BtnCheck_Click(object? sender, EventArgs e)
        {
            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            var operationId = _performanceMetricsService.StartOperation("CheckForUpdates");
            try
            {
                ShowProgress("Checking for available updates...", 0, 0, showCancel: true);
                LogMessage("Checking for available updates...");
                var source = cmbSource.SelectedItem?.ToString() ?? "winget";
                var apps = await _packageService.CheckForUpdatesAsync(source, verboseLogging);
                
                token.ThrowIfCancellationRequested();
                
                lock (upgradableAppsLock)
                {
                    upgradableApps.Clear();
                    upgradableApps.AddRange(apps);
                }
                
                UpdatePackageList();
                var message = apps.Count > 0 
                    ? $"Found {apps.Count} package(s) with available updates"
                    : "No updates available";
                LogMessage(message);
                ShowNotification(message, apps.Count > 0 ? NotificationType.Success : NotificationType.Info, 4000);
                HideWelcomePanel();
                
                _performanceMetricsService.EndOperation(operationId, true);
            }
            catch (OperationCanceledException)
            {
                LogMessage("Check updates operation cancelled by user");
                ShowNotification("Operation cancelled", NotificationType.Warning, 3000);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking updates: {ex.Message}");
                ShowNotification($"Failed to check updates: {ex.Message}", NotificationType.Error, 5000);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
                HideProgress();
            }
        }

        private async void BtnUpgrade_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                ShowNotification("Please select packages to upgrade", NotificationType.Info);
                return;
            }

            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            var operationId = _performanceMetricsService.StartOperation("UpgradePackages");
            var successCount = 0;
            var failCount = 0;
            var failedPackages = new List<string>();
            _lastOperationType = "Upgrade";
            var startTime = DateTime.Now;
            
            try
            {
                var total = selectedItems.Count;
                ShowProgress($"Upgrading {total} packages...", 0, total, showCancel: true);
                LogMessage($"Upgrading {total} selected packages...");
                
                var current = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    token.ThrowIfCancellationRequested();
                    
                    var packageId = item.SubItems[1].Text; // ID column
                    var packageName = item.SubItems[0].Text;
                    current++;
                    UpdateProgress($"Upgrading {packageName}...", current, total);
                    
                    // Periodically save progress state
                    if (current % 5 == 0)
                    {
                        SaveProgressState();
                    }
                    
                    var (success, message) = await _packageService.UpgradePackageAsync(packageId, verboseLogging);
                    
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() =>
                        {
                            if (success)
                            {
                                item.SubItems[5].Text = "✅ Upgraded";
                                successCount++;
                            }
                            else
                            {
                                item.SubItems[5].Text = "❌ Failed";
                                failCount++;
                                failedPackages.Add(packageName);
                            }
                        });
                    }
                    else
                    {
                        if (success)
                        {
                            item.SubItems[5].Text = "✅ Upgraded";
                            successCount++;
                        }
                        else
                        {
                            item.SubItems[5].Text = "❌ Failed";
                            failCount++;
                            failedPackages.Add(packageName);
                        }
                    }
                    
                    LogMessage(success ? $"Successfully upgraded {packageName}" : $"Failed to upgrade {packageName}: {message}");
                    
                    // Log to history
                    AddOperationHistory("Upgrade", packageName, packageId, success, message);
                }
                
                var duration = DateTime.Now - startTime;
                var overallSuccess = failCount == 0;
                
                // Log batch operation to history
                AddOperationHistory("Upgrade Batch", "", "", overallSuccess, 
                    $"Upgraded {total} packages", total, successCount, failCount, duration);
                
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
                
                // Show summary notification
                if (failCount == 0)
                {
                    ShowNotification($"✅ Successfully upgraded {successCount} package(s)", NotificationType.Success, 5000);
                }
                else
                {
                    var summary = $"⚠️ Upgrade complete: {successCount} succeeded, {failCount} failed";
                    ShowNotification(summary, NotificationType.Warning, 5000);
                    
                    // Show detailed summary dialog for failures
                    if (failCount > 0)
                    {
                        var summaryText = $"Upgrade Summary:\n\n✅ Successful: {successCount}\n❌ Failed: {failCount}";
                        if (failedPackages.Count > 0)
                        {
                            summaryText += "\n\nFailed packages:\n• " + string.Join("\n• ", failedPackages.Take(10));
                            if (failedPackages.Count > 10)
                                summaryText += $"\n... and {failedPackages.Count - 10} more";
                        }
                        
                        var result = MessageBox.Show(
                            summaryText + "\n\nWould you like to retry failed packages?",
                            "Upgrade Summary",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information
                        );
                        
                        if (result == DialogResult.Yes)
                        {
                            // Retry failed packages
                            RetryFailedPackages("upgrade", failedPackages);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Upgrade operation cancelled by user");
                ShowNotification("Operation cancelled", NotificationType.Warning, 3000);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during upgrade: {ex.Message}");
                ShowNotification($"Upgrade failed: {ex.Message}", NotificationType.Error, 5000);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
                HideProgress();
            }
        }

        private async void BtnUpgradeAll_Click(object? sender, EventArgs e)
        {
            // Add confirmation for bulk operation
            var result = MessageBox.Show(
                "This will upgrade ALL available packages. This may take a while.\n\nContinue?",
                "Confirm Bulk Upgrade",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result != DialogResult.Yes)
                return;

            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            var operationId = _performanceMetricsService.StartOperation("UpgradeAllPackages");
            _lastOperationType = "UpgradeAll";
            var startTime = DateTime.Now;
            
            try
            {
                ShowProgress("Upgrading all available packages...", 0, 0, showCancel: true);
                LogMessage("Upgrading all available packages...");
                var (success, message) = await _packageService.UpgradeAllPackagesAsync(verboseLogging);
                
                token.ThrowIfCancellationRequested();
                
                var duration = DateTime.Now - startTime;
                
                // Log to history
                AddOperationHistory("Upgrade All", "", "", success, message, 0, success ? 1 : 0, success ? 0 : 1, duration);
                
                if (success)
                {
                    LogMessage("All packages upgraded successfully");
                    ShowNotification("✅ All packages upgraded successfully!", NotificationType.Success, 5000);
                    _performanceMetricsService.EndOperation(operationId, true);
                }
                else
                {
                    LogMessage($"Upgrade all failed: {message}");
                    ShowNotification($"Upgrade failed: {message}", NotificationType.Error, 5000);
                    _performanceMetricsService.EndOperation(operationId, false);
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Bulk upgrade operation cancelled by user");
                ShowNotification("Operation cancelled", NotificationType.Warning, 3000);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during bulk upgrade: {ex.Message}");
                ShowNotification($"Bulk upgrade failed: {ex.Message}", NotificationType.Error, 5000);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
                HideProgress();
            }
        }

        private async void BtnListAll_Click(object? sender, EventArgs e)
        {
            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            try
            {
                ShowProgress("Listing all installed applications...", 0, 0, showCancel: true);
                LogMessage("Listing all installed applications...");
                var source = cmbSource.SelectedItem?.ToString() ?? "winget";
                var apps = await _packageService.ListAllAppsAsync(source, verboseLogging);
                
                token.ThrowIfCancellationRequested();
                
                lock (upgradableAppsLock)
                {
                    upgradableApps.Clear();
                    upgradableApps.AddRange(apps);
                }
                
                UpdatePackageList();
                var message = $"Found {apps.Count} installed application(s)";
                LogMessage(message);
                ShowNotification(message, NotificationType.Success, 4000);
                HideWelcomePanel();
            }
            catch (OperationCanceledException)
            {
                LogMessage("List all operation cancelled by user");
                ShowNotification("Operation cancelled", NotificationType.Warning, 3000);
            }
            catch (Exception ex)
            {
                LogMessage($"Error listing applications: {ex.Message}");
                ShowNotification($"Failed to list applications: {ex.Message}", NotificationType.Error, 5000);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
                HideProgress();
            }
        }

        private async void BtnInstall_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                ShowNotification("Please select packages to install", NotificationType.Info);
                return;
            }

            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            var successCount = 0;
            var failCount = 0;
            var failedPackages = new List<string>();
            _lastOperationType = "Install";
            var startTime = DateTime.Now;

            try
            {
                var total = selectedItems.Count;
                ShowProgress($"Installing {total} packages...", 0, total, showCancel: true);
                LogMessage($"Installing {total} selected packages...");
                
                var current = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    token.ThrowIfCancellationRequested();
                    
                    var packageId = item.SubItems[1].Text; // ID column
                    var packageName = item.SubItems[0].Text;
                    current++;
                    UpdateProgress($"Installing {packageName}...", current, total);
                    
                    var (success, message) = await _packageService.InstallPackageAsync(packageId, verboseLogging);
                    
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() =>
                        {
                            if (success)
                            {
                                item.SubItems[5].Text = "✅ Installed";
                                successCount++;
                            }
                            else
                            {
                                item.SubItems[5].Text = "❌ Failed";
                                failCount++;
                                failedPackages.Add(packageName);
                            }
                        });
                    }
                    else
                    {
                        if (success)
                        {
                            item.SubItems[5].Text = "✅ Installed";
                            successCount++;
                        }
                        else
                        {
                            item.SubItems[5].Text = "❌ Failed";
                            failCount++;
                            failedPackages.Add(packageName);
                        }
                    }
                    
                    LogMessage(success ? $"Successfully installed {packageName}" : $"Failed to install {packageName}: {message}");
                    
                    // Log to history
                    AddOperationHistory("Install", packageName, packageId, success, message);
                }
                
                var duration = DateTime.Now - startTime;
                // Log batch operation to history
                AddOperationHistory("Install Batch", "", "", failCount == 0, 
                    $"Installed {total} packages", total, successCount, failCount, duration);
                
                // Show summary
                if (failCount == 0)
                {
                    ShowNotification($"✅ Successfully installed {successCount} package(s)", NotificationType.Success, 5000);
                }
                else
                {
                    var summary = $"⚠️ Installation complete: {successCount} succeeded, {failCount} failed";
                    ShowNotification(summary, NotificationType.Warning, 5000);
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Installation operation cancelled by user");
                ShowNotification("Operation cancelled", NotificationType.Warning, 3000);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during installation: {ex.Message}");
                ShowNotification($"Installation failed: {ex.Message}", NotificationType.Error, 5000);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
                HideProgress();
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

            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            var successCount = 0;
            var failCount = 0;
            var failedPackages = new List<string>();
            _lastOperationType = "Uninstall";
            var startTime = DateTime.Now;

            try
            {
                var total = selectedItems.Count;
                ShowProgress($"Uninstalling {total} packages...", 0, total, showCancel: true);
                LogMessage($"Uninstalling {total} selected packages...");
                
                var current = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    token.ThrowIfCancellationRequested();
                    
                    var packageId = item.SubItems[1].Text; // ID column
                    var packageName = item.SubItems[0].Text;
                    current++;
                    UpdateProgress($"Uninstalling {packageName}...", current, total);
                    
                    var (success, message) = await _packageService.UninstallPackageAsync(packageId, verboseLogging);
                    
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() =>
                        {
                            if (success)
                            {
                                item.SubItems[5].Text = "✅ Uninstalled";
                                successCount++;
                            }
                            else
                            {
                                item.SubItems[5].Text = "❌ Failed";
                                failCount++;
                                failedPackages.Add(packageName);
                            }
                        });
                    }
                    else
                    {
                        if (success)
                        {
                            item.SubItems[5].Text = "✅ Uninstalled";
                            successCount++;
                        }
                        else
                        {
                            item.SubItems[5].Text = "❌ Failed";
                            failCount++;
                            failedPackages.Add(packageName);
                        }
                    }
                    
                    LogMessage(success ? $"Successfully uninstalled {packageName}" : $"Failed to uninstall {packageName}: {message}");
                    
                    // Log to history
                    AddOperationHistory("Uninstall", packageName, packageId, success, message);
                }
                
                var duration = DateTime.Now - startTime;
                // Log batch operation to history
                AddOperationHistory("Uninstall Batch", "", "", failCount == 0, 
                    $"Uninstalled {total} packages", total, successCount, failCount, duration);
                
                // Show summary
                if (failCount == 0)
                {
                    ShowNotification($"✅ Successfully uninstalled {successCount} package(s)", NotificationType.Success, 5000);
                }
                else
                {
                    var summary = $"⚠️ Uninstall complete: {successCount} succeeded, {failCount} failed";
                    ShowNotification(summary, NotificationType.Warning, 5000);
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Uninstall operation cancelled by user");
                ShowNotification("Operation cancelled", NotificationType.Warning, 3000);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during uninstallation: {ex.Message}");
                ShowNotification($"Uninstallation failed: {ex.Message}", NotificationType.Error, 5000);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
                HideProgress();
            }
        }

        private async void BtnRepair_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                ShowNotification("Please select packages to repair", NotificationType.Info);
                return;
            }

            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            var successCount = 0;
            var failCount = 0;
            var failedPackages = new List<string>();
            _lastOperationType = "Repair";
            var startTime = DateTime.Now;

            try
            {
                var total = selectedItems.Count;
                ShowProgress($"Repairing {total} packages...", 0, total, showCancel: true);
                LogMessage($"Repairing {total} selected packages...");
                
                var current = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    token.ThrowIfCancellationRequested();
                    
                    var packageId = item.SubItems[1].Text; // ID column
                    var packageName = item.SubItems[0].Text;
                    current++;
                    UpdateProgress($"Repairing {packageName}...", current, total);
                    
                    var (success, message) = await _packageService.RepairPackageAsync(packageId, verboseLogging);
                    
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() =>
                        {
                            if (success)
                            {
                                item.SubItems[5].Text = "✅ Repaired";
                                successCount++;
                            }
                            else
                            {
                                item.SubItems[5].Text = "❌ Failed";
                                failCount++;
                                failedPackages.Add(packageName);
                            }
                        });
                    }
                    else
                    {
                        if (success)
                        {
                            item.SubItems[5].Text = "✅ Repaired";
                            successCount++;
                        }
                        else
                        {
                            item.SubItems[5].Text = "❌ Failed";
                            failCount++;
                            failedPackages.Add(packageName);
                        }
                    }
                    
                    LogMessage(success ? $"Successfully repaired {packageName}" : $"Failed to repair {packageName}: {message}");
                    
                    // Log to history
                    AddOperationHistory("Repair", packageName, packageId, success, message);
                }
                
                var duration = DateTime.Now - startTime;
                // Log batch operation to history
                AddOperationHistory("Repair Batch", "", "", failCount == 0, 
                    $"Repaired {total} packages", total, successCount, failCount, duration);
                
                // Show summary
                if (failCount == 0)
                {
                    ShowNotification($"✅ Successfully repaired {successCount} package(s)", NotificationType.Success, 5000);
                }
                else
                {
                    var summary = $"⚠️ Repair complete: {successCount} succeeded, {failCount} failed";
                    ShowNotification(summary, NotificationType.Warning, 5000);
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Repair operation cancelled by user");
                ShowNotification("Operation cancelled", NotificationType.Warning, 3000);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during repair: {ex.Message}");
                ShowNotification($"Repair failed: {ex.Message}", NotificationType.Error, 5000);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
                HideProgress();
            }
        }

        private async void BtnResearch_Click(object? sender, EventArgs e)
        {
            var selectedItems = lstApps.CheckedItems;
            if (selectedItems.Count == 0)
            {
                ShowNotification("Please select packages for AI research", NotificationType.Info);
                return;
            }

            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            _lastOperationType = "Research";
            var startTime = DateTime.Now;
            var successCount = 0;
            var failCount = 0;

            try
            {
                var total = selectedItems.Count;
                ShowProgress("Starting AI research...", 0, total, showCancel: true);
                LogMessage($"Starting AI research for {total} packages...");
                var recommendations = new List<(UpgradableApp app, string recommendation)>();
                
                var current = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    token.ThrowIfCancellationRequested();
                    
                    var app = new UpgradableApp
                    {
                        Name = item.SubItems[0].Text,
                        Id = item.SubItems[1].Text,
                        Version = item.SubItems[2].Text,
                        Available = item.SubItems[3].Text
                    };
                    
                    current++;
                    UpdateProgress($"Researching {app.Name}...", current, total);
                    LogMessage($"Researching {app.Name}...");
                    
                    try
                    {
                        var recommendation = await _aiService.GetAIRecommendationAsync(app);
                        recommendations.Add((app, recommendation));
                        successCount++;
                        
                        // Log to history
                        AddOperationHistory("Research", app.Name, app.Id, true, "AI research completed");
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        LogMessage($"Failed to research {app.Name}: {ex.Message}");
                        AddOperationHistory("Research", app.Name, app.Id, false, ex.Message);
                    }
                    
                    // Update the AI Recommendation column
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() => 
                        {
                            if (recommendations.Count > 0 && recommendations.Last().app.Id == app.Id)
                            {
                                item.SubItems[6].Text = SafeSubstring(recommendations.Last().recommendation, 50);
                            }
                        });
                    }
                    else
                    {
                        if (recommendations.Count > 0 && recommendations.Last().app.Id == app.Id)
                        {
                            item.SubItems[6].Text = SafeSubstring(recommendations.Last().recommendation, 50);
                        }
                    }
                }
                
                token.ThrowIfCancellationRequested();
                UpdateProgress("Saving reports...", total, total);
                
                // Save individual reports
                var markdownContent = _reportService.CreateMarkdownContent(recommendations, true, selectedAiModel);
                var reportsSaved = _reportService.SaveIndividualPackageReports(markdownContent);
                
                var duration = DateTime.Now - startTime;
                // Log batch operation to history
                AddOperationHistory("Research Batch", "", "", failCount == 0, 
                    $"Researched {total} packages, {reportsSaved} reports saved", total, successCount, failCount, duration);
                
                LogMessage($"AI research complete! {reportsSaved} individual reports saved.");
                ShowNotification($"✅ AI research complete! {reportsSaved} report(s) saved to AI_Reports folder", 
                    NotificationType.Success, 6000);
                
                // Update status columns with report links
                UpdateStatusColumnsWithReportLinks();
            }
            catch (OperationCanceledException)
            {
                LogMessage("AI research operation cancelled by user");
                ShowNotification("Operation cancelled", NotificationType.Warning, 3000);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during AI research: {ex.Message}");
                ShowNotification($"AI research failed: {ex.Message}", NotificationType.Error, 5000);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
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
                    ShowNotification($"✅ Package list exported to {Path.GetFileName(saveDialog.FileName)}", 
                        NotificationType.Success, 4000);
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
            
            helpMenu.BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY);
            helpMenu.ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
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
            settingsMenu.Items.Add("📜 Operation History", null, (s, args) => ShowOperationHistory());
            settingsMenu.Items.Add("-");
            settingsMenu.Items.Add("Reset API Keys", null, (s, args) => ResetApiKeys());
            
            settingsMenu.BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY);
            settingsMenu.ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
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
                                ShowMarkdownReport(reportPath, packageName);
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Failed to open report: {ex.Message}");
                                ShowNotification($"Failed to open report: {ex.Message}", NotificationType.Error, 5000);
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
            form.BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY);
            form.ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
            
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
                richTextBox.BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY);
                richTextBox.ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
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

        /// <summary>
        /// Creates a cool minimalist button with refined styling - rounded corners, subtle shadows, smooth transitions.
        /// Inspired by modern design systems like Vercel, Linear, and Raycast.
        /// </summary>
        private Button CreateModernButton(string text, Color accentColor, string? tooltip = null)
        {
            var isDark = isDarkMode;
            var button = new Button
            {
                Text = text,
                BackColor = isDark ? BG_DARK_SECONDARY : BG_SECONDARY,
                ForeColor = isDark ? TEXT_DARK_PRIMARY : TEXT_PRIMARY,
                FlatStyle = FlatStyle.Flat,
                Font = CreateFont(10.5F, FontStyle.Regular),
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 6, 6, 6),
                Padding = new Padding(16, 10, 16, 10),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            // Set FlatAppearance after button creation (required for proper initialization)
            // Note: Can't use Color.Transparent for BorderColor, so set BorderSize to 0 instead
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.BorderColor = button.BackColor; // Use same as background to hide border
            
            // Custom paint for rounded corners and subtle shadow
            button.Paint += (s, e) =>
            {
                var btn = s as Button;
                if (btn == null) return;
                
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                
                // Draw subtle shadow on hover (safer check)
                try
                {
                    var mousePos = btn.PointToClient(Control.MousePosition);
                    if (btn.ClientRectangle.Contains(mousePos))
                    {
                        using var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0));
                        var shadowRect = new Rectangle(btn.ClientRectangle.X + 2, btn.ClientRectangle.Y + 2, 
                                                       btn.ClientRectangle.Width, btn.ClientRectangle.Height);
                        using var path = new System.Drawing.Drawing2D.GraphicsPath();
                        path.AddEllipse(shadowRect);
                        e.Graphics.FillPath(shadowBrush, path);
                    }
                }
                catch
                {
                    // Ignore mouse position errors during initialization
                }
                
                // Draw rounded rectangle background
                using var bgBrush = new SolidBrush(btn.BackColor);
                var rect = new Rectangle(0, 0, btn.Width, btn.Height);
                using var bgPath = CreateRoundedRectanglePath(rect, 8);
                e.Graphics.FillPath(bgBrush, bgPath);
                
                // Draw text
                var textRect = new Rectangle(btn.Padding.Left, btn.Padding.Top, 
                                            btn.Width - btn.Padding.Horizontal, 
                                            btn.Height - btn.Padding.Vertical);
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(btn.Text, btn.Font, new SolidBrush(btn.ForeColor), textRect, sf);
            };
            
            // Store original colors for hover effects
            var originalBackColor = button.BackColor;
            var originalForeColor = button.ForeColor;
            
            // Cool minimalist hover effect - smooth color transition
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(248, 248, 248);
                button.ForeColor = accentColor;
                button.Invalidate(); // Trigger repaint for shadow
            };
            
            button.MouseLeave += (s, e) =>
            {
                button.BackColor = originalBackColor;
                button.ForeColor = originalForeColor;
                button.Invalidate();
            };
            
            // Pressed state - subtle press effect
            button.MouseDown += (s, e) =>
            {
                button.BackColor = accentColor;
                button.ForeColor = Color.White;
                button.Invalidate();
            };
            
            button.MouseUp += (s, e) =>
            {
                button.BackColor = isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(248, 248, 248);
                button.ForeColor = accentColor;
                button.Invalidate();
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
        private void ShowProgress(string message, int current = 0, int total = 0, bool showCancel = false)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowProgress(message, current, total, showCancel)));
                return;
            }
            
            var progressPanel = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "progress");
            if (progressPanel != null)
            {
                progressPanel.Visible = true;
                if (current == 0 && total > 0)
                {
                    _operationStartTime = DateTime.Now;
                }
                _operationCurrent = current;
                _operationTotal = total;
                
                if (total > 0)
                {
                    // Determinate progress with percentage
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Maximum = total;
                    progressBar.Value = Math.Min(current, total);
                    var percentage = (int)((double)current / total * 100);
                    
                    // Calculate ETA if we have timing data
                    string etaText = "";
                    if (current > 0 && _operationStartTime.HasValue)
                    {
                        var elapsed = DateTime.Now - _operationStartTime.Value;
                        var avgTimePerItem = elapsed.TotalMilliseconds / current;
                        var remaining = (total - current) * avgTimePerItem;
                        var eta = TimeSpan.FromMilliseconds(remaining);
                        etaText = $" - ETA: {eta:mm\\:ss}";
                    }
                    
                    statusLabel.Text = $"{message} ({current}/{total} - {percentage}%){etaText}";
                }
                else
                {
                    // Indeterminate progress
                    progressBar.Style = ProgressBarStyle.Marquee;
                    statusLabel.Text = message;
                }
                
                // Show/hide cancel button
                if (_cancelButton != null)
                {
                    _cancelButton.Visible = showCancel;
                }
            }
        }
        
        private void UpdateProgress(string message, int current = 0, int total = 0)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(message, current, total)));
                return;
            }
            
            _operationCurrent = current;
            _operationTotal = total;
            
            if (total > 0)
            {
                // Update determinate progress
                progressBar.Value = Math.Min(current, total);
                var percentage = (int)((double)current / total * 100);
                
                // Calculate ETA
                string etaText = "";
                if (current > 0 && _operationStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - _operationStartTime.Value;
                    var avgTimePerItem = elapsed.TotalMilliseconds / current;
                    var remaining = (total - current) * avgTimePerItem;
                    var eta = TimeSpan.FromMilliseconds(remaining);
                    etaText = $" - ETA: {eta:mm\\:ss}";
                }
                
                statusLabel.Text = $"{message} ({current}/{total} - {percentage}%){etaText}";
            }
            else
            {
                statusLabel.Text = message;
            }
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
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.Value = 0;
                
                if (_cancelButton != null)
                {
                    _cancelButton.Visible = false;
                }
            }
            
            _operationStartTime = null;
            _operationCurrent = 0;
            _operationTotal = 0;
        }
        
        // Notification system to replace MessageBox spam
        private void ShowNotification(string message, NotificationType type = NotificationType.Info, int durationMs = 3000)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowNotification(message, type, durationMs)));
                return;
            }
            
            // Use status bar for non-critical notifications
            var color = type switch
            {
                NotificationType.Success => Color.FromArgb(34, 197, 94),
                NotificationType.Warning => Color.FromArgb(245, 158, 11),
                NotificationType.Error => Color.FromArgb(239, 68, 68),
                _ => GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215))
            };
            
            statusLabel.ForeColor = color;
            statusLabel.Text = message;
            LogMessage(message);
            
            // Auto-clear after duration
            if (durationMs > 0)
            {
                var timer = new System.Windows.Forms.Timer { Interval = durationMs };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    if (statusLabel.Text == message) // Only clear if message hasn't changed
                    {
                        statusLabel.Text = "Ready";
                        statusLabel.ForeColor = GetThemeColor(Color.FromArgb(100, 200, 255), Color.FromArgb(0, 120, 215));
                    }
                };
                timer.Start();
            }
        }
        
        private enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
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

        /// <summary>
        /// Displays a markdown report in an in-app viewer with cool minimalist styling
        /// </summary>
        private void ShowMarkdownReport(string reportPath, string packageName)
        {
            if (!File.Exists(reportPath))
            {
                ShowNotification("Report file not found", NotificationType.Warning, 3000);
                return;
            }

            var reportForm = new Form
            {
                Text = $"AI Report: {packageName}",
                Size = new Size(1000, 700),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimumSize = new Size(800, 500)
            };
            ApplyThemeToForm(reportForm);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Header panel
            var headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                Padding = new Padding(20, 12, 20, 12)
            };

            var titleLabel = new Label
            {
                Text = $"📄 {packageName} - AI Research Report",
                Font = CreateFont(14F, FontStyle.Bold),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                AutoSize = true,
                Location = new Point(20, 12)
            };
            ApplyThemeToControl(titleLabel);

            var closeButton = new Button
            {
                Text = "✕",
                Font = CreateFont(12F, FontStyle.Bold),
                ForeColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY),
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(reportForm.Width - 50, 10),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.BorderColor = closeButton.BackColor;
            closeButton.Click += (s, e) => reportForm.Close();
            closeButton.MouseEnter += (s, e) => closeButton.ForeColor = ERROR_RED;
            closeButton.MouseLeave += (s, e) => closeButton.ForeColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY);

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(closeButton);

            // Content panel with WebBrowser for markdown rendering
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY),
                Padding = new Padding(30, 20, 30, 20)
            };

            // Read and convert markdown to HTML
            // Use RichTextBox as primary viewer (WebBrowser not available in .NET 6.0)
            try
            {
                var markdownContent = File.ReadAllText(reportPath, Encoding.UTF8);
                
                // Use RichTextBox with formatted markdown display
                var markdownViewer = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY),
                    ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                    Font = CreateFont(11F),
                    BorderStyle = BorderStyle.None,
                    WordWrap = true
                };
                ApplyThemeToControl(markdownViewer);
                
                // Format markdown for better readability
                FormatMarkdownForRichTextBox(markdownViewer, markdownContent);
                
                contentPanel.Controls.Add(markdownViewer);
            }
            catch (Exception ex)
            {
                // Fallback to plain text if formatting fails
                var fallbackText = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY),
                    ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                    Font = CreateFont(11F),
                    BorderStyle = BorderStyle.None,
                    Text = File.ReadAllText(reportPath, Encoding.UTF8)
                };
                ApplyThemeToControl(fallbackText);
                contentPanel.Controls.Add(fallbackText);
                
                LogMessage($"Using fallback text viewer for report: {ex.Message}");
            }

            mainPanel.Controls.Add(headerPanel, 0, 0);
            mainPanel.Controls.Add(contentPanel, 0, 1);

            reportForm.Controls.Add(mainPanel);
            reportForm.ShowDialog(this);
        }

        /// <summary>
        /// Formats markdown content for display in RichTextBox with basic styling
        /// </summary>
        private void FormatMarkdownForRichTextBox(RichTextBox rtb, string markdown)
        {
            rtb.Clear();
            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed.StartsWith("# "))
                {
                    rtb.SelectionFont = CreateFont(18F, FontStyle.Bold);
                    rtb.SelectionColor = PRIMARY_BLUE;
                    rtb.AppendText(trimmed.Substring(2) + "\n");
                }
                else if (trimmed.StartsWith("## "))
                {
                    rtb.SelectionFont = CreateFont(16F, FontStyle.Bold);
                    rtb.SelectionColor = ACCENT_BLUE;
                    rtb.AppendText(trimmed.Substring(3) + "\n");
                }
                else if (trimmed.StartsWith("### "))
                {
                    rtb.SelectionFont = CreateFont(14F, FontStyle.Bold);
                    rtb.SelectionColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
                    rtb.AppendText(trimmed.Substring(4) + "\n");
                }
                else if (trimmed == "---" || trimmed == "***")
                {
                    rtb.SelectionFont = CreateFont(11F);
                    rtb.SelectionColor = GetThemeColor(BORDER_DARK, BORDER_LIGHT);
                    rtb.AppendText(new string('─', 80) + "\n");
                }
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    rtb.SelectionFont = CreateFont(11F);
                    rtb.SelectionColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
                    rtb.AppendText("  • " + trimmed.Substring(2) + "\n");
                }
                else if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
                {
                    rtb.SelectionFont = CreateFont(11F);
                    rtb.SelectionColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
                    rtb.AppendText("  " + trimmed + "\n");
                }
                else if (trimmed.StartsWith("```"))
                {
                    rtb.SelectionFont = new Font("Consolas", 10F);
                    rtb.SelectionColor = GetThemeColor(TEXT_DARK_SECONDARY, TEXT_SECONDARY);
                }
                else if (!string.IsNullOrEmpty(trimmed))
                {
                    // Process inline formatting
                    rtb.SelectionFont = CreateFont(11F);
                    rtb.SelectionColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
                    
                    // Handle bold **text**
                    var processedLine = ProcessMarkdownLine(rtb, line);
                    rtb.AppendText(processedLine + "\n");
                }
                else
                {
                    rtb.AppendText("\n");
                }
            }
        }

        /// <summary>
        /// Processes a markdown line and applies formatting to RichTextBox
        /// </summary>
        private string ProcessMarkdownLine(RichTextBox rtb, string line)
        {
            // Simple processing - remove markdown syntax for now
            var processed = line;
            processed = Regex.Replace(processed, @"\*\*(.+?)\*\*", "$1"); // Remove bold
            processed = Regex.Replace(processed, @"\*(.+?)\*", "$1"); // Remove italic
            processed = Regex.Replace(processed, @"`(.+?)`", "$1"); // Remove code
            return processed;
        }

        /// <summary>
        /// Converts markdown to HTML (kept for potential future use)
        /// </summary>
        private string ConvertMarkdownToHtml(string markdown)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; padding: 20px; max-width: 1200px; margin: 0 auto; }");
            html.AppendLine("h1 { color: #3b82f6; border-bottom: 2px solid #3b82f6; padding-bottom: 10px; }");
            html.AppendLine("h2 { color: #60a5fa; margin-top: 30px; }");
            html.AppendLine("h3 { color: #93c5fd; margin-top: 20px; }");
            html.AppendLine("code { background-color: rgba(59, 130, 246, 0.1); padding: 2px 6px; border-radius: 3px; font-family: 'Consolas', 'Monaco', monospace; }");
            html.AppendLine("pre { background-color: rgba(0, 0, 0, 0.05); padding: 15px; border-radius: 8px; overflow-x: auto; }");
            html.AppendLine("pre code { background-color: transparent; padding: 0; }");
            html.AppendLine("ul, ol { margin-left: 20px; }");
            html.AppendLine("li { margin: 8px 0; }");
            html.AppendLine("strong { color: #3b82f6; font-weight: 600; }");
            html.AppendLine("em { font-style: italic; }");
            html.AppendLine("blockquote { border-left: 4px solid #3b82f6; padding-left: 15px; margin-left: 0; color: #6b7280; }");
            html.AppendLine("hr { border: none; border-top: 1px solid #e5e7eb; margin: 30px 0; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
            html.AppendLine("th, td { border: 1px solid #e5e7eb; padding: 12px; text-align: left; }");
            html.AppendLine("th { background-color: rgba(59, 130, 246, 0.1); font-weight: 600; }");
            html.AppendLine("a { color: #3b82f6; text-decoration: none; }");
            html.AppendLine("a:hover { text-decoration: underline; }");
            if (isDarkMode)
            {
                html.AppendLine("body { background-color: #0d0d0d; color: #fafafa; }");
                html.AppendLine("code { background-color: rgba(59, 130, 246, 0.2); }");
                html.AppendLine("pre { background-color: rgba(255, 255, 255, 0.05); }");
                html.AppendLine("th { background-color: rgba(59, 130, 246, 0.2); }");
                html.AppendLine("th, td { border-color: #374151; }");
                html.AppendLine("hr { border-color: #374151; }");
            }
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Simple markdown to HTML conversion
            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool inCodeBlock = false;
            bool inList = false;
            string listType = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Code blocks
                if (trimmed.StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        html.AppendLine("</pre>");
                        inCodeBlock = false;
                    }
                    else
                    {
                        var language = trimmed.Length > 3 ? trimmed.Substring(3).Trim() : "";
                        html.AppendLine($"<pre><code class='language-{language}'>");
                        inCodeBlock = true;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    html.AppendLine(WebUtility.HtmlEncode(line));
                    continue;
                }

                // Close list if needed
                if (inList && !trimmed.StartsWith("-") && !trimmed.StartsWith("*") && !trimmed.StartsWith("+") && !Regex.IsMatch(trimmed, @"^\d+\.\s") && trimmed != "")
                {
                    html.AppendLine($"</{listType}>");
                    inList = false;
                }

                // Headers
                if (trimmed.StartsWith("# "))
                {
                    html.AppendLine($"<h1>{ProcessInlineMarkdown(trimmed.Substring(2))}</h1>");
                }
                else if (trimmed.StartsWith("## "))
                {
                    html.AppendLine($"<h2>{ProcessInlineMarkdown(trimmed.Substring(3))}</h2>");
                }
                else if (trimmed.StartsWith("### "))
                {
                    html.AppendLine($"<h3>{ProcessInlineMarkdown(trimmed.Substring(4))}</h3>");
                }
                else if (trimmed.StartsWith("#### "))
                {
                    html.AppendLine($"<h4>{ProcessInlineMarkdown(trimmed.Substring(5))}</h4>");
                }
                // Horizontal rule
                else if (trimmed == "---" || trimmed == "***")
                {
                    html.AppendLine("<hr>");
                }
                // Unordered list
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ "))
                {
                    if (!inList || listType != "ul")
                    {
                        if (inList) html.AppendLine($"</{listType}>");
                        html.AppendLine("<ul>");
                        inList = true;
                        listType = "ul";
                    }
                    html.AppendLine($"<li>{ProcessInlineMarkdown(trimmed.Substring(2))}</li>");
                }
                // Ordered list
                else if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
                {
                    if (!inList || listType != "ol")
                    {
                        if (inList) html.AppendLine($"</{listType}>");
                        html.AppendLine("<ol>");
                        inList = true;
                        listType = "ol";
                    }
                    var listContent = Regex.Replace(trimmed, @"^\d+\.\s", "");
                    html.AppendLine($"<li>{ProcessInlineMarkdown(listContent)}</li>");
                }
                // Empty line
                else if (trimmed == "")
                {
                    html.AppendLine("<br>");
                }
                // Regular paragraph
                else
                {
                    html.AppendLine($"<p>{ProcessInlineMarkdown(trimmed)}</p>");
                }
            }

            if (inList)
            {
                html.AppendLine($"</{listType}>");
            }
            if (inCodeBlock)
            {
                html.AppendLine("</pre>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        /// <summary>
        /// Processes inline markdown formatting (bold, italic, code, links)
        /// </summary>
        private string ProcessInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Escape HTML first
            text = WebUtility.HtmlEncode(text);

            // Bold **text** or __text__
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            text = Regex.Replace(text, @"__(.+?)__", "<strong>$1</strong>");

            // Italic *text* or _text_ (but not if part of bold)
            text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "<em>$1</em>");
            text = Regex.Replace(text, @"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)", "<em>$1</em>");

            // Inline code `code`
            text = Regex.Replace(text, @"`(.+?)`", "<code>$1</code>");

            // Links [text](url)
            text = Regex.Replace(text, @"\[(.+?)\]\((.+?)\)", "<a href='$2' target='_blank'>$1</a>");

            return text;
        }

        /// <summary>
        /// Shows a command palette for quick action access (Ctrl+K)
        /// </summary>
        private void ShowCommandPalette()
        {
            var paletteForm = new Form
            {
                Text = "Command Palette",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                TopMost = true
            };
            ApplyThemeToForm(paletteForm);
            
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            
            // Search box
            var searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = CreateFont(14F),
                BackColor = GetThemeColor(BG_DARK_SECONDARY, BG_SECONDARY),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(20, 12, 20, 12)
            };
            ApplyThemeToControl(searchBox);
            
            // Add placeholder text manually (TextBox doesn't have PlaceholderText in .NET 6.0)
            var placeholderText = "Type to search commands... (Ctrl+K to open)";
            searchBox.Text = placeholderText;
            searchBox.ForeColor = GetThemeColor(TEXT_DARK_TERTIARY, TEXT_TERTIARY);
            searchBox.Enter += (s, e) =>
            {
                if (searchBox.Text == placeholderText)
                {
                    searchBox.Text = "";
                    searchBox.ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY);
                }
            };
            searchBox.Leave += (s, e) =>
            {
                if (string.IsNullOrEmpty(searchBox.Text))
                {
                    searchBox.Text = placeholderText;
                    searchBox.ForeColor = GetThemeColor(TEXT_DARK_TERTIARY, TEXT_TERTIARY);
                }
            };
            
            // Commands list
            var commandsList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = CreateFont(11F),
                BackColor = GetThemeColor(BG_DARK_PRIMARY, BG_PRIMARY),
                ForeColor = GetThemeColor(TEXT_DARK_PRIMARY, TEXT_PRIMARY),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };
            ApplyThemeToControl(commandsList);
            
            // Define commands
            (string name, string icon, Action action)[] commands = new (string, string, Action)[]
            {
                ("Check Updates", "🔄", () => BtnCheck_Click(null, EventArgs.Empty)),
                ("List All Apps", "📋", () => BtnListAll_Click(null, EventArgs.Empty)),
                ("Search & Install", "🔍", () => BtnSearchInstall_Click(null, EventArgs.Empty)),
                ("Upgrade Selected", "⬆️", () => BtnUpgrade_Click(null, EventArgs.Empty)),
                ("Upgrade All", "🚀", () => BtnUpgradeAll_Click(null, EventArgs.Empty)),
                ("Install Selected", "📦", () => BtnInstall_Click(null, EventArgs.Empty)),
                ("Uninstall Selected", "🗑️", () => BtnUninstall_Click(null, EventArgs.Empty)),
                ("Repair Selected", "🔧", () => BtnRepair_Click(null, EventArgs.Empty)),
                ("AI Research", "🤖", () => BtnResearch_Click(null, EventArgs.Empty)),
                ("Export", "💾", () => ExportUpgradeList(null, EventArgs.Empty)),
                ("Show Logs", "📄", () => BtnLogs_Click(null, EventArgs.Empty)),
                ("Operation History", "📜", () => ShowOperationHistory()),
                ("Settings", "⚙️", () => ShowSettingsMenu(null, EventArgs.Empty)),
                ("Help", "❓", () => ShowHelpMenu(null, EventArgs.Empty))
            };
            
            foreach ((string name, string icon, Action _) in commands)
            {
                commandsList.Items.Add($"{icon} {name}");
            }
            
            // Filter commands based on search
            searchBox.TextChanged += (s, e) =>
            {
                var search = searchBox.Text.ToLower();
                if (search == placeholderText.ToLower()) return;
                
                commandsList.Items.Clear();
                foreach ((string name, string icon, Action _) in commands)
                {
                    if (string.IsNullOrEmpty(search) || name.ToLower().Contains(search) || icon.Contains(search))
                    {
                        commandsList.Items.Add($"{icon} {name}");
                    }
                }
                if (commandsList.Items.Count > 0)
                    commandsList.SelectedIndex = 0;
            };
            
            // Execute command on Enter or double-click
            commandsList.DoubleClick += (s, e) => ExecuteCommand(commands, commandsList, paletteForm);
            searchBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && commandsList.SelectedIndex >= 0)
                {
                    ExecuteCommand(commands, commandsList, paletteForm);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    paletteForm.Close();
                }
                else if (e.KeyCode == Keys.Down)
                {
                    if (commandsList.Items.Count > 0)
                    {
                        commandsList.SelectedIndex = Math.Min(commandsList.SelectedIndex + 1, commandsList.Items.Count - 1);
                        commandsList.Focus();
                    }
                }
                else if (e.KeyCode == Keys.Up)
                {
                    commandsList.SelectedIndex = Math.Max(commandsList.SelectedIndex - 1, 0);
                    commandsList.Focus();
                }
            };
            
            commandsList.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ExecuteCommand(commands, commandsList, paletteForm);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    paletteForm.Close();
                }
            };
            
            // Close on click outside
            paletteForm.Deactivate += (s, e) => paletteForm.Close();
            
            mainPanel.Controls.Add(searchBox, 0, 0);
            mainPanel.Controls.Add(commandsList, 0, 1);
            paletteForm.Controls.Add(mainPanel);
            
            paletteForm.Show();
            searchBox.Focus();
        }
        
        /// <summary>
        /// Executes the selected command from the command palette
        /// </summary>
        private void ExecuteCommand((string name, string icon, Action action)[] commands, ListBox commandsList, Form paletteForm)
        {
            if (commandsList.SelectedIndex < 0) return;
            
            var selected = commandsList.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;
            
            // Find matching command
            foreach (var (name, icon, action) in commands)
            {
                if (selected.Contains(name))
                {
                    paletteForm.Close();
                    action();
                    return;
                }
            }
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

        // Keyboard shortcuts
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F5:
                    // Refresh - Check for updates
                    if (btnCheck.Enabled)
                    {
                        BtnCheck_Click(null, EventArgs.Empty);
                        return true;
                    }
                    break;
                case Keys.Escape:
                    // Cancel current operation
                    if (_currentOperationCancellation != null && !_currentOperationCancellation.Token.IsCancellationRequested)
                    {
                        _currentOperationCancellation.Cancel();
                        LogMessage("Operation cancelled via ESC key");
                        return true;
                    }
                    // Close dialogs if no operation running
                    if (this.ActiveControl is Form activeForm)
                    {
                        activeForm.Close();
                        return true;
                    }
                    break;
                case Keys.Control | Keys.U:
                    // Upgrade selected
                    if (btnUpgrade.Enabled)
                    {
                        BtnUpgrade_Click(null, EventArgs.Empty);
                        return true;
                    }
                    break;
                case Keys.Control | Keys.Shift | Keys.U:
                    // Upgrade all
                    if (btnUpgradeAll.Enabled)
                    {
                        BtnUpgradeAll_Click(null, EventArgs.Empty);
                        return true;
                    }
                    break;
                case Keys.Control | Keys.F:
                    // Search & Install
                    if (btnSearchInstall.Enabled)
                    {
                        BtnSearchInstall_Click(null, EventArgs.Empty);
                        return true;
                    }
                    break;
                case Keys.Control | Keys.R:
                    // AI Research
                    if (btnResearch.Enabled)
                    {
                        BtnResearch_Click(null, EventArgs.Empty);
                        return true;
                    }
                    break;
                case Keys.Control | Keys.L:
                    // List all apps
                    if (btnListAll.Enabled)
                    {
                        BtnListAll_Click(null, EventArgs.Empty);
                        return true;
                    }
                    break;
                case Keys.Control | Keys.A:
                    // Select all packages
                    if (lstApps.Items.Count > 0)
                    {
                        foreach (ListViewItem item in lstApps.Items)
                        {
                            item.Checked = true;
                        }
                        return true;
                    }
                    break;
                case Keys.Control | Keys.D:
                    // Deselect all packages
                    if (lstApps.Items.Count > 0)
                    {
                        foreach (ListViewItem item in lstApps.Items)
                        {
                            item.Checked = false;
                        }
                        return true;
                    }
                    break;
                case Keys.Control | Keys.E:
                    // Export
                    if (btnExport.Enabled)
                    {
                        ExportUpgradeList(null, EventArgs.Empty);
                        return true;
                    }
                    break;
                case Keys.Control | Keys.S:
                    // Settings
                    ShowSettingsMenu(null, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.H:
                    // Help
                    ShowHelpMenu(null, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.K:
                    // Command Palette
                    ShowCommandPalette();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Retry failed packages
        private async void RetryFailedPackages(string operationType, List<string> failedPackageNames)
        {
            if (failedPackageNames.Count == 0) return;

            // Find the failed packages in the list
            var failedItems = new List<ListViewItem>();
            foreach (ListViewItem item in lstApps.Items)
            {
                if (failedPackageNames.Contains(item.SubItems[0].Text))
                {
                    item.Checked = true;
                    failedItems.Add(item);
                }
            }

            if (failedItems.Count == 0)
            {
                ShowNotification("Could not find failed packages to retry", NotificationType.Warning);
                return;
            }

            // Create cancellation token source
            _currentOperationCancellation?.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();
            var token = _currentOperationCancellation.Token;

            var successCount = 0;
            var failCount = 0;
            var operationId = _performanceMetricsService.StartOperation($"Retry{operationType}");
            var startTime = DateTime.Now;

            try
            {
                var total = failedItems.Count;
                ShowProgress($"Retrying {total} failed package(s)...", 0, total, showCancel: true);
                LogMessage($"Retrying {total} failed {operationType} operation(s)...");

                var current = 0;
                foreach (var item in failedItems)
                {
                    token.ThrowIfCancellationRequested();

                    var packageId = item.SubItems[1].Text;
                    var packageName = item.SubItems[0].Text;
                    current++;
                    UpdateProgress($"Retrying {packageName}...", current, total);

                    var (success, message) = operationType.ToLower() switch
                    {
                        "upgrade" => await _packageService.UpgradePackageAsync(packageId, verboseLogging),
                        "install" => await _packageService.InstallPackageAsync(packageId, verboseLogging),
                        "uninstall" => await _packageService.UninstallPackageAsync(packageId, verboseLogging),
                        "repair" => await _packageService.RepairPackageAsync(packageId, verboseLogging),
                        _ => (false, "Unknown operation type")
                    };

                    if (this.InvokeRequired)
                    {
                        this.Invoke(() =>
                        {
                            if (success)
                            {
                                item.SubItems[5].Text = $"✅ {operationType}ed";
                                successCount++;
                            }
                            else
                            {
                                item.SubItems[5].Text = "❌ Failed";
                                failCount++;
                            }
                        });
                    }
                    else
                    {
                        if (success)
                        {
                            item.SubItems[5].Text = $"✅ {operationType}ed";
                            successCount++;
                        }
                        else
                        {
                            item.SubItems[5].Text = "❌ Failed";
                            failCount++;
                        }
                    }

                    // Log to history
                    AddOperationHistory($"Retry {operationType}", packageName, packageId, success, message);

                    LogMessage(success 
                        ? $"Successfully retried {packageName}" 
                        : $"Retry failed for {packageName}: {message}");
                }

                var duration = DateTime.Now - startTime;
                AddOperationHistory($"Retry {operationType} Batch", "", "", failCount == 0, 
                    $"Retried {total} packages", total, successCount, failCount, duration);

                if (failCount == 0)
                {
                    ShowNotification($"✅ Successfully retried {successCount} package(s)", NotificationType.Success, 5000);
                }
                else
                {
                    ShowNotification($"⚠️ Retry complete: {successCount} succeeded, {failCount} still failed", 
                        NotificationType.Warning, 5000);
                }

                _performanceMetricsService.EndOperation(operationId, failCount == 0);
            }
            catch (OperationCanceledException)
            {
                LogMessage("Retry operation cancelled by user");
                ShowNotification("Retry cancelled", NotificationType.Warning, 3000);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during retry: {ex.Message}");
                ShowNotification($"Retry failed: {ex.Message}", NotificationType.Error, 5000);
                _performanceMetricsService.EndOperation(operationId, false);
            }
            finally
            {
                _currentOperationCancellation?.Dispose();
                _currentOperationCancellation = null;
                HideProgress();
            }
        }

        // Progress persistence
        private void SaveProgressState()
        {
            try
            {
                if (_operationTotal > 0 && _operationCurrent < _operationTotal)
                {
                    var state = new
                    {
                        OperationType = _lastOperationType,
                        Current = _operationCurrent,
                        Total = _operationTotal,
                        StartTime = _operationStartTime,
                        Timestamp = DateTime.Now
                    };

                    var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_progressStatePath, json, Encoding.UTF8);
                    LogMessage($"Progress state saved: {_operationCurrent}/{_operationTotal}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to save progress state: {ex.Message}");
            }
        }

        private void LoadProgressState()
        {
            try
            {
                if (File.Exists(_progressStatePath))
                {
                    var json = File.ReadAllText(_progressStatePath, Encoding.UTF8);
                    var state = JsonSerializer.Deserialize<JsonElement>(json);

                    if (state.TryGetProperty("Timestamp", out var timestampProp))
                    {
                        var timestamp = timestampProp.GetDateTime();
                        // Only restore if less than 1 hour old
                        if (DateTime.Now - timestamp < TimeSpan.FromHours(1))
                        {
                            var operationType = state.TryGetProperty("OperationType", out var opType) 
                                ? opType.GetString() : "";
                            var current = state.TryGetProperty("Current", out var curr) ? curr.GetInt32() : 0;
                            var total = state.TryGetProperty("Total", out var tot) ? tot.GetInt32() : 0;

                            if (total > 0 && current < total)
                            {
                                var result = MessageBox.Show(
                                    $"Previous {operationType} operation was interrupted.\n\nProgress: {current}/{total}\n\nWould you like to see the operation history?",
                                    "Resume Operation?",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question);

                                if (result == DialogResult.Yes)
                                {
                                    ShowOperationHistory();
                                }
                            }
                        }
                    }

                    // Clean up old state file
                    File.Delete(_progressStatePath);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to load progress state: {ex.Message}");
            }
        }

        // Operation history
        private void AddOperationHistory(string operationType, string packageName, string packageId, 
            bool success, string message, int total = 0, int successCount = 0, int failCount = 0, TimeSpan? duration = null)
        {
            try
            {
                var entry = new OperationHistoryEntry
                {
                    Timestamp = DateTime.Now,
                    OperationType = operationType,
                    PackageName = packageName,
                    PackageId = packageId,
                    Success = success,
                    Message = message,
                    TotalPackages = total,
                    SuccessCount = successCount,
                    FailCount = failCount,
                    Duration = duration
                };

                lock (_operationHistory)
                {
                    _operationHistory.Add(entry);
                    // Keep only last 1000 entries
                    if (_operationHistory.Count > 1000)
                    {
                        _operationHistory.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to add operation history: {ex.Message}");
            }
        }

        private void SaveOperationHistory()
        {
            try
            {
                lock (_operationHistory)
                {
                    if (_operationHistory.Count > 0)
                    {
                        var json = JsonSerializer.Serialize(_operationHistory, new JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        File.WriteAllText(_operationHistoryPath, json, Encoding.UTF8);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to save operation history: {ex.Message}");
            }
        }

        private void LoadOperationHistory()
        {
            try
            {
                if (File.Exists(_operationHistoryPath))
                {
                    var json = File.ReadAllText(_operationHistoryPath, Encoding.UTF8);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var loaded = JsonSerializer.Deserialize<List<OperationHistoryEntry>>(json, options);
                    
                    if (loaded != null)
                    {
                        lock (_operationHistory)
                        {
                            _operationHistory.Clear();
                            _operationHistory.AddRange(loaded);
                        }
                        LogMessage($"Loaded {_operationHistory.Count} operation history entries");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to load operation history: {ex.Message}");
            }
        }

        private void ShowOperationHistory()
        {
            var historyForm = new Form
            {
                Text = "Operation History",
                Size = new Size(900, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimumSize = new Size(700, 400)
            };
            ApplyThemeToForm(historyForm);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Header with filter
            var headerPanel = new Panel { Dock = DockStyle.Fill };
            var filterLabel = new Label
            {
                Text = "Filter:",
                Location = new Point(10, 15),
                AutoSize = true
            };
            ApplyThemeToControl(filterLabel);

            var filterCombo = new ComboBox
            {
                Location = new Point(60, 12),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterCombo.Items.AddRange(new[] { "All", "Upgrade", "Install", "Uninstall", "Repair", "Research", "Retry" });
            filterCombo.SelectedIndex = 0;
            ApplyThemeToControl(filterCombo);

            var clearButton = new Button
            {
                Text = "Clear History",
                Location = new Point(220, 10),
                Width = 100,
                Height = 30
            };
            ApplyThemeToControl(clearButton);
            clearButton.Click += (s, e) =>
            {
                if (MessageBox.Show("Clear all operation history?", "Confirm", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    lock (_operationHistory)
                    {
                        _operationHistory.Clear();
                    }
                    SaveOperationHistory();
                    historyForm.Close();
                    ShowNotification("Operation history cleared", NotificationType.Success);
                }
            };

            headerPanel.Controls.Add(filterLabel);
            headerPanel.Controls.Add(filterCombo);
            headerPanel.Controls.Add(clearButton);

            // History list
            var historyList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                Columns = {
                    new ColumnHeader { Text = "Time", Width = 150 },
                    new ColumnHeader { Text = "Operation", Width = 100 },
                    new ColumnHeader { Text = "Package", Width = 200 },
                    new ColumnHeader { Text = "Status", Width = 80 },
                    new ColumnHeader { Text = "Details", Width = 300 }
                }
            };
            ApplyThemeToControl(historyList);

            void RefreshHistory()
            {
                historyList.Items.Clear();
                var filter = filterCombo.SelectedItem?.ToString() ?? "All";

                lock (_operationHistory)
                {
                    var filtered = filter == "All"
                        ? _operationHistory
                        : _operationHistory.Where(e => e.OperationType.Contains(filter, StringComparison.OrdinalIgnoreCase));

                    foreach (var entry in filtered.OrderByDescending(e => e.Timestamp).Take(500))
                    {
                        var item = new ListViewItem(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                        item.SubItems.Add(entry.OperationType);
                        item.SubItems.Add(string.IsNullOrEmpty(entry.PackageName) ? entry.PackageId : entry.PackageName);
                        item.SubItems.Add(entry.Success ? "✅ Success" : "❌ Failed");
                        
                        var details = entry.Message;
                        if (entry.TotalPackages > 0)
                        {
                            details = $"{entry.SuccessCount}/{entry.TotalPackages} succeeded";
                            if (entry.FailCount > 0)
                                details += $", {entry.FailCount} failed";
                        }
                        if (entry.Duration.HasValue)
                        {
                            details += $" ({entry.Duration.Value:mm\\:ss})";
                        }
                        item.SubItems.Add(details);

                        item.ForeColor = entry.Success 
                            ? Color.FromArgb(34, 197, 94) 
                            : Color.FromArgb(239, 68, 68);

                        historyList.Items.Add(item);
                    }
                }
            }

            filterCombo.SelectedIndexChanged += (s, e) => RefreshHistory();
            RefreshHistory();

            mainPanel.Controls.Add(headerPanel, 0, 0);
            mainPanel.Controls.Add(historyList, 0, 1);

            historyForm.Controls.Add(mainPanel);
            historyForm.ShowDialog(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) 
            { 
                // Save progress before disposing
                SaveProgressState();
                
                buttonToolTips?.Dispose();
                _aiService?.Dispose();
                _healthCheckService?.Dispose();
                _performanceMetricsService?.Dispose();
                _currentOperationCancellation?.Dispose();
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
