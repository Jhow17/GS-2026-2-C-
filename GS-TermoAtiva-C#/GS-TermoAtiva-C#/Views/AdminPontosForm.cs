using System;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Painel de administração e zeladoria municipal do Totem TermoAtiva.
    /// Integra consulta assíncrona ao webservice ViaCEP para autopreenchimento de endereço,
    /// cálculo automático de distância ao totem e auditoria completa de ocorrências.
    /// </summary>
    public class AdminPontosForm : Form
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        private readonly TotemRepository _repository;

        // Campos de Cadastro de Recurso (Endereço Decomposto + ViaCEP)
        private TextBox _txtNome = null!;
        private ComboBox _cboTipo = null!;
        private TextBox _txtCep = null!;
        private System.Windows.Forms.Button _btnBuscarCep = null!;
        private TextBox _txtLogradouro = null!;
        private TextBox _txtNumero = null!;
        private TextBox _txtBairro = null!;
        private TextBox _txtCidade = null!;
        private TextBox _txtUf = null!;
        private CheckBox _chkDisponivel = null!;
        private Label _lblStatusCep = null!;

        private DataGridView _gridPontos = null!;
        private DataGridView _gridOcorrencias = null!;
        private string _ultimoCepConsultado = string.Empty;

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
                Padding = new Padding(28, 20, 28, 20)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 118f)); // Cabeçalho Padronizado Sem Truncamento
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 235f)); // Seção 1: Cadastro com Integração ViaCEP
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Seção 2: Abas de Gestão e Auditoria

            // 1. CABEÇALHO DO ADMINISTRADOR COM BOTÃO DE SAÍDA À DIREITA
            var btnVoltar = new System.Windows.Forms.Button
            {
                Text = "Encerrar Sessão Admin",
                Width = 220,
                Height = 48
            };
            Tema.EstilizarBotaoSecundario(btnVoltar, 48);
            btnVoltar.Click += (s, e) => this.Close();

            var pnlHeader = Tema.CriarCabecalhoPadrao(
                "GESTÃO OPERACIONAL E ZELADORIA MUNICIPAL",
                "PAINEL ADMINISTRATIVO DE RECURSOS CLIMÁTICOS",
                "Cadastre novos pontos de apoio via CEP, gerencie interdições técnicas e audite os relatos dos cidadãos.",
                Tema.AzulPrimario,
                btnVoltar);
            pnlPrincipal.Controls.Add(pnlHeader, 0, 0);

            // 2. SEÇÃO 1: FORMULÁRIO DE CADASTRO COM VIACEP (SEM DISTÂNCIA MANUAL)
            var pnlCadastro = CriarSecaoCadastroViaCep();
            pnlPrincipal.Controls.Add(pnlCadastro, 0, 1);

            // 3. SEÇÃO 2: ABAS COM TABELAS DE PONTOS E OCORRÊNCIAS
            var pnlGestao = CriarSecaoGestaoEAuditoria();
            pnlPrincipal.Controls.Add(pnlGestao, 0, 2);

            this.Controls.Add(pnlPrincipal);

            CarregarTabelaPontos();
            CarregarTabelaOcorrencias();
        }

        private System.Windows.Forms.Panel CriarSecaoCadastroViaCep()
        {
            var card = new KioskCardPanel(Tema.VerdeSucesso)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(22, 14, 22, 14),
                Margin = new Padding(0, 0, 0, 12)
            };

            var layoutCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layoutCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f)); // Título da seção + feedback ViaCEP
            layoutCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f)); // Linha 1: Nome, Categoria, CEP, Botão Buscar CEP
            layoutCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f)); // Linha 2: Logradouro, Número, Bairro, Cidade, UF
            layoutCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Linha 3: Disponibilidade + Botão Cadastrar

            // Barra de título da seção
            var pnlTopoSecao = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            pnlTopoSecao.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            pnlTopoSecao.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            var lblSecao = new Label
            {
                Text = "CADASTRAR NOVO PONTO DE APOIO (INTEGRAÇÃO AUTOMÁTICA VIACEP)",
                Font = Tema.FonteTag,
                ForeColor = Tema.VerdeSucesso,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblStatusCep = new Label
            {
                Text = "Informe o CEP (8 dígitos) para preenchimento automático do logradouro e bairro.",
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlTopoSecao.Controls.Add(lblSecao, 0, 0);
            pnlTopoSecao.Controls.Add(_lblStatusCep, 1, 0);

            // LINHA 1 DE CAMPOS: Nome do Local, Categoria, CEP e Botão Buscar CEP
            var linha1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Margin = new Padding(0, 2, 0, 2)
            };
            linha1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f)); // Nome
            linha1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f)); // Categoria
            linha1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f)); // CEP
            linha1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f)); // Botão Buscar CEP
            linha1.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            linha1.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));

            linha1.Controls.Add(CriarLabelCampo("Nome do Ponto / Equipamento Urbano:"), 0, 0);
            linha1.Controls.Add(CriarLabelCampo("Categoria do Recurso:"), 1, 0);
            linha1.Controls.Add(CriarLabelCampo("CEP (Somente números ou 00000-000):"), 2, 0);
            linha1.Controls.Add(CriarLabelCampo("Consulta Webservice:"), 3, 0);

            _txtNome = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 10, 2)
            };

            _cboTipo = new ComboBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 2, 10, 2)
            };
            _cboTipo.Items.Add("Bebedouro Público");
            _cboTipo.Items.Add("Área de Sombra / Descanso");
            _cboTipo.Items.Add("Posto Climatizado");
            _cboTipo.SelectedIndex = 0;

            _txtCep = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 9,
                PlaceholderText = "01001-000",
                Margin = new Padding(0, 2, 10, 2)
            };
            _txtCep.Leave += async (s, e) =>
            {
                string cepLimpo = ExtrairDigitosCep(_txtCep.Text);
                if (cepLimpo.Length == 8 && cepLimpo != _ultimoCepConsultado)
                {
                    await ConsultarViaCepAsync(exibirAlertaSeVazio: false);
                }
            };

            _btnBuscarCep = new System.Windows.Forms.Button
            {
                Text = "Buscar CEP",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 1, 0, 3)
            };
            Tema.EstilizarBotaoSecundario(_btnBuscarCep, 34);
            _btnBuscarCep.Font = Tema.FontePequenaBold;
            _btnBuscarCep.BackColor = Tema.CorFundoBebedouro;
            _btnBuscarCep.ForeColor = Tema.CorTextoBebedouro;
            _btnBuscarCep.Click += async (s, e) => await ConsultarViaCepAsync(exibirAlertaSeVazio: true);

            linha1.Controls.Add(_txtNome, 0, 1);
            linha1.Controls.Add(_cboTipo, 1, 1);
            linha1.Controls.Add(_txtCep, 2, 1);
            linha1.Controls.Add(_btnBuscarCep, 3, 1);

            // LINHA 2 DE CAMPOS: Logradouro/Rua, Número, Bairro, Cidade, UF
            var linha2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                Margin = new Padding(0, 2, 0, 2)
            };
            linha2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f)); // Logradouro
            linha2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f)); // Número
            linha2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f)); // Bairro
            linha2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f)); // Cidade
            linha2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f)); // UF
            linha2.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            linha2.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));

            linha2.Controls.Add(CriarLabelCampo("Logradouro / Rua:"), 0, 0);
            linha2.Controls.Add(CriarLabelCampo("Número:"), 1, 0);
            linha2.Controls.Add(CriarLabelCampo("Bairro:"), 2, 0);
            linha2.Controls.Add(CriarLabelCampo("Cidade:"), 3, 0);
            linha2.Controls.Add(CriarLabelCampo("UF:"), 4, 0);

            _txtLogradouro = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 10, 2)
            };

            _txtNumero = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Ex: 150 ou s/n",
                Margin = new Padding(0, 2, 10, 2)
            };

            _txtBairro = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 10, 2)
            };

            _txtCidade = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 2, 10, 2)
            };

            _txtUf = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 2,
                CharacterCasing = CharacterCasing.Upper,
                Margin = new Padding(0, 2, 0, 2)
            };

            linha2.Controls.Add(_txtLogradouro, 0, 1);
            linha2.Controls.Add(_txtNumero, 1, 1);
            linha2.Controls.Add(_txtBairro, 2, 1);
            linha2.Controls.Add(_txtCidade, 3, 1);
            linha2.Controls.Add(_txtUf, 4, 1);

            // LINHA 3: Status Inicial + Nota de Distância Automática + Botão Cadastrar
            var linha3 = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 4, 0, 0)
            };

            _chkDisponivel = new CheckBox
            {
                Text = "Recurso Ativo e Disponível Imediatamente",
                Checked = true,
                Dock = DockStyle.Left,
                Width = 340,
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal
            };

            var lblNotaDistancia = new Label
            {
                Text = "Nota: A distância até o totem é calculada automaticamente pelo sistema com base no CEP/endereço.",
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoSuave,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnCadastrar = Tema.CriarBotaoReaLTaiizor("Cadastrar Recurso no Totem", Tema.VerdeSucesso, 40, 8);
            btnCadastrar.Dock = DockStyle.Right;
            btnCadastrar.Width = 280;
            btnCadastrar.Font = Tema.FonteGeralDestaque;
            btnCadastrar.Click += (s, e) => ExecutarCadastroPonto();

            linha3.Controls.Add(lblNotaDistancia);
            linha3.Controls.Add(_chkDisponivel);
            linha3.Controls.Add(btnCadastrar);

            layoutCard.Controls.Add(pnlTopoSecao, 0, 0);
            layoutCard.Controls.Add(linha1, 0, 1);
            layoutCard.Controls.Add(linha2, 0, 2);
            layoutCard.Controls.Add(linha3, 0, 3);

            card.Controls.Add(layoutCard);
            return card;
        }

        private static Label CriarLabelCampo(string texto)
        {
            return new Label
            {
                Text = texto,
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
        }

        private static string ExtrairDigitosCep(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            return new string(texto.Where(char.IsDigit).ToArray());
        }

        private async Task ConsultarViaCepAsync(bool exibirAlertaSeVazio)
        {
            string cepDigitos = ExtrairDigitosCep(_txtCep.Text);

            if (cepDigitos.Length != 8)
            {
                if (exibirAlertaSeVazio)
                {
                    MessageBox.Show(
                        "Informe um CEP válido contendo exatamente 8 dígitos numéricos (ex: 01001-000).",
                        "Formato de CEP Inválido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    _txtCep.Focus();
                }
                return;
            }

            try
            {
                _btnBuscarCep.Enabled = false;
                _btnBuscarCep.Text = "Consultando...";
                _lblStatusCep.Text = $"Consultando CEP {cepDigitos} no webservice ViaCEP...";
                _lblStatusCep.ForeColor = Tema.AzulPrimario;

                string url = $"https://viacep.com.br/ws/{cepDigitos}/json/";
                string json = await _httpClient.GetStringAsync(url);

                var resposta = JsonSerializer.Deserialize<ViaCepResponse>(json);
                if (resposta == null || resposta.Erro == true)
                {
                    _lblStatusCep.Text = "CEP não encontrado na base nacional dos Correios.";
                    _lblStatusCep.ForeColor = Tema.VermelhoAlerta;
                    MessageBox.Show(
                        $"O CEP '{cepDigitos}' não foi localizado na base do ViaCEP.\nVerifique os números ou preencha o endereço manualmente.",
                        "CEP Não Localizado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _ultimoCepConsultado = cepDigitos;
                _txtCep.Text = $"{cepDigitos[..5]}-{cepDigitos[5..]}";
                _txtLogradouro.Text = resposta.Logradouro ?? string.Empty;
                _txtBairro.Text = resposta.Bairro ?? string.Empty;
                _txtCidade.Text = resposta.Localidade ?? string.Empty;
                _txtUf.Text = resposta.Uf ?? string.Empty;

                _lblStatusCep.Text = $"Endereço preenchido via ViaCEP ({_txtCidade.Text}/{_txtUf.Text}). Informe o Número.";
                _lblStatusCep.ForeColor = Tema.VerdeSucesso;

                _txtNumero.Focus();
            }
            catch (Exception ex)
            {
                _lblStatusCep.Text = "Serviço ViaCEP indisponível no momento (modo offline permitido).";
                _lblStatusCep.ForeColor = Tema.LaranjaAviso;
                MessageBox.Show(
                    $"Não foi possível consultar o webservice ViaCEP no momento ({ex.Message}).\nVocê pode preencher os campos de endereço manualmente.",
                    "Aviso de Conectividade ViaCEP",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            finally
            {
                _btnBuscarCep.Enabled = true;
                _btnBuscarCep.Text = "Buscar CEP";
            }
        }

        private System.Windows.Forms.Panel CriarSecaoGestaoEAuditoria()
        {
            var pnl = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(18)
            };

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Tema.FonteGeralDestaque,
                Padding = new Point(18, 8)
            };

            // ABA 1: PONTOS DE RECURSO
            var tabPontos = new TabPage("Gerenciar Pontos de Apoio Cadastrados")
            {
                BackColor = Tema.FundoCard,
                Padding = new Padding(10)
            };

            _gridPontos = new DataGridView { Dock = DockStyle.Fill };
            Tema.EstilizarGrade(_gridPontos);
            ConfigurarColunasGradePontos();

            var pnlAcoesPontos = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                Padding = new Padding(0, 8, 0, 0)
            };

            var btnAlternarStatus = Tema.CriarBotaoReaLTaiizor(
                "Alternar Disponibilidade (Ativar / Interditar Selecionado)",
                Tema.AzulPrimario,
                44,
                8);
            btnAlternarStatus.Dock = DockStyle.Left;
            btnAlternarStatus.Width = 460;
            btnAlternarStatus.Font = Tema.FonteGeralDestaque;
            btnAlternarStatus.Click += (s, e) => AlternarStatusPontoSelecionado();

            var btnAtualizarPontos = new System.Windows.Forms.Button
            {
                Text = "Recarregar Tabela",
                Dock = DockStyle.Right,
                Width = 180
            };
            Tema.EstilizarBotaoSecundario(btnAtualizarPontos, 44);
            btnAtualizarPontos.Font = Tema.FontePequenaBold;
            btnAtualizarPontos.Click += (s, e) => CarregarTabelaPontos();

            pnlAcoesPontos.Controls.Add(btnAlternarStatus);
            pnlAcoesPontos.Controls.Add(btnAtualizarPontos);

            tabPontos.Controls.Add(_gridPontos);
            tabPontos.Controls.Add(pnlAcoesPontos);

            // ABA 2: OCORRÊNCIAS REPORTADAS POR MUNÍCIPES
            var tabOcorrencias = new TabPage("Auditoria de Ocorrências e Relatos dos Cidadãos")
            {
                BackColor = Tema.FundoCard,
                Padding = new Padding(10)
            };

            _gridOcorrencias = new DataGridView { Dock = DockStyle.Fill };
            Tema.EstilizarGrade(_gridOcorrencias);
            ConfigurarColunasGradeOcorrencias();

            var pnlAcoesOcorrencias = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                Padding = new Padding(0, 8, 0, 0)
            };

            var btnAtualizarOcorrencias = Tema.CriarBotaoReaLTaiizor(
                "Atualizar Lista de Ocorrências",
                Tema.LaranjaAviso,
                44,
                8);
            btnAtualizarOcorrencias.Dock = DockStyle.Left;
            btnAtualizarOcorrencias.Width = 320;
            btnAtualizarOcorrencias.Font = Tema.FonteGeralDestaque;
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

            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 6 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "NOME DO PONTO", FillWeight = 24 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "CATEGORIA", FillWeight = 18 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cep", HeaderText = "CEP", FillWeight = 11 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Endereco", HeaderText = "ENDEREÇO COMPLETO", FillWeight = 28 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Distancia", HeaderText = "DISTÂNCIA", FillWeight = 10 });
            _gridPontos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", FillWeight = 13 });

            _gridPontos.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                string colName = _gridPontos.Columns[e.ColumnIndex].Name;
                if (colName == "Status" && e.Value != null)
                {
                    string status = e.Value.ToString() ?? string.Empty;
                    if (status.Contains("DISPONÍVEL", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle!.ForeColor = Tema.VerdeSucesso;
                        e.CellStyle.Font = Tema.FontePequenaBold;
                    }
                    else
                    {
                        e.CellStyle!.ForeColor = Tema.VermelhoAlerta;
                        e.CellStyle.Font = Tema.FontePequenaBold;
                    }
                }
                else if (colName == "Tipo" && e.Value != null)
                {
                    string tipo = e.Value.ToString() ?? string.Empty;
                    if (tipo.Contains("Bebedouro", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle!.BackColor = Tema.CorFundoBebedouro;
                        e.CellStyle.ForeColor = Tema.CorTextoBebedouro;
                        e.CellStyle.Font = Tema.FontePequenaBold;
                    }
                    else if (tipo.Contains("Sombra", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle!.BackColor = Tema.CorFundoSombra;
                        e.CellStyle.ForeColor = Tema.CorTextoSombra;
                        e.CellStyle.Font = Tema.FontePequenaBold;
                    }
                    else if (tipo.Contains("Climatizado", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle!.BackColor = Tema.CorFundoPosto;
                        e.CellStyle.ForeColor = Tema.CorTextoPosto;
                        e.CellStyle.Font = Tema.FontePequenaBold;
                    }
                }
            };
        }

        private void ConfigurarColunasGradeOcorrencias()
        {
            _gridOcorrencias.Columns.Clear();

            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "DataHora", HeaderText = "DATA / HORA", FillWeight = 15 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "PontoApoio", HeaderText = "LOCAL AFETADO", FillWeight = 24 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "TipoProblema", HeaderText = "PROBLEMA", FillWeight = 16 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cidadao", HeaderText = "CIDADÃO", FillWeight = 15 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descricao", HeaderText = "DESCRIÇÃO DETALHADA", FillWeight = 20 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "SITUAÇÃO", FillWeight = 10 });

            _gridOcorrencias.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (_gridOcorrencias.Columns[e.ColumnIndex].Name == "TipoProblema" && e.Value != null)
                {
                    e.CellStyle!.ForeColor = Tema.VermelhoAlerta;
                    e.CellStyle.Font = Tema.FontePequenaBold;
                }
            };
        }

        private void CarregarTabelaPontos()
        {
            var pontos = _repository.ObterTodosPontos();
            _gridPontos.Rows.Clear();

            foreach (var p in pontos)
            {
                string status = p.Disponivel ? "DISPONÍVEL" : "INTERDITADO";
                string cepExibicao = string.IsNullOrWhiteSpace(p.CEP) ? "-" : p.CEP;
                _gridPontos.Rows.Add(
                    p.Id,
                    p.Nome,
                    p.ObterTipoFormatado(),
                    cepExibicao,
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
                string status = o.Resolvido ? "ATENDIDO" : "PENDENTE";
                _gridOcorrencias.Rows.Add(
                    o.DataHora.ToString("dd/MM/yyyy HH:mm"),
                    o.NomePontoApoio,
                    o.TipoProblema,
                    o.ObterCidadaoFormatado(),
                    o.ObterDescricaoFormatada(),
                    status
                );
            }
        }

        private void ExecutarCadastroPonto()
        {
            try
            {
                string nome = _txtNome.Text.Trim();
                string cep = _txtCep.Text.Trim();
                string logradouro = _txtLogradouro.Text.Trim();
                string numero = string.IsNullOrWhiteSpace(_txtNumero.Text) ? "s/n" : _txtNumero.Text.Trim();
                string bairro = _txtBairro.Text.Trim();
                string cidade = _txtCidade.Text.Trim();
                string uf = _txtUf.Text.Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(nome))
                {
                    MessageBox.Show("Por favor, informe o Nome do Ponto de Apoio.", "Campo Obrigatório", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtNome.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cep) || ExtrairDigitosCep(cep).Length != 8)
                {
                    MessageBox.Show("Por favor, informe um CEP válido de 8 dígitos.", "CEP Obrigatório", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtCep.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(logradouro) || string.IsNullOrWhiteSpace(bairro) || string.IsNullOrWhiteSpace(cidade) || string.IsNullOrWhiteSpace(uf))
                {
                    MessageBox.Show(
                        "Certifique-se de que Logradouro, Bairro, Cidade e UF estejam preenchidos (utilize o botão 'Buscar CEP' ou preencha os campos).",
                        "Endereço Incompleto",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    _txtLogradouro.Focus();
                    return;
                }

                TipoPontoApoio tipo = _cboTipo.SelectedIndex switch
                {
                    0 => TipoPontoApoio.BebedouroPublico,
                    1 => TipoPontoApoio.AreaSombraDescanso,
                    2 => TipoPontoApoio.PostoSaudeClimatizado,
                    _ => TipoPontoApoio.BebedouroPublico
                };

                string cepFormatado = $"{ExtrairDigitosCep(cep)[..5]}-{ExtrairDigitosCep(cep)[5..]}";

                var novoPonto = new PontoApoio(
                    0,
                    nome,
                    tipo,
                    cepFormatado,
                    logradouro,
                    numero,
                    bairro,
                    cidade,
                    uf,
                    _chkDisponivel.Checked);

                var pontoSalvo = _repository.CadastrarPontoApoio(novoPonto);

                MessageBox.Show(
                    $"Ponto de apoio cadastrado com sucesso!\n\n" +
                    $"Local: {pontoSalvo.Nome}\n" +
                    $"Endereço: {pontoSalvo.Endereco}\n" +
                    $"Distância Calculada até o Totem: {pontoSalvo.DistanciaMetros} metros",
                    "Cadastro Concluído",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Limpa os campos após o cadastro
                _txtNome.Clear();
                _txtCep.Clear();
                _txtLogradouro.Clear();
                _txtNumero.Clear();
                _txtBairro.Clear();
                _txtCidade.Clear();
                _txtUf.Clear();
                _ultimoCepConsultado = string.Empty;
                _chkDisponivel.Checked = true;
                _cboTipo.SelectedIndex = 0;
                _lblStatusCep.Text = "Informe o CEP (8 dígitos) para preenchimento automático do logradouro e bairro.";
                _lblStatusCep.ForeColor = Tema.TextoSecundario;

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
                MessageBox.Show("Selecione um ponto na tabela para alternar sua disponibilidade.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var linha = _gridPontos.SelectedRows[0];
                int id = Convert.ToInt32(linha.Cells["Id"].Value);
                string statusAtual = linha.Cells["Status"].Value?.ToString() ?? string.Empty;
                bool novoStatus = !statusAtual.Contains("DISPONÍVEL", StringComparison.OrdinalIgnoreCase);

                _repository.AlternarDisponibilidadePonto(id, novoStatus);

                string msg = novoStatus ? "habilitado como DISPONÍVEL" : "marcado como INTERDITADO";
                MessageBox.Show($"O Ponto ID #{id} foi {msg} no sistema.", "Status Atualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                CarregarTabelaPontos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alternar status do ponto: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class ViaCepResponse
        {
            [JsonPropertyName("cep")]
            public string? Cep { get; set; }

            [JsonPropertyName("logradouro")]
            public string? Logradouro { get; set; }

            [JsonPropertyName("bairro")]
            public string? Bairro { get; set; }

            [JsonPropertyName("localidade")]
            public string? Localidade { get; set; }

            [JsonPropertyName("uf")]
            public string? Uf { get; set; }

            [JsonPropertyName("erro")]
            public bool? Erro { get; set; }
        }
    }
}
