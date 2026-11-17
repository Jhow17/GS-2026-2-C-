using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GS_TermoAtiva_C_.Models
{
    /// <summary>
    /// Serviço de geoprocessamento e consumo da API Weather Maps 1.0 da OpenWeather:
    /// https://tile.openweathermap.org/map/{layer}/{z}/{x}/{y}.png?appid={API key}
    /// Realiza projeção Web Mercator (WGS-84), cache de tiles e composição visual de camadas climáticas.
    /// </summary>
    public class OpenWeatherMapService
    {
        public const int TamanhoTile = 256;
        public const int ZoomMinimo = 12;
        public const int ZoomMaximo = 16;

        private static readonly HttpClient _httpClient = CriarHttpClient();
        private static readonly ConcurrentDictionary<string, byte[]> _cacheTiles = new();

        private readonly string _apiKey;

        public sealed record CamadaClimaticaInfo(string Codigo, string Titulo, string DescricaoCurta);

        public static readonly IReadOnlyList<CamadaClimaticaInfo> CamadasDisponiveis = new List<CamadaClimaticaInfo>
        {
            new("temp_new", "Temperatura (temp_new)", "Mancha térmica e ilhas de calor urbano"),
            new("precipitation_new", "Precipitação (precipitation_new)", "Volume de chuva e instabilidade"),
            new("clouds_new", "Nuvens (clouds_new)", "Cobertura de nebulosidade e sombra atmosférica"),
            new("wind_new", "Vento (wind_new)", "Intensidade de circulação e corredores de vento"),
            new("pressure_new", "Pressão (pressure_new)", "Pressão atmosférica ao nível do mar"),
            new("none", "Somente Mapa Base", "Exibe apenas a malha viária sem sobreposição climática")
        };

        public OpenWeatherMapService(string? apiKeyManual = null)
        {
            _apiKey = !string.IsNullOrWhiteSpace(apiKeyManual)
                ? apiKeyManual.Trim()
                : ResolverChaveApiOpenWeather();
        }

        public bool PossuiChaveConfigurada =>
            !string.IsNullOrWhiteSpace(_apiKey) &&
            !string.Equals(_apiKey, "sua_chave_openweather_aqui", StringComparison.OrdinalIgnoreCase);

        private static HttpClient CriarHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TermoAtiva-Kiosk-FIAP/1.0 (+https://github.com/Jhow17/GS-2026-2-C-)");
            return client;
        }

        /// <summary>
        /// Monta a URL oficial da Weather Maps 1.0 API da OpenWeather para um tile {z}/{x}/{y}.
        /// </summary>
        public string ConstruirUrlTileOpenWeather(string layer, int z, int x, int y)
        {
            return $"https://tile.openweathermap.org/map/{layer}/{z}/{x}/{y}.png?appid={_apiKey}";
        }

        /// <summary>
        /// Monta a URL de exibição segura (mascarando a chave de API para logs/interface).
        /// </summary>
        public string ConstruirUrlTemplateExibicao(string layer, int z, int x, int y)
        {
            string chaveMascarada = PossuiChaveConfigurada && _apiKey.Length >= 8
                ? $"{_apiKey[..4]}...{_apiKey[^4..]}"
                : "{API_KEY}";
            return $"https://tile.openweathermap.org/map/{layer}/{z}/{x}/{y}.png?appid={chaveMascarada}";
        }

        /// <summary>
        /// Converte Latitude/Longitude (WGS-84) para coordenadas globais em pixels na projeção Web Mercator.
        /// </summary>
        public static (double WorldX, double WorldY) LatLonParaMundoPixel(double lat, double lon, int zoom)
        {
            double latLimitada = Math.Clamp(lat, -85.05112878, 85.05112878);
            double escala = (double)TamanhoTile * (1 << zoom);

            double x = ((lon + 180.0) / 360.0) * escala;
            double senLat = Math.Sin(latLimitada * Math.PI / 180.0);
            double y = (0.5 - Math.Log((1.0 + senLat) / (1.0 - senLat)) / (4.0 * Math.PI)) * escala;

            return (x, y);
        }

        /// <summary>
        /// Converte coordenadas globais em pixels de volta para Latitude/Longitude (WGS-84).
        /// </summary>
        public static (double Latitude, double Longitude) MundoPixelParaLatLon(double worldX, double worldY, int zoom)
        {
            double escala = (double)TamanhoTile * (1 << zoom);
            double lon = (worldX / escala) * 360.0 - 180.0;

            double n = Math.PI - (2.0 * Math.PI * worldY) / escala;
            double lat = (180.0 / Math.PI) * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));

            return (Math.Clamp(lat, -85.0, 85.0), Math.Clamp(lon, -180.0, 180.0));
        }

        /// <summary>
        /// Converte uma coordenada geográfica (lat, lon) para a posição (X, Y) em pixels dentro do viewport do mapa.
        /// </summary>
        public static PointF LatLonParaPosicaoTela(
            double lat,
            double lon,
            double centroLat,
            double centroLon,
            int zoom,
            int larguraViewport,
            int alturaViewport)
        {
            var (centroX, centroY) = LatLonParaMundoPixel(centroLat, centroLon, zoom);
            var (pontoX, pontoY) = LatLonParaMundoPixel(lat, lon, zoom);

            float telaX = (float)((larguraViewport / 2.0) + (pontoX - centroX));
            float telaY = (float)((alturaViewport / 2.0) + (pontoY - centroY));
            return new PointF(telaX, telaY);
        }

        /// <summary>
        /// Obtém os índices inteiros do tile central (x, y) correspondente a uma coordenada e nível de zoom.
        /// </summary>
        public static (int TileX, int TileY) LatLonParaTileXY(double lat, double lon, int zoom)
        {
            var (wx, wy) = LatLonParaMundoPixel(lat, lon, zoom);
            int numTiles = 1 << zoom;
            int tx = Math.Clamp((int)Math.Floor(wx / TamanhoTile), 0, numTiles - 1);
            int ty = Math.Clamp((int)Math.Floor(wy / TamanhoTile), 0, numTiles - 1);
            return (tx, ty);
        }

        /// <summary>
        /// Baixa todos os tiles visíveis para o viewport atual (base cartográfica + camada climática OpenWeather 1.0).
        /// </summary>
        public async Task<(int TilesBaseCarregados, int TilesClimaCarregados, string StatusMensagem)> CarregarTilesViewportAsync(
            double centroLat,
            double centroLon,
            int zoom,
            int larguraViewport,
            int alturaViewport,
            string camadaClimatica)
        {
            int z = Math.Clamp(zoom, ZoomMinimo, ZoomMaximo);
            int numTiles = 1 << z;

            var (centroWorldX, centroWorldY) = LatLonParaMundoPixel(centroLat, centroLon, z);
            double esquerdaWorld = centroWorldX - (larguraViewport / 2.0);
            double topoWorld = centroWorldY - (alturaViewport / 2.0);
            double direitaWorld = centroWorldX + (larguraViewport / 2.0);
            double baseWorld = centroWorldY + (alturaViewport / 2.0);

            int minTileX = Math.Clamp((int)Math.Floor(esquerdaWorld / TamanhoTile), 0, numTiles - 1);
            int maxTileX = Math.Clamp((int)Math.Floor(direitaWorld / TamanhoTile), 0, numTiles - 1);
            int minTileY = Math.Clamp((int)Math.Floor(topoWorld / TamanhoTile), 0, numTiles - 1);
            int maxTileY = Math.Clamp((int)Math.Floor(baseWorld / TamanhoTile), 0, numTiles - 1);

            var tarefas = new List<Task>();
            int baseCount = 0;
            int climaCount = 0;
            bool buscarClima = PossuiChaveConfigurada &&
                               !string.IsNullOrWhiteSpace(camadaClimatica) &&
                               !string.Equals(camadaClimatica, "none", StringComparison.OrdinalIgnoreCase);

            for (int tx = minTileX; tx <= maxTileX; tx++)
            {
                for (int ty = minTileY; ty <= maxTileY; ty++)
                {
                    int x = tx;
                    int y = ty;

                    tarefas.Add(Task.Run(async () =>
                    {
                        string chaveBase = $"osm:{z}:{x}:{y}";
                        string urlBase = $"https://tile.openstreetmap.org/{z}/{x}/{y}.png";
                        if (await GarantirTileNoCacheAsync(chaveBase, urlBase))
                        {
                            System.Threading.Interlocked.Increment(ref baseCount);
                        }
                    }));

                    if (buscarClima)
                    {
                        tarefas.Add(Task.Run(async () =>
                        {
                            string chaveClima = $"owm:{camadaClimatica}:{z}:{x}:{y}";
                            string urlClima = ConstruirUrlTileOpenWeather(camadaClimatica, z, x, y);
                            if (await GarantirTileNoCacheAsync(chaveClima, urlClima))
                            {
                                System.Threading.Interlocked.Increment(ref climaCount);
                            }
                        }));
                    }
                }
            }

            await Task.WhenAll(tarefas);

            var (tileCentroX, tileCentroY) = LatLonParaTileXY(centroLat, centroLon, z);
            string status;
            if (!PossuiChaveConfigurada)
            {
                status = "Chave OPENWEATHER_API_KEY não configurada no .env (usando simulação térmica local).";
            }
            else if (string.Equals(camadaClimatica, "none", StringComparison.OrdinalIgnoreCase))
            {
                status = $"Mapa base ativo (Z={z}, X={tileCentroX}, Y={tileCentroY}) — camada climática oculta.";
            }
            else if (climaCount > 0)
            {
                status = $"OpenWeather Maps 1.0 ativo: camada '{camadaClimatica}' (Tile Z={z}/X={tileCentroX}/Y={tileCentroY}).";
            }
            else
            {
                status = $"Modo resiliente: falha de rede ao obter tiles '{camadaClimatica}' (exibindo malha local).";
            }

            return (baseCount, climaCount, status);
        }

        private static async Task<bool> GarantirTileNoCacheAsync(string chaveCache, string url)
        {
            if (_cacheTiles.ContainsKey(chaveCache))
            {
                return true;
            }

            try
            {
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                if (bytes.Length > 64)
                {
                    _cacheTiles[chaveCache] = bytes;
                    return true;
                }
            }
            catch
            {
                // Resiliência silenciosa em caso de modo offline
            }

            return false;
        }

        /// <summary>
        /// Desenha os tiles em cache (base cartográfica + overlay climático OpenWeather) diretamente no Graphics do painel.
        /// </summary>
        public (int DesenhadosBase, int DesenhadosClima) DesenharCamadasNoGraphics(
            Graphics g,
            double centroLat,
            double centroLon,
            int zoom,
            int larguraViewport,
            int alturaViewport,
            string camadaClimatica,
            float opacidadeClima = 0.72f)
        {
            int z = Math.Clamp(zoom, ZoomMinimo, ZoomMaximo);
            int numTiles = 1 << z;

            var (centroWorldX, centroWorldY) = LatLonParaMundoPixel(centroLat, centroLon, z);
            double esquerdaWorld = centroWorldX - (larguraViewport / 2.0);
            double topoWorld = centroWorldY - (alturaViewport / 2.0);
            double direitaWorld = centroWorldX + (larguraViewport / 2.0);
            double baseWorld = centroWorldY + (alturaViewport / 2.0);

            int minTileX = Math.Clamp((int)Math.Floor(esquerdaWorld / TamanhoTile), 0, numTiles - 1);
            int maxTileX = Math.Clamp((int)Math.Floor(direitaWorld / TamanhoTile), 0, numTiles - 1);
            int minTileY = Math.Clamp((int)Math.Floor(topoWorld / TamanhoTile), 0, numTiles - 1);
            int maxTileY = Math.Clamp((int)Math.Floor(baseWorld / TamanhoTile), 0, numTiles - 1);

            int desenhadosBase = 0;
            int desenhadosClima = 0;
            bool desenharClima = !string.IsNullOrWhiteSpace(camadaClimatica) &&
                                 !string.Equals(camadaClimatica, "none", StringComparison.OrdinalIgnoreCase);

            using var attrOpacidade = CriarAtributosOpacidade(opacidadeClima);

            for (int tx = minTileX; tx <= maxTileX; tx++)
            {
                for (int ty = minTileY; ty <= maxTileY; ty++)
                {
                    int destX = (int)Math.Round((tx * TamanhoTile) - esquerdaWorld);
                    int destY = (int)Math.Round((ty * TamanhoTile) - topoWorld);
                    var destRect = new Rectangle(destX, destY, TamanhoTile, TamanhoTile);

                    string chaveBase = $"osm:{z}:{tx}:{ty}";
                    if (_cacheTiles.TryGetValue(chaveBase, out byte[]? bytesBase) && bytesBase != null)
                    {
                        using var msBase = new MemoryStream(bytesBase);
                        using var imgBase = Image.FromStream(msBase);
                        g.DrawImage(imgBase, destRect);
                        desenhadosBase++;
                    }

                    if (desenharClima)
                    {
                        string chaveClima = $"owm:{camadaClimatica}:{z}:{tx}:{ty}";
                        if (_cacheTiles.TryGetValue(chaveClima, out byte[]? bytesClima) && bytesClima != null)
                        {
                            using var msClima = new MemoryStream(bytesClima);
                            using var imgClima = Image.FromStream(msClima);
                            g.DrawImage(
                                imgClima,
                                destRect,
                                0,
                                0,
                                imgClima.Width,
                                imgClima.Height,
                                GraphicsUnit.Pixel,
                                attrOpacidade);
                            desenhadosClima++;
                        }
                    }
                }
            }

            return (desenhadosBase, desenhadosClima);
        }

        private static ImageAttributes CriarAtributosOpacidade(float opacidade)
        {
            var matrix = new ColorMatrix
            {
                Matrix33 = Math.Clamp(opacidade, 0.1f, 1.0f)
            };
            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            return attributes;
        }

        /// <summary>
        /// Consulta dados meteorológicos atuais de São Paulo na OpenWeather para sincronizar o alerta térmico no JSON.
        /// </summary>
        public async Task<(bool Sucesso, double TemperaturaC, int UmidadePct, double SensacaoC, string Mensagem)> ConsultarClimaAtualAsync(
            double lat = PontoApoio.LatitudeTotemCentral,
            double lon = PontoApoio.LongitudeTotemCentral)
        {
            if (!PossuiChaveConfigurada)
            {
                return (false, 0, 0, 0, "Chave OPENWEATHER_API_KEY não configurada no arquivo .env.");
            }

            try
            {
                string latStr = lat.ToString(CultureInfo.InvariantCulture);
                string lonStr = lon.ToString(CultureInfo.InvariantCulture);
                string url = $"https://api.openweathermap.org/data/2.5/weather?lat={latStr}&lon={lonStr}&units=metric&lang=pt_br&appid={_apiKey}";

                string json = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("main", out JsonElement main))
                {
                    double temp = main.TryGetProperty("temp", out var tEl) ? tEl.GetDouble() : 30.0;
                    int hum = main.TryGetProperty("humidity", out var hEl) ? hEl.GetInt32() : 50;
                    double feelsLike = main.TryGetProperty("feels_like", out var fEl)
                        ? fEl.GetDouble()
                        : AlertaClimatico.CalcularIndiceCalor(temp, hum);

                    double indiceCalor = Math.Max(Math.Round(feelsLike, 1), AlertaClimatico.CalcularIndiceCalor(temp, hum));
                    return (true, Math.Round(temp, 1), Math.Clamp(hum, 0, 100), indiceCalor, "Sincronizado com OpenWeather API.");
                }
            }
            catch (Exception ex)
            {
                return (false, 0, 0, 0, $"Falha ao sincronizar com OpenWeather: {ex.Message}");
            }

            return (false, 0, 0, 0, "Resposta inesperada da API OpenWeather.");
        }

        /// <summary>
        /// Localiza a chave da API OpenWeather em variáveis de ambiente ou no arquivo .env do projeto.
        /// </summary>
        public static string ResolverChaveApiOpenWeather()
        {
            string? envDireta = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY")
                                ?? Environment.GetEnvironmentVariable("EXPO_PUBLIC_OPENWEATHER_API_KEY");
            if (!string.IsNullOrWhiteSpace(envDireta))
            {
                return envDireta.Trim();
            }

            DirectoryInfo? dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                string envArquivo = Path.Combine(dir.FullName, ".env");
                string? chave = LerChaveDeArquivoEnv(envArquivo);
                if (!string.IsNullOrWhiteSpace(chave))
                {
                    return chave;
                }

                dir = dir.Parent;
            }

            return string.Empty;
        }

        private static string? LerChaveDeArquivoEnv(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo)) return null;

            try
            {
                foreach (string linhaBruta in File.ReadAllLines(caminhoArquivo))
                {
                    string linha = linhaBruta.Trim();
                    if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith('#')) continue;

                    int idxIgual = linha.IndexOf('=');
                    if (idxIgual <= 0) continue;

                    string nome = linha[..idxIgual].Trim();
                    string valor = linha[(idxIgual + 1)..].Trim().Trim('"', '\'');

                    if ((string.Equals(nome, "OPENWEATHER_API_KEY", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(nome, "EXPO_PUBLIC_OPENWEATHER_API_KEY", StringComparison.OrdinalIgnoreCase)) &&
                        !string.IsNullOrWhiteSpace(valor) &&
                        !string.Equals(valor, "sua_chave_openweather_aqui", StringComparison.OrdinalIgnoreCase))
                    {
                        return valor;
                    }
                }
            }
            catch
            {
                // Ignora erros de leitura de arquivo
            }

            return null;
        }
    }
}
