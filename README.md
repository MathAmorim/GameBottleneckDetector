# 🎮 Game Bottleneck Detector & Diagnostic

Aplicativo focado para diagnóstico em tempo real de gargalo e travamentos em jogos, baseado no núcleo de sensores e telemetria do ResetExplode, porém com melhorias críticas de estabilidade e análise aprofundada.

---

## ✨ Principais Melhorias em Relação ao ResetExplode Original

1. **Anti-Flicker com Média Móvel de 5 Segundos & Histerese:**
   - O card "AGORA" não pisca mais de preto para branco a cada 1 segundo.
   - Amortecimento das leituras com média ponderada dos últimos 5 segundos.
   - Transição suave com indicador de status (*badge pill* colorida: Verde para ideal, Vermelho para térmico, Rosa para CPU, Amarelo para VRAM).

2. **Temperatura Máxima da CPU no Resumo da Partida:**
   - O aplicativo original apenas mostrava a temperatura máxima da GPU no resumo final.
   - Agora registra e destaca expressamente a **Temperatura Máxima e Média da CPU**.
   - Emite **Alerta de Thermal Throttling Crítico** se a CPU passar de 95 °C (especialmente importante para processadores como o Intel Core i7-7700HQ).

3. **Exportação de Relatório Interativo em HTML:**
   - Ao finalizar uma sessão (1, 3, 5 ou 10 minutos), um clique no botão **EXPORTAR RELATÓRIO (.HTML)** gera uma página web completa e moderna em tema escuro.
   - Gráfico de linha do tempo interativo (Chart.js) comparando **FPS vs Temperatura da CPU vs Carga da GPU vs VRAM**.
   - Identificação do exato segundo em que ocorreu o congelamento e qual componente causou a queda.
   - Tabela segundo a segundo com destaque visual para os momentos de gargalo.

4. **App Leve e Dedicado:**
   - Sem as dezenas de módulos pesados que não são usados durante o jogo (stress tests, benchmarks de disco/Vortice, instaladores de drivers seriais, limpadores USB, etc.).
   - Inicialização rápida e consumo mínimo de recursos em segundo plano.

---

## 🚀 Como Executar

Execute o script `iniciar_detector.bat` ou abra um terminal na pasta e execute:

```powershell
dotnet run
```

> **Nota:** É recomendado executar como Administrador para que o Windows libere a leitura direta dos sensores de temperatura da CPU (via LibreHardwareMonitor) e dos eventos de apresentação de quadros via ETW (PresentMon).

---

## 📤 Como Subir para o seu Próprio GitHub

1. Inicialize o repositório git local:
   ```powershell
   git init
   git add .
   git commit -m "feat: initial commit do Game Bottleneck Detector com média móvel e exportador html"
   ```
2. Crie um novo repositório vazio na sua conta do GitHub (ex: `GameBottleneckDetector`).
3. Conecte o remoto e envie:
   ```powershell
   git remote add origin https://github.com/SEU_USUARIO/GameBottleneckDetector.git
   git branch -M main
   git push -u origin main
   ```
