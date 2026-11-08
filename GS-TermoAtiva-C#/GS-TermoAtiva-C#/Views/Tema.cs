using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Classe estática responsável por centralizar o design system do totem TermoAtiva.
    /// Define paleta de cores de alto contraste solar, tipografia ampliada para toque e legibilidade sob luz solar,
    /// além de rotinas de estilização de controles Windows Forms.
    /// </summary>
    public static class Tema
    {
        // -------------------------------------------------------------
        // PALETA DE ALTO CONTRASTE SOLAR
        // -------------------------------------------------------------
        public static readonly Color FundoPrincipal = Color.FromArgb(248, 249, 250);   // Gelo/Claro anti-reflexo
        public static readonly Color FundoCard = Color.White;                          // Branco para cartões e modais
        public static readonly Color TextoPrincipal = Color.FromArgb(17, 24, 39);      // Grafite escuro profundo (alto contraste)
        public static readonly Color TextoSecundario = Color.FromArgb(75, 85, 99);     // Cinza escuro para detalhes e legendas
        public static readonly Color AzulPrimario = Color.FromArgb(15, 76, 129);       // Azul institucional clássico
        public static readonly Color VermelhoAlerta = Color.FromArgb(185, 28, 28);     // Vermelho vivo para risco extremo
        public static readonly Color LaranjaAviso = Color.FromArgb(217, 119, 6);       // Âmbar/Laranja para risco alto/aviso
        public static readonly Color VerdeSucesso = Color.FromArgb(22, 101, 52);       // Verde floresta para status seguro/disponível
        public static readonly Color CinzaBorda = Color.FromArgb(209, 213, 219);       // Borda suave para separadores
        public static readonly Color CinzaDesativado = Color.FromArgb(156, 163, 175);  // Cinza para itens em manutenção

        // -------------------------------------------------------------
        // TIPOGRAFIA AMPLIADA (Segoe UI) - MODO TOTEM / QUIOSQUE URBANO
        // -------------------------------------------------------------
        public static readonly Font FonteDestaqueTermico = new Font("Segoe UI", 42f, FontStyle.Bold);
        public static readonly Font FonteTitulo = new Font("Segoe UI", 26f, FontStyle.Bold);
        public static readonly Font FonteSubtitulo = new Font("Segoe UI", 20f, FontStyle.Bold);
        public static readonly Font FonteBotaoToque = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static readonly Font FonteGeralDestaque = new Font("Segoe UI", 16f, FontStyle.Bold);
        public static readonly Font FonteGeral = new Font("Segoe UI", 15f, FontStyle.Regular);
        public static readonly Font FontePequena = new Font("Segoe UI", 12f, FontStyle.Regular);
        public static readonly Font FontePequenaBold = new Font("Segoe UI", 12f, FontStyle.Bold);

        // -------------------------------------------------------------
        // MÉTODOS UTILITÁRIOS DE CONFIGURAÇÃO DE INTERFACE
        // -------------------------------------------------------------

        /// <summary>
        /// Configura a janela para o padrão de totem urbano (tela cheia sem bordas e centralizada).
        /// </summary>
        public static void ConfigurarJanelaTotem(Form form)
        {
            if (form == null) return;

            form.WindowState = FormWindowState.Maximized;
            form.FormBorderStyle = FormBorderStyle.None;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.BackColor = FundoPrincipal;
            form.ForeColor = TextoPrincipal;
            form.Font = FonteGeral;
            typeof(Form).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(form, true, null);
            form.KeyPreview = true;
        }

        /// <summary>
        /// Aplica estilização otimizada para botão de toque (touchscreen de grande dimensão e alto contraste).
        /// </summary>
        public static void EstilizarBotao(Button btn, Color corFundo, int altura = 85)
        {
            if (btn == null) return;

            btn.Height = altura;
            btn.BackColor = corFundo;
            btn.ForeColor = Color.White;
            btn.Font = FonteBotaoToque;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = AjustarBrilho(corFundo, 1.12f);
            btn.FlatAppearance.MouseDownBackColor = AjustarBrilho(corFundo, 0.88f);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        /// <summary>
        /// Estiliza botão secundário (contorno ou tom neutro).
        /// </summary>
        public static void EstilizarBotaoSecundario(Button btn, int altura = 60)
        {
            if (btn == null) return;

            btn.Height = altura;
            btn.BackColor = FundoCard;
            btn.ForeColor = TextoPrincipal;
            btn.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = CinzaBorda;
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 242, 246);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        /// <summary>
        /// Estiliza DataGridView para leitura acessível em quiosques de rua (linhas altas, fontes grandes e seleção clara).
        /// </summary>
        public static void EstilizarGrade(DataGridView grid)
        {
            if (grid == null) return;

            grid.BackgroundColor = FundoCard;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = CinzaBorda;
            grid.EnableHeadersVisualStyles = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Cabeçalho
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 55;
            grid.ColumnHeadersDefaultCellStyle.BackColor = AzulPrimario;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);

            // Linhas
            grid.RowTemplate.Height = 52;
            grid.DefaultCellStyle.BackColor = FundoCard;
            grid.DefaultCellStyle.ForeColor = TextoPrincipal;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 13f, FontStyle.Regular);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254); // Azul pastel de seleção
            grid.DefaultCellStyle.SelectionForeColor = TextoPrincipal;
            grid.DefaultCellStyle.Padding = new Padding(12, 4, 12, 4);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TextoPrincipal;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextoPrincipal;
        }

        /// <summary>
        /// Cria um painel estilo Card com fundo branco e cantos bem definidos.
        /// </summary>
        public static Panel CriarCard(int padding = 16)
        {
            var pnl = new Panel
            {
                BackColor = FundoCard,
                Padding = new Padding(padding)
            };
            return pnl;
        }

        /// <summary>
        /// Ajusta luminosidade de uma cor para efeitos de hover/clique.
        /// </summary>
        private static Color AjustarBrilho(Color cor, float fator)
        {
            int r = Math.Clamp((int)(cor.R * fator), 0, 255);
            int g = Math.Clamp((int)(cor.G * fator), 0, 255);
            int b = Math.Clamp((int)(cor.B * fator), 0, 255);
            return Color.FromArgb(cor.A, r, g, b);
        }
    }
}
