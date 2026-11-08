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

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Tema.FundoPrincipal,
                Padding = new Padding(28)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 220f)); // Cartão de Risco e Destaque Térmico
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Lista de Recomendações de Saúde
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 95f));    // Botão Voltar

            // 1. CARTOES SUPERIORES COM DADOS TÉRMICOS
            var pnlCabecalho = CriarCardCabecalho();
            pnlPrincipal.Controls.Add(pnlCabecalho, 0, 0);

            // 2. CORPO: RECOMENDAÇÕES DE SAÚDE PÚBLICA
            var pnlInstrucoes = CriarPainelRecomendacoes();
            pnlPrincipal.Controls.Add(pnlInstrucoes, 0, 1);

            // 3. RODAPÉ: BOTÃO RETORNAR AO MENU
            var btnVoltar = new Button
            {
                Text = "⬅  VOLTAR AO MENU PRINCIPAL",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 0)
            };
            Tema.EstilizarBotao(btnVoltar, Tema.AzulPrimario, 85);
            btnVoltar.Click += (s, e) => this.Close();
            pnlPrincipal.Controls.Add(btnVoltar, 0, 2);

            this.Controls.Add(pnlPrincipal);
        }

        private Panel CriarCardCabecalho()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(24),
                Margin = new Padding(0, 0, 0, 16)
            };

            string risco = _alerta.ClassificarNivelRisco();
            Color corRisco = risco switch
            {
                "Risco Extremo" => Tema.VermelhoAlerta,
                "Risco Alto" => Tema.LaranjaAviso,
                _ => Tema.VerdeSucesso
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            // Coluna Esquerda: Badge de Risco e Título
            var pnlEsquerda = new Panel { Dock = DockStyle.Fill };

            var badgeRisco = new Label
            {
                Text = $"  CLASSIFICAÇÃO OFICIAL: {risco.ToUpperInvariant()}  ",
                Font = Tema.FonteBotaoToque,
                ForeColor = Color.White,
                BackColor = corRisco,
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblDescricaoRisco = new Label
            {
                Text = risco == "Risco Extremo"
                    ? "Condição climática crítica com risco de choque térmico, desidratação severa e insolação."
                    : "Calor intenso. Requer hidratação constante e pausas periódicas em locais cobertos.",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 10, 5, 0)
            };

            pnlEsquerda.Controls.Add(lblDescricaoRisco);
            pnlEsquerda.Controls.Add(badgeRisco);

            // Coluna Direita: Métricas em Gigante
            var pnlDireita = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            pnlDireita.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            pnlDireita.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            pnlDireita.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));

            pnlDireita.Controls.Add(CriarCardMetrica("TEMPERATURA", $"{_alerta.TemperaturaAtual:F1}°C", corRisco), 0, 0);
            pnlDireita.Controls.Add(CriarCardMetrica("SENSAÇÃO", $"{_alerta.SensacaoTermica:F1}°C", corRisco), 1, 0);
            pnlDireita.Controls.Add(CriarCardMetrica("UMIDADE", $"{_alerta.UmidadeRelativa}%", Tema.AzulPrimario), 2, 0);

            table.Controls.Add(pnlEsquerda, 0, 0);
            table.Controls.Add(pnlDireita, 1, 0);

            card.Controls.Add(table);
            return card;
        }

        private Panel CriarCardMetrica(string legenda, string valor, Color corValor)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(249, 250, 251),
                Margin = new Padding(6),
                Padding = new Padding(8)
            };

            var lblLegenda = new Label
            {
                Text = legenda,
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblValor = new Label
            {
                Text = valor,
                Font = Tema.FonteTitulo,
                ForeColor = corValor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.Add(lblValor);
            pnl.Controls.Add(lblLegenda);
            return pnl;
        }

        private Panel CriarPainelRecomendacoes()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(24),
                Margin = new Padding(0, 0, 0, 16)
            };

            var lblTitulo = new Label
            {
                Text = "📋 ORIENTAÇÕES DE SAÚDE PÚBLICA E PROTEÇÃO CIVIL",
                Font = Tema.FonteSubtitulo,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlLista = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };

            var recomendacoes = _alerta.ObterRecomendacoes();
            int index = 1;
            foreach (var item in recomendacoes)
            {
                var itemCard = new Panel
                {
                    Width = 1100,
                    Height = 75,
                    BackColor = Color.FromArgb(248, 249, 250),
                    Margin = new Padding(0, 0, 0, 12),
                    Padding = new Padding(16, 12, 16, 12)
                };

                var lblItem = new Label
                {
                    Text = $"{index}. {item}",
                    Font = Tema.FonteGeralDestaque,
                    ForeColor = Tema.TextoPrincipal,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                itemCard.Controls.Add(lblItem);
                pnlLista.Controls.Add(itemCard);
                index++;
            }

            card.Controls.Add(pnlLista);
            card.Controls.Add(lblTitulo);
            return card;
        }
    }
}
