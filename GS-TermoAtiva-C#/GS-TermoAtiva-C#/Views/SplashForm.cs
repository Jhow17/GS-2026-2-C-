using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;
using ReaLTaiizor.Controls;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela de inicialização do Totem TermoAtiva.
    /// Realiza a checagem resiliente de persistência local, prepara os repositórios
    /// e transiciona suavemente para o menu principal de autoatendimento.
    /// </summary>
    public class SplashForm : Form
    {
        private HopeProgressBar _progressBar = null!;
        private Label _lblStatus = null!;
        private System.Windows.Forms.Timer _timerAnimacao = null!;
        private TotemRepository? _repository;
        private int _ticksContador = 0;

        public SplashForm()
        {
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            Tema.ConfigurarJanelaTotem(this);
            this.Text = "TermoAtiva - Carregando Sistema";

            var pnlCentro = new KioskCardPanel(Tema.AzulPrimario)
            {
                Size = new Size(880, 480),
                BackColor = Tema.FundoCard,
                Padding = new Padding(44, 36, 44, 36)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // Tag Institucional
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));  // Título Principal
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));  // Subtítulo
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Descrição
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // Status de Inicialização
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));  // Barra de Progresso + Versão

            var lblTag = new Label
            {
                Text = "DEFESA CIVIL MUNICIPAL  |  REDE DE PROTEÇÃO CLIMÁTICA",
                Font = Tema.FonteTag,
                ForeColor = Tema.LaranjaAviso,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var lblLogo = new Label
            {
                Text = "TERMOATIVA  |  TERMINAL URBANO",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = Tema.TextoPrincipal,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var lblSubtitulo = new Label
            {
                Text = "Sistema de Mitigação ao Calor Extremo e Apoio ao Cidadão",
                Font = Tema.FonteSubtitulo,
                ForeColor = Tema.AzulPrimario,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var lblDescricao = new Label
            {
                Text = "Atendimento público de alta visibilidade voltado a trabalhadores ao ar livre,\nidosos, garis e munícipes em situação de vulnerabilidade térmica.",
                Font = Tema.FonteGeral,
                ForeColor = Tema.TextoSecundario,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            _lblStatus = new Label
            {
                Text = "Inicializando sensores micrometeorológicos e base local de refúgio...",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoPrincipal,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            var pnlProgresso = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40, 4, 40, 0)
            };

            _progressBar = new HopeProgressBar
            {
                Dock = DockStyle.Top,
                Height = 24,
                ValueNumber = 10,
                FullBarColor = Tema.AzulPrimario,
                BarColor = Tema.AzulPrimario,
                BaseColor = Tema.FundoSecundario
            };

            var lblVersao = new Label
            {
                Text = "Modo Quiosque Autônomo Resiliente  |  .NET 8.0 Windows Forms",
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoSuave,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 26
            };

            pnlProgresso.Controls.Add(_progressBar);
            pnlProgresso.Controls.Add(lblVersao);

            layout.Controls.Add(lblTag, 0, 0);
            layout.Controls.Add(lblLogo, 0, 1);
            layout.Controls.Add(lblSubtitulo, 0, 2);
            layout.Controls.Add(lblDescricao, 0, 3);
            layout.Controls.Add(_lblStatus, 0, 4);
            layout.Controls.Add(pnlProgresso, 0, 5);

            pnlCentro.Controls.Add(layout);
            this.Controls.Add(pnlCentro);

            this.Resize += (s, e) =>
            {
                pnlCentro.Location = new Point(
                    Math.Max(20, (this.ClientSize.Width - pnlCentro.Width) / 2),
                    Math.Max(20, (this.ClientSize.Height - pnlCentro.Height) / 2)
                );
            };

            _timerAnimacao = new System.Windows.Forms.Timer
            {
                Interval = 100
            };
            _timerAnimacao.Tick += TimerAnimacao_Tick;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _timerAnimacao.Start();
        }

        private void TimerAnimacao_Tick(object? sender, EventArgs e)
        {
            _ticksContador++;
            int progresso = Math.Min(_ticksContador * 5, 100);
            _progressBar.ValueNumber = progresso;

            if (_ticksContador == 5)
            {
                _lblStatus.Text = "Carregando catálogo georreferenciado de bebedouros e áreas de sombra...";
                try
                {
                    _repository = new TotemRepository();
                }
                catch
                {
                    _repository = new TotemRepository();
                }
            }
            else if (_ticksContador == 12)
            {
                _lblStatus.Text = "Validando alertas térmicos e parâmetros de estresse de calor...";
            }
            else if (_ticksContador >= 20)
            {
                _timerAnimacao.Stop();
                _timerAnimacao.Dispose();

                AbrirMenuPrincipal();
            }
        }

        private void AbrirMenuPrincipal()
        {
            _repository ??= new TotemRepository();
            var menuForm = new MenuForm(_repository);
            menuForm.FormClosed += (s, args) => this.Close();
            this.Hide();
            menuForm.Show();
        }
    }
}
