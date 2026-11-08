using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela do Fluxo 2 (Munícipe) - Consulta pública SOMENTE LEITURA de recursos urbanos
    /// de alívio e refúgio térmico (bebedouros públicos, parques sombreados e postos climatizados).
    /// </summary>
    public class PontosApoioForm : Form
    {
        private readonly TotemRepository _repository;
        private DataGridView _grid = null!;
        private Button _btnFiltroTodos = null!;
        private Button _btnFiltroBebedouros = null!;
        private Button _btnFiltroSombra = null!;
        private Button _btnFiltroPostos = null!;
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
                Padding = new Padding(28)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f)); // Cabeçalho e Título
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 85f));  // Filtros Rápidos por Toque
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Grade de Pontos (DataGridView)
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 95f));  // Botão Voltar ao Menu

            // 1. CABEÇALHO
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(20, 15, 20, 15),
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblTitulo = new Label
            {
                Text = "💧 PONTOS DE ALÍVIO TÉRMICO E HIDRATAÇÃO PRÓXIMOS",
                Font = Tema.FonteTitulo,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblInstrucao = new Label
            {
                Text = "Consulte abaixo os pontos públicos gratuitos para beber água, descansar à sombra ou se refrescar em ar-condicionado.",
                Font = Tema.FonteGeral,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(lblInstrucao);
            pnlHeader.Controls.Add(lblTitulo);
            pnlPrincipal.Controls.Add(pnlHeader, 0, 0);

            // 2. BOTÕES DE FILTRO RÁPIDO POR TOQUE
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

            _btnFiltroTodos = CriarBotaoFiltro("📋 Todos", null);
            _btnFiltroBebedouros = CriarBotaoFiltro("💧 Bebedouros", TipoPontoApoio.BebedouroPublico);
            _btnFiltroSombra = CriarBotaoFiltro("🌳 Sombra / Descanso", TipoPontoApoio.AreaSombraDescanso);
            _btnFiltroPostos = CriarBotaoFiltro("🏥 Postos Climatizados", TipoPontoApoio.PostoSaudeClimatizado);

            pnlFiltros.Controls.Add(_btnFiltroTodos, 0, 0);
            pnlFiltros.Controls.Add(_btnFiltroBebedouros, 1, 0);
            pnlFiltros.Controls.Add(_btnFiltroSombra, 2, 0);
            pnlFiltros.Controls.Add(_btnFiltroPostos, 3, 0);
            pnlPrincipal.Controls.Add(pnlFiltros, 0, 1);

            // 3. GRADE DE CONSULTA SOMENTE LEITURA
            var cardGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(16),
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

            // 4. BOTÃO VOLTAR
            var btnVoltar = new Button
            {
                Text = "⬅  VOLTAR AO MENU PRINCIPAL",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 0)
            };
            Tema.EstilizarBotao(btnVoltar, Tema.AzulPrimario, 85);
            btnVoltar.Click += (s, e) => this.Close();
            pnlPrincipal.Controls.Add(btnVoltar, 0, 3);

            this.Controls.Add(pnlPrincipal);

            AtualizarDestaqueFiltros();
            CarregarDados();
        }

        private Button CriarBotaoFiltro(string texto, TipoPontoApoio? tipo)
        {
            var btn = new Button
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4)
            };
            btn.FlatAppearance.BorderSize = 0;
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
            AplicarEstiloFiltro(_btnFiltroTodos, _filtroAtual == null);
            AplicarEstiloFiltro(_btnFiltroBebedouros, _filtroAtual == TipoPontoApoio.BebedouroPublico);
            AplicarEstiloFiltro(_btnFiltroSombra, _filtroAtual == TipoPontoApoio.AreaSombraDescanso);
            AplicarEstiloFiltro(_btnFiltroPostos, _filtroAtual == TipoPontoApoio.PostoSaudeClimatizado);
        }

        private static void AplicarEstiloFiltro(Button btn, bool ativo)
        {
            if (ativo)
            {
                btn.BackColor = Tema.AzulPrimario;
                btn.ForeColor = Color.White;
            }
            else
            {
                btn.BackColor = Color.FromArgb(238, 242, 246);
                btn.ForeColor = Tema.TextoPrincipal;
            }
        }

        private void ConfigurarColunasGrade()
        {
            _grid.Columns.Clear();

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Nome",
                HeaderText = "LOCAL / PONTO DE RECURSO",
                FillWeight = 32
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Tipo",
                HeaderText = "CATEGORIA",
                FillWeight = 22
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Endereco",
                HeaderText = "ENDEREÇO COMPLETO",
                FillWeight = 30
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Distancia",
                HeaderText = "DISTÂNCIA",
                FillWeight = 16
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "SITUAÇÃO ATUAL",
                FillWeight = 18
            });

            _grid.CellFormatting += Grid_CellFormatting;
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (_grid.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                if (status.Contains("Disponível"))
                {
                    e.CellStyle!.ForeColor = Tema.VerdeSucesso;
                    e.CellStyle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle!.ForeColor = Tema.VermelhoAlerta;
                    e.CellStyle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
                }
            }
            else if (_grid.Columns[e.ColumnIndex].Name == "Distancia" && e.Value != null)
            {
                e.CellStyle!.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
                e.CellStyle.ForeColor = Tema.AzulPrimario;
            }
        }

        private void CarregarDados()
        {
            var pontos = _repository.ObterPontosPorTipo(_filtroAtual);
            _grid.Rows.Clear();

            foreach (var p in pontos)
            {
                string status = p.Disponivel ? "✓ Disponível" : "⚠️ Em Manutenção";
                _grid.Rows.Add(
                    p.Nome,
                    p.ObterTipoFormatado(),
                    p.Endereco,
                    $"{p.DistanciaMetros} m",
                    status
                );
            }
        }
    }
}
