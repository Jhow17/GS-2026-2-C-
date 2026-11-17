using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela de visualização geográfica interativa dos pontos de refúgio e recursos climáticos.
    /// Consome a API Weather Maps 1.0 da OpenWeather:
    /// https://tile.openweathermap.org/map/{layer}/{z}/{x}/{y}.png?appid={API key}
    /// sobreposta à malha cartográfica urbana (WGS-84) com pinos georreferenciados interativos.
    /// </summary>
    public class MapaForm : Form
    {
        private readonly TotemRepository _repository;
        private readonly OpenWeatherMapService _weatherMapService;

        private List<PontoApoio> _pontos = new();
        private PontoApoio? _pontoSelecionado;

        private double _centroLat = PontoApoio.LatitudeTotemCentral;
        private double _centroLon = PontoApoio.LongitudeTotemCentral;
        private int _zoom = 13;
        private string _camadaAtual = "temp_new";

        private bool _arrastandoMapa;
        private Point _pontoInicioArraste;
        private double _centroLatInicioArraste;
        private double _centroLonInicioArraste;

        private CanvasMapaPanel _canvasMapa = null!;
        private ComboBox _cboCamada = null!;
        private Label _lblZoom = null!;
        private Label _lblStatusTile = null!;
        private Label _lblDetalhePonto = null!;
        private ListBox _lstPontos = null!;

        private readonly List<(RectangleF AreaToque, PontoApoio Ponto)> _areasToquePinos = new();

        public MapaForm(TotemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _weatherMapService = new OpenWeatherMapService();

            var alerta = _repository.ObterAlertaAtual();
            if (!string.IsNullOrWhiteSpace(alerta.CamadaMapaPadrao))
            {
                _camadaAtual = alerta.CamadaMapaPadrao;
            }

            _pontos = _repository.ObterTodosPontos();
            _pontoSelecionado = _pontos.FirstOrDefault();

            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            Tema.ConfigurarJanelaTotem(this);
            this.Text = "TermoAtiva - Mapa de Refúgio e Camadas Climáticas OpenWeather 1.0";

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Tema.FundoPrincipal,
                Padding = new Padding(28, 22, 28, 22)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 122f)); // Cabeçalho sem corte
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Corpo (Viewport do Mapa + Legenda)
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 78f));  // Botão Inferior Voltar ao Menu

            // 1. CABEÇALHO PADRONIZADO
            var header = Tema.CriarCabecalhoPadrao(
                "GEOPROCESSAMENTO E CAMADAS CLIMÁTICAS OPENWEATHER MAPS 1.0",
                "MAPA DE REFÚGIO E RECURSOS CLIMÁTICOS",
                "Visualize as camadas meteorológicas em tempo real e toque nos pontos para localizar água potável, sombra e postos climatizados.",
                Tema.CianoMapa);
            pnlPrincipal.Controls.Add(header, 0, 0);

            // 2. CORPO CENTRAL: VIEWPORT GEOGRÁFICO + PAINEL DE LEGENDA
            var splitCorpo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 12)
            };
            splitCorpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));
            splitCorpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));

            var pnlViewportMapa = CriarPainelViewportMapa();
            var pnlLegenda = CriarPainelLegendaRecursos();

            splitCorpo.Controls.Add(pnlViewportMapa, 0, 0);
            splitCorpo.Controls.Add(pnlLegenda, 1, 0);
            pnlPrincipal.Controls.Add(splitCorpo, 0, 1);

            // 3. BOTÃO LARGO INFERIOR
            var btnVoltar = Tema.CriarBotaoReaLTaiizor("VOLTAR AO MENU PRINCIPAL", Tema.AzulPrimario, 66);
            btnVoltar.Dock = DockStyle.Fill;
            btnVoltar.Click += (s, e) => this.Close();
            pnlPrincipal.Controls.Add(btnVoltar, 0, 2);

            this.Controls.Add(pnlPrincipal);

            this.Shown += async (s, e) =>
            {
                AtualizarPainelDetalhePonto();
                await AtualizarTilesMapaAsync();
            };
        }

        private System.Windows.Forms.Panel CriarPainelViewportMapa()
        {
            var cardMapa = new KioskCardPanel(Tema.CianoMapa)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(14),
                Margin = new Padding(0, 0, 14, 0)
            };

            var layoutMapa = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layoutMapa.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  // Barra de controles de camada e zoom
            layoutMapa.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));  // Barra técnica de URL/Tile OpenWeather 1.0
            layoutMapa.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Canvas interativo de tiles + pinos
            layoutMapa.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));  // Barra de detalhes do ponto selecionado

            // 1. BARRA DE CONTROLES: Camada OpenWeather + Zoom + Centralizar
            var pnlControles = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 0, 0, 4)
            };
            pnlControles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155f)); // Rótulo Camada
            pnlControles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // ComboBox Camada
            pnlControles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52f));  // Zoom -
            pnlControles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92f));  // Indicador Zoom
            pnlControles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52f));  // Zoom +
            pnlControles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165f)); // Centralizar Totem

            var lblCamada = new Label
            {
                Text = "CAMADA OPENWEATHER:",
                Font = Tema.FonteTag,
                ForeColor = Tema.CianoMapa,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _cboCamada = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Tema.FontePequenaBold,
                Margin = new Padding(0, 4, 10, 4)
            };

            int indiceSelecionado = 0;
            for (int i = 0; i < OpenWeatherMapService.CamadasDisponiveis.Count; i++)
            {
                var info = OpenWeatherMapService.CamadasDisponiveis[i];
                _cboCamada.Items.Add(new ItemCamadaCombo(info.Codigo, $"{info.Titulo} — {info.DescricaoCurta}"));
                if (string.Equals(info.Codigo, _camadaAtual, StringComparison.OrdinalIgnoreCase))
                {
                    indiceSelecionado = i;
                }
            }
            _cboCamada.SelectedIndex = indiceSelecionado;
            _cboCamada.SelectedIndexChanged += async (s, e) =>
            {
                if (_cboCamada.SelectedItem is ItemCamadaCombo item)
                {
                    _camadaAtual = item.Codigo;
                    _repository.SalvarCamadaMapaPadrao(_camadaAtual);
                    await AtualizarTilesMapaAsync();
                }
            };

            var btnZoomMenos = new System.Windows.Forms.Button
            {
                Text = "-",
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 2, 2, 2)
            };
            Tema.EstilizarBotaoSecundario(btnZoomMenos, 36);
            btnZoomMenos.Font = Tema.FonteSubtitulo;
            btnZoomMenos.Click += async (s, e) => await AlterarZoomAsync(-1);

            _lblZoom = new Label
            {
                Text = $"ZOOM {_zoom}",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnZoomMais = new System.Windows.Forms.Button
            {
                Text = "+",
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 2, 6, 2)
            };
            Tema.EstilizarBotaoSecundario(btnZoomMais, 36);
            btnZoomMais.Font = Tema.FonteSubtitulo;
            btnZoomMais.Click += async (s, e) => await AlterarZoomAsync(1);

            var btnCentralizar = new System.Windows.Forms.Button
            {
                Text = "Centralizar Totem",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 2, 0, 2)
            };
            Tema.EstilizarBotaoSecundario(btnCentralizar, 36);
            btnCentralizar.Font = Tema.FontePequenaBold;
            btnCentralizar.BackColor = Tema.CorFundoBebedouro;
            btnCentralizar.ForeColor = Tema.CorTextoBebedouro;
            btnCentralizar.Click += async (s, e) =>
            {
                _centroLat = PontoApoio.LatitudeTotemCentral;
                _centroLon = PontoApoio.LongitudeTotemCentral;
                _zoom = 13;
                _lblZoom.Text = $"ZOOM {_zoom}";
                await AtualizarTilesMapaAsync();
            };

            pnlControles.Controls.Add(lblCamada, 0, 0);
            pnlControles.Controls.Add(_cboCamada, 1, 0);
            pnlControles.Controls.Add(btnZoomMenos, 2, 0);
            pnlControles.Controls.Add(_lblZoom, 3, 0);
            pnlControles.Controls.Add(btnZoomMais, 4, 0);
            pnlControles.Controls.Add(btnCentralizar, 5, 0);

            // 2. BARRA DE STATUS TÉCNICO DA ROTA OPENWEATHER MAPS 1.0
            var pnlTopoGis = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoHeader,
                Padding = new Padding(12, 0, 12, 0),
                Margin = new Padding(0, 0, 0, 4)
            };

            _lblStatusTile = new Label
            {
                Text = "Carregando tiles OpenWeather Weather Maps 1.0...",
                Font = Tema.FontePequenaBold,
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTopoGis.Controls.Add(_lblStatusTile);

            // 3. CANVAS DE RENDERIZAÇÃO DOS TILES E PINOS GEORREFERENCIADOS
            _canvasMapa = new CanvasMapaPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(226, 232, 240),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 6)
            };
            _canvasMapa.Paint += CanvasMapa_Paint;
            _canvasMapa.Resize += async (s, e) =>
            {
                _canvasMapa.Invalidate();
                await AtualizarTilesMapaAsync();
            };
            _canvasMapa.MouseDown += CanvasMapa_MouseDown;
            _canvasMapa.MouseMove += CanvasMapa_MouseMove;
            _canvasMapa.MouseUp += CanvasMapa_MouseUp;
            _canvasMapa.MouseWheel += async (s, e) =>
            {
                int delta = e.Delta > 0 ? 1 : -1;
                await AlterarZoomAsync(delta);
            };

            // 4. BARRA INFERIOR DE DETALHES DO PONTO SELECIONADO NO MAPA
            var pnlDetalhe = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 8, 16, 8),
                Margin = new Padding(0)
            };

            _lblDetalhePonto = new Label
            {
                Text = "Toque em um pino no mapa ou selecione um ponto na lista ao lado para inspecionar coordenadas e endereço.",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlDetalhe.Controls.Add(_lblDetalhePonto);

            layoutMapa.Controls.Add(pnlControles, 0, 0);
            layoutMapa.Controls.Add(pnlTopoGis, 0, 1);
            layoutMapa.Controls.Add(_canvasMapa, 0, 2);
            layoutMapa.Controls.Add(pnlDetalhe, 0, 3);

            cardMapa.Controls.Add(layoutMapa);
            return cardMapa;
        }

        private System.Windows.Forms.Panel CriarPainelLegendaRecursos()
        {
            var cardLegenda = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(22, 18, 22, 18),
                Margin = new Padding(0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // Título da Legenda
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 195f)); // Blocos de Categorias
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));  // Título da Lista Rápida
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Pontos Mapeados Interativos

            var lblTituloLegenda = new Label
            {
                Text = "LEGENDA VISUAL DOS RECURSOS",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlCategorias = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0, 4, 0, 8)
            };
            pnlCategorias.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            pnlCategorias.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            pnlCategorias.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

            pnlCategorias.Controls.Add(CriarItemLegenda(
                "Bebedouros Públicos (Azul)",
                "Água potável gratuita para hidratação imediata",
                Tema.CorFundoBebedouro,
                Tema.CorTextoBebedouro), 0, 0);

            pnlCategorias.Controls.Add(CriarItemLegenda(
                "Áreas de Sombra / Descanso (Verde)",
                "Praças arborizadas e refúgios naturais",
                Tema.CorFundoSombra,
                Tema.CorTextoSombra), 0, 1);

            pnlCategorias.Controls.Add(CriarItemLegenda(
                "Postos Climatizados (Âmbar)",
                "UBS e ambientes com resfriamento interno",
                Tema.CorFundoPosto,
                Tema.CorTextoPosto), 0, 2);

            var lblTituloPontos = new Label
            {
                Text = "TOQUE EM UM PONTO PARA FOCAR NO MAPA",
                Font = Tema.FonteTag,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lstPontos = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = Tema.FontePequenaBold,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Tema.TextoPrincipal,
                IntegralHeight = false
            };

            foreach (var p in _pontos)
            {
                string disp = p.Disponivel ? "ATIVO" : "MANUTENÇÃO";
                _lstPontos.Items.Add($"#{p.Id} [{p.DistanciaMetros}m] {p.Nome} ({disp})");
            }

            if (_lstPontos.Items.Count > 0)
            {
                _lstPontos.SelectedIndex = 0;
            }

            _lstPontos.SelectedIndexChanged += async (s, e) =>
            {
                int idx = _lstPontos.SelectedIndex;
                if (idx >= 0 && idx < _pontos.Count)
                {
                    _pontoSelecionado = _pontos[idx];
                    _centroLat = _pontoSelecionado.Latitude;
                    _centroLon = _pontoSelecionado.Longitude;
                    if (_zoom < 14)
                    {
                        _zoom = 14;
                        _lblZoom.Text = $"ZOOM {_zoom}";
                    }
                    AtualizarPainelDetalhePonto();
                    _canvasMapa.Invalidate();
                    await AtualizarTilesMapaAsync();
                }
            };

            layout.Controls.Add(lblTituloLegenda, 0, 0);
            layout.Controls.Add(pnlCategorias, 0, 1);
            layout.Controls.Add(lblTituloPontos, 0, 2);
            layout.Controls.Add(_lstPontos, 0, 3);

            cardLegenda.Controls.Add(layout);
            return cardLegenda;
        }

        private async Task AlterarZoomAsync(int delta)
        {
            int novoZoom = Math.Clamp(_zoom + delta, OpenWeatherMapService.ZoomMinimo, OpenWeatherMapService.ZoomMaximo);
            if (novoZoom == _zoom) return;

            _zoom = novoZoom;
            _lblZoom.Text = $"ZOOM {_zoom}";
            _canvasMapa.Invalidate();
            await AtualizarTilesMapaAsync();
        }

        private async Task AtualizarTilesMapaAsync()
        {
            if (_canvasMapa.Width <= 10 || _canvasMapa.Height <= 10) return;

            var (tx, ty) = OpenWeatherMapService.LatLonParaTileXY(_centroLat, _centroLon, _zoom);
            string urlExibicao = _weatherMapService.ConstruirUrlTemplateExibicao(_camadaAtual, _zoom, tx, ty);
            _lblStatusTile.Text = $"Sincronizando tiles: {urlExibicao}";

            var resultado = await _weatherMapService.CarregarTilesViewportAsync(
                _centroLat,
                _centroLon,
                _zoom,
                _canvasMapa.Width,
                _canvasMapa.Height,
                _camadaAtual);

            _lblStatusTile.Text = $"{resultado.StatusMensagem}  |  Endpoint: {urlExibicao}";
            _canvasMapa.Invalidate();
        }

        private void AtualizarPainelDetalhePonto()
        {
            if (_pontoSelecionado == null)
            {
                _lblDetalhePonto.Text = "Toque em um pino no mapa ou selecione um ponto na lista lateral.";
                return;
            }

            string situacao = _pontoSelecionado.Disponivel ? "DISPONÍVEL PARA ATENDIMENTO" : "INTERDITADO / EM MANUTENÇÃO";
            _lblDetalhePonto.Text =
                $"SELECIONADO: #{_pontoSelecionado.Id} {_pontoSelecionado.Nome} ({_pontoSelecionado.ObterTipoFormatado()})  |  {situacao}\n" +
                $"Endereço: {_pontoSelecionado.Endereco} (CEP {_pontoSelecionado.CEP})  |  " +
                $"Coordenadas: ({_pontoSelecionado.Latitude:F4}, {_pontoSelecionado.Longitude:F4})  |  Distância ao Totem: {_pontoSelecionado.DistanciaMetros}m";
        }

        private void CanvasMapa_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // Verifica primeiro se clicou sobre algum pino de ponto de apoio
            for (int i = _areasToquePinos.Count - 1; i >= 0; i--)
            {
                var item = _areasToquePinos[i];
                if (item.AreaToque.Contains(e.Location))
                {
                    _pontoSelecionado = item.Ponto;
                    int idxLista = _pontos.FindIndex(p => p.Id == item.Ponto.Id);
                    if (idxLista >= 0 && _lstPontos.SelectedIndex != idxLista)
                    {
                        _lstPontos.SelectedIndex = idxLista;
                    }
                    else
                    {
                        AtualizarPainelDetalhePonto();
                        _canvasMapa.Invalidate();
                    }
                    return;
                }
            }

            _arrastandoMapa = true;
            _pontoInicioArraste = e.Location;
            _centroLatInicioArraste = _centroLat;
            _centroLonInicioArraste = _centroLon;
            _canvasMapa.Cursor = Cursors.SizeAll;
        }

        private void CanvasMapa_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_arrastandoMapa) return;

            int dx = e.X - _pontoInicioArraste.X;
            int dy = e.Y - _pontoInicioArraste.Y;

            var (inicioWorldX, inicioWorldY) = OpenWeatherMapService.LatLonParaMundoPixel(
                _centroLatInicioArraste,
                _centroLonInicioArraste,
                _zoom);

            var (novoLat, novoLon) = OpenWeatherMapService.MundoPixelParaLatLon(
                inicioWorldX - dx,
                inicioWorldY - dy,
                _zoom);

            _centroLat = novoLat;
            _centroLon = novoLon;
            _canvasMapa.Invalidate();
        }

        private async void CanvasMapa_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!_arrastandoMapa) return;

            _arrastandoMapa = false;
            _canvasMapa.Cursor = Cursors.Hand;
            await AtualizarTilesMapaAsync();
        }

        private void CanvasMapa_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = _canvasMapa.Width;
            int h = _canvasMapa.Height;

            // 1. Fundo cartográfico base (caso alguns tiles ainda estejam baixando ou offline)
            DesenharGradeCartograficaResiliente(g, w, h);

            // 2. Desenha tiles reais (OpenStreetMap + OpenWeather Weather Maps 1.0 API)
            var (tilesBase, tilesClima) = _weatherMapService.DesenharCamadasNoGraphics(
                g,
                _centroLat,
                _centroLon,
                _zoom,
                w,
                h,
                _camadaAtual,
                opacidadeClima: 0.72f);

            // Se a camada climática estiver ativa mas sem tile remoto (ex.: offline), desenha simulação térmica suave
            if (tilesClima == 0 && !string.Equals(_camadaAtual, "none", StringComparison.OrdinalIgnoreCase))
            {
                DesenharManchaTermicaSimulada(g, w, h);
            }

            // 3. Desenha anéis de cobertura a partir da posição geográfica do Totem Central
            PointF posTotem = OpenWeatherMapService.LatLonParaPosicaoTela(
                PontoApoio.LatitudeTotemCentral,
                PontoApoio.LongitudeTotemCentral,
                _centroLat,
                _centroLon,
                _zoom,
                w,
                h);

            using (var penRaio = new Pen(Color.FromArgb(170, 14, 92, 168), 1.6f) { DashStyle = DashStyle.Dash })
            {
                float escalaRaio = (float)Math.Pow(2, _zoom - 13);
                float r1 = 80f * escalaRaio;
                float r2 = 160f * escalaRaio;
                g.DrawEllipse(penRaio, posTotem.X - r1, posTotem.Y - r1, r1 * 2, r1 * 2);
                g.DrawEllipse(penRaio, posTotem.X - r2, posTotem.Y - r2, r2 * 2, r2 * 2);
            }

            // 4. Desenha Pino do Totem Central (Praça da Sé)
            DesenharMarcadorTotemCentral(g, posTotem);

            // 5. Desenha Pinos Georreferenciados dos Pontos de Apoio (Data/pontos_apoio.json)
            _areasToquePinos.Clear();
            foreach (var ponto in _pontos)
            {
                PointF pos = OpenWeatherMapService.LatLonParaPosicaoTela(
                    ponto.Latitude,
                    ponto.Longitude,
                    _centroLat,
                    _centroLon,
                    _zoom,
                    w,
                    h);

                bool selecionado = _pontoSelecionado != null && _pontoSelecionado.Id == ponto.Id;
                RectangleF rectToque = DesenharPinoPontoApoio(g, ponto, pos, selecionado);
                _areasToquePinos.Add((rectToque, ponto));
            }

            // 6. Selo informativo da camada no canto inferior direito do mapa
            DesenharSeloCamadaAtiva(g, w, h, tilesBase, tilesClima);
        }

        private static void DesenharGradeCartograficaResiliente(Graphics g, int w, int h)
        {
            g.Clear(Color.FromArgb(236, 242, 248));

            using var penGrid = new Pen(Color.FromArgb(214, 224, 234), 1f);
            for (int x = 0; x < w; x += 64)
            {
                g.DrawLine(penGrid, x, 0, x, h);
            }
            for (int y = 0; y < h; y += 64)
            {
                g.DrawLine(penGrid, 0, y, w, y);
            }
        }

        private void DesenharManchaTermicaSimulada(Graphics g, int w, int h)
        {
            PointF centro = OpenWeatherMapService.LatLonParaPosicaoTela(
                PontoApoio.LatitudeTotemCentral,
                PontoApoio.LongitudeTotemCentral,
                _centroLat,
                _centroLon,
                _zoom,
                w,
                h);

            Color corMancha = _camadaAtual switch
            {
                "precipitation_new" => Color.FromArgb(65, 14, 116, 144),
                "clouds_new" => Color.FromArgb(65, 100, 116, 139),
                "wind_new" => Color.FromArgb(65, 8, 145, 178),
                "pressure_new" => Color.FromArgb(65, 124, 58, 237),
                _ => Color.FromArgb(75, 220, 38, 38)
            };

            float raio = Math.Min(w, h) * 0.44f;
            var rect = new RectangleF(centro.X - raio, centro.Y - raio, raio * 2, raio * 2);
            using var path = new GraphicsPath();
            path.AddEllipse(rect);
            using var pgb = new PathGradientBrush(path)
            {
                CenterColor = corMancha,
                SurroundColors = new[] { Color.FromArgb(0, corMancha.R, corMancha.G, corMancha.B) }
            };
            g.FillPath(pgb, path);
        }

        private static void DesenharMarcadorTotemCentral(Graphics g, PointF pos)
        {
            float r = 13f;
            var rect = new RectangleF(pos.X - r, pos.Y - r, r * 2, r * 2);

            using (var brushHalo = new SolidBrush(Color.FromArgb(75, 15, 23, 42)))
            {
                g.FillEllipse(brushHalo, pos.X - 20, pos.Y - 20, 40, 40);
            }

            using (var brushCentro = new SolidBrush(Tema.FundoHeader))
            using (var penBorda = new Pen(Color.White, 2.5f))
            {
                g.FillEllipse(brushCentro, rect);
                g.DrawEllipse(penBorda, rect);
            }

            var rectTag = new Rectangle((int)pos.X - 62, (int)pos.Y + 15, 124, 22);
            using (var brushTag = new SolidBrush(Color.FromArgb(230, 15, 23, 42)))
            {
                g.FillRectangle(brushTag, rectTag);
            }

            TextRenderer.DrawText(
                g,
                "TOTEM CENTRAL (SÉ)",
                Tema.FonteTag,
                rectTag,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static RectangleF DesenharPinoPontoApoio(Graphics g, PontoApoio ponto, PointF pos, bool selecionado)
        {
            Color corPino = !ponto.Disponivel
                ? Tema.VermelhoAlerta
                : ponto.Tipo switch
                {
                    TipoPontoApoio.BebedouroPublico => Tema.CorTextoBebedouro,
                    TipoPontoApoio.AreaSombraDescanso => Tema.CorTextoSombra,
                    TipoPontoApoio.PostoSaudeClimatizado => Tema.CorTextoPosto,
                    _ => Tema.AzulPrimario
                };

            float raio = selecionado ? 18f : 14f;
            var rectPino = new RectangleF(pos.X - raio, pos.Y - raio, raio * 2, raio * 2);

            if (selecionado)
            {
                using var brushDestaque = new SolidBrush(Color.FromArgb(95, corPino));
                g.FillEllipse(brushDestaque, pos.X - 26, pos.Y - 26, 52, 52);
            }

            using (var brushFundo = new SolidBrush(corPino))
            using (var penBorda = new Pen(Color.White, selecionado ? 3f : 2f))
            {
                g.FillEllipse(brushFundo, rectPino);
                g.DrawEllipse(penBorda, rectPino);
            }

            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var brushTexto = new SolidBrush(Color.White))
            {
                g.DrawString(ponto.Id.ToString(), Tema.FontePequenaBold, brushTexto, rectPino, sf);
            }

            string rotuloCurto = ponto.Nome.Length > 26 ? ponto.Nome[..26] + "..." : ponto.Nome;
            Size tamTexto = TextRenderer.MeasureText(rotuloCurto, Tema.FontePequenaBold);
            var rectLabel = new Rectangle(
                (int)(pos.X - (tamTexto.Width / 2f) - 6),
                (int)(pos.Y - raio - 26),
                tamTexto.Width + 12,
                22);

            using (var brushLabel = new SolidBrush(selecionado ? corPino : Color.FromArgb(235, 255, 255, 255)))
            using (var penLabel = new Pen(corPino, 1.2f))
            {
                g.FillRectangle(brushLabel, rectLabel);
                g.DrawRectangle(penLabel, rectLabel);
            }

            TextRenderer.DrawText(
                g,
                rotuloCurto,
                Tema.FontePequenaBold,
                rectLabel,
                selecionado ? Color.White : Tema.TextoPrincipal,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            return new RectangleF(pos.X - 24, pos.Y - 24, 48, 48);
        }

        private void DesenharSeloCamadaAtiva(Graphics g, int w, int h, int tilesBase, int tilesClima)
        {
            string infoCamada = $"Camada: {_camadaAtual}  |  Tiles Base: {tilesBase}  |  Tiles OpenWeather: {tilesClima}  |  Arraste para mover";
            var rectSelo = new Rectangle(12, h - 34, Math.Min(w - 24, 590), 26);

            using (var brushSelo = new SolidBrush(Color.FromArgb(215, 15, 23, 42)))
            {
                g.FillRectangle(brushSelo, rectSelo);
            }

            TextRenderer.DrawText(
                g,
                infoCamada,
                Tema.FontePequenaBold,
                rectSelo,
                Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private static System.Windows.Forms.Panel CriarItemLegenda(string titulo, string descricao, Color corFundo, Color corTexto)
        {
            var pnl = new KioskCardPanel(corTexto)
            {
                Dock = DockStyle.Fill,
                BackColor = corFundo,
                Margin = new Padding(0, 3, 0, 3),
                Padding = new Padding(14, 6, 10, 6)
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = Tema.FontePequenaBold,
                ForeColor = corTexto,
                Dock = DockStyle.Top,
                Height = 20
            };

            var lblDesc = new Label
            {
                Text = descricao,
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill
            };

            pnl.Controls.Add(lblDesc);
            pnl.Controls.Add(lblTitulo);
            return pnl;
        }

        private sealed record ItemCamadaCombo(string Codigo, string Descricao)
        {
            public override string ToString() => Descricao;
        }

        private sealed class CanvasMapaPanel : System.Windows.Forms.Panel
        {
            public CanvasMapaPanel()
            {
                this.DoubleBuffered = true;
                this.ResizeRedraw = true;
            }
        }
    }
}
