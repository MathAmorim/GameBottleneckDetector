# Graph Report - GameBottleneckDetector  (2026-10-26)

## Corpus Check
- 11 files · ~7,542 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 3 file(s) not represented in the graph (top: (none) 1, .manifest 1, .bat 1)

## Summary
- 233 nodes · 377 edges · 9 communities
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 15 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `39decb7a`
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
1. `Window` - 67 edges
2. `SensorMonitor` - 30 edges
3. `TextBlock` - 25 edges
4. `BottleneckSession` - 24 edges
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
Nodes (55): ColorBrush, Label, Pct, PctText, BadgeAdmin, BarBalance, BarCpuCore, BarCpuLoad (+47 more)

### Community 1 - "FrameMonitor"
Cohesion: 0.12
Nodes (4): FrameMonitor, Error, Running, FrameStats

### Community 2 - "GameBottleneckDetector"
Cohesion: 0.09
Nodes (5): Application, App, GameBottleneckDetector, HtmlReportExporter, DistributionItem

### Community 4 - "MainWindow"
Cohesion: 0.11
Nodes (12): AppSettings, RamDisplayMode, VramDisplayMode, MemoryDisplayMode, Gigabytes, Megabytes, Percentage, BtnDiscardSession (+4 more)

### Community 5 - "SensorMonitor"
Cohesion: 0.09
Nodes (19): SensorMonitor, CpuLoad, CpuName, CpuTemperature, GpuLoad, GpuName, GpuTemperature, IsElevated (+11 more)

### Community 6 - "SensorRow"
Cohesion: 0.12
Nodes (11): SensorRow, Category, Hardware, HardwareType, Id, Max, Min, Name (+3 more)

### Community 7 - "Limiter"
Cohesion: 0.08
Nodes (22): BottleneckAnalyzer, BottleneckSession, AvgFps, AvgLow1, Count, Done, Duration, IsContinuous (+14 more)

### Community 9 - "GameBottleneckDetector.csproj"
Cohesion: 0.33
Nodes (5): net8.0-windows, LibreHardwareMonitorLib (0.9.6), Microsoft.Diagnostics.Tracing.TraceEvent (3.2.8), System.Management (10.0.2), Microsoft.NET.Sdk

### Community 11 - "RadioButton"
Cohesion: 0.17
Nodes (14): Rb10Min, Rb1Min, Rb3Min, Rb5Min, RbContinuous, RbRamGb, RbRamMb, RbRamPct (+6 more)

## Knowledge Gaps
- **62 isolated node(s):** `Percentage`, `Gigabytes`, `Megabytes`, `RamDisplayMode`, `VramDisplayMode` (+57 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 90 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainWindow` connect `MainWindow` to `Window`, `FrameMonitor`, `GameBottleneckDetector`, `SensorMonitor`, `Limiter`, `RadioButton`?**
  _High betweenness centrality (0.594) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `MainWindow` (e.g. with `.Dispose()` and `.Dispose()`) actually correct?**
  _`MainWindow` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Percentage`, `Gigabytes`, `Megabytes` to the rest of the system?**
  _62 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Window` be split into smaller, more focused modules?**
  _Cohesion score 0.06168831168831169 - nodes in this community are weakly interconnected._
- **Why does `Window` connect `Window` to `RadioButton`, `MainWindow`?**
  _High betweenness centrality (0.465) - this node is a cross-community bridge._
- **Should `FrameMonitor` be split into smaller, more focused modules?**
  _Cohesion score 0.11857707509881422 - nodes in this community are weakly interconnected._
- **Why does `SensorMonitor` connect `SensorMonitor` to `FrameMonitor`, `GameBottleneckDetector`, `MainWindow`, `SensorRow`, `Limiter`?**
  _High betweenness centrality (0.298) - this node is a cross-community bridge._