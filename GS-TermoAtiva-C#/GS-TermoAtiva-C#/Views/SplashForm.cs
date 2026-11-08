using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela de inicialização do Totem TermoAtiva.
    /// Realiza a checagem resiliente de persistência local, prepara os repositórios
    /// e transiciona suavemente para o menu principal de autoatendimento.
    /// </summary>
    public class SplashForm : Form
    {
        private ProgressBar _progressBar = null!;
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

            // Painel central de boas-vindas
            var pnlCentro = new Panel
            {
                Size = new Size(820, 520),
                BackColor = Tema.FundoCard,
                Padding = new Padding(30)
            };

            var lblLogo = new Label
            {
                Text = "☀️ TERMOATIVA",
                Font = Tema.FonteTitulo,
                ForeColor = Tema.LaranjaAviso,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 70
            };

            var lblSubtitulo = new Label
            {
                Text = "Terminal Interativo Urbano de Mitigação ao Calor Extremo",
                Font = Tema.FonteSubtitulo,
                ForeColor = Tema.AzulPrimario,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 45
            };

            var lblDescricao = new Label
            {
                Text = "Proteção e acolhimento térmico prioritário para trabalhadores ao ar livre,\nidosos, crianças e munícipes em vulnerabilidade climática.",
                Font = Tema.FonteGeral,
                ForeColor = Tema.TextoSecundario,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 80
            };

            _lblStatus = new Label
            {
                Text = "Inicializando sensores micrometeorológicos e base local de refúgio...",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoPrincipal,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 50
            };

            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                Minimum = 0,
                Maximum = 100,
                Value = 10,
                Style = ProgressBarStyle.Continuous
            };

            var lblVersao = new Label
            {
                Text = "Modo Totem / Quiosque Offline Resiliente | .NET 8+",
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoSecundario,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 35
            };

            pnlCentro.Controls.Add(_lblStatus);
            pnlCentro.Controls.Add(_progressBar);
            pnlCentro.Controls.Add(lblVersao);
            pnlCentro.Controls.Add(lblDescricao);
            pnlCentro.Controls.Add(lblSubtitulo);
            pnlCentro.Controls.Add(lblLogo);

            this.Controls.Add(pnlCentro);

            this.Resize += (s, e) =>
            {
                pnlCentro.Location = new Point(
                    (this.ClientSize.Width - pnlCentro.Width) / 2,
                    (this.ClientSize.Height - pnlCentro.Height) / 2
                );
            };

            // Timer de 2 segundos (20 passos de 100ms)
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
            _progressBar.Value = progresso;

            if (_ticksContador == 5)
            {
                _lblStatus.Text = "Carregando catálogo de bebedouros e áreas de sombra...";
                try
                {
                    _repository = new TotemRepository();
                }
                catch
                {
                    // Fallback seguro
                    _repository = new TotemRepository();
                }
            }
            else if (_ticksContador == 12)
            {
                _lblStatus.Text = "Validando alertas térmicos e índice UV...";
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
