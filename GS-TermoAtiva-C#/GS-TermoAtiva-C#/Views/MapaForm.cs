using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;
using ReaLTaiizor.Controls;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela de visualização geográfica dos pontos de refúgio e recursos climáticos.
    /// Preparada como container arquitetural para acoplamento do provedor GMap.NET.
    /// </summary>
    public class MapaForm : Form
    {
        private readonly TotemRepository _repository;

        public MapaForm(TotemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            Tema.ConfigurarJanelaTotem(this);
            this.Text = "TermoAtiva - Mapa de Refúgio e Recursos";

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
                "GEOPROCESSAMENTO E REDE DE APOIO MUNICIPAL",
                "MAPA DE REFÚGIO E RECURSOS",
                "Localização espacial dos bebedouros públicos, áreas arborizadas e postos de saúde climatizados no perímetro do totem.",
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
            var btnVoltar = Tema.CriarBotaoReaLTaiizor("VOLTAR AO MENU", Tema.AzulPrimario, 66);
            btnVoltar.Dock = DockStyle.Fill;
            btnVoltar.Click += (s, e) => this.Close();
            pnlPrincipal.Controls.Add(btnVoltar, 0, 2);

            this.Controls.Add(pnlPrincipal);
        }

        private System.Windows.Forms.Panel CriarPainelViewportMapa()
        {
            var cardMapa = new KioskCardPanel(Tema.CianoMapa)
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24),
                Margin = new Padding(0, 0, 14, 0)
            };

            // Desenho de grade cartográfica técnica ao fundo do container
            cardMapa.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var penGrid = new Pen(Color.FromArgb(226, 232, 240), 1f) { DashStyle = DashStyle.Dot };
                for (int x = 40; x < cardMapa.Width; x += 60)
                {
                    g.DrawLine(penGrid, x, 20, x, cardMapa.Height - 20);
                }
                for (int y = 40; y < cardMapa.Height; y += 60)
                {
                    g.DrawLine(penGrid, 20, y, cardMapa.Width - 20, y);
                }

                // Círculos de raio de proximidade a partir do Totem (100m, 500m, 1km)
                int cx = cardMapa.Width / 2;
                int cy = cardMapa.Height / 2;
                using var penRaio = new Pen(Color.FromArgb(186, 230, 253), 1.5f) { DashStyle = DashStyle.Dash };
                g.DrawEllipse(penRaio, cx - 90, cy - 90, 180, 180);
                g.DrawEllipse(penRaio, cx - 170, cy - 170, 340, 340);
                g.DrawEllipse(penRaio, cx - 250, cy - 250, 500, 500);
            };

            // Barra de status superior da camada GIS
            var pnlTopoGis = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Tema.FundoHeader,
                Padding = new Padding(16, 0, 16, 0)
            };

            var lblCoord = new Label
            {
                Text = "ORIGEM DO TOTEM: PRAÇA DA SÉ (-23.5505, -46.6333)  |  PROJEÇÃO: WGS-84  |  RAIO DE COBERTURA: 1.500m",
                Font = Tema.FontePequenaBold,
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTopoGis.Controls.Add(lblCoord);

            // Card central de aviso de implantação do GMap.NET
            var pnlAvisoCentral = new KioskCardPanel(Tema.AzulPrimario)
            {
                Size = new Size(640, 210),
                BackColor = Color.White,
                Padding = new Padding(28, 22, 28, 22)
            };

            var layoutAviso = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layoutAviso.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            layoutAviso.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));
            layoutAviso.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblTagCamada = new Label
            {
                Text = "CAMADA DE RENDERIZAÇÃO CARTOGRÁFICA",
                Font = Tema.FonteTag,
                ForeColor = Tema.CianoMapa,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblMensagemPrincipal = new Label
            {
                Text = "Módulo de Mapeamento Geográfico\n(Integração GMap.NET em implantação)",
                Font = Tema.FonteSubtitulo,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblDetalheTecnico = new Label
            {
                Text = "O container de visualização está configurado para acoplamento do controle GMapControl (OpenStreetMap / Cache Offline).\nOs pontos de apoio e distâncias calculadas pelo totem estão listados na legenda lateral.",
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            layoutAviso.Controls.Add(lblTagCamada, 0, 0);
            layoutAviso.Controls.Add(lblMensagemPrincipal, 0, 1);
            layoutAviso.Controls.Add(lblDetalheTecnico, 0, 2);
            pnlAvisoCentral.Controls.Add(layoutAviso);

            cardMapa.Controls.Add(pnlAvisoCentral);
            cardMapa.Controls.Add(pnlTopoGis);

            cardMapa.Resize += (s, e) =>
            {
                pnlAvisoCentral.Location = new Point(
                    Math.Max(20, (cardMapa.ClientSize.Width - pnlAvisoCentral.Width) / 2),
                    Math.Max(60, (cardMapa.ClientSize.Height - pnlAvisoCentral.Height) / 2)
                );
            };

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
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Pontos Mapeados

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
                "Bebedouros Públicos",
                "Água potável gratuita para hidratação",
                Tema.CorFundoBebedouro,
                Tema.CorTextoBebedouro), 0, 0);

            pnlCategorias.Controls.Add(CriarItemLegenda(
                "Áreas de Sombra / Descanso",
                "Praças arborizadas e refúgios naturais",
                Tema.CorFundoSombra,
                Tema.CorTextoSombra), 0, 1);

            pnlCategorias.Controls.Add(CriarItemLegenda(
                "Postos de Saúde Climatizados",
                "UBS e ambientes com resfriamento interno",
                Tema.CorFundoPosto,
                Tema.CorTextoPosto), 0, 2);

            var lblTituloPontos = new Label
            {
                Text = "PONTOS GEORREFERENCIADOS NO PERÍMETRO",
                Font = Tema.FonteTag,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lstPontos = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = Tema.FontePequena,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Tema.TextoPrincipal,
                IntegralHeight = false
            };

            foreach (var p in _repository.ObterTodosPontos())
            {
                string disp = p.Disponivel ? "ATIVO" : "MANUTENÇÃO";
                lstPontos.Items.Add($"[{p.DistanciaMetros}m] {p.Nome} - {p.Bairro} ({disp})");
            }

            layout.Controls.Add(lblTituloLegenda, 0, 0);
            layout.Controls.Add(pnlCategorias, 0, 1);
            layout.Controls.Add(lblTituloPontos, 0, 2);
            layout.Controls.Add(lstPontos, 0, 3);

            cardLegenda.Controls.Add(layout);
            return cardLegenda;
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
    }
}
