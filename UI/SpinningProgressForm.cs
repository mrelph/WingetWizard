using System;
using System.Drawing;
using System.Windows.Forms;

namespace UpgradeApp.UI
{
    /// <summary>
    /// Custom spinning progress form with animated WingetWizard logo.
    /// Features smooth rotation animation, dark theme styling, and customizable status messages.
    /// Designed to center on parent window and provide elegant visual feedback during operations.
    /// </summary>
    public class SpinningProgressForm : Form
    {
        private readonly System.Windows.Forms.Timer timer = new(); // Animation timer for smooth rotation
        private int rotationAngle = 0; // Current rotation angle for animation
        private readonly Image iconImage; // WingetWizard logo for spinning animation
        private readonly Label statusLabel; // Status message display

        /// <summary>
        /// Initializes a new spinning progress form with customizable status message.
        /// Creates a borderless, dark-themed popup with animated logo and status text.
        /// </summary>
        /// <param name="message">Status message to display (default: "Processing...")</param>
        public SpinningProgressForm(string message = "Processing...")
        {
            // Configure borderless, dark-themed popup window
            this.FormBorderStyle = FormBorderStyle.None; // Clean borderless design
            this.StartPosition = FormStartPosition.Manual; // Manual positioning for centering
            this.Size = new Size(200, 150); // Compact size for non-intrusive display
            this.BackColor = Color.FromArgb(45, 45, 48); // Dark theme background
            this.ShowInTaskbar = false; // Hide from taskbar for cleaner experience
            this.TopMost = true; // Always on top during operations

            // Load WingetWizard logo with fallback to system icon
            try
            {
                iconImage = Image.FromFile("Logo.ico"); // Primary: WingetWizard logo
            }
            catch
            {
                iconImage = SystemIcons.Information.ToBitmap(); // Fallback: System icon
            }

            // Status label with modern Calibri typography
            statusLabel = new Label
            {
                Text = message,
                ForeColor = Color.White, // High contrast white text
                Font = new Font("Calibri", 10F), // Modern Calibri font
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 30
            };

            this.Controls.Add(statusLabel);

            // Timer for smooth spinning animation (50ms intervals = 20 FPS)
            timer.Interval = 50; // Smooth animation at 20 FPS
            timer.Tick += (s, e) =>
            {
                rotationAngle = (rotationAngle + 10) % 360; // 10-degree increments for smooth rotation
                this.Invalidate(); // Trigger repaint for animation frame
            };

            this.Paint += OnPaint; // Register custom paint handler for logo rendering
            timer.Start(); // Begin animation immediately
        }

        /// <summary>
        /// Custom paint handler for rendering the spinning WingetWizard logo with smooth animation.
        /// Uses advanced graphics transformations for anti-aliased rotation around the center point.
        /// </summary>
        /// <param name="sender">The form triggering the paint event</param>
        /// <param name="e">Paint event arguments containing graphics context</param>
        private void OnPaint(object? sender, PaintEventArgs e)
        {
            if (iconImage == null) return; // Skip if icon failed to load

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; // Enable anti-aliasing for smooth edges

            // Calculate center position for logo placement (accounting for status label space)
            var centerX = this.Width / 2;
            var centerY = (this.Height - statusLabel.Height) / 2;
            var iconSize = 48; // Standard icon size for visibility and performance

            // Save current graphics state for restoration after transformations
            var state = g.Save();

            // Apply transformation matrix for rotation around center point
            g.TranslateTransform(centerX, centerY);       // Move origin to center
            g.RotateTransform(rotationAngle);             // Rotate by current angle
            g.TranslateTransform(-iconSize / 2, -iconSize / 2); // Offset for icon center

            // Render the rotated WingetWizard logo
            g.DrawImage(iconImage, 0, 0, iconSize, iconSize);

            // Restore original graphics state to prevent transformation leakage
            g.Restore(state);
        }

        /// <summary>
        /// Updates the status message displayed below the spinning logo.
        /// Provides real-time feedback during different operation phases.
        /// </summary>
        /// <param name="message">New status message to display</param>
        public void UpdateMessage(string message)
        {
            if (statusLabel != null)
                statusLabel.Text = message;
        }

        /// <summary>
        /// Centers the spinner popup on the parent window for optimal user experience.
        /// Calculates center position based on parent window location and dimensions.
        /// </summary>
        /// <param name="parent">Parent form to center on (typically MainForm)</param>
        public void CenterOnParent(Form parent)
        {
            if (parent != null)
            {
                // Calculate center position relative to parent window
                this.Location = new Point(
                    parent.Location.X + (parent.Width - this.Width) / 2,   // Horizontal center
                    parent.Location.Y + (parent.Height - this.Height) / 2  // Vertical center
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
}
