# Visual Design Specifications
## WingetWizard Windows 11 Design System

**Version:** 1.0
**Last Updated:** January 26, 2026
**Design Language:** Windows 11 Fluent Design

---

## Color System

### Light Theme Palette

```
Base Colors:
├─ Surface Primary:    #FFFFFF (rgb(255, 255, 255))
├─ Surface Secondary:  #F9F9F9 (rgb(249, 249, 249))
├─ Surface Tertiary:   #F3F3F3 (rgb(243, 243, 243))
├─ Card Background:    #FFFFFF with subtle shadow
├─ Stroke/Border:      #E3E3E3 (rgb(227, 227, 227))
└─ Divider:            #EBEBEB (rgb(235, 235, 235))

Text Colors:
├─ Primary:            #000000 (rgb(0, 0, 0))
├─ Secondary:          #606060 (rgb(96, 96, 96))
├─ Tertiary:           #969696 (rgb(150, 150, 150))
└─ Disabled:           #C7C7C7 (rgb(199, 199, 199))

Semantic Colors:
├─ Success:            #107C10 (rgb(16, 124, 16))
├─ Warning:            #F49C00 (rgb(244, 156, 0))
├─ Error:              #C42B1C (rgb(196, 43, 28))
├─ Info:               #0078D4 (rgb(0, 120, 212))
└─ Accent:             System Accent Color
```

### Dark Theme Palette

```
Base Colors:
├─ Surface Primary:    #202020 (rgb(32, 32, 32))
├─ Surface Secondary:  #2C2C2C (rgb(44, 44, 44))
├─ Surface Tertiary:   #1C1C1C (rgb(28, 28, 28))
├─ Card Background:    #2C2C2C with subtle highlight
├─ Stroke/Border:      #3A3A3A (rgb(58, 58, 58))
└─ Divider:            #333333 (rgb(51, 51, 51))

Text Colors:
├─ Primary:            #FFFFFF (rgb(255, 255, 255))
├─ Secondary:          #C8C8C8 (rgb(200, 200, 200))
├─ Tertiary:           #828282 (rgb(130, 130, 130))
└─ Disabled:           #5C5C5C (rgb(92, 92, 92))

Semantic Colors:
├─ Success:            #6CCB5F (rgb(108, 203, 95))
├─ Warning:            #FCE100 (rgb(252, 225, 0))
├─ Error:              #FF99A4 (rgb(255, 153, 164))
├─ Info:               #60CDFF (rgb(96, 205, 255))
└─ Accent:             System Accent Color (lighter variant)
```

### Color Usage Guidelines

**DO:**
- Use system accent color for primary actions
- Maintain 4.5:1 contrast ratio for text (WCAG AA)
- Use semantic colors consistently (green=success, red=error)
- Test colors in both light and dark themes

