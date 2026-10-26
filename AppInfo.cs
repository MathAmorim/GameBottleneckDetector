using System.Reflection;

namespace GameBottleneckDetector;

/// <summary>
/// Informações globais da aplicação extraídas dinamicamente dos metadados do projeto (.csproj).
/// </summary>
public static class AppInfo
{
    private static string? _version;

    /// <summary>
    /// Obtém a versão da aplicação definida na tag &lt;Version&gt; do arquivo .csproj.
    /// Exemplo: "1.2.0"
    /// </summary>
    public static string Version
    {
        get
        {
            if (_version != null)
                return _version;

            try
            {
                var assembly = typeof(AppInfo).Assembly;
                var infoVer = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                if (!string.IsNullOrWhiteSpace(infoVer))
                {
                    // No .NET 8+, InformationalVersion pode incluir hash de commit do Git (ex: 1.2.0+8dfe845).
                    // Removemos o sufixo '+' para manter apenas a versão semântica limpa.
                    int plusIndex = infoVer.IndexOf('+');
                    _version = plusIndex > 0 ? infoVer[..plusIndex] : infoVer;
                    return _version;
                }

                var ver = assembly.GetName().Version;
                if (ver != null)
                {
                    _version = $"{ver.Major}.{ver.Minor}.{ver.Build}";
                    return _version;
                }
            }
            catch
            {
                // Fallback silencioso de segurança
            }

            _version = "1.0.0";
            return _version;
        }
    }
}
