using System.IO;
using System.Text;
using System.Text.Json;

namespace GameBottleneckDetector;

public static class HtmlReportExporter
{
    public static string GenerateHtmlReport(
        BottleneckSession session,
        string cpuName,
        string gpuName,
        string outputPath)
    {
        var main = session.Main ?? Limiter.Capped;
        var samples = session.Samples;
        string gameName = samples.FirstOrDefault(s => !string.IsNullOrEmpty(s.GameProcess))?.GameProcess ?? "Jogo / Aplicação 3D";

        double avgFps = session.AvgFps ?? 0;
        double minFps = session.MinFps ?? 0;
        double avgLow1 = session.AvgLow1 ?? 0;
        double maxCpuTemp = session.Max(x => x.CpuTemp) ?? 0;
        double avgCpuTemp = session.Avg(x => x.CpuTemp) ?? 0;
        double maxGpuTemp = session.Max(x => x.GpuTemp) ?? 0;
        double avgGpuTemp = session.Avg(x => x.GpuTemp) ?? 0;
        double maxCpuLoad = session.Max(x => x.CpuTotal) ?? 0;
        double avgCpuLoad = session.Avg(x => x.CpuTotal) ?? 0;
        double maxGpuLoad = session.Max(x => x.GpuLoad) ?? 0;
        double avgGpuLoad = session.Avg(x => x.GpuLoad) ?? 0;
        double maxVram = session.Max(x => x.VramPct) ?? 0;
        double maxRam = session.Max(x => x.RamPct) ?? 0;

        bool cpuThrottled = maxCpuTemp >= 95;
        bool vramFull = maxVram >= 97;

        var dist = session.Distribution();

        // Dados para o gráfico Chart.js
        var labels = samples.Select((s, i) => $"{i}s").ToArray();
        var fpsData = samples.Select(s => s.Fps ?? 0).ToArray();
        var low1Data = samples.Select(s => s.Low1Fps ?? 0).ToArray();
        var cpuTempData = samples.Select(s => s.CpuTemp ?? 0).ToArray();
        var gpuTempData = samples.Select(s => s.GpuTemp ?? 0).ToArray();
        var cpuLoadData = samples.Select(s => s.CpuTotal ?? 0).ToArray();
        var gpuLoadData = samples.Select(s => s.GpuLoad ?? 0).ToArray();
        var vramData = samples.Select(s => s.VramPct ?? 0).ToArray();

        var sb = new StringBuilder();
        sb.Append($@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Relatório de Desempenho & Gargalo - {gameName}</title>
    <script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>
    <style>
        :root {{
            --bg: #0d1117;
            --surface: #161b22;
            --surface-hover: #1f242c;
            --border: #30363d;
            --text-main: #f0f6fc;
            --text-muted: #8b949e;
            --accent-green: #238636;
            --accent-red: #da3633;
            --accent-orange: #d29922;
            --accent-blue: #58a6ff;
            --accent-purple: #bc8cff;
        }}
        * {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{
            background-color: var(--bg);
            color: var(--text-main);
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            padding: 30px 20px;
            line-height: 1.5;
        }}
        .container {{ max-width: 1200px; margin: 0 auto; }}
        header {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding-bottom: 20px;
            border-bottom: 1px solid var(--border);
            margin-bottom: 24px;
            flex-wrap: wrap;
            gap: 15px;
        }}
        h1 {{ font-size: 26px; font-weight: 700; color: #fff; }}
        .badge {{
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 13px;
            font-weight: 600;
            display: inline-block;
        }}
        .badge-red {{ background: rgba(218, 54, 51, 0.2); color: #f85149; border: 1px solid #da3633; }}
        .badge-orange {{ background: rgba(210, 153, 34, 0.2); color: #e3b341; border: 1px solid #d29922; }}
        .badge-green {{ background: rgba(35, 134, 54, 0.2); color: #3fb950; border: 1px solid #238636; }}
        .badge-blue {{ background: rgba(88, 166, 255, 0.2); color: #58a6ff; border: 1px solid #1f6feb; }}

        .kpi-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
            gap: 16px;
            margin-bottom: 24px;
        }}
        .card {{
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 10px;
            padding: 18px;
        }}
        .card-title {{ font-size: 13px; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }}
        .card-value {{ font-size: 28px; font-weight: 700; }}
        .card-sub {{ font-size: 12px; color: var(--text-muted); margin-top: 4px; }}
        .val-danger {{ color: #f85149 !important; }}
        .val-warning {{ color: #e3b341 !important; }}
        .val-good {{ color: #3fb950 !important; }}

        .alert-box {{
            background: rgba(218, 54, 51, 0.1);
            border-left: 4px solid var(--accent-red);
            border-radius: 6px;
            padding: 16px 20px;
            margin-bottom: 24px;
        }}
        .alert-box.info {{
            background: rgba(88, 166, 255, 0.1);
            border-left-color: var(--accent-blue);
        }}
        .alert-title {{ font-weight: 700; font-size: 16px; margin-bottom: 4px; }}

        .section-title {{ font-size: 18px; font-weight: 600; margin: 30px 0 15px; color: #fff; }}

        .chart-container {{
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 10px;
            padding: 20px;
            margin-bottom: 30px;
            height: 380px;
        }}

        .dist-bar {{
            display: flex;
            height: 24px;
            border-radius: 8px;
            overflow: hidden;
            margin-bottom: 12px;
            background: #21262d;
        }}
        .dist-segment {{
            height: 100%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 11px;
            font-weight: 600;
            color: #fff;
        }}
        .legend {{ display: flex; flex-wrap: wrap; gap: 15px; font-size: 13px; margin-bottom: 25px; }}
        .legend-item {{ display: flex; align-items: center; gap: 6px; }}
        .legend-color {{ width: 12px; height: 12px; border-radius: 3px; }}

        table {{
            width: 100%;
            border-collapse: collapse;
            font-size: 13px;
            margin-top: 10px;
        }}
        th, td {{
            padding: 10px 12px;
            text-align: left;
            border-bottom: 1px solid var(--border);
        }}
        th {{ background: #21262d; color: var(--text-muted); font-weight: 600; }}
        tr:hover {{ background: var(--surface-hover); }}
        .table-wrap {{
            max-height: 400px;
            overflow-y: auto;
            border: 1px solid var(--border);
            border-radius: 8px;
        }}
    </style>
</head>
<body>
<div class=""container"">
    <header>
        <div>
            <h1>Relatório de Diagnóstico de Jogo</h1>
            <div style=""color: var(--text-muted); font-size: 14px; margin-top: 4px;"">
                Jogo: <strong>{gameName}</strong> · Registrado em: {session.Start:dd/MM/yyyy HH:mm:ss} ({session.Count} amostras / {session.Count}s)
            </div>
            <div style=""color: var(--text-muted); font-size: 13px; margin-top: 2px;"">
                Hardware: <strong>{cpuName}</strong> + <strong>{gpuName}</strong>
            </div>
        </div>
        <div>
            <span class=""badge {(cpuThrottled ? "badge-red" : "badge-green")}"" style=""font-size: 15px; padding: 8px 16px;"">
                Diagnóstico Principal: {BottleneckAnalyzer.Title(main)}
            </span>
        </div>
    </header>

    {(cpuThrottled ? $@"
    <div class=""alert-box"">
        <div class=""alert-title"" style=""color: #f85149;"">⚠️ SUPER-AQUECIMENTO DETECTADO: Thermal Throttling da CPU</div>
        <p>A temperatura da CPU atingiu <strong>{maxCpuTemp:0} °C</strong> durante a sessão! Processadores de notebook (como o Core i7-7700HQ) reduzem automaticamente suas frequências de operação ao atingir 95–100 °C para prevenir danos físicos. Essa queda repentina de clock é a causa imediata dos travamentos e congelamentos (*stuttering*) que você experimentou.</p>
    </div>" : "")}

    {(vramFull ? $@"
    <div class=""alert-box"" style=""border-left-color: var(--accent-orange);"">
        <div class=""alert-title"" style=""color: #e3b341;"">⚠️ ESTOURO DE MEMÓRIA DE VÍDEO (VRAM)</div>
        <p>O uso de VRAM atingiu <strong>{maxVram:0}%</strong> dos 4 GB da sua GTX 1050 Ti. Quando a memória gráfica esgota, as texturas são descarregadas na memória RAM principal através do barramento PCIe, gerando congelamentos de 1 a 3 segundos. Reduza a qualidade gráfica das texturas.</p>
    </div>" : "")}

    <div class=""kpi-grid"">
        <div class=""card"">
            <div class=""card-title"">Taxa de Quadros (FPS)</div>
            <div class=""card-value val-good"">{avgFps:0.0} <span style=""font-size: 16px;"">FPS</span></div>
            <div class=""card-sub"">1% Low médio: <strong>{avgLow1:0.0} FPS</strong> · Mín: {minFps:0.0}</div>
        </div>
        <div class=""card"">
            <div class=""card-title"">Temperatura Máx. CPU</div>
            <div class=""card-value {(maxCpuTemp >= 95 ? "val-danger" : maxCpuTemp >= 85 ? "val-warning" : "val-good")}"">{maxCpuTemp:0} °C</div>
            <div class=""card-sub"">Média na partida: {avgCpuTemp:0.0} °C {(maxCpuTemp >= 95 ? "· ⚠️ Throttling" : "")}</div>
        </div>
        <div class=""card"">
            <div class=""card-title"">Temperatura Máx. GPU</div>
            <div class=""card-value {(maxGpuTemp >= 85 ? "val-danger" : maxGpuTemp >= 78 ? "val-warning" : "val-good")}"">{maxGpuTemp:0} °C</div>
            <div class=""card-sub"">Média na partida: {avgGpuTemp:0.0} °C</div>
        </div>
        <div class=""card"">
            <div class=""card-title"">Pico de VRAM (GPU)</div>
            <div class=""card-value {(maxVram >= 95 ? "val-danger" : maxVram >= 85 ? "val-warning" : "val-good")}"">{maxVram:0}%</div>
            <div class=""card-sub"">Limite físico: 4.0 GB GDDR5</div>
        </div>
        <div class=""card"">
            <div class=""card-title"">Carga Média CPU / GPU</div>
            <div class=""card-value"">{avgCpuLoad:0}% / {avgGpuLoad:0}%</div>
            <div class=""card-sub"">Pico CPU: {maxCpuLoad:0}% · Pico GPU: {maxGpuLoad:0}%</div>
        </div>
    </div>

    <div class=""section-title"">Distribuição de Gargalo ao Longo da Partida</div>
    <div class=""dist-bar"">");

        // Construir barra de distribuição
        foreach (var (lim, pct) in dist)
        {
            if (pct < 1) continue;
            string color = lim switch
            {
                Limiter.Thermal => "#da3633",
                Limiter.Vram => "#d29922",
                Limiter.Cpu => "#e91e63",
                Limiter.Gpu => "#238636",
                Limiter.Ram => "#bc8cff",
                _ => "#58a6ff"
            };
            string pctCss = pct.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            sb.Append($@"<div class=""dist-segment"" style=""width: {pctCss}%; flex: {pctCss} 0 auto; background: {color};"" title=""{BottleneckAnalyzer.Title(lim)}: {pct:0.0}%"">{pct:0}%</div>");
        }

        sb.Append(@"</div>
    <div class=""legend"">
        <div class=""legend-item""><div class=""legend-color"" style=""background: #238636;""></div> GPU no Limite (Esperado)</div>
        <div class=""legend-item""><div class=""legend-color"" style=""background: #da3633;""></div> Temperatura (Throttling)</div>
        <div class=""legend-item""><div class=""legend-color"" style=""background: #e91e63;""></div> Gargalo de CPU</div>
        <div class=""legend-item""><div class=""legend-color"" style=""background: #d29922;""></div> VRAM Cheia (Texturas)</div>
        <div class=""legend-item""><div class=""legend-color"" style=""background: #bc8cff;""></div> RAM Cheia</div>
        <div class=""legend-item""><div class=""legend-color"" style=""background: #58a6ff;""></div> Sem Gargalo (Trava FPS / Menu)</div>
    </div>

    <div class=""section-title"">Linha do Tempo: FPS vs Temperaturas e Cargas</div>
    <div class=""chart-container"">
        <canvas id=""perfChart""></canvas>
    </div>

    <div class=""section-title"">Recomendações Práticas para seu PC</div>
    <div class=""card"" style=""line-height: 1.8; margin-bottom: 30px;"">
        <p><strong>1. Gestão Térmica:</strong> " + (cpuThrottled ? "A CPU está atingindo a temperatura máxima segura (95°C+). Eleve a parte traseira do seu notebook Acer em 2 a 3 cm da mesa para facilitar a entrada de ar. É altamente recomendado realizar a troca de pasta térmica e limpeza de ventoinhas." : "As temperaturas mantiveram-se dentro dos parâmetros aceitáveis durante este teste.") + @"</p>
        <p><strong>2. Configuração de Taxa de Quadros:</strong> Trave o FPS do jogo em 60 FPS (ou 50 FPS) nas opções gráficas ou no Painel NVIDIA. Isso impede que a CPU trabalhe desnecessariamente a 100% tentando renderizar frames que o monitor de 60Hz não exibe.</p>
        <p><strong>3. Otimização de Gráficos:</strong> " + (vramFull ? "Mantenha a qualidade das texturas no Médio para não ultrapassar a VRAM de 4GB." : "Ajuste configurações que pesam na CPU (distância de visão, densidade de NPCs/multidões) para aliviar o i7-7700HQ.") + @"</p>
    </div>

    <div class=""section-title"">Leituras Detalhadas Segundo a Segundo (" + samples.Count + @" Amostras)</div>
    <div class=""table-wrap"">
        <table>
            <thead>
                <tr>
                    <th>Tempo</th>
                    <th>FPS</th>
                    <th>1% Low</th>
                    <th>Temp CPU</th>
                    <th>Temp GPU</th>
                    <th>Carga CPU</th>
                    <th>Núcleo Crítico</th>
                    <th>Carga GPU</th>
                    <th>VRAM</th>
                    <th>Diagnóstico no Momento</th>
                </tr>
            </thead>
            <tbody>");

        int sec = 0;
        foreach (var s in samples)
        {
            var l = s.Classify();
            string rowClass = l == Limiter.Thermal ? "style=\"background: rgba(218,54,51,0.15);\"" :
                             l == Limiter.Vram ? "style=\"background: rgba(210,153,34,0.15);\"" : "";
            sb.Append($@"
                <tr {rowClass}>
                    <td>{sec++}s</td>
                    <td><strong>{(s.Fps is > 0 ? $"{s.Fps:0}" : "—")}</strong></td>
                    <td>{(s.Low1Fps is > 0 ? $"{s.Low1Fps:0}" : "—")}</td>
                    <td style=""color: {(s.CpuTemp >= 95 ? "#f85149; font-weight: bold;" : "#f0f6fc")}"">{s.CpuTemp:0} °C</td>
                    <td>{s.GpuTemp:0} °C</td>
                    <td>{s.CpuTotal:0} %</td>
                    <td>{(s.CpuMaxCore != null ? $"{s.CpuMaxCore:0} % ({s.BusiestCore})" : "—")}</td>
                    <td>{s.GpuLoad:0} %</td>
                    <td style=""color: {(s.VramPct >= 95 ? "#e3b341; font-weight: bold;" : "#f0f6fc")}"">{s.VramPct:0} %</td>
                    <td>{BottleneckAnalyzer.Title(l)}</td>
                </tr>");
        }

        sb.Append($@"
            </tbody>
        </table>
    </div>
</div>

<script>
    const ctx = document.getElementById('perfChart').getContext('2d');
    new Chart(ctx, {{
        type: 'line',
        data: {{
            labels: {JsonSerializer.Serialize(labels)},
            datasets: [
                {{
                    label: 'FPS',
                    data: {JsonSerializer.Serialize(fpsData)},
                    borderColor: '#3fb950',
                    backgroundColor: 'rgba(63, 185, 80, 0.1)',
                    yAxisID: 'yFps',
                    borderWidth: 2,
                    pointRadius: 0,
                    tension: 0.2
                }},
                {{
                    label: 'Temp CPU (°C)',
                    data: {JsonSerializer.Serialize(cpuTempData)},
                    borderColor: '#f85149',
                    yAxisID: 'yPct',
                    borderWidth: 2,
                    borderDash: [4, 4],
                    pointRadius: 0
                }},
                {{
                    label: 'Carga GPU (%)',
                    data: {JsonSerializer.Serialize(gpuLoadData)},
                    borderColor: '#58a6ff',
                    yAxisID: 'yPct',
                    borderWidth: 1.5,
                    pointRadius: 0
                }},
                {{
                    label: 'VRAM (%)',
                    data: {JsonSerializer.Serialize(vramData)},
                    borderColor: '#e3b341',
                    yAxisID: 'yPct',
                    borderWidth: 1.5,
                    pointRadius: 0
                }}
            ]
        }},
        options: {{
            responsive: true,
            maintainAspectRatio: false,
            interaction: {{ mode: 'index', intersect: false }},
            scales: {{
                x: {{
                    grid: {{ color: '#21262d' }},
                    ticks: {{ color: '#8b949e', maxTicksLimit: 20 }}
                }},
                yFps: {{
                    type: 'linear',
                    position: 'left',
                    title: {{ display: true, text: 'FPS', color: '#3fb950' }},
                    grid: {{ color: '#21262d' }},
                    ticks: {{ color: '#3fb950' }}
                }},
                yPct: {{
                    type: 'linear',
                    position: 'right',
                    title: {{ display: true, text: '% / °C', color: '#8b949e' }},
                    grid: {{ display: false }},
                    ticks: {{ color: '#8b949e' }},
                    min: 0,
                    max: 105
                }}
            }},
            plugins: {{
                legend: {{ labels: {{ color: '#c9d1d9' }} }}
            }}
        }}
    }});
</script>
</body>
</html>");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        return outputPath;
    }
}