**DON'T:**
- Use pure black (#000000) or pure white (#FFFFFF) for large areas
- Mix warm and cool grays
- Use color as the only indicator (accessibility)
- Override system accent color for branding

---

## Typography Scale

### Font Families

**Primary:** Segoe UI Variable Display (Windows 11+)
**Secondary:** Segoe UI Variable Text (body text)
**Fallback:** Segoe UI → System Sans Serif

### Type Scale

```
Display:
├─ Size: 40px / 2.5rem
├─ Weight: 700 (Bold)
├─ Line Height: 52px / 1.3
├─ Letter Spacing: -0.5px
└─ Usage: Hero headings, welcome messages

Title Large:
├─ Size: 28px / 1.75rem
├─ Weight: 600 (SemiBold)
├─ Line Height: 36px / 1.28
├─ Letter Spacing: 0px
└─ Usage: Section headers, dialog titles

Title:
├─ Size: 18px / 1.125rem
├─ Weight: 600 (SemiBold)
├─ Line Height: 24px / 1.33
├─ Letter Spacing: 0px
└─ Usage: Card titles, subsection headers

Subtitle:
├─ Size: 14px / 0.875rem
├─ Weight: 600 (SemiBold)
├─ Line Height: 20px / 1.42
├─ Letter Spacing: 0px
└─ Usage: Group labels, emphasized text

Body:
├─ Size: 14px / 0.875rem
├─ Weight: 400 (Regular)
├─ Line Height: 20px / 1.42
├─ Letter Spacing: 0px
└─ Usage: Primary body text, descriptions

Body Strong:
├─ Size: 14px / 0.875rem
├─ Weight: 600 (SemiBold)
├─ Line Height: 20px / 1.42
├─ Letter Spacing: 0px
└─ Usage: Emphasized body text, labels

Caption:
├─ Size: 12px / 0.75rem
├─ Weight: 400 (Regular)
├─ Line Height: 16px / 1.33
├─ Letter Spacing: 0px
└─ Usage: Secondary information, timestamps

Caption Strong:
├─ Size: 12px / 0.75rem
├─ Weight: 600 (SemiBold)
├─ Line Height: 16px / 1.33
├─ Letter Spacing: 0px
└─ Usage: Small labels, metadata
```

### Typography Guidelines

**DO:**
- Use variable fonts for better rendering
- Maintain consistent line heights
- Use font weights for hierarchy (not size alone)
- Test readability at 125% and 150% scaling

**DON'T:**
- Use more than 3 font sizes on one screen
- Use font sizes smaller than 12px
- Use all caps for long text
- Rely solely on font weight for hierarchy

---

## Spacing System

### Base Unit: 4px

```
Spacing Scale:
├─ XXS:  4px   (0.25rem)  - Tight padding, icon spacing
├─ XS:   8px   (0.5rem)   - Compact padding, small gaps
├─ S:    12px  (0.75rem)  - Default padding, button spacing
├─ M:    16px  (1rem)     - Standard padding, card spacing
├─ L:    24px  (1.5rem)   - Section spacing, generous padding
├─ XL:   32px  (2rem)     - Large gaps, section headers
├─ XXL:  48px  (3rem)     - Major sections, hero spacing
└─ XXXL: 64px  (4rem)     - Page margins, dramatic spacing
```

### Component Spacing

```
Buttons:
├─ Horizontal Padding: 16px (M)
├─ Vertical Padding:   8px (XS)
├─ Icon-Text Gap:      8px (XS)
└─ Button Gap:         8px (XS)

Cards:
├─ Internal Padding:   20px (L-M)
├─ Card Gap:           12px (S)
├─ Card-to-Edge:       16px (M)
└─ Content Gap:        12px (S)

Lists:
├─ Row Height:         48px (min)
├─ Row Padding:        12px vertical
├─ Cell Padding:       12px horizontal
└─ Section Gap:        24px (L)

Panels:
├─ Panel Padding:      24px (L)
├─ Section Gap:        32px (XL)
├─ Edge Margins:       16px (M)
└─ Content Max Width:  1200px
```

---

## Corner Radius

### Radius Scale

```
Border Radius:
├─ None:      0px    - Raw, technical elements
├─ Small:     4px    - Buttons, inputs, chips
├─ Medium:    8px    - Cards, panels, dialogs
├─ Large:     12px   - Large cards, modals
└─ Circle:    50%    - Avatars, icons, badges
```

### Usage Examples

- **Buttons:** 4px
- **Cards:** 8px
- **Dialogs:** 8px
- **Tooltips:** 4px
- **Progress bars:** 4px
- **Badges:** 12px or circle

---

## Elevation & Shadows

### Shadow Levels

```
Level 0 (Base):
└─ No shadow - Flush with background

Level 1 (Card):
├─ Offset: 0px 2px
├─ Blur: 4px
├─ Spread: 0px
├─ Color: rgba(0, 0, 0, 0.08) light / rgba(0, 0, 0, 0.24) dark
└─ Usage: Cards, tiles

Level 2 (Elevated):
├─ Offset: 0px 4px
├─ Blur: 8px
├─ Spread: 0px
├─ Color: rgba(0, 0, 0, 0.12) light / rgba(0, 0, 0, 0.32) dark
└─ Usage: Dropdowns, floating elements

Level 3 (Dialog):
├─ Offset: 0px 8px
├─ Blur: 16px
├─ Spread: 0px
├─ Color: rgba(0, 0, 0, 0.16) light / rgba(0, 0, 0, 0.40) dark
└─ Usage: Dialogs, modals

Level 4 (Menu):
├─ Offset: 0px 16px
├─ Blur: 32px
├─ Spread: 0px
├─ Color: rgba(0, 0, 0, 0.20) light / rgba(0, 0, 0, 0.48) dark
└─ Usage: Context menus, tooltips
```

### Implementation

```csharp
// Windows Forms shadow simulation
private void ApplyShadow(Panel panel, int level)
{
    panel.Paint += (s, e) =>
    {
        var shadowOffset = level * 2;
        var shadowBlur = level * 4;
        var shadowOpacity = isDarkMode ? level * 12 : level * 4;

        var shadowRect = new Rectangle(
            shadowOffset,
            shadowOffset,
            panel.Width - shadowOffset,
            panel.Height - shadowOffset
        );

        using (var path = CreateRoundedRectanglePath(shadowRect, 8))
        using (var brush = new SolidBrush(Color.FromArgb(shadowOpacity, 0, 0, 0)))
        {
            e.Graphics.FillPath(brush, path);
        }

        // Draw actual panel on top
        var panelRect = new Rectangle(0, 0, panel.Width - shadowOffset, panel.Height - shadowOffset);
        using (var path = CreateRoundedRectanglePath(panelRect, 8))
        using (var brush = new SolidBrush(panel.BackColor))
        {
            e.Graphics.FillPath(brush, path);
        }
    };
}
```

---

## Icon System

### Icon Sizes

```
Icon Scale:
├─ 12px - Inline icons, very small indicators
├─ 16px - Standard UI icons, buttons
├─ 20px - Prominent icons, navigation
├─ 24px - Large buttons, key actions
└─ 32px - Feature icons, illustrations
```

### Recommended Icons (Segoe Fluent Icons)

```
Navigation & Actions:
├─ Refresh:           \uE72C
├─ Search:            \uE721
├─ Settings:          \uE713
├─ Help:              \uE897
├─ Close:             \uE711
└─ More:              \uE712

Content:
├─ Checkmark:         \uE73E
├─ CheckmarkCircle:   \uE8FB
├─ ErrorBadge:        \uEA39
├─ Warning:           \uE7BA
└─ Info:              \uE946

Package Management:
├─ Package:           \uE7B8
├─ Download:          \uE896
├─ Upload:            \uE898
├─ CloudDownload:     \uEBD3
└─ Delete:            \uE74D

Navigation:
├─ ChevronRight:      \uE76C
├─ ChevronDown:       \uE70D
├─ ChevronLeft:       \uE76B
└─ ChevronUp:         \uE70E
```

---

## Component Specifications

### Button Styles

#### Primary Button
```
Appearance:
├─ Background: System Accent Color
├─ Text: White / Contrast color
├─ Border: None
├─ Height: 32px
├─ Padding: 16px horizontal, 8px vertical
├─ Border Radius: 4px
└─ Font: Body Strong

States:
├─ Hover: 10% lighter
├─ Pressed: 10% darker
├─ Disabled: 40% opacity
└─ Focus: 2px accent border
```

#### Secondary Button
```
Appearance:
├─ Background: Transparent
├─ Text: System Accent Color
├─ Border: 1px Stroke color
├─ Height: 32px
├─ Padding: 16px horizontal, 8px vertical
├─ Border Radius: 4px
└─ Font: Body Strong

States:
├─ Hover: Surface Secondary background
├─ Pressed: Surface Tertiary background
├─ Disabled: 40% opacity
└─ Focus: 2px accent border
```

### Card Specifications

```
Standard Card:
├─ Background: Card Background color
├─ Border: 1px Stroke color (optional)
├─ Border Radius: 8px
├─ Padding: 20px
├─ Shadow: Level 1
├─ Margin: 12px
└─ Min Height: 120px

Hover State:
├─ Background: Slightly lighter/darker
├─ Shadow: Level 2
├─ Border: Accent color (2px)
└─ Transition: 150ms ease

Content Structure:
├─ Accent Bar: 4px width, left side
├─ Icon: 24px, top 20px, left 20px
├─ Title: Body Strong, top 20px
├─ Description: Body, top 48px
└─ Action Icon: Chevron right, right 20px
```

### List View Specifications

```
Header Row:
├─ Height: 40px
├─ Background: Surface Secondary
├─ Border Bottom: 1px Stroke color
├─ Text: Caption Strong
├─ Text Color: Secondary
└─ Padding: 12px horizontal

Data Row:
├─ Height: 48px (min)
├─ Background: Alternating (Primary/Secondary)
├─ Hover Background: Surface Tertiary
├─ Text: Body
├─ Padding: 12px horizontal, 8px vertical
└─ Separator: None (use alternating bg)

Selected Row:
├─ Background: Accent color (10% opacity)
├─ Left Border: 4px Accent color
├─ Text: Primary color
└─ Checkbox: Accent color
```

### Progress Indicators

```
Linear Progress Bar:
├─ Height: 4px
├─ Background: Surface Tertiary
├─ Fill: Accent color
├─ Border Radius: 4px
└─ Animation: Smooth fill, 300ms

Circular Progress Ring:
├─ Diameter: 32px
├─ Stroke Width: 3px
├─ Background Arc: Accent color (20% opacity)
├─ Progress Arc: Accent color
├─ Animation: Continuous rotation (indeterminate)
└─ Cap: Round

Indeterminate States:
├─ Linear: Shimmer effect
├─ Circular: 90-degree arc rotating
└─ Speed: 1 rotation per 1.5 seconds
```

---

## Animation Specifications

### Duration Scale

```
Timing:
├─ Instant:      0ms     - Immediate feedback
├─ Fast:         100ms   - Quick transitions, tooltips
├─ Normal:       200ms   - Standard transitions, hover
├─ Moderate:     300ms   - Dialogs, panels
├─ Slow:         500ms   - Page transitions, major changes
└─ Deliberate:   1000ms  - Loading states, celebrations
```

### Easing Functions

```
Standard Easing:
├─ Ease Out:     cubic-bezier(0.33, 1, 0.68, 1)
│                - Elements entering view, expanding
├─ Ease In:      cubic-bezier(0.32, 0, 0.67, 0)
│                - Elements leaving view, collapsing
└─ Ease In Out:  cubic-bezier(0.65, 0, 0.35, 1)
                 - State changes, toggles
```

### Common Animations

```
Fade In:
├─ Duration: 200ms
├─ Easing: Ease Out
├─ Property: Opacity (0 → 1)
└─ Usage: Tooltips, notifications

Slide In (from right):
├─ Duration: 300ms
├─ Easing: Ease Out
├─ Property: Transform (translateX(100%) → 0)
└─ Usage: Sidepanels, drawers

Scale Pop:
├─ Duration: 150ms
├─ Easing: Ease Out
├─ Property: Transform (scale(0.95) → scale(1))
└─ Usage: Modals, dialogs

Ripple Effect:
├─ Duration: 400ms
├─ Easing: Ease Out
├─ Property: Opacity + Scale
└─ Usage: Button press feedback
```

---

## Accessibility Requirements

### Color Contrast Ratios

```
WCAG AA Requirements:
├─ Normal Text (< 18px):     4.5:1 minimum
├─ Large Text (≥ 18px):      3:1 minimum
├─ UI Components:            3:1 minimum
└─ Graphical Objects:        3:1 minimum
```

### Touch Targets

```
Minimum Sizes:
├─ Touch Target:       44px × 44px
├─ Mouse Target:       32px × 32px
├─ Spacing Between:    8px minimum
└─ Icon Hit Area:      Larger than visual icon
```

### Focus Indicators

```
Focus Ring:
├─ Width: 2px
├─ Color: System Accent Color
├─ Offset: 2px from element
├─ Style: Solid
└─ Visible: Always when focused
```

### Screen Reader Labels

```
Required Attributes:
├─ AccessibleName: Short description
├─ AccessibleDescription: Detailed explanation
├─ AccessibleRole: Proper ARIA role
└─ Live Regions: For dynamic content
```

---

## Responsive Breakpoints

```
Layout Breakpoints:
├─ Compact:    < 1000px width
│              - Collapsed sidebar (icon only)
│              - Single column cards
│              - Reduced padding
│
├─ Medium:     1000-1400px width
│              - Standard sidebar (240px)
│              - Two column cards
│              - Normal spacing
│
└─ Expanded:   > 1400px width
               - Wide sidebar (280px)
               - Three+ column cards
               - Generous spacing
```

---

## Code Implementation Examples

### Color Helper Functions

```csharp
// Centralized color management
public static class DesignColors
{
    // Get theme-aware color
    public static Color GetColor(string colorName, bool isDark)
    {
        return colorName switch
        {
            "SurfacePrimary" => isDark ?
                Color.FromArgb(32, 32, 32) : Color.FromArgb(255, 255, 255),
            "SurfaceSecondary" => isDark ?
                Color.FromArgb(44, 44, 44) : Color.FromArgb(249, 249, 249),
            "TextPrimary" => isDark ?
                Color.FromArgb(255, 255, 255) : Color.FromArgb(0, 0, 0),
            "TextSecondary" => isDark ?
                Color.FromArgb(200, 200, 200) : Color.FromArgb(96, 96, 96),
            "Success" => isDark ?
                Color.FromArgb(108, 203, 95) : Color.FromArgb(16, 124, 16),
            "Warning" => isDark ?
                Color.FromArgb(252, 225, 0) : Color.FromArgb(244, 156, 0),
            "Error" => isDark ?
                Color.FromArgb(255, 153, 164) : Color.FromArgb(196, 43, 28),
            _ => Color.Transparent
        };
    }

    // Get system accent color
    public static Color GetAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int colorValue)
            {
                return Color.FromArgb(
                    (byte)((colorValue >> 16) & 0xFF),
                    (byte)((colorValue >> 8) & 0xFF),
                    (byte)(colorValue & 0xFF)
                );
            }
        }
        catch { }
        return Color.FromArgb(0, 120, 212);
    }
}
```

### Typography Helper Functions

```csharp
public static class DesignTypography
{
    private const string PRIMARY_FONT = "Segoe UI Variable Display";
    private const string TEXT_FONT = "Segoe UI Variable Text";

    public static Font Display => CreateFont(PRIMARY_FONT, 40F, FontStyle.Bold);
    public static Font TitleLarge => CreateFont(PRIMARY_FONT, 28F, FontStyle.SemiBold);
    public static Font Title => CreateFont(PRIMARY_FONT, 18F, FontStyle.SemiBold);
    public static Font Subtitle => CreateFont(TEXT_FONT, 14F, FontStyle.SemiBold);
    public static Font Body => CreateFont(TEXT_FONT, 14F, FontStyle.Regular);
    public static Font BodyStrong => CreateFont(PRIMARY_FONT, 14F, FontStyle.SemiBold);
    public static Font Caption => CreateFont(TEXT_FONT, 12F, FontStyle.Regular);

    private static Font CreateFont(string family, float size, FontStyle style)
    {
        try { return new Font(family, size, style); }
        catch
        {
            try { return new Font("Segoe UI", size, style); }
            catch { return new Font(FontFamily.GenericSansSerif, size, style); }
        }
    }
}
```

### Spacing Helper Functions

```csharp
public static class DesignSpacing
{
    public const int XXS = 4;
    public const int XS = 8;
    public const int S = 12;
    public const int M = 16;
    public const int L = 24;
    public const int XL = 32;
    public const int XXL = 48;
    public const int XXXL = 64;

    public static Padding GetPadding(int all) => new Padding(all);
    public static Padding GetPadding(int horizontal, int vertical) =>
        new Padding(horizontal, vertical, horizontal, vertical);
}
```

---

## Design Checklist

### Before Shipping Any UI

- [ ] Colors meet WCAG AA contrast requirements
- [ ] All text uses defined typography scale
- [ ] Spacing uses 4px grid system
- [ ] Corner radius is consistent across similar elements
- [ ] Shadows are appropriate for elevation level
- [ ] Icons are from Segoe Fluent Icons set
- [ ] Buttons have proper hover/pressed/disabled states
- [ ] Focus indicators are clearly visible
- [ ] Component works in both light and dark themes
- [ ] Touch targets meet 44×44px minimum
- [ ] Keyboard navigation is fully functional
- [ ] Screen reader labels are comprehensive
- [ ] Animations use appropriate duration and easing
- [ ] Layout is responsive at different window sizes

---

**Design Philosophy:** Every pixel serves a purpose. Every interaction delights. Every user feels empowered.

**Next Steps:** Use this specification as reference during implementation. When in doubt, favor simplicity and consistency over complexity.
