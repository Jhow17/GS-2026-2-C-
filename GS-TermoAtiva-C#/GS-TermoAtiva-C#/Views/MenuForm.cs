using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;
using ReaLTaiizor.Controls;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela principal pública do Totem TermoAtiva (Interface de Autoatendimento Urbano).
    /// Apresenta telemetria climática em tempo real e módulos de navegação por toque em grade 2x2.
    /// </summary>
    public class MenuForm : Form
    {
        private readonly TotemRepository _repository;
        private Label _lblTemperatura = null!;
        private Label _lblSensacao = null!;
        private Label _lblUmidade = null!;
        private Label _lblBadgeRisco = null!;
        private Label _lblDataHora = null!;
        private System.Windows.Forms.Timer _relogioTimer = null!;

        public MenuForm(TotemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            Tema.ConfigurarJanelaTotem(this);
            this.Text = "TermoAtiva - Totem Urbano de Refúgio Climático";

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Tema.FundoPrincipal,
                Padding = new Padding(28, 22, 28, 22)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 210f)); // Painel de Telemetria Climática Superior
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Grade 2x2 de Módulos de Autoatendimento
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 82f));  // Barra de Status e Controle Operacional

            // 1. TOPO: Painel de Telemetria Climática
            var pnlTopo = CriarPainelClimatico();
            pnlPrincipal.Controls.Add(pnlTopo, 0, 0);

            // 2. CENTRO: Grade de Módulos Interativos (4 Módulos incluindo Mapa)
            var pnlBotoes = CriarPainelModulosAcao();
            pnlPrincipal.Controls.Add(pnlBotoes, 0, 1);

            // 3. RODAPÉ: Relógio Digital e Acesso Restrito
            var pnlRodape = CriarPainelRodape();
            pnlPrincipal.Controls.Add(pnlRodape, 0, 2);

            this.Controls.Add(pnlPrincipal);

            _relogioTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _relogioTimer.Tick += (s, e) => AtualizarRelogio();
            _relogioTimer.Start();

            this.FormClosed += (s, e) => _relogioTimer.Dispose();

            AtualizarDadosClimaticos();
            AtualizarRelogio();
        }

        private System.Windows.Forms.Panel CriarPainelClimatico()
        {
            var cardClima = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(26, 18, 26, 18),
                Margin = new Padding(0, 0, 0, 14)
            };

            var tableClima = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tableClima.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            tableClima.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));

            // Coluna Esquerda: Cabeçalho Institucional Sem Truncamento + Badge de Risco
            var pnlEsquerda = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0)
            };
            pnlEsquerda.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f)); // Tag superior
            pnlEsquerda.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f)); // Título principal
            pnlEsquerda.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f)); // Subtítulo
            pnlEsquerda.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Badge de Risco

            var lblTag = new Label
            {
                Text = "SISTEMA MUNICIPAL DE MITIGAÇÃO TÉRMICA  |  TERMINAL DE AUTOATENDIMENTO",
                Font = Tema.FonteTag,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblTitulo = new Label
            {
                Text = "TERMOATIVA  |  APOIO CLIMÁTICO URBANO",
                Font = Tema.FonteTitulo,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSubtitulo = new Label
            {
                Text = "Toque em um dos módulos abaixo para localizar água potável, áreas de sombra ou orientações.",
                Font = Tema.FonteGeral,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlContainerBadge = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 30, 2)
            };

            _lblBadgeRisco = new Label
            {
                Text = "CLASSIFICAÇÃO ATUAL: RISCO EXTREMO",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Color.White,
                BackColor = Tema.VermelhoAlerta,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Tema.AplicarRegiaoArredondada(_lblBadgeRisco, 6);
            pnlContainerBadge.Controls.Add(_lblBadgeRisco);

            pnlEsquerda.Controls.Add(lblTag, 0, 0);
            pnlEsquerda.Controls.Add(lblTitulo, 0, 1);
            pnlEsquerda.Controls.Add(lblSubtitulo, 0, 2);
            pnlEsquerda.Controls.Add(pnlContainerBadge, 0, 3);

            // Coluna Direita: Três Cartões de Telemetria (Temperatura, Sensação e Umidade)
            var tableMetricas = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            tableMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            tableMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            tableMetricas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

            var cardTemp = CriarBlocoTelemetria("TEMPERATURA", out _lblTemperatura, "36.5°C", Tema.VermelhoAlerta);
            var cardSensacao = CriarBlocoTelemetria("SENSAÇÃO TÉRMICA", out _lblSensacao, "40.2°C", Tema.LaranjaAviso);
            var cardUmidade = CriarBlocoTelemetria("UMIDADE DO AR", out _lblUmidade, "28%", Tema.AzulPrimario);

            tableMetricas.Controls.Add(cardTemp, 0, 0);
            tableMetricas.Controls.Add(cardSensacao, 1, 0);
            tableMetricas.Controls.Add(cardUmidade, 2, 0);

            tableClima.Controls.Add(pnlEsquerda, 0, 0);
            tableClima.Controls.Add(tableMetricas, 1, 0);

            cardClima.Controls.Add(tableClima);
            return cardClima;
        }

        private static System.Windows.Forms.Panel CriarBlocoTelemetria(string rotulo, out Label lblValor, string valorInicial, Color corDestaque)
        {
            var card = new KioskCardPanel(corDestaque)
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Margin = new Padding(6, 4, 0, 4),
                Padding = new Padding(14, 12, 10, 12)
            };

            var lblRotulo = new Label
            {
                Text = rotulo,
                Font = Tema.FonteTag,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblValor = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = corDestaque,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(lblValor);
            card.Controls.Add(lblRotulo);
            return card;
        }

        private System.Windows.Forms.Panel CriarPainelModulosAcao()
        {
            var gridModulos = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(-8, 0, -8, 0)
            };

            gridModulos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            gridModulos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            gridModulos.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            gridModulos.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            // MÓDULO 01: ALERTA DE CALOR AGORA
            var tileAlerta = new KioskActionTile(
                "01",
                "ALERTA DE CALOR AGORA",
                "Consulte orientações imediatas de saúde pública, intervalos de hidratação e sinais de risco térmico.",
                Tema.VermelhoAlerta)
            {
                Dock = DockStyle.Fill
            };
            tileAlerta.Click += (s, e) => AbrirAlerta();

            // MÓDULO 02: ONDE BEBER ÁGUA / DESCANSAR
            var tilePontos = new KioskActionTile(
                "02",
                "ONDE BEBER ÁGUA / DESCANSAR",
                "Veja a lista de bebedouros públicos, áreas arborizadas de descanso e postos de saúde climatizados.",
                Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill
            };
            tilePontos.Click += (s, e) => AbrirPontosApoio();

            // MÓDULO 03: MAPA DE REFÚGIO E RECURSOS (Novo requisito 3.1)
            var tileMapa = new KioskActionTile(
                "03",
                "MAPA DE REFÚGIO E RECURSOS",
                "Visualize a distribuição geográfica dos pontos de apoio e o perímetro de cobertura do totem.",
                Tema.CianoMapa)
            {
                Dock = DockStyle.Fill
            };
            tileMapa.Click += (s, e) => AbrirMapa();

            // MÓDULO 04: INFORMAR PROBLEMA EM BEBEDOURO
            var tileOcorrencia = new KioskActionTile(
                "04",
                "INFORMAR PROBLEMA EM BEBEDOURO",
                "Notifique a zeladoria municipal caso um ponto esteja sem água, com torneira danificada ou fechado.",
                Tema.LaranjaAviso)
            {
                Dock = DockStyle.Fill
            };
            tileOcorrencia.Click += (s, e) => AbrirOcorrencia();

            gridModulos.Controls.Add(tileAlerta, 0, 0);
            gridModulos.Controls.Add(tilePontos, 1, 0);
            gridModulos.Controls.Add(tileMapa, 0, 1);
            gridModulos.Controls.Add(tileOcorrencia, 1, 1);

            var wrapper = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            wrapper.Controls.Add(gridModulos);
            return wrapper;
        }

        private System.Windows.Forms.Panel CriarPainelRodape()
        {
            var pnlRodape = new KioskCardPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(20, 12, 20, 12),
                Margin = new Padding(0, 14, 0, 0)
            };

            _lblDataHora = new Label
            {
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Left,
                Width = 480,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlBotoesRodape = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 2, 0, 0)
            };

            var btnSair = new System.Windows.Forms.Button
            {
                Text = "Sair do Sistema",
                Size = new Size(165, 48),
                Margin = new Padding(10, 0, 0, 0)
            };
            Tema.EstilizarBotaoSecundario(btnSair, 48);
            btnSair.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(
                    "Deseja realmente encerrar a operação deste terminal?",
                    "Encerrar Terminal",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (confirm == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };

            var btnOperador = Tema.CriarBotaoReaLTaiizor("Acesso Operador", Tema.FundoHeader, 48, 8);
            btnOperador.Width = 195;
            btnOperador.Font = Tema.FonteGeralDestaque;
            btnOperador.Margin = new Padding(10, 0, 0, 0);
            btnOperador.Click += (s, e) => AbrirModoAdmin();

            pnlBotoesRodape.Controls.Add(btnSair);
            pnlBotoesRodape.Controls.Add(btnOperador);

            pnlRodape.Controls.Add(_lblDataHora);
            pnlRodape.Controls.Add(pnlBotoesRodape);

            return pnlRodape;
        }

        public void AtualizarDadosClimaticos()
        {
            var alerta = _repository.ObterAlertaAtual();
            _lblTemperatura.Text = $"{alerta.TemperaturaAtual:F1}°C";
            _lblSensacao.Text = $"{alerta.SensacaoTermica:F1}°C";
            _lblUmidade.Text = $"{alerta.UmidadeRelativa}%";

            string risco = alerta.ClassificarNivelRisco();
            _lblBadgeRisco.Text = $"NÍVEL DE ALERTA OFICIAL: {risco.ToUpperInvariant()}";

            if (risco == "Risco Extremo")
            {
                _lblBadgeRisco.BackColor = Tema.VermelhoAlerta;
                _lblTemperatura.ForeColor = Tema.VermelhoAlerta;
            }
            else if (risco == "Risco Alto")
            {
                _lblBadgeRisco.BackColor = Tema.LaranjaAviso;
                _lblTemperatura.ForeColor = Tema.LaranjaAviso;
            }
            else
            {
                _lblBadgeRisco.BackColor = Tema.VerdeSucesso;
                _lblTemperatura.ForeColor = Tema.VerdeSucesso;
            }
        }

        private void AtualizarRelogio()
        {
            _lblDataHora.Text = $"HORÁRIO LOCAL: {DateTime.Now:dd/MM/yyyy  |  HH:mm:ss}";
        }

        private void AbrirAlerta()
        {
            using var form = new AlertaForm(_repository.ObterAlertaAtual());
            form.ShowDialog(this);
            AtualizarDadosClimaticos();
        }

        private void AbrirPontosApoio()
        {
            using var form = new PontosApoioForm(_repository);
            form.ShowDialog(this);
        }

        private void AbrirMapa()
        {
            using var form = new MapaForm(_repository);
            form.ShowDialog(this);
        }

        private void AbrirOcorrencia()
        {
            using var form = new OcorrenciaForm(_repository);
            form.ShowDialog(this);
        }

        private void AbrirModoAdmin()
        {
            using var auth = new AdminAuthForm(_repository);
            auth.ShowDialog(this);
            AtualizarDadosClimaticos();
        }
    }
}
