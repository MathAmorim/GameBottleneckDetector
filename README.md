# 🎮 Game Bottleneck Detector & Diagnostic

Aplicativo focado para diagnóstico em tempo real de gargalos, congelamentos (*stuttering*) e quedas de FPS em jogos no Windows, com relatórios detalhados e análise contínua.

---

## 📋 Pré-requisitos & Dependências

Para executar ou compilar o projeto em qualquer máquina Windows, são necessários os seguintes componentes:

### 1. Sistema Operacional
* **Windows 10** ou **Windows 11** (64-bit).

### 2. Ambiente de Execução (.NET 8.0)
* **Para Usuários Finais (Executar o programa pronto):**
  * **Versão Standalone (Portátil - Recomendada):** Não requer nada instalado, inclui o .NET 8 e todas as dependências embutidas em um único executável. Baixe o `.zip` ou `.exe` diretamente na aba **Releases do GitHub**.
  * **Versão Framework-Dependent (Leve):** Requer o **[.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)** instalado e utiliza a pasta `publish\GameBottleneckDetector.exe`.
* **Para Desenvolvedores (Compilar e rodar via terminal):**
  * Requer o **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** para utilizar os comandos `dotnet run` ou `dotnet build`.

### 3. Privilégios de Administrador (Obrigatório)
O aplicativo deve sempre ser executado como **Administrador** (`Executar como administrador`). 
**Por que isso é necessário?**
1. **Leitura dos Sensores de Temperatura:** O driver de baixo nível (*LibreHardwareMonitor*) necessita de acesso em anel de privilégio elevado (Ring 0) para consultar os registradores MSR/SMN da CPU (como as temperaturas de cada núcleo e consumo elétrico). Sem ele, o Windows não libera o acesso aos sensores térmicos do processador.
2. **Medição Limpa de FPS (Sem Risco de Banimento):** A biblioteca *TraceEvent* escuta os eventos nativos de apresentação do DirectX/Windows (ETW - *Event Tracing for Windows*, estilo PresentMon), medindo FPS médio e 1% Low sem injetar DLLs no executável do jogo, evitando falsos-positivos em sistemas anti-cheat (como Easy Anti-Cheat, BattlEye, Vanguard).

### 4. Pacotes e Bibliotecas Utilizadas (NuGet)
As dependências são baixadas e restauradas automaticamente na primeira compilação pelo .NET:
* `FontAwesome.Sharp` (v6.6.0) – Ícones vetoriais modernos e de alta definição na interface.
* `LibreHardwareMonitorLib` (v0.9.6) – Monitoramento de sensores de CPU, GPU, VRAM, RAM e temperaturas.
* `Microsoft.Diagnostics.Tracing.TraceEvent` (v3.2.8) – Coleta em tempo real de tempos de quadro (*frametimes*) via ETW.
* `System.Management` (v10.0.2) – Detecção de controladores do sistema e WMI.

---

## 🧠 Conceitos Importantes de Diagnóstico

### O que é o "Núcleo Mais Ocupado" e por que ele é crucial?
A maioria dos processadores possui 4, 8 ou 16 threads. Quando você abre o Gerenciador de Tarefas, ele exibe a **Carga Total da CPU**, que é uma média matemática de todos os núcleos.
* **A armadilha:** Em jogos pesados, a carga total pode parecer baixa (ex: 40% ou 50%), dando a impressão de que a CPU está com bastante folga.
* **A realidade dos jogos:** As *engines* de jogos possuem uma thread mestra de renderização e lógica (*Main Render Thread*) que é executada predominantemente em **um único núcleo**.
* Se um único núcleo atingir **95% a 100% de uso**, todo o jogo travará esperando esse núcleo, mesmo que os demais núcleos estejam com 10% de uso.
* **Portanto:** O indicador **"Núcleo mais ocupado"** revela o verdadeiro gargalo de processamento (*single-thread bottleneck*) em jogos.

### O que é o Thermal Throttling da CPU?
Em processadores de notebook (como o Intel Core i7-7700HQ), o limite térmico crítico (*TJMax*) é de 100 °C.
* Ao atingir temperaturas entre 95 °C e 100 °C, o hardware ativa automaticamente o mecanismo de autoproteção **PROCHOT (Thermal Throttling)**: as frequências de clock despencam bruscamente (de ~3.4 GHz para 800 MHz – 1.2 GHz) para não queimar o silício.
* Essa queda repentina de frequência é a **causa número 1 de travamentos violentos, microcongelamentos (*stuttering*) e quedas brutais de FPS** em notebooks gamers.

---

## ✨ Recursos & Melhorias Implementadas

1. **Anti-Flicker com Média Móvel de 5 Segundos & Histerese:**
   * O card "AGORA" não pisca violentamente entre preto e branco a cada segundo.
   * Amortecimento inteligente com média ponderada de 5 segundos e *badge pill* colorida estável.
2. **Medidores Independentes de Temperatura:**
   * Cartões dedicados e separados para **Temperatura da CPU** e **Temperatura da GPU**, com barras de progresso e escalas de cores dinâmicas para superaquecimento.
3. **Aba de Configurações & Visualização Real de RAM e VRAM:**
   * Escolha entre visualizar o uso de memória em **Percentual (%)**, **Gigabytes Reais (ex: 7.8 GB / 16.0 GB)** ou **Megabytes (MB)**.
   * Preferências salvas automaticamente no arquivo local `settings.json`.
4. **Análise de Sessão Livre / Contínua:**
   * Tempos predefinidos: `[1 min]`, `[3 min]`, `[5 min]` e `[10 min]`.
   * Modo `[Livre / Até Parar]`: jogue partidas completas (30 min, 1h ou mais) consumindo menos de 0,05% de CPU e ~300 KB de RAM por hora.
   * Botão inteligente **"CONCLUIR ANÁLISE"**: encerre a qualquer momento e gere o relatório completo com todos os dados registrados até ali.
5. **Relatório Interativo em HTML (.html):**
   * Gera uma página web moderna e autossuficiente com gráfico de linha do tempo interativo (Chart.js) comparando **FPS vs Temp CPU vs Temp GPU vs VRAM vs Cargas**.
   * Identifica o exato segundo em que o jogo travou e qual componente causou a queda.
   * Tabela segundo a segundo com destaque visual para eventos críticos.

---

## 🚀 Como Executar

### Opção 1: Via Executável (Recomendado)
Execute o `GameBottleneckDetector.exe` como Administrador (clique com o botão direito e selecione **"Executar como administrador"**):
**[GameBottleneckDetector.exe](https://github.com/MathAmorim/GameBottleneckDetector/releases)**


### Opção 2: Via Terminal (.NET SDK)
Abra o PowerShell como Administrador na pasta do projeto e execute:
```powershell
dotnet run
```