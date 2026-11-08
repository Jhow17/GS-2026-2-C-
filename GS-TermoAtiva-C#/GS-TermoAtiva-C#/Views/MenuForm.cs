using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela principal pública do Totem TermoAtiva.
    /// Projetada com ergonomia tátil para uso sob luz solar direta por pessoas idosas,
    /// trabalhadores ao ar livre e cidadãos sem telefone celular.
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

            // Layout principal em 3 seções: Topo (Clima), Centro (Ações de Toque) e Rodapé (Relógio e Operador)
            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Tema.FundoPrincipal,
                Padding = new Padding(24)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 220f)); // Topo Climático
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Ações Centrais
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));   // Rodapé

            // 1. TOPO: Painel de Monitoramento Climático em Tempo Real
            var pnlTopo = CriarPainelClimatico();
            pnlPrincipal.Controls.Add(pnlTopo, 0, 0);

            // 2. CENTRO: Três Grandes Botões de Toque
            var pnlBotoes = CriarPainelBotoesAcao();
            pnlPrincipal.Controls.Add(pnlBotoes, 0, 1);

            // 3. RODAPÉ: Relógio e Acesso Operador
            var pnlRodape = CriarPainelRodape();
            pnlPrincipal.Controls.Add(pnlRodape, 0, 2);

            this.Controls.Add(pnlPrincipal);

            // Timer para atualização do relógio em tempo real
            _relogioTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _relogioTimer.Tick += (s, e) => AtualizarRelogio();
            _relogioTimer.Start();

            this.FormClosed += (s, e) => _relogioTimer.Dispose();

            AtualizarDadosClimaticos();
            AtualizarRelogio();
        }

        private Panel CriarPainelClimatico()
        {
            var cardClima = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, 0, 15)
            };

            var tableClima = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tableClima.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tableClima.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            // Lado Esquerdo: Identificação do Totem e Cidade
            var pnlIdentificacao = new Panel { Dock = DockStyle.Fill };

            var lblTitulo = new Label
            {
                Text = "☀️ TERMOATIVA | TOTEM DE APOIO CLIMÁTICO",
                Font = Tema.FonteTitulo,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSubtitulo = new Label
            {
                Text = "Defesa Civil & Saúde Urbana • Ponto de Hidratação e Acolhimento Térmico",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblBadgeRisco = new Label
            {
                Text = "ALERTA: RISCO EXTREMO",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Tema.VermelhoAlerta,
                Dock = DockStyle.Bottom,
                Height = 55,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlIdentificacao.Controls.Add(_lblBadgeRisco);
            pnlIdentificacao.Controls.Add(lblSubtitulo);
            pnlIdentificacao.Controls.Add(lblTitulo);

            // Lado Direito: Métricas em Destaque Térmico
            var pnlMetricas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };

            _lblTemperatura = new Label
            {
                Text = "36.5°C",
                Font = Tema.FonteDestaqueTermico,
                ForeColor = Tema.VermelhoAlerta,
                AutoSize = true,
                Margin = new Padding(20, 0, 0, 0)
            };

            var pnlDetalhesMetricas = new Panel
            {
                Size = new Size(240, 100),
                Margin = new Padding(0)
            };

            _lblSensacao = new Label
            {
                Text = "Sensação: 40.2°C",
                Font = Tema.FonteSubtitulo,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleRight
            };

            _lblUmidade = new Label
            {
                Text = "Umidade do Ar: 28%",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlDetalhesMetricas.Controls.Add(_lblUmidade);
            pnlDetalhesMetricas.Controls.Add(_lblSensacao);

            pnlMetricas.Controls.Add(_lblTemperatura);
            pnlMetricas.Controls.Add(pnlDetalhesMetricas);

            tableClima.Controls.Add(pnlIdentificacao, 0, 0);
            tableClima.Controls.Add(pnlMetricas, 1, 0);

            cardClima.Controls.Add(tableClima);
            return cardClima;
        }

        private Panel CriarPainelBotoesAcao()
        {
            var pnlAcoes = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };

            pnlAcoes.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            pnlAcoes.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            pnlAcoes.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

            // BOTÃO 1: ALERTA DE CALOR AGORA
            var btnAlerta = new Button
            {
                Text = "🌡️  ALERTA DE CALOR AGORA\nInstruções imediatas de proteção à saúde, riscos e sintomas",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 15)
            };
            Tema.EstilizarBotao(btnAlerta, Tema.VermelhoAlerta, 110);
            btnAlerta.Click += (s, e) => AbrirAlerta();

            // BOTÃO 2: ONDE BEBER ÁGUA / DESCANSAR
            var btnPontos = new Button
            {
                Text = "💧  ONDE BEBER ÁGUA / DESCANSAR\nConsulte bebedouros públicos, praças arborizadas e postos climatizados",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 15)
            };
            Tema.EstilizarBotao(btnPontos, Tema.AzulPrimario, 110);
            btnPontos.Click += (s, e) => AbrirPontosApoio();

            // BOTÃO 3: INFORMAR PROBLEMA EM BEBEDOURO
            var btnOcorrencia = new Button
            {
                Text = "⚠️  INFORMAR PROBLEMA EM BEBEDOURO\nAvise se a torneira está quebrada, sem água ou o local está fechado",
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            Tema.EstilizarBotao(btnOcorrencia, Tema.LaranjaAviso, 110);
            btnOcorrencia.Click += (s, e) => AbrirOcorrencia();

            pnlAcoes.Controls.Add(btnAlerta, 0, 0);
            pnlAcoes.Controls.Add(btnPontos, 0, 1);
            pnlAcoes.Controls.Add(btnOcorrencia, 0, 2);

            return pnlAcoes;
        }

        private Panel CriarPainelRodape()
        {
            var pnlRodape = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(15, 10, 15, 10),
                Margin = new Padding(0, 15, 0, 0)
            };

            _lblDataHora = new Label
            {
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Left,
                Width = 420,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlBotoesRodape = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false
            };

            var btnSair = new Button
            {
                Text = "Sair do Totem",
                Size = new Size(160, 55),
                Margin = new Padding(10, 0, 0, 0)
            };
            Tema.EstilizarBotaoSecundario(btnSair, 55);
            btnSair.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(
                    "Deseja realmente encerrar a aplicação do Totem?",
                    "Encerrar Totem",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (confirm == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };

            var btnOperador = new Button
            {
                Text = "🔒 Acesso Operador",
                Size = new Size(200, 55),
                Margin = new Padding(10, 0, 0, 0)
            };
            Tema.EstilizarBotaoSecundario(btnOperador, 55);
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
            _lblSensacao.Text = $"Sensação: {alerta.SensacaoTermica:F1}°C";
            _lblUmidade.Text = $"Umidade do Ar: {alerta.UmidadeRelativa}%";

            string risco = alerta.ClassificarNivelRisco();
            _lblBadgeRisco.Text = $"ALERTA DE SAÚDE: {risco.ToUpperInvariant()}";

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
            _lblDataHora.Text = $"🕒 {DateTime.Now:dddd, dd 'de' MMMM 'de' yyyy • HH:mm:ss}";
        }

        private void AbrirAlerta()
        {
            var form = new AlertaForm(_repository.ObterAlertaAtual());
            form.ShowDialog(this);
            AtualizarDadosClimaticos();
        }

        private void AbrirPontosApoio()
        {
            var form = new PontosApoioForm(_repository);
            form.ShowDialog(this);
        }

        private void AbrirOcorrencia()
        {
            var form = new OcorrenciaForm(_repository);
            form.ShowDialog(this);
        }

        private void AbrirModoAdmin()
        {
            var auth = new AdminAuthForm(_repository);
            auth.ShowDialog(this);
            AtualizarDadosClimaticos();
        }
    }
}
