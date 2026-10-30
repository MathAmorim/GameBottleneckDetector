# Graph Report - GameBottleneckDetector  (2026-10-30)

## Corpus Check
- 14 files · ~12,262 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 1, .ico 1, .manifest 1)

## Summary
- 316 nodes · 535 edges · 16 communities (14 shown, 2 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 21 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `552a811d`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Window
- FrameMonitor
- GameBottleneckDetector
- CompanionServer
- MainWindow
- SensorMonitor
- QrCodeGenerator
- Limiter
- Border
- GameBottleneckDetector.csproj
- ProgressBar
- RadioButton
- IconImage
- StackPanel
- ImgCompanionQr
- ListDistribution

## God Nodes (most connected - your core abstractions)
1. `Window` - 81 edges
2. `SensorMonitor` - 33 edges
3. `MainWindow` - 30 edges
4. `BottleneckSession` - 27 edges
5. `TextBlock` - 27 edges
6. `CompanionServer` - 21 edges
7. `FrameMonitor` - 18 edges
8. `SensorRow` - 18 edges
9. `RadioButton` - 16 edges
10. `Limiter` - 14 edges

## Surprising Connections (you probably didn't know these)
- `MainWindow` --references--> `AppSettings`  [EXTRACTED]
  MainWindow.xaml.cs → AppSettings.cs
- `MainWindow` --references--> `BottleneckSmoother`  [EXTRACTED]
  MainWindow.xaml.cs → BottleneckAnalyzer.cs
- `MainWindow` --references--> `BottleneckSession`  [EXTRACTED]
  MainWindow.xaml.cs → BottleneckAnalyzer.cs
- `MainWindow` --references--> `CompanionServer`  [EXTRACTED]
  MainWindow.xaml.cs → CompanionServer.cs
- `MainWindow` --references--> `FrameMonitor`  [EXTRACTED]
  MainWindow.xaml.cs → FrameMonitor.cs

## Import Cycles
- None detected.

## Communities (16 total, 2 thin omitted)

### Community 0 - "Window"
Cohesion: 0.10
Nodes (34): ColorBrush, Foreground, Label, Pct, PctText, LblCpuCore, TxtAdminStatus, TxtBalanceDesc (+26 more)

### Community 1 - "FrameMonitor"
Cohesion: 0.12
Nodes (4): FrameMonitor, Error, Running, FrameStats

### Community 2 - "GameBottleneckDetector"
Cohesion: 0.06
Nodes (14): AppInfo, Version, AppSettings, CompanionEnabled, CompanionPort, MarkerWindowSeconds, RamDisplayMode, VramDisplayMode (+6 more)

### Community 3 - "CompanionServer"
Cohesion: 0.11
Nodes (5): CompanionServer, ErrorMessage, IsRunning, LocalUrl, Port

### Community 4 - "MainWindow"
Cohesion: 0.15
Nodes (7): BtnCopyCompanionUrl, BtnDiscardSession, BtnExportHtml, BtnOpenCompanionBrowser, BtnSession, MainWindow, Button

### Community 5 - "SensorMonitor"
Cohesion: 0.05
Nodes (30): SensorMonitor, CpuLoad, CpuName, CpuTemperature, GpuLoad, GpuName, GpuTemperature, IsElevated (+22 more)

### Community 6 - "QrCodeGenerator"
Cohesion: 0.14
Nodes (3): Application, App, QrCodeGenerator

### Community 7 - "Limiter"
Cohesion: 0.07
Nodes (25): BottleneckAnalyzer, BottleneckSession, AvgFps, AvgLow1, Count, Done, Duration, IsContinuous (+17 more)

### Community 8 - "Border"
Cohesion: 0.17
Nodes (10): BadgeAdmin, BadgeCompanionTop, BoxThermalAlert, CardAgora, optBorder, PanelResults, PillStatus, sqBox (+2 more)

### Community 9 - "GameBottleneckDetector.csproj"
Cohesion: 0.29
Nodes (6): net8.0-windows, FontAwesome.Sharp (6.6.0), LibreHardwareMonitorLib (0.9.6), Microsoft.Diagnostics.Tracing.TraceEvent (3.2.8), System.Management (10.0.2), Microsoft.NET.Sdk

### Community 10 - "ProgressBar"
Cohesion: 0.22
Nodes (9): BarBalance, BarCpuCore, BarCpuLoad, BarCpuTemp, BarGpuLoad, BarGpuTemp, BarRam, BarVram (+1 more)

### Community 11 - "RadioButton"
Cohesion: 0.14
Nodes (19): ChkCompanionEnabled, Rb10Min, Rb1Min, Rb3Min, Rb5Min, RbContinuous, RbMarker20s, RbMarker30s (+11 more)

### Community 12 - "IconImage"
Cohesion: 0.50
Nodes (4): checkGlyph, IconAdminStatus, IconPillStatus, IconImage

### Community 13 - "StackPanel"
Cohesion: 0.50
Nodes (4): PanelDurations, TabContentDetector, TabContentSettings, StackPanel

## Knowledge Gaps
- **75 isolated node(s):** `Version`, `Percentage`, `Gigabytes`, `Megabytes`, `RamDisplayMode` (+70 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 112 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainWindow` connect `MainWindow` to `Window`, `FrameMonitor`, `GameBottleneckDetector`, `CompanionServer`, `SensorMonitor`, `Limiter`, `Border`, `RadioButton`?**
  _High betweenness centrality (0.608) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `MainWindow` (e.g. with `.Dispose()` and `.Dispose()`) actually correct?**
  _`MainWindow` has 3 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Version`, `Percentage`, `Gigabytes` to the rest of the system?**
  _75 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Window` be split into smaller, more focused modules?**
  _Cohesion score 0.10084033613445378 - nodes in this community are weakly interconnected._
- **Why does `Window` connect `Window` to `MainWindow`, `Border`, `ProgressBar`, `RadioButton`, `IconImage`, `StackPanel`, `ImgCompanionQr`, `ListDistribution`?**
  _High betweenness centrality (0.420) - this node is a cross-community bridge._
- **Should `FrameMonitor` be split into smaller, more focused modules?**
  _Cohesion score 0.12121212121212122 - nodes in this community are weakly interconnected._
- **Why does `SensorMonitor` connect `SensorMonitor` to `FrameMonitor`, `GameBottleneckDetector`, `CompanionServer`, `MainWindow`, `Limiter`?**
  _High betweenness centrality (0.248) - this node is a cross-community bridge._