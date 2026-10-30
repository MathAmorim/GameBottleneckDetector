using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameBottleneckDetector;

/// <summary>
/// Gerador nativo e offline de QR Code (ISO/IEC 18004, Byte Mode, Nível L) para exibir o link da Segunda Tela no WPF.
/// Suporta URLs de até 53 bytes (Versão 3-L, 29x29 módulos).
/// </summary>
public static class QrCodeGenerator
{
    public static BitmapSource Generate(string text, int pixelSize = 180)
    {
        byte[] payload = Encoding.UTF8.GetBytes(text);
        // Usamos Versão 3-L (29x29), 55 data codewords + 15 EC codewords (suporta até 53 bytes em modo Byte)
        const int size = 29;
        const int dataCodewords = 55;
        const int ecCodewords = 15;

        if (payload.Length > 53)
            payload = payload[..53];

        // 1. Montar fluxo de bits de dados (Modo Byte = 0100 + tamanho em 8 bits + bytes + terminador)
        var bits = new List<bool>(dataCodewords * 8);
        AddBits(bits, 0b0100, 4);
        AddBits(bits, payload.Length, 8);
        foreach (byte b in payload)
            AddBits(bits, b, 8);

        int maxBits = dataCodewords * 8;
        int terminator = Math.Min(4, maxBits - bits.Count);
        AddBits(bits, 0, terminator);
        while (bits.Count % 8 != 0)
            bits.Add(false);

        byte[] padBytes = { 0xEC, 0x11 };
        int padIdx = 0;
        while (bits.Count < maxBits)
        {
            AddBits(bits, padBytes[padIdx % 2], 8);
            padIdx++;
        }

        byte[] data = new byte[dataCodewords];
        for (int i = 0; i < dataCodewords; i++)
        {
            int val = 0;
            for (int b = 0; b < 8; b++)
                val = (val << 1) | (bits[i * 8 + b] ? 1 : 0);
            data[i] = (byte)val;
        }

        // 2. Calcular Reed-Solomon Error Correction (15 codewords em GF(256) com polinômio 0x11D)
        byte[] ec = ComputeReedSolomon(data, ecCodewords);
        byte[] allCodewords = new byte[dataCodewords + ecCodewords];
        Buffer.BlockCopy(data, 0, allCodewords, 0, dataCodewords);
        Buffer.BlockCopy(ec, 0, allCodewords, dataCodewords, ecCodewords);

        // 3. Criar matriz 29x29 e padrões funcionais
        bool[,] modules = new bool[size, size];
        bool[,] isFunction = new bool[size, size];

        DrawFinderPattern(modules, isFunction, 0, 0, size);
        DrawFinderPattern(modules, isFunction, size - 7, 0, size);
        DrawFinderPattern(modules, isFunction, 0, size - 7, size);
        DrawAlignmentPattern(modules, isFunction, 22, 22);

        // Timing patterns
        for (int i = 8; i < size - 8; i++)
        {
            modules[6, i] = (i % 2 == 0);
            isFunction[6, i] = true;
            modules[i, 6] = (i % 2 == 0);
            isFunction[i, 6] = true;
        }

        // Dark module e reserva de área de formato
        modules[size - 8, 8] = true;
        isFunction[size - 8, 8] = true;
        ReserveFormatAreas(isFunction, size);

        // 4. Preencher bits de dados em zigue-zague (direita para esquerda, colunas duplas)
        int bitIndex = 0;
        int totalDataBits = allCodewords.Length * 8;
        bool upward = true;

        for (int right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5; // Pular coluna do timing vertical
            for (int vert = 0; vert < size; vert++)
            {
                int r = upward ? (size - 1 - vert) : vert;
                for (int j = 0; j < 2; j++)
                {
                    int c = right - j;
                    if (isFunction[r, c]) continue;
                    bool bit = false;
                    if (bitIndex < totalDataBits)
                    {
                        bit = ((allCodewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                        bitIndex++;
                    }
                    // Aplicar Máscara 0: (r + c) % 2 == 0
                    if ((r + c) % 2 == 0)
                        bit = !bit;
                    modules[r, c] = bit;
                }
            }
            upward = !upward;
        }

        // 5. Desenhar bits de formato (Nível L = 01, Máscara 0 = 000 -> bits BCH = 0x77C4)
        DrawFormatBits(modules, size, 0x77C4);

        // 6. Renderizar com Quiet Zone de 3 módulos em WriteableBitmap
        const int quietZone = 3;
        int totalModules = size + quietZone * 2;
        int scale = Math.Max(4, pixelSize / totalModules);
        int imgSize = totalModules * scale;

        var bmp = new WriteableBitmap(imgSize, imgSize, 96, 96, PixelFormats.Bgra32, null);
        int[] pixels = new int[imgSize * imgSize];
        const int white = unchecked((int)0xFFFFFFFF);
        const int black = unchecked((int)0xFF0D1117);

        Array.Fill(pixels, white);

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (!modules[r, c]) continue;
                int startY = (r + quietZone) * scale;
                int startX = (c + quietZone) * scale;
                for (int dy = 0; dy < scale; dy++)
                {
                    int rowOffset = (startY + dy) * imgSize + startX;
                    for (int dx = 0; dx < scale; dx++)
                        pixels[rowOffset + dx] = black;
                }
            }
        }

        bmp.WritePixels(new Int32Rect(0, 0, imgSize, imgSize), pixels, imgSize * 4, 0);
        bmp.Freeze();
        return bmp;
    }

    private static void AddBits(List<bool> list, int val, int len)
    {
        for (int i = len - 1; i >= 0; i--)
            list.Add(((val >> i) & 1) != 0);
    }

    private static void DrawFinderPattern(bool[,] modules, bool[,] isFunction, int top, int left, int size)
    {
        for (int dr = -1; dr <= 7; dr++)
        {
            for (int dc = -1; dc <= 7; dc++)
            {
                int r = top + dr;
                int c = left + dc;
                if (r < 0 || r >= size || c < 0 || c >= size) continue;
                bool dark = (dr >= 0 && dr <= 6 && (dc == 0 || dc == 6)) ||
                            (dc >= 0 && dc <= 6 && (dr == 0 || dr == 6)) ||
                            (dr >= 2 && dr <= 4 && dc >= 2 && dc <= 4);
                modules[r, c] = dark;
                isFunction[r, c] = true;
            }
        }
    }

    private static void DrawAlignmentPattern(bool[,] modules, bool[,] isFunction, int centerR, int centerC)
    {
        for (int dr = -2; dr <= 2; dr++)
        {
            for (int dc = -2; dc <= 2; dc++)
            {
                int r = centerR + dr;
                int c = centerC + dc;
                bool dark = Math.Max(Math.Abs(dr), Math.Abs(dc)) != 1;
                modules[r, c] = dark;
                isFunction[r, c] = true;
            }
        }
    }

    private static void ReserveFormatAreas(bool[,] isFunction, int size)
    {
        for (int i = 0; i <= 8; i++)
        {
            if (i != 6)
            {
                isFunction[8, i] = true;
                isFunction[i, 8] = true;
            }
        }
        for (int i = 0; i < 8; i++)
        {
            isFunction[8, size - 1 - i] = true;
            isFunction[size - 1 - i, 8] = true;
        }
    }

    private static void DrawFormatBits(bool[,] modules, int size, int formatBits)
    {
        // Cópia 1: ao redor do finder superior esquerdo (i=0 é bit 14 MSB, i=14 é bit 0 LSB)
        for (int i = 0; i <= 5; i++)
            modules[8, i] = GetBit(formatBits, i);
        modules[8, 7] = GetBit(formatBits, 6);
        modules[8, 8] = GetBit(formatBits, 7);
        modules[7, 8] = GetBit(formatBits, 8);
        for (int i = 9; i < 15; i++)
            modules[14 - i, 8] = GetBit(formatBits, i);

        // Cópia 2: vertical inferior esquerdo (bits 14..8 -> i=0..6) e horizontal superior direito (bits 7..0 -> i=7..14)
        for (int i = 0; i < 7; i++)
            modules[size - 1 - i, 8] = GetBit(formatBits, i);
        for (int i = 7; i < 15; i++)
            modules[8, size - 15 + i] = GetBit(formatBits, i);
    }

    private static bool GetBit(int val, int indexFromMsb)
    {
        return ((val >> (14 - indexFromMsb)) & 1) != 0;
    }

    private static byte[] ComputeReedSolomon(byte[] data, int degree)
    {
        byte[] gen = new byte[degree];
        gen[degree - 1] = 1;
        int root = 1;
        for (int i = 0; i < degree; i++)
        {
            for (int j = 0; j < degree; j++)
            {
                gen[j] = GfMultiply(gen[j], (byte)root);
                if (j + 1 < degree)
                    gen[j] ^= gen[j + 1];
            }
            root = GfMultiply((byte)root, 0x02);
        }

        byte[] result = new byte[degree];
        foreach (byte b in data)
        {
            byte factor = (byte)(b ^ result[0]);
            Array.Copy(result, 1, result, 0, degree - 1);
            result[degree - 1] = 0;
            for (int j = 0; j < degree; j++)
                result[j] ^= GfMultiply(gen[j], factor);
        }
        return result;
    }

    private static byte GfMultiply(byte x, byte y)
    {
        int z = 0;
        for (int i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return (byte)z;
    }
}
