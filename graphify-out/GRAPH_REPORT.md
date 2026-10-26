# Graph Report - GameBottleneckDetector  (2026-10-26)

## Corpus Check
- 12 files · ~8,434 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 1, .ico 1, .manifest 1)

## Summary
- 248 nodes · 401 edges · 9 communities
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 15 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8dfe845f`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Window
- FrameMonitor
- GameBottleneckDetector
- MainWindow
- SensorMonitor
- SensorRow
- Limiter
- GameBottleneckDetector.csproj
- RadioButton

## God Nodes (most connected - your core abstractions)
1. `Window` - 70 edges
2. `SensorMonitor` - 33 edges
3. `BottleneckSession` - 24 edges
4. `TextBlock` - 24 edges
5. `MainWindow` - 20 edges
6. `FrameMonitor` - 18 edges
7. `SensorRow` - 18 edges
8. `Limiter` - 14 edges
9. `RadioButton` - 13 edges
10. `LoadSample` - 10 edges

## Surprising Connections (you probably didn't know these)
- `MainWindow` --references--> `BottleneckSmoother`  [EXTRACTED]
  MainWindow.xaml.cs → BottleneckAnalyzer.cs
- `MainWindow` --references--> `BottleneckSession`  [EXTRACTED]
  MainWindow.xaml.cs → BottleneckAnalyzer.cs
- `MainWindow` --references--> `FrameMonitor`  [EXTRACTED]
  MainWindow.xaml.cs → FrameMonitor.cs
- `MainWindow` --inherits--> `Window`  [EXTRACTED]
  MainWindow.xaml.cs → MainWindow.xaml
- `MainWindow` --references--> `SensorMonitor`  [EXTRACTED]
  MainWindow.xaml.cs → SensorMonitor.cs

## Import Cycles
- None detected.

## Communities (9 total, 0 thin omitted)

### Community 0 - "Window"
Cohesion: 0.06
Nodes (59): ColorBrush, Foreground, Label, Pct, PctText, BadgeAdmin, BarBalance, BarCpuCore (+51 more)

### Community 1 - "FrameMonitor"
Cohesion: 0.15
Nodes (4): FrameMonitor, Error, Running, FrameStats

### Community 2 - "GameBottleneckDetector"
Cohesion: 0.08
Nodes (6): Application, App, AppInfo, Version, GameBottleneckDetector, DistributionItem

### Community 4 - "MainWindow"
Cohesion: 0.09
Nodes (12): AppSettings, RamDisplayMode, VramDisplayMode, MemoryDisplayMode, Gigabytes, Megabytes, Percentage, BtnDiscardSession (+4 more)

### Community 5 - "SensorMonitor"
Cohesion: 0.08
Nodes (20): MEMORYSTATUSEX, SensorMonitor, CpuLoad, CpuName, CpuTemperature, GpuLoad, GpuName, GpuTemperature (+12 more)

### Community 6 - "SensorRow"
Cohesion: 0.12
Nodes (11): SensorRow, Category, Hardware, HardwareType, Id, Max, Min, Name (+3 more)

### Community 7 - "Limiter"
Cohesion: 0.08
Nodes (23): BottleneckAnalyzer, BottleneckSession, AvgFps, AvgLow1, Count, Done, Duration, IsContinuous (+15 more)

### Community 9 - "GameBottleneckDetector.csproj"
Cohesion: 0.29
Nodes (6): net8.0-windows, FontAwesome.Sharp (6.6.0), LibreHardwareMonitorLib (0.9.6), Microsoft.Diagnostics.Tracing.TraceEvent (3.2.8), System.Management (10.0.2), Microsoft.NET.Sdk

### Community 11 - "RadioButton"
Cohesion: 0.17
Nodes (14): Rb10Min, Rb1Min, Rb3Min, Rb5Min, RbContinuous, RbRamGb, RbRamMb, RbRamPct (+6 more)

## Knowledge Gaps
- **65 isolated node(s):** `Version`, `Percentage`, `Gigabytes`, `Megabytes`, `RamDisplayMode` (+60 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 95 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainWindow` connect `MainWindow` to `Window`, `FrameMonitor`, `GameBottleneckDetector`, `SensorMonitor`, `Limiter`, `RadioButton`?**
  _High betweenness centrality (0.569) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `MainWindow` (e.g. with `.Dispose()` and `.Dispose()`) actually correct?**
  _`MainWindow` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Version`, `Percentage`, `Gigabytes` to the rest of the system?**
  _65 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Window` be split into smaller, more focused modules?**
  _Cohesion score 0.05649717514124294 - nodes in this community are weakly interconnected._
- **Why does `Window` connect `Window` to `RadioButton`, `MainWindow`?**
  _High betweenness centrality (0.463) - this node is a cross-community bridge._
- **Should `FrameMonitor` be split into smaller, more focused modules?**
  _Cohesion score 0.14619883040935672 - nodes in this community are weakly interconnected._
- **Why does `SensorMonitor` connect `SensorMonitor` to `FrameMonitor`, `GameBottleneckDetector`, `MainWindow`, `SensorRow`, `Limiter`?**
  _High betweenness centrality (0.307) - this node is a cross-community bridge._