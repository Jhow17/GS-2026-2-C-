using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela do Fluxo 1 (Munícipe) - Detalhamento das condições térmicas locais e orientações
    /// de proteção civil, saúde pública e hidratação obrigatória para trabalhadores e idosos.
    /// </summary>
    public class AlertaForm : Form
    {
        private readonly AlertaClimatico _alerta;

        public AlertaForm(AlertaClimatico alerta)
        {
            _alerta = alerta ?? throw new ArgumentNullException(nameof(alerta));
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            Tema.ConfigurarJanelaTotem(this);
            this.Text = "TermoAtiva - Alerta de Calor Extremo e Cuidados";

            string risco = _alerta.ClassificarNivelRisco();
            Color corRisco = risco switch
            {
                "Risco Extremo" => Tema.VermelhoAlerta,
                "Risco Alto" => Tema.LaranjaAviso,
                _ => Tema.VerdeSucesso
            };

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Tema.FundoPrincipal,
                Padding = new Padding(28, 22, 28, 22)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 122f)); // Cabeçalho Padronizado Sem Truncamento
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f)); // Painel de Indicadores Térmicos
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Diretrizes de Saúde Pública
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 78f));  // Botão Voltar

            // 1. CABEÇALHO
            var header = Tema.CriarCabecalhoPadrao(
                "PROTOCOLO DE VIGILÂNCIA EM SAÚDE AMBIENTAL",
                "ALERTA DE CALOR E PROTOCOLO DE PROTEÇÃO",
                "Verifique os índices micrometeorológicos atuais e siga as recomendações preventivas de hidratação e abrigo.",
                corRisco);
            pnlPrincipal.Controls.Add(header, 0, 0);

            // 2. INDICADORES TÉRMICOS EM DESTAQUE
            var pnlIndicadores = CriarPainelIndicadores(risco, corRisco);
            pnlPrincipal.Controls.Add(pnlIndicadores, 0, 1);

            // 3. LISTA DE RECOMENDAÇÕES DE SAÚDE PÚBLICA
            var pnlInstrucoes = CriarPainelRecomendacoes(corRisco);
            pnlPrincipal.Controls.Add(pnlInstrucoes, 0, 2);

            // 4. BOTÃO LARGO INFERIOR
            var btnVoltar = Tema.CriarBotaoReaLTaiizor("VOLTAR AO MENU PRINCIPAL", Tema.AzulPrimario, 66);
            btnVoltar.Dock = DockStyle.Fill;
            btnVoltar.Click += (s, e) => this.Close();
            pnlPrincipal.Controls.Add(btnVoltar, 0, 3);

            this.Controls.Add(pnlPrincipal);
        }

        private System.Windows.Forms.Panel CriarPainelIndicadores(string risco, Color corRisco)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 14)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23f));

            // Card 1: Classificação de Risco
            var cardClassificacao = new KioskCardPanel(corRisco)
            {
                Dock = DockStyle.Fill,
                BackColor = corRisco,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(20, 14, 20, 14)
            };

            var lblRotuloRisco = new Label
            {
                Text = "CLASSIFICAÇÃO DE RISCO",
                Font = Tema.FonteTag,
                ForeColor = Color.FromArgb(241, 245, 249),
                Dock = DockStyle.Top,
                Height = 24
            };

            var lblValorRisco = new Label
            {
                Text = risco.ToUpperInvariant(),
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblAtualizacao = new Label
            {
                Text = $"Leitura atualizada às {_alerta.UltimaAtualizacao:HH:mm}",
                Font = Tema.FontePequena,
                ForeColor = Color.FromArgb(241, 245, 249),
                Dock = DockStyle.Bottom,
                Height = 22
            };

            cardClassificacao.Controls.Add(lblValorRisco);
            cardClassificacao.Controls.Add(lblAtualizacao);
            cardClassificacao.Controls.Add(lblRotuloRisco);

            grid.Controls.Add(cardClassificacao, 0, 0);
            grid.Controls.Add(CriarCardMetrica("TEMPERATURA NO LOCAL", $"{_alerta.TemperaturaAtual:F1}°C", "Medição bulbo seco", corRisco, true), 1, 0);
            grid.Controls.Add(CriarCardMetrica("SENSAÇÃO TÉRMICA", $"{_alerta.SensacaoTermica:F1}°C", "Índice de calor aparente", corRisco, true), 2, 0);
            grid.Controls.Add(CriarCardMetrica("UMIDADE RELATIVA", $"{_alerta.UmidadeRelativa}%", "Abaixo do ideal (60%)", Tema.AzulPrimario, false), 3, 0);

            var wrapper = new System.Windows.Forms.Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
            wrapper.Controls.Add(grid);
            return wrapper;
        }

        private static System.Windows.Forms.Panel CriarCardMetrica(string legenda, string valor, string subtexto, Color corValor, bool margemDireita)
        {
            var pnl = new KioskCardPanel(corValor)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Margin = new Padding(0, 0, margemDireita ? 10 : 0, 0),
                Padding = new Padding(18, 12, 16, 12)
            };

            var lblLegenda = new Label
            {
                Text = legenda,
                Font = Tema.FonteTag,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Top,
                Height = 22
            };

            var lblValor = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = corValor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSub = new Label
            {
                Text = subtexto,
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoSuave,
                Dock = DockStyle.Bottom,
                Height = 22
            };

            pnl.Controls.Add(lblValor);
            pnl.Controls.Add(lblSub);
            pnl.Controls.Add(lblLegenda);
            return pnl;
        }

        private System.Windows.Forms.Panel CriarPainelRecomendacoes(Color corRisco)
        {
            var card = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(26, 18, 26, 18),
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblTitulo = new Label
            {
                Text = "ORIENTAÇÕES DE SAÚDE PÚBLICA E PROTEÇÃO AO TRABALHADOR",
                Font = Tema.FonteSubtitulo,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var recomendacoes = _alerta.ObterRecomendacoes();

            var tableLista = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = recomendacoes.Count,
                Padding = new Padding(0, 8, 0, 0)
            };

            float percentualLinha = 100f / Math.Max(1, recomendacoes.Count);
            for (int i = 0; i < recomendacoes.Count; i++)
            {
                tableLista.RowStyles.Add(new RowStyle(SizeType.Percent, percentualLinha));

                var itemPanel = new KioskCardPanel(corRisco)
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Margin = new Padding(0, 4, 0, 4),
                    Padding = new Padding(18, 8, 18, 8)
                };

                var lblItem = new Label
                {
                    Text = $"DIRETRIZ 0{i + 1}   |   {recomendacoes[i]}",
                    Font = Tema.FonteGeralDestaque,
                    ForeColor = Tema.TextoPrincipal,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                itemPanel.Controls.Add(lblItem);
                tableLista.Controls.Add(itemPanel, 0, i);
            }

            card.Controls.Add(tableLista);
            card.Controls.Add(lblTitulo);
            return card;
        }
    }
}
