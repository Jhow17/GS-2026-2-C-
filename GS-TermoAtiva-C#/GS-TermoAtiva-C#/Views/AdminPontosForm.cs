using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Painel de administração e zeladoria municipal do Totem TermoAtiva.
    /// Permite cadastrar novos pontos de refúgio, alternar disponibilidade em manutenções
    /// e auditar as ocorrências reportadas pelos munícipes durante as ondas de calor.
    /// </summary>
    public class AdminPontosForm : Form
    {
        private readonly TotemRepository _repository;
        private TextBox _txtNome = null!;
        private ComboBox _cboTipo = null!;
        private TextBox _txtEndereco = null!;
        private NumericUpDown _numDistancia = null!;
        private CheckBox _chkDisponivel = null!;
        private DataGridView _gridPontos = null!;
        private DataGridView _gridOcorrencias = null!;

        public AdminPontosForm(TotemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            Tema.ConfigurarJanelaTotem(this);
            this.Text = "TermoAtiva - Painel do Administrador e Gestão de Recursos";

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Tema.FundoPrincipal,
                Padding = new Padding(24)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));  // Cabeçalho Administrativo
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f)); // Seção 1: Cadastro de Novo Ponto
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Seção 2: Abas de Gestão e Auditoria

            // 1. CABEÇALHO DO ADMINISTRADOR
            var pnlHeader = CriarCabecalhoAdmin();
            pnlPrincipal.Controls.Add(pnlHeader, 0, 0);

            // 2. SEÇÃO 1: FORMULÁRIO DE CADASTRO
            var pnlCadastro = CriarSecaoCadastro();
            pnlPrincipal.Controls.Add(pnlCadastro, 0, 1);

            // 3. SEÇÃO 2: ABAS COM TABELAS DE PONTOS E OCORRÊNCIAS
            var pnlGestao = CriarSecaoGestaoEAuditoria();
            pnlPrincipal.Controls.Add(pnlGestao, 0, 2);

            this.Controls.Add(pnlPrincipal);

            CarregarTabelaPontos();
            CarregarTabelaOcorrencias();
        }

        private Panel CriarCabecalhoAdmin()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(20, 12, 20, 12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var btnVoltar = new Button
            {
                Text = "⬅  Sair do Modo Admin",
                Dock = DockStyle.Right,
                Width = 260
            };
            Tema.EstilizarBotaoSecundario(btnVoltar, 55);
            btnVoltar.Click += (s, e) => this.Close();

            var lblTitulo = new Label
            {
                Text = "🛡️ PAINEL ADMINISTRATIVO & ZELADORIA URBANA",
                Font = Tema.FonteTitulo,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSubtitulo = new Label
            {
                Text = "Gestão de recursos municipais de mitigação térmica e auditoria de chamados da população.",
                Font = Tema.FonteGeral,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(btnVoltar);
            pnl.Controls.Add(lblSubtitulo);
            pnl.Controls.Add(lblTitulo);

            return pnl;
        }

        private Panel CriarSecaoCadastro()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(20, 14, 20, 14),
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblSecao = new Label
            {
                Text = "➕ CADASTRAR NOVO PONTO DE RECURSO / REFÚGIO CLIMÁTICO",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 28
            };

            var tableCampos = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(0, 8, 0, 0)
            };
            tableCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            tableCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            tableCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            tableCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
            tableCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));

            tableCampos.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f)); // Rótulos
            tableCampos.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // Campos

            // Rótulos
            tableCampos.Controls.Add(new Label { Text = "Nome do Local / Ponto:", Font = Tema.FontePequenaBold, Dock = DockStyle.Fill }, 0, 0);
            tableCampos.Controls.Add(new Label { Text = "Categoria / Tipo:", Font = Tema.FontePequenaBold, Dock = DockStyle.Fill }, 1, 0);
            tableCampos.Controls.Add(new Label { Text = "Endereço Completo:", Font = Tema.FontePequenaBold, Dock = DockStyle.Fill }, 2, 0);
            tableCampos.Controls.Add(new Label { Text = "Distância (m):", Font = Tema.FontePequenaBold, Dock = DockStyle.Fill }, 3, 0);
            tableCampos.Controls.Add(new Label { Text = "Disponível?", Font = Tema.FontePequenaBold, Dock = DockStyle.Fill }, 4, 0);

            // Inputs
            _txtNome = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f) };
            _cboTipo = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboTipo.Items.Add("Bebedouro Público");
            _cboTipo.Items.Add("Área de Sombra / Descanso");
            _cboTipo.Items.Add("Posto Climatizado");
            _cboTipo.SelectedIndex = 0;

            _txtEndereco = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f) };

            _numDistancia = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                Minimum = 5,
                Maximum = 15000,
                Value = 150,
                Increment = 10
            };

            _chkDisponivel = new CheckBox
            {
                Text = "Ativo",
                Checked = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold)
            };

            tableCampos.Controls.Add(_txtNome, 0, 1);
            tableCampos.Controls.Add(_cboTipo, 1, 1);
            tableCampos.Controls.Add(_txtEndereco, 2, 1);
            tableCampos.Controls.Add(_numDistancia, 3, 1);
            tableCampos.Controls.Add(_chkDisponivel, 4, 1);

            var pnlBotoesCadastro = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45
            };

            var btnCadastrar = new Button
            {
                Text = "➕  Cadastrar Recurso no Totem",
                Dock = DockStyle.Right,
                Width = 280
            };
            Tema.EstilizarBotao(btnCadastrar, Tema.VerdeSucesso, 45);
            btnCadastrar.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnCadastrar.Click += (s, e) => ExecutarCadastroPonto();

            pnlBotoesCadastro.Controls.Add(btnCadastrar);

            card.Controls.Add(tableCampos);
            card.Controls.Add(pnlBotoesCadastro);
            card.Controls.Add(lblSecao);

            return card;
        }

        private Panel CriarSecaoGestaoEAuditoria()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(16)
            };

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Padding = new Point(16, 8)
            };

            // ABA 1: PONTOS DE RECURSO
            var tabPontos = new TabPage("📍 Gerenciar Pontos de Apoio");
            tabPontos.BackColor = Tema.FundoCard;
            tabPontos.Padding = new Padding(10);

            _gridPontos = new DataGridView { Dock = DockStyle.Fill };
            Tema.EstilizarGrade(_gridPontos);
            ConfigurarColunasGradePontos();

            var pnlAcoesPontos = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                Padding = new Padding(0, 10, 0, 0)
            };

            var btnAlternarStatus = new Button
            {
                Text = "🔄  Alternar Disponibilidade (Ativar / Interditar)",
                Dock = DockStyle.Left,
                Width = 420
            };
            Tema.EstilizarBotao(btnAlternarStatus, Tema.AzulPrimario, 45);
            btnAlternarStatus.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnAlternarStatus.Click += (s, e) => AlternarStatusPontoSelecionado();

            var btnAtualizarPontos = new Button
            {
                Text = "Atualizar Tabela",
                Dock = DockStyle.Right,
                Width = 180
            };
            Tema.EstilizarBotaoSecundario(btnAtualizarPontos, 45);
            btnAtualizarPontos.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnAtualizarPontos.Click += (s, e) => CarregarTabelaPontos();

            pnlAcoesPontos.Controls.Add(btnAlternarStatus);
            pnlAcoesPontos.Controls.Add(btnAtualizarPontos);

            tabPontos.Controls.Add(_gridPontos);
            tabPontos.Controls.Add(pnlAcoesPontos);

            // ABA 2: OCORRÊNCIAS REPORTADAS POR MUNÍCIPES
            var tabOcorrencias = new TabPage("⚠️ Auditoria de Ocorrências e Zeladoria");
            tabOcorrencias.BackColor = Tema.FundoCard;
            tabOcorrencias.Padding = new Padding(10);

            _gridOcorrencias = new DataGridView { Dock = DockStyle.Fill };
            Tema.EstilizarGrade(_gridOcorrencias);
            ConfigurarColunasGradeOcorrencias();

            var pnlAcoesOcorrencias = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                Padding = new Padding(0, 10, 0, 0)
            };

            var btnAtualizarOcorrencias = new Button
            {
                Text = "🔄  Atualizar Lista de Ocorrências",
                Dock = DockStyle.Left,
                Width = 320
            };
            Tema.EstilizarBotao(btnAtualizarOcorrencias, Tema.LaranjaAviso, 45);
            btnAtualizarOcorrencias.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnAtualizarOcorrencias.Click += (s, e) => CarregarTabelaOcorrencias();

            pnlAcoesOcorrencias.Controls.Add(btnAtualizarOcorrencias);

            tabOcorrencias.Controls.Add(_gridOcorrencias);
            tabOcorrencias.Controls.Add(pnlAcoesOcorrencias);

            tabControl.TabPages.Add(tabPontos);
            tabControl.TabPages.Add(tabOcorrencias);

            pnl.Controls.Add(tabControl);
            return pnl;
        }

        private void ConfigurarColunasGradePontos()
        {
            _gridPontos.Columns.Clear();

            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 8 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "NOME DO PONTO", FillWeight = 28 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "CATEGORIA", FillWeight = 22 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Endereco", HeaderText = "ENDEREÇO", FillWeight = 26 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Distancia", HeaderText = "DISTÂNCIA", FillWeight = 12 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS OPERACIONAL", FillWeight = 16 });

            _gridPontos.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (_gridPontos.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
                {
                    string status = e.Value.ToString() ?? "";
                    if (status.Contains("Disponível"))
                    {
                        e.CellStyle!.ForeColor = Tema.VerdeSucesso;
                        e.CellStyle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                    }
                    else
                    {
                        e.CellStyle!.ForeColor = Tema.VermelhoAlerta;
                        e.CellStyle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                    }
                }
            };
        }

        private void ConfigurarColunasGradeOcorrencias()
        {
            _gridOcorrencias.Columns.Clear();

            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "DataHora", HeaderText = "DATA / HORA", FillWeight = 20 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "PontoApoio", HeaderText = "LOCAL AFETADO", FillWeight = 35 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "TipoProblema", HeaderText = "PROBLEMA REPORTADO", FillWeight = 28 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "SITUAÇÃO", FillWeight = 17 });

            _gridOcorrencias.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (_gridOcorrencias.Columns[e.ColumnIndex].Name == "TipoProblema" && e.Value != null)
                {
                    e.CellStyle!.ForeColor = Tema.VermelhoAlerta;
                    e.CellStyle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
                }
            };
        }

        private void CarregarTabelaPontos()
        {
            var pontos = _repository.ObterTodosPontos();
            _gridPontos.Rows.Clear();

            foreach (var p in pontos)
            {
                string status = p.Disponivel ? "✓ Disponível" : "⚠️ Interditado";
                _gridPontos.Rows.Add(
                    p.Id,
                    p.Nome,
                    p.ObterTipoFormatado(),
                    p.Endereco,
                    $"{p.DistanciaMetros} m",
                    status
                );
            }
        }

        private void CarregarTabelaOcorrencias()
        {
            var ocorrencias = _repository.ObterTodasOcorrencias();
            _gridOcorrencias.Rows.Clear();

            foreach (var o in ocorrencias)
            {
                string status = o.Resolvido ? "✓ Atendido" : "⏳ Pendente";
                _gridOcorrencias.Rows.Add(
                    o.DataHora.ToString("dd/MM/yyyy HH:mm:ss"),
                    o.NomePontoApoio,
                    o.TipoProblema,
                    status
                );
            }
        }

        private void ExecutarCadastroPonto()
        {
            try
            {
                string nome = _txtNome.Text.Trim();
                string endereco = _txtEndereco.Text.Trim();

                if (string.IsNullOrWhiteSpace(nome))
                {
                    MessageBox.Show("Por favor, informe o nome do ponto de apoio.", "Campo Obrigatório", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtNome.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(endereco))
                {
                    MessageBox.Show("Por favor, informe o endereço completo do local.", "Campo Obrigatório", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtEndereco.Focus();
                    return;
                }

                TipoPontoApoio tipo = _cboTipo.SelectedIndex switch
                {
                    0 => TipoPontoApoio.BebedouroPublico,
                    1 => TipoPontoApoio.AreaSombraDescanso,
                    2 => TipoPontoApoio.PostoSaudeClimatizado,
                    _ => TipoPontoApoio.BebedouroPublico
                };

                int distancia = (int)_numDistancia.Value;
                bool disponivel = _chkDisponivel.Checked;

                var novoPonto = new PontoApoio(0, nome, tipo, endereco, distancia, disponivel);
                _repository.CadastrarPontoApoio(novoPonto);

                MessageBox.Show(
                    $"✓ Ponto de apoio '{nome}' cadastrado com sucesso e persistido no totem!",
                    "Cadastro Concluído",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Limpa campos e recarrega
                _txtNome.Clear();
                _txtEndereco.Clear();
                _numDistancia.Value = 150;
                _chkDisponivel.Checked = true;
                _cboTipo.SelectedIndex = 0;

                CarregarTabelaPontos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar ponto de apoio: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AlternarStatusPontoSelecionado()
        {
            if (_gridPontos.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um ponto na tabela acima para alterar a disponibilidade.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var linha = _gridPontos.SelectedRows[0];
                int id = Convert.ToInt32(linha.Cells["Id"].Value);
                string statusAtual = linha.Cells["Status"].Value?.ToString() ?? "";
                bool novoStatus = !statusAtual.Contains("Disponível");

                _repository.AlternarDisponibilidadePonto(id, novoStatus);

                string msg = novoStatus ? "habilitado como DISPONÍVEL" : "marcado como INTERDITADO / EM MANUTENÇÃO";
                MessageBox.Show($"Ponto ID #{id} foi {msg} no sistema.", "Status Alterado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                CarregarTabelaPontos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alternar status do ponto: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
