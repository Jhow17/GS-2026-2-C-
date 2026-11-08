using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using ReaLTaiizor.Controls;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Design System central do terminal urbano TermoAtiva (Smart City Kiosk OS).
    /// Integra componentes ReaLTaiizor e renderização GDI+ de alta definição,
    /// tipografia calibrada sem truncamento e paleta de alto contraste solar sem emojis.
    /// </summary>
    public static class Tema
    {
        // -------------------------------------------------------------
        // PALETA DE CORES INSTITUCIONAL & ALTO CONTRASTE SOLAR
        // -------------------------------------------------------------
        public static readonly Color FundoPrincipal = Color.FromArgb(241, 245, 249);    // Slate 100 anti-reflexo
        public static readonly Color FundoSecundario = Color.FromArgb(226, 232, 240);   // Slate 200 para containers
        public static readonly Color FundoCard = Color.White;                           // Superfície de cartões
        public static readonly Color FundoHeader = Color.FromArgb(15, 23, 42);          // Slate 900 para barra superior de OS

        public static readonly Color TextoPrincipal = Color.FromArgb(15, 23, 42);       // Slate 900 (contraste máximo)
        public static readonly Color TextoSecundario = Color.FromArgb(71, 85, 105);     // Slate 600 para subtítulos
        public static readonly Color TextoSuave = Color.FromArgb(100, 116, 139);        // Slate 500 para metadados

        public static readonly Color AzulPrimario = Color.FromArgb(14, 92, 168);        // Azul institucional governamental
        public static readonly Color AzulEscuro = Color.FromArgb(10, 62, 118);          // Azul profundo
        public static readonly Color CianoMapa = Color.FromArgb(8, 145, 178);           // Ciano escuro para módulo geográfico
        public static readonly Color VermelhoAlerta = Color.FromArgb(185, 28, 28);      // Vermelho alerta climático
        public static readonly Color LaranjaAviso = Color.FromArgb(194, 90, 12);        // Âmbar/Laranja de atenção
        public static readonly Color VerdeSucesso = Color.FromArgb(21, 115, 71);        // Verde operacional

        public static readonly Color CinzaBorda = Color.FromArgb(203, 213, 225);        // Borda sutil Slate 300
        public static readonly Color CinzaDesativado = Color.FromArgb(148, 163, 184);

        // Cores sutis por Categoria de Ponto de Apoio (Item 3.3)
        public static readonly Color CorFundoBebedouro = Color.FromArgb(224, 242, 254); // Azul claro sutil
        public static readonly Color CorTextoBebedouro = Color.FromArgb(7, 89, 133);    // Azul escuro legível

        public static readonly Color CorFundoSombra = Color.FromArgb(220, 252, 231);    // Verde claro sutil
        public static readonly Color CorTextoSombra = Color.FromArgb(22, 101, 52);      // Verde escuro legível

        public static readonly Color CorFundoPosto = Color.FromArgb(254, 243, 199);     // Laranja/Âmbar claro sutil
        public static readonly Color CorTextoPosto = Color.FromArgb(146, 64, 14);       // Âmbar escuro legível

        // -------------------------------------------------------------
        // TIPOGRAFIA CALIBRADA (SEGOE UI) - SEM TRUNCAMENTO EM ALTA DPI
        // -------------------------------------------------------------
        public static readonly Font FonteDestaqueTermico = new Font("Segoe UI", 32f, FontStyle.Bold);
        public static readonly Font FonteTitulo = new Font("Segoe UI", 20f, FontStyle.Bold);
        public static readonly Font FonteSubtitulo = new Font("Segoe UI", 15f, FontStyle.Bold);
        public static readonly Font FonteBotaoToque = new Font("Segoe UI", 15f, FontStyle.Bold);
        public static readonly Font FonteGeralDestaque = new Font("Segoe UI", 12.5f, FontStyle.Bold);
        public static readonly Font FonteGeral = new Font("Segoe UI", 12f, FontStyle.Regular);
        public static readonly Font FontePequena = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        public static readonly Font FontePequenaBold = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        public static readonly Font FonteTag = new Font("Segoe UI", 9.5f, FontStyle.Bold);

        // -------------------------------------------------------------
        // CONFIGURAÇÃO DE JANELA E COMPONENTES REALTAIIZOR / KIOSK
        // -------------------------------------------------------------

        /// <summary>
        /// Configura o formulário para operar como terminal de autoatendimento (tela cheia, sem bordas).
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
            form.AutoScaleMode = AutoScaleMode.Dpi;
            typeof(Form).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(form, true, null);
            form.KeyPreview = true;
        }

        /// <summary>
        /// Cria um botão arredondado moderno da biblioteca ReaLTaiizor (ParrotButton) otimizado para toque.
        /// </summary>
        public static ParrotButton CriarBotaoReaLTaiizor(string texto, Color corFundo, int altura = 64, int raioBorda = 10)
        {
            var btn = new ParrotButton
            {
                ButtonText = texto,
                Text = texto,
                Height = altura,
                BackgroundColor = corFundo,
                HoverBackgroundColor = AjustarBrilho(corFundo, 1.12f),
                ClickBackColor = AjustarBrilho(corFundo, 0.88f),
                TextColor = Color.White,
                HoverTextColor = Color.White,
                ClickTextColor = Color.White,
                CornerRadius = raioBorda,
                Font = FonteBotaoToque,
                Cursor = Cursors.Hand,
                ButtonStyle = ParrotButton.Style.MaterialRounded,
                SmoothingType = SmoothingMode.AntiAlias,
                TextRenderingType = TextRenderingHint.ClearTypeGridFit
            };
            return btn;
        }

        /// <summary>
        /// Estiliza um Button padrão do Windows Forms com cantos arredondados GDI+, barra de destaque e alto contraste.
        /// </summary>
        public static void EstilizarBotao(System.Windows.Forms.Button btn, Color corFundo, int altura = 68)
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
            btn.Padding = new Padding(16, 6, 16, 6);

            AplicarRegiaoArredondada(btn, 10);
        }

        /// <summary>
        /// Estiliza botão secundário (contorno corporativo de alta definição).
        /// </summary>
        public static void EstilizarBotaoSecundario(System.Windows.Forms.Button btn, int altura = 52)
        {
            if (btn == null) return;

            btn.Height = altura;
            btn.BackColor = FundoCard;
            btn.ForeColor = TextoPrincipal;
            btn.Font = FonteGeralDestaque;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = CinzaBorda;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(203, 213, 225);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.Padding = new Padding(14, 4, 14, 4);
        }

        /// <summary>
        /// Cria um cartão visual moderno baseado em ReaLTaiizor.Controls.Panel com borda suave e faixa lateral opcional.
        /// </summary>
        public static System.Windows.Forms.Panel CriarCardModerno(Padding? padding = null, Color? corFaixaLateral = null)
        {
            var card = new KioskCardPanel(corFaixaLateral)
            {
                BackColor = FundoCard,
                Padding = padding ?? new Padding(22, 18, 22, 18)
            };
            return card;
        }

        /// <summary>
        /// Cria um cabeçalho padronizado de tela de quiosque que garante que nenhum título ou subtítulo seja cortado.
        /// </summary>
        public static System.Windows.Forms.Panel CriarCabecalhoPadrao(
            string categoriaModulo,
            string tituloPrincipal,
            string subtitulo,
            Color corDestaque,
            Control? controleDireita = null)
        {
            var cardHeader = new KioskCardPanel(corDestaque)
            {
                Dock = DockStyle.Fill,
                BackColor = FundoCard,
                Padding = new Padding(26, 14, 24, 14),
                Margin = new Padding(0, 0, 0, 14)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = controleDireita != null ? 2 : 1,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            if (controleDireita != null)
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }

            var pnlTextos = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            pnlTextos.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            pnlTextos.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            pnlTextos.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblTag = new Label
            {
                Text = categoriaModulo.ToUpperInvariant(),
                Font = FonteTag,
                ForeColor = corDestaque,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = false
            };

            var lblTitulo = new Label
            {
                Text = tituloPrincipal,
                Font = FonteTitulo,
                ForeColor = TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = false
            };

            var lblSubtitulo = new Label
            {
                Text = subtitulo,
                Font = FonteGeral,
                ForeColor = TextoSecundario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = false
            };

            pnlTextos.Controls.Add(lblTag, 0, 0);
            pnlTextos.Controls.Add(lblTitulo, 0, 1);
            pnlTextos.Controls.Add(lblSubtitulo, 0, 2);

            layout.Controls.Add(pnlTextos, 0, 0);

            if (controleDireita != null)
            {
                var pnlRight = new System.Windows.Forms.Panel
                {
                    Dock = DockStyle.Fill,
                    Width = controleDireita.Width + 16,
                    Padding = new Padding(8, 12, 0, 12)
                };
                controleDireita.Dock = DockStyle.Fill;
                pnlRight.Controls.Add(controleDireita);
                layout.Controls.Add(pnlRight, 1, 0);
            }

            cardHeader.Controls.Add(layout);
            return cardHeader;
        }

        /// <summary>
        /// Estiliza DataGridView para leitura acessível em quiosques de rua (linhas altas, fontes claras e cabeçalho sólido).
        /// </summary>
        public static void EstilizarGrade(DataGridView grid)
        {
            if (grid == null) return;

            grid.BackgroundColor = FundoCard;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(226, 232, 240);
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
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 52;
            grid.ColumnHeadersDefaultCellStyle.BackColor = FundoHeader;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = FundoHeader;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(14, 0, 14, 0);

            // Linhas
            grid.RowTemplate.Height = 54;
            grid.DefaultCellStyle.BackColor = FundoCard;
            grid.DefaultCellStyle.ForeColor = TextoPrincipal;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 12f, FontStyle.Regular);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = TextoPrincipal;
            grid.DefaultCellStyle.Padding = new Padding(14, 4, 14, 4);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TextoPrincipal;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextoPrincipal;
        }

        public static void AplicarRegiaoArredondada(Control control, int radius)
        {
            control.Resize += (s, e) =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using var path = CriarCaminhoArredondado(new Rectangle(0, 0, control.Width, control.Height), radius);
                control.Region = new Region(path);
            };
        }

        public static GraphicsPath CriarCaminhoArredondado(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0 || rect.Width < d || rect.Height < d)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color AjustarBrilho(Color cor, float fator)
        {
            int r = Math.Clamp((int)(cor.R * fator), 0, 255);
            int g = Math.Clamp((int)(cor.G * fator), 0, 255);
            int b = Math.Clamp((int)(cor.B * fator), 0, 255);
            return Color.FromArgb(cor.A, r, g, b);
        }
    }

    /// <summary>
    /// Painel de cartão moderno com bordas sutis, anti-aliasing e barra de destaque lateral opcional.
    /// </summary>
    public class KioskCardPanel : System.Windows.Forms.Panel
    {
        public Color? CorDestaqueLateral { get; set; }
        public Color CorBorda { get; set; } = Tema.CinzaBorda;
        public int EspessuraDestaque { get; set; } = 6;

        public KioskCardPanel(Color? corDestaqueLateral = null)
        {
            CorDestaqueLateral = corDestaqueLateral;
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.BackColor = Tema.FundoCard;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (var pen = new Pen(CorBorda, 1f))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }

            if (CorDestaqueLateral.HasValue)
            {
                using var brush = new SolidBrush(CorDestaqueLateral.Value);
                e.Graphics.FillRectangle(brush, 0, 0, EspessuraDestaque, this.Height);
            }
        }
    }

    /// <summary>
    /// Tile interativo de alta definição para o Menu Principal do Totem.
    /// Renderiza indicador numérico/código vetorial, título amplo, subtítulo explicativo e seta de ação.
    /// </summary>
    public class KioskActionTile : Control
    {
        public string CodigoModulo { get; set; }
        public string Titulo { get; set; }
        public string Subtitulo { get; set; }
        public Color CorDestaque { get; set; }
        private bool _hover;
        private bool _pressed;

        public KioskActionTile(string codigoModulo, string titulo, string subtitulo, Color corDestaque)
        {
            CodigoModulo = codigoModulo;
            Titulo = titulo;
            Subtitulo = subtitulo;
            CorDestaque = corDestaque;

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            this.Cursor = Cursors.Hand;
            this.BackColor = Tema.FundoCard;
            this.Margin = new Padding(8);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _pressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            Color bg = _pressed
                ? Color.FromArgb(226, 232, 240)
                : (_hover ? Color.FromArgb(248, 250, 252) : Color.White);

            using (var path = Tema.CriarCaminhoArredondado(rect, 12))
            using (var brushBg = new SolidBrush(bg))
            using (var penBorder = new Pen(_hover ? CorDestaque : Tema.CinzaBorda, _hover ? 2f : 1.2f))
            {
                g.FillPath(brushBg, path);
                g.DrawPath(penBorder, path);
            }

            // Barra de acento esquerda
            using (var brushAccent = new SolidBrush(CorDestaque))
            {
                g.FillRectangle(brushAccent, 2, 14, 7, Height - 28);
            }

            // Badge de módulo vetorial à esquerda
            int badgeSize = 58;
            int badgeX = 26;
            int badgeY = (Height - badgeSize) / 2;
            var badgeRect = new Rectangle(badgeX, badgeY, badgeSize, badgeSize);
            using (var pathBadge = Tema.CriarCaminhoArredondado(badgeRect, 12))
            using (var brushBadge = new SolidBrush(CorDestaque))
            {
                g.FillPath(brushBadge, pathBadge);
            }

            using (var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var fontBadge = new Font("Segoe UI", 13f, FontStyle.Bold))
            using (var brushWhite = new SolidBrush(Color.White))
            {
                g.DrawString(CodigoModulo, fontBadge, brushWhite, badgeRect, sfCenter);
            }

            // Textos principais
            int textLeft = badgeX + badgeSize + 22;
            int textWidth = Math.Max(100, Width - textLeft - 70);

            var rectTitulo = new Rectangle(textLeft, badgeY - 2, textWidth, 34);
            var rectSubtitulo = new Rectangle(textLeft, badgeY + 32, textWidth, 44);

            TextRenderer.DrawText(
                g,
                Titulo,
                Tema.FonteSubtitulo,
                rectTitulo,
                Tema.TextoPrincipal,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(
                g,
                Subtitulo,
                Tema.FonteGeral,
                rectSubtitulo,
                Tema.TextoSecundario,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak);

            // Indicador de navegação à direita (seta vetorial)
            int arrowCenterX = Width - 36;
            int arrowCenterY = Height / 2;
            using (var penArrow = new Pen(_hover ? CorDestaque : Tema.TextoSuave, 2.5f))
            {
                penArrow.StartCap = LineCap.Round;
                penArrow.EndCap = LineCap.Round;
                g.DrawLine(penArrow, arrowCenterX - 6, arrowCenterY - 8, arrowCenterX + 4, arrowCenterY);
                g.DrawLine(penArrow, arrowCenterX + 4, arrowCenterY, arrowCenterX - 6, arrowCenterY + 8);
            }
        }
    }
}
