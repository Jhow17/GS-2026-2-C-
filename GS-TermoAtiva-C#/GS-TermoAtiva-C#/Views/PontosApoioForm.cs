using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela do Fluxo 2 (Munícipe) - Consulta pública SOMENTE LEITURA de recursos urbanos
    /// de alívio e refúgio térmico com diferenciação visual cromática por categoria.
    /// </summary>
    public class PontosApoioForm : Form
    {
        private readonly TotemRepository _repository;
        private DataGridView _grid = null!;
        private System.Windows.Forms.Button _btnFiltroTodos = null!;
        private System.Windows.Forms.Button _btnFiltroBebedouros = null!;
        private System.Windows.Forms.Button _btnFiltroSombra = null!;
        private System.Windows.Forms.Button _btnFiltroPostos = null!;
        private TipoPontoApoio? _filtroAtual = null;

        public PontosApoioForm(TotemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            Tema.ConfigurarJanelaTotem(this);
            this.Text = "TermoAtiva - Onde Beber Água e Descansar";

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Tema.FundoPrincipal,
                Padding = new Padding(28, 22, 28, 22)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 122f)); // Cabeçalho sem corte
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 74f));  // Barra de Filtros Rápidos
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Grade com cores por categoria
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 78f));  // Botão Voltar

            // 1. CABEÇALHO PADRONIZADO
            var header = Tema.CriarCabecalhoPadrao(
                "REDE PÚBLICA DE ACOLHIMENTO E HIDRATAÇÃO",
                "PONTOS DE ALÍVIO TÉRMICO E HIDRATAÇÃO PRÓXIMOS",
                "Consulte abaixo os pontos gratuitos para beber água potável, descansar à sombra ou se abrigar em ambiente climatizado.",
                Tema.AzulPrimario);
            pnlPrincipal.Controls.Add(header, 0, 0);

            // 2. BARRA DE FILTROS POR CATEGORIA (SEM EMOJIS)
            var pnlFiltros = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _btnFiltroTodos = CriarBotaoFiltro("TODOS OS RECURSOS", null);
            _btnFiltroBebedouros = CriarBotaoFiltro("BEBEDOUROS PÚBLICOS", TipoPontoApoio.BebedouroPublico);
            _btnFiltroSombra = CriarBotaoFiltro("SOMBRA E DESCANSO", TipoPontoApoio.AreaSombraDescanso);
            _btnFiltroPostos = CriarBotaoFiltro("POSTOS CLIMATIZADOS", TipoPontoApoio.PostoSaudeClimatizado);

            pnlFiltros.Controls.Add(_btnFiltroTodos, 0, 0);
            pnlFiltros.Controls.Add(_btnFiltroBebedouros, 1, 0);
            pnlFiltros.Controls.Add(_btnFiltroSombra, 2, 0);
            pnlFiltros.Controls.Add(_btnFiltroPostos, 3, 0);
            pnlPrincipal.Controls.Add(pnlFiltros, 0, 1);

            // 3. GRADE DE CONSULTA SOMENTE LEITURA COM CORES POR CATEGORIA
            var cardGrid = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(18),
                Margin = new Padding(0, 0, 0, 12)
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            Tema.EstilizarGrade(_grid);
            ConfigurarColunasGrade();

            cardGrid.Controls.Add(_grid);
            pnlPrincipal.Controls.Add(cardGrid, 0, 2);

            // 4. BOTÃO LARGO INFERIOR
            var btnVoltar = Tema.CriarBotaoReaLTaiizor("VOLTAR AO MENU PRINCIPAL", Tema.AzulPrimario, 66);
            btnVoltar.Dock = DockStyle.Fill;
            btnVoltar.Click += (s, e) => this.Close();
            pnlPrincipal.Controls.Add(btnVoltar, 0, 3);

            this.Controls.Add(pnlPrincipal);

            AtualizarDestaqueFiltros();
            CarregarDados();
        }

        private System.Windows.Forms.Button CriarBotaoFiltro(string texto, TipoPontoApoio? tipo)
        {
            var btn = new System.Windows.Forms.Button
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = Tema.FonteGeralDestaque,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 4, 0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Tema.CinzaBorda;
            btn.Click += (s, e) =>
            {
                _filtroAtual = tipo;
                AtualizarDestaqueFiltros();
                CarregarDados();
            };
            return btn;
        }

        private void AtualizarDestaqueFiltros()
        {
            AplicarEstiloFiltro(_btnFiltroTodos, _filtroAtual == null, Tema.FundoHeader, Color.White);
            AplicarEstiloFiltro(_btnFiltroBebedouros, _filtroAtual == TipoPontoApoio.BebedouroPublico, Tema.CorTextoBebedouro, Color.White);
            AplicarEstiloFiltro(_btnFiltroSombra, _filtroAtual == TipoPontoApoio.AreaSombraDescanso, Tema.CorTextoSombra, Color.White);
            AplicarEstiloFiltro(_btnFiltroPostos, _filtroAtual == TipoPontoApoio.PostoSaudeClimatizado, Tema.CorTextoPosto, Color.White);
        }

        private static void AplicarEstiloFiltro(System.Windows.Forms.Button btn, bool ativo, Color corAtivoFundo, Color corAtivoTexto)
        {
            if (ativo)
            {
                btn.BackColor = corAtivoFundo;
                btn.ForeColor = corAtivoTexto;
                btn.FlatAppearance.BorderColor = corAtivoFundo;
            }
            else
            {
                btn.BackColor = Tema.FundoCard;
                btn.ForeColor = Tema.TextoPrincipal;
                btn.FlatAppearance.BorderColor = Tema.CinzaBorda;
            }
        }

        private void ConfigurarColunasGrade()
        {
            _grid.Columns.Clear();

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Nome",
                HeaderText = "NOME DO LOCAL / RECURSO",
                FillWeight = 28
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Tipo",
                HeaderText = "CATEGORIA DO PONTO",
                FillWeight = 22
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Endereco",
                HeaderText = "ENDEREÇO COMPLETO",
                FillWeight = 32
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Distancia",
                HeaderText = "DISTÂNCIA ESTIMADA",
                FillWeight = 14
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "SITUAÇÃO OPERACIONAL",
                FillWeight = 16
            });

            _grid.CellFormatting += Grid_CellFormatting;
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = _grid.Rows[e.RowIndex];
            string categoria = row.Cells["Tipo"].Value?.ToString() ?? string.Empty;

            // Define cores sutis de fundo para a linha inteira e destaque forte para a célula de categoria (Item 3.3)
            Color corFundoLinha = Tema.FundoCard;
            Color corFundoBadge = Tema.FundoCard;
            Color corTextoBadge = Tema.TextoPrincipal;

            if (categoria.Contains("Bebedouro", StringComparison.OrdinalIgnoreCase))
            {
                corFundoLinha = Color.FromArgb(240, 249, 255); // Tom sutil de azul
                corFundoBadge = Tema.CorFundoBebedouro;
                corTextoBadge = Tema.CorTextoBebedouro;
            }
            else if (categoria.Contains("Sombra", StringComparison.OrdinalIgnoreCase))
            {
                corFundoLinha = Color.FromArgb(240, 253, 244); // Tom sutil de verde
                corFundoBadge = Tema.CorFundoSombra;
                corTextoBadge = Tema.CorTextoSombra;
            }
            else if (categoria.Contains("Climatizado", StringComparison.OrdinalIgnoreCase))
            {
                corFundoLinha = Color.FromArgb(255, 251, 235); // Tom sutil de laranja/âmbar
                corFundoBadge = Tema.CorFundoPosto;
                corTextoBadge = Tema.CorTextoPosto;
            }

            e.CellStyle!.BackColor = corFundoLinha;
            e.CellStyle.SelectionBackColor = Tema.AjustarBrilho(corFundoBadge, 0.94f);
            e.CellStyle.SelectionForeColor = Tema.TextoPrincipal;

            string colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName == "Tipo")
            {
                e.CellStyle.BackColor = corFundoBadge;
                e.CellStyle.ForeColor = corTextoBadge;
                e.CellStyle.Font = Tema.FonteGeralDestaque;
            }
            else if (colName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? string.Empty;
                if (status.Contains("Disponível", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Tema.VerdeSucesso;
                    e.CellStyle.Font = Tema.FonteGeralDestaque;
                }
                else
                {
                    e.CellStyle.ForeColor = Tema.VermelhoAlerta;
                    e.CellStyle.Font = Tema.FonteGeralDestaque;
                }
            }
            else if (colName == "Distancia")
            {
                e.CellStyle.Font = Tema.FonteGeralDestaque;
                e.CellStyle.ForeColor = Tema.AzulPrimario;
            }
        }

        private void CarregarDados()
        {
            var pontos = _repository.ObterPontosPorTipo(_filtroAtual);
            _grid.Rows.Clear();

            foreach (var p in pontos)
            {
                string status = p.Disponivel ? "DISPONÍVEL" : "EM MANUTENÇÃO";
                _grid.Rows.Add(
                    p.Nome,
                    p.ObterTipoFormatado(),
                    p.Endereco,
                    $"{p.DistanciaMetros} m",
                    status
                );
            }

            _grid.ClearSelection();
        }
    }
}
