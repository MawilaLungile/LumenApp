# Lumen — Personal Finance, Beautifully Done

A feature-rich **.NET MAUI** (.NET 10) mobile app for tracking spending, budgets and savings goals.
Built as a portfolio piece to demonstrate production-style MAUI architecture, custom graphics, and polished UI/UX.

---

## Run it (2 steps)

1. Open **`Lumen.sln`** in **Visual Studio 2026** (or VS 2022 17.14+ with .NET 10) with the **.NET Multi-platform App UI** workload installed.
2. Choose a target in the toolbar — **Windows Machine** or an **Android Emulator** — and press **F5**.

NuGet packages restore automatically. No database setup, API keys or certificates needed:

| Package | Why |
|---|---|
| `CommunityToolkit.Mvvm` | Source-generated `[ObservableProperty]` / `[RelayCommand]` MVVM |
| `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green` | Local, offline SQLite database (created automatically on first launch) |

> On Windows the app opens in a phone-sized window (440×900) so the mobile design shows off properly.
> To build for iOS / macOS, uncomment the Apple target line in `Lumen.csproj` (requires a paired Mac).

---

## Features

**Onboarding**
- 4-slide animated `CarouselView` + `IndicatorView` welcome tour on a gradient background
- Personalisation (your name) stored with `Preferences`; can be replayed from Settings

**Home dashboard**
- Time-aware greeting and gradient avatar with your initials
- Glowing gradient **balance card** with monthly income / expenses and savings rate
- **Weekly insight** engine comparing this week vs last week
- **Animated bar chart** of the last 7 days (custom-drawn, highlights the peak day)
- **Animated donut chart** of spending by category with legend
- Recent activity feed, pull-to-refresh, staggered entrance animations, floating action button

**Activity**
- Transactions **grouped by day** with daily net totals
- Live **search** (title, category, note) and **filter chips** (All / Income / Expenses)
- **Swipe-to-delete** with confirmation; tap any row to edit

**Add / edit transaction**
- Expense ↔ Income segmented toggle with colour feedback
- Large amount entry with currency symbol, title, note, date & time
- Tappable **category grid** with spring animation + haptic feedback
- Full create / update / delete against SQLite

**Budgets**
- Overall monthly usage **progress ring**, spent vs. budgeted, remaining amount
- Per-category budget cards with **animated progress bars** that turn amber at 80 % and red when over
- "Left per day" guidance; add, change or delete budgets via native dialogs

**Savings goals**
- Two-column grid of goals with **progress rings**, deadlines and "per month needed"
- Add money / withdraw / delete; **celebration** when a goal is reached

**Settings**
- Light / Dark / System **theme switching** at runtime (`AppThemeBinding` throughout)
- Currency symbol picker (R, $, €, £, ₦, KSh, ₹, ¥)
- **Export to CSV** via the native share sheet
- Reload demo data / erase everything / replay onboarding

---

## Architecture

```
Lumen/
├── Charts/          Hand-drawn, animated charts on Microsoft.Maui.Graphics (no chart library)
│                    BarChartView · DonutChartView · ProgressRingView · LinearProgressView
├── Models/          SQLite entities (TransactionEntry, Budget, SavingsGoal) + Category catalogue
├── Services/        DatabaseService (async, lazy, thread-safe), SettingsService, Money formatting,
│                    SeedData (realistic demo data), Dialogs/Haptics helpers, ServiceHelper
├── ViewModels/      One VM per screen, CommunityToolkit.Mvvm source generators
├── Views/           XAML pages with compiled bindings (x:DataType) + reusable TransactionRow
├── Resources/       Design tokens (Colors.xaml), styles (Styles.xaml), SVG icons, splash
└── Platforms/       Android, Windows (unpackaged), iOS, MacCatalyst
```

**Patterns & techniques worth discussing in an interview**
- **MVVM** with source generators (`[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]`)
- **Dependency injection** via `MauiProgram` and `IPlatformApplication.Current.Services`
- **Shell navigation** with tab bar, registered routes and query parameters (`editor?id=42`) via `IQueryAttributable`
- **Compiled bindings** everywhere (`x:DataType`) for performance and build-time type safety
- **Custom controls**: `GraphicsView` + `IDrawable` with `BindableProperty` and the MAUI animation system
- **Async SQLite repository** with lazy initialisation guarded by `SemaphoreSlim`
- **Handler customisation** (borderless `Entry` on Android/Windows)
- **Theming** with design tokens, `AppThemeBinding`, and runtime `UserAppTheme` switching
- **Essentials APIs**: Preferences, Share, FileSystem, HapticFeedback, AppInfo, DeviceInfo
- `CollectionView` grouping, headers, empty views, `GridItemsLayout`, `SwipeView`, `RefreshView`, `CarouselView`
- 100 % offline and private by design

---

## Ideas to extend it
- Biometric lock (Plugin.Fingerprint) · Recurring transactions · Local notifications for budgets
- Unit tests for ViewModels (xUnit + an in-memory DatabaseService)
- Cloud sync (Azure Mobile Apps / Supabase) · Receipt scanning with the camera (MediaPicker + OCR)
