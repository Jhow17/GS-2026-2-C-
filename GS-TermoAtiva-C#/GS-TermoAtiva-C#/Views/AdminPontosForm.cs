using System;
using System.Drawing;
using System.Globalization;
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
    /// Integra consulta assíncrona ao webservice ViaCEP, CRUD completo de pontos de apoio,
    /// auditoria/resolução de ocorrências, gestão de logins de operadores e telemetria climática em JSON.
    /// </summary>
    public class AdminPontosForm : Form
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        private readonly TotemRepository _repository;
        private readonly OperadorMunicipal? _operadorAutenticado;
        private readonly OpenWeatherMapService _openWeatherService = new();

        // Campos de Cadastro / Edição de Recurso (Endereço Decomposto + ViaCEP)
        private int? _pontoEmEdicaoId = null;
        private Label _lblSecaoCadastro = null!;
        private ReaLTaiizor.Controls.ParrotButton _btnCadastrarPonto = null!;
        private System.Windows.Forms.Button _btnCancelarEdicao = null!;
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

        // Tabelas de Gestão (Abas 1, 2 e 3)
        private DataGridView _gridPontos = null!;
        private DataGridView _gridOcorrencias = null!;
        private DataGridView _gridOperadores = null!;
        private string _ultimoCepConsultado = string.Empty;

        // Campos da Aba 3: Telemetria Climática & Operadores
        private TextBox _txtTempClima = null!;
        private TextBox _txtUmidadeClima = null!;
        private TextBox _txtBairroClima = null!;
        private ComboBox _cboCamadaPadrao = null!;
        private Label _lblResumoClimaSalvo = null!;

        private TextBox _txtNovoOpUsuario = null!;
        private TextBox _txtNovoOpSenha = null!;
        private TextBox _txtNovoOpNome = null!;
        private TextBox _txtNovoOpCargo = null!;

        public AdminPontosForm(TotemRepository repository, OperadorMunicipal? operadorAutenticado = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _operadorAutenticado = operadorAutenticado;
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
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 235f)); // Seção 1: Cadastro/Edição com Integração ViaCEP
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Seção 2: Abas de Gestão, Ocorrências, Clima e Logins

            // 1. CABEÇALHO DO ADMINISTRADOR COM BOTÃO DE SAÍDA À DIREITA
            var btnVoltar = new System.Windows.Forms.Button
            {
                Text = "Encerrar Sessão Admin",
                Width = 220,
                Height = 48
            };
            Tema.EstilizarBotaoSecundario(btnVoltar, 48);
            btnVoltar.Click += (s, e) => this.Close();

            string operadorTag = _operadorAutenticado != null
                ? $"OPERADOR AUTENTICADO: {_operadorAutenticado.NomeCompleto.ToUpperInvariant()} ({_operadorAutenticado.Usuario})"
                : "GESTÃO OPERACIONAL E ZELADORIA MUNICIPAL";

            var pnlHeader = Tema.CriarCabecalhoPadrao(
                operadorTag,
                "PAINEL ADMINISTRATIVO DE RECURSOS CLIMÁTICOS",
                "Cadastre ou edite pontos via CEP, audite relatos de cidadãos e gerencie a telemetria e logins em JSON.",
                Tema.AzulPrimario,
                btnVoltar);
            pnlPrincipal.Controls.Add(pnlHeader, 0, 0);

            // 2. SEÇÃO 1: FORMULÁRIO DE CADASTRO / EDIÇÃO COM VIACEP
            var pnlCadastro = CriarSecaoCadastroViaCep();
            pnlPrincipal.Controls.Add(pnlCadastro, 0, 1);

            // 3. SEÇÃO 2: ABAS COM TABELAS DE PONTOS, OCORRÊNCIAS E TELEMETRIA/OPERADORES
            var pnlGestao = CriarSecaoGestaoEAuditoria();
            pnlPrincipal.Controls.Add(pnlGestao, 0, 2);

            this.Controls.Add(pnlPrincipal);

            CarregarTabelaPontos();
            CarregarTabelaOcorrencias();
            CarregarTabelaOperadores();
            CarregarCamposClimaAtual();
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
            layoutCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Linha 3: Disponibilidade + Botões Cadastrar/Salvar

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

            _lblSecaoCadastro = new Label
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

            pnlTopoSecao.Controls.Add(_lblSecaoCadastro, 0, 0);
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

            // LINHA 3: Status Inicial + Nota de Distância Automática + Botões Cadastrar / Cancelar Edição
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
                Text = "Nota: Distância e coordenadas WGS-84 são calculadas automaticamente pelo CEP/endereço.",
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoSuave,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnCancelarEdicao = new System.Windows.Forms.Button
            {
                Text = "Cancelar Edição",
                Dock = DockStyle.Right,
                Width = 160,
                Visible = false
            };
            Tema.EstilizarBotaoSecundario(_btnCancelarEdicao, 40);
            _btnCancelarEdicao.Font = Tema.FontePequenaBold;
            _btnCancelarEdicao.Click += (s, e) => LimparFormularioPonto();

            _btnCadastrarPonto = Tema.CriarBotaoReaLTaiizor("Cadastrar Recurso no Totem", Tema.VerdeSucesso, 40, 8);
            _btnCadastrarPonto.Dock = DockStyle.Right;
            _btnCadastrarPonto.Width = 280;
            _btnCadastrarPonto.Font = Tema.FonteGeralDestaque;
            _btnCadastrarPonto.Click += (s, e) => ExecutarCadastroOuEdicaoPonto();

            linha3.Controls.Add(lblNotaDistancia);
            linha3.Controls.Add(_chkDisponivel);
            linha3.Controls.Add(_btnCancelarEdicao);
            linha3.Controls.Add(_btnCadastrarPonto);

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

            // ABA 1: PONTOS DE RECURSO (pontos_apoio.json)
            var tabPontos = new TabPage("1. Pontos de Apoio (pontos_apoio.json)")
            {
                BackColor = Tema.FundoCard,
                Padding = new Padding(10)
            };

            _gridPontos = new DataGridView { Dock = DockStyle.Fill };
            Tema.EstilizarGrade(_gridPontos);
            ConfigurarColunasGradePontos();

            var pnlAcoesPontos = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0)
            };

            var btnAlternarStatus = Tema.CriarBotaoReaLTaiizor(
                "Alternar Status (Ativar / Interditar)",
                Tema.AzulPrimario,
                44,
                8);
            btnAlternarStatus.Width = 340;
            btnAlternarStatus.Font = Tema.FonteGeralDestaque;
            btnAlternarStatus.Margin = new Padding(0, 0, 10, 0);
            btnAlternarStatus.Click += (s, e) => AlternarStatusPontoSelecionado();

            var btnEditarPonto = new System.Windows.Forms.Button
            {
                Text = "Carregar Selecionado p/ Edição",
                Width = 260,
                Margin = new Padding(0, 0, 10, 0)
            };
            Tema.EstilizarBotaoSecundario(btnEditarPonto, 44);
            btnEditarPonto.Font = Tema.FontePequenaBold;
            btnEditarPonto.Click += (s, e) => CarregarPontoSelecionadoParaEdicao();

            var btnExcluirPonto = new System.Windows.Forms.Button
            {
                Text = "Excluir Ponto",
                Width = 150,
                Margin = new Padding(0, 0, 10, 0)
            };
            Tema.EstilizarBotaoSecundario(btnExcluirPonto, 44);
            btnExcluirPonto.Font = Tema.FontePequenaBold;
            btnExcluirPonto.ForeColor = Tema.VermelhoAlerta;
            btnExcluirPonto.Click += (s, e) => ExcluirPontoSelecionado();

            var btnAtualizarPontos = new System.Windows.Forms.Button
            {
                Text = "Recarregar Tabela",
                Width = 170
            };
            Tema.EstilizarBotaoSecundario(btnAtualizarPontos, 44);
            btnAtualizarPontos.Font = Tema.FontePequenaBold;
            btnAtualizarPontos.Click += (s, e) => CarregarTabelaPontos();

            pnlAcoesPontos.Controls.Add(btnAlternarStatus);
            pnlAcoesPontos.Controls.Add(btnEditarPonto);
            pnlAcoesPontos.Controls.Add(btnExcluirPonto);
            pnlAcoesPontos.Controls.Add(btnAtualizarPontos);

            tabPontos.Controls.Add(_gridPontos);
            tabPontos.Controls.Add(pnlAcoesPontos);

            // ABA 2: OCORRÊNCIAS REPORTADAS POR MUNÍCIPES (ocorrencias.json)
            var tabOcorrencias = new TabPage("2. Ocorrências Cidadãs (ocorrencias.json)")
            {
                BackColor = Tema.FundoCard,
                Padding = new Padding(10)
            };

            _gridOcorrencias = new DataGridView { Dock = DockStyle.Fill };
            Tema.EstilizarGrade(_gridOcorrencias);
            ConfigurarColunasGradeOcorrencias();

            var pnlAcoesOcorrencias = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0)
            };

            var btnAlternarOcorrencia = Tema.CriarBotaoReaLTaiizor(
                "Alternar Situação (Atendido / Pendente)",
                Tema.VerdeSucesso,
                44,
                8);
            btnAlternarOcorrencia.Width = 360;
            btnAlternarOcorrencia.Font = Tema.FonteGeralDestaque;
            btnAlternarOcorrencia.Margin = new Padding(0, 0, 10, 0);
            btnAlternarOcorrencia.Click += (s, e) => AlternarSituacaoOcorrenciaSelecionada();

            var btnExcluirOcorrencia = new System.Windows.Forms.Button
            {
                Text = "Excluir Ocorrência",
                Width = 180,
                Margin = new Padding(0, 0, 10, 0)
            };
            Tema.EstilizarBotaoSecundario(btnExcluirOcorrencia, 44);
            btnExcluirOcorrencia.Font = Tema.FontePequenaBold;
            btnExcluirOcorrencia.ForeColor = Tema.VermelhoAlerta;
            btnExcluirOcorrencia.Click += (s, e) => ExcluirOcorrenciaSelecionada();

            var btnAtualizarOcorrencias = new System.Windows.Forms.Button
            {
                Text = "Recarregar Lista",
                Width = 170
            };
            Tema.EstilizarBotaoSecundario(btnAtualizarOcorrencias, 44);
            btnAtualizarOcorrencias.Font = Tema.FontePequenaBold;
            btnAtualizarOcorrencias.Click += (s, e) => CarregarTabelaOcorrencias();

            pnlAcoesOcorrencias.Controls.Add(btnAlternarOcorrencia);
            pnlAcoesOcorrencias.Controls.Add(btnExcluirOcorrencia);
            pnlAcoesOcorrencias.Controls.Add(btnAtualizarOcorrencias);

            tabOcorrencias.Controls.Add(_gridOcorrencias);
            tabOcorrencias.Controls.Add(pnlAcoesOcorrencias);

            // ABA 3: TELEMETRIA CLIMÁTICA & OPERADORES (alerta_climatico.json + operadores.json)
            var tabSistema = CriarAbaTelemetriaEOperadores();

            tabControl.TabPages.Add(tabPontos);
            tabControl.TabPages.Add(tabOcorrencias);
            tabControl.TabPages.Add(tabSistema);

            pnl.Controls.Add(tabControl);
            return pnl;
        }

        private TabPage CriarAbaTelemetriaEOperadores()
        {
            var tab = new TabPage("3. Clima & Operadores (alerta_climatico.json / operadores.json)")
            {
                BackColor = Tema.FundoCard,
                Padding = new Padding(10)
            };

            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f)); // Telemetria Climática
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f)); // Operadores / Logins

            // PAINEL ESQUERDO: Telemetria Climática Persistida (Data/alerta_climatico.json)
            var cardClima = new KioskCardPanel(Tema.LaranjaAviso)
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(18, 14, 18, 14),
                Margin = new Padding(0, 0, 10, 0)
            };

            var layoutClima = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6
            };
            layoutClima.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layoutClima.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layoutClima.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f)); // Título
            layoutClima.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f)); // Temp + Umidade
            layoutClima.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f)); // Bairro + Camada Padrão
            layoutClima.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f)); // Resumo índice calculado
            layoutClima.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f)); // Botões Salvar / Sincronizar OpenWeather
            layoutClima.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblTituloClima = new Label
            {
                Text = "TELEMETRIA CLIMÁTICA (Data/alerta_climatico.json)",
                Font = Tema.FonteTag,
                ForeColor = Tema.LaranjaAviso,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layoutClima.Controls.Add(lblTituloClima, 0, 0);
            layoutClima.SetColumnSpan(lblTituloClima, 2);

            _txtTempClima = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle };
            _txtUmidadeClima = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle };
            _txtBairroClima = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle };

            _cboCamadaPadrao = new ComboBox
            {
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI", 11f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var c in OpenWeatherMapService.CamadasDisponiveis)
            {
                _cboCamadaPadrao.Items.Add(c.Codigo);
            }
            _cboCamadaPadrao.SelectedIndex = 0;

            layoutClima.Controls.Add(EmpacotarCampo("Temperatura Atual (°C):", _txtTempClima), 0, 1);
            layoutClima.Controls.Add(EmpacotarCampo("Umidade Relativa (%):", _txtUmidadeClima), 1, 1);
            layoutClima.Controls.Add(EmpacotarCampo("Bairro de Referência:", _txtBairroClima), 0, 2);
            layoutClima.Controls.Add(EmpacotarCampo("Camada Padrão Weather Maps:", _cboCamadaPadrao), 1, 2);

            _lblResumoClimaSalvo = new Label
            {
                Text = "Sensação Térmica (Rothfusz/NOAA): --",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layoutClima.Controls.Add(_lblResumoClimaSalvo, 0, 3);
            layoutClima.SetColumnSpan(_lblResumoClimaSalvo, 2);

            var btnSalvarClima = Tema.CriarBotaoReaLTaiizor("Salvar Telemetria no JSON", Tema.LaranjaAviso, 40, 8);
            btnSalvarClima.Dock = DockStyle.Fill;
            btnSalvarClima.Font = Tema.FontePequenaBold;
            btnSalvarClima.Margin = new Padding(0, 2, 6, 2);
            btnSalvarClima.Click += (s, e) => SalvarTelemetriaClimaticaManual();

            var btnSyncOpenWeather = new System.Windows.Forms.Button
            {
                Text = "Sincronizar OpenWeather",
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 2, 0, 2)
            };
            Tema.EstilizarBotaoSecundario(btnSyncOpenWeather, 40);
            btnSyncOpenWeather.Font = Tema.FontePequenaBold;
            btnSyncOpenWeather.Click += async (s, e) => await SincronizarClimaOpenWeatherAsync(btnSyncOpenWeather);

            layoutClima.Controls.Add(btnSalvarClima, 0, 4);
            layoutClima.Controls.Add(btnSyncOpenWeather, 1, 4);

            cardClima.Controls.Add(layoutClima);

            // PAINEL DIREITO: Operadores Municipais (Data/operadores.json)
            var cardOperadores = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(18, 14, 18, 14),
                Margin = new Padding(0)
            };

            var layoutOp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layoutOp.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f)); // Título
            layoutOp.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f)); // Linha cadastro rápido de operador
            layoutOp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Grade de operadores
            layoutOp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f)); // Ações de operadores

            var lblTituloOp = new Label
            {
                Text = "CREDENCIAIS DE OPERADORES MUNICIPAIS (Data/operadores.json)",
                Font = Tema.FonteTag,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var linhaNovoOp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Margin = new Padding(0, 2, 0, 4)
            };
            linhaNovoOp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            linhaNovoOp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            linhaNovoOp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            linhaNovoOp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            linhaNovoOp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));

            _txtNovoOpUsuario = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 11f), PlaceholderText = "usuário" };
            _txtNovoOpSenha = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 11f), PlaceholderText = "senha" };
            _txtNovoOpNome = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 11f), PlaceholderText = "Nome Completo" };
            _txtNovoOpCargo = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 11f), PlaceholderText = "Cargo/Setor" };

            var btnAddOp = new System.Windows.Forms.Button
            {
                Text = "Adicionar",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            Tema.EstilizarBotaoSecundario(btnAddOp, 30);
            btnAddOp.Font = Tema.FontePequenaBold;
            btnAddOp.BackColor = Tema.CorFundoBebedouro;
            btnAddOp.ForeColor = Tema.CorTextoBebedouro;
            btnAddOp.Click += (s, e) => ExecutarCadastroOperador();

            linhaNovoOp.Controls.Add(EmpacotarCampo("Usuário:", _txtNovoOpUsuario), 0, 0);
            linhaNovoOp.Controls.Add(EmpacotarCampo("Senha:", _txtNovoOpSenha), 1, 0);
            linhaNovoOp.Controls.Add(EmpacotarCampo("Nome:", _txtNovoOpNome), 2, 0);
            linhaNovoOp.Controls.Add(EmpacotarCampo("Cargo:", _txtNovoOpCargo), 3, 0);
            linhaNovoOp.Controls.Add(btnAddOp, 4, 0);

            _gridOperadores = new DataGridView { Dock = DockStyle.Fill };
            Tema.EstilizarGrade(_gridOperadores);
            _gridOperadores.RowTemplate.Height = 40;
            _gridOperadores.ColumnHeadersHeight = 40;
            ConfigurarColunasGradeOperadores();

            var pnlAcoesOp = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 4, 0, 0)
            };

            var btnAlternarOp = new System.Windows.Forms.Button
            {
                Text = "Ativar / Inativar Operador Selecionado",
                Width = 280
            };
            Tema.EstilizarBotaoSecundario(btnAlternarOp, 38);
            btnAlternarOp.Font = Tema.FontePequenaBold;
            btnAlternarOp.Click += (s, e) => AlternarStatusOperadorSelecionado();

            pnlAcoesOp.Controls.Add(btnAlternarOp);

            layoutOp.Controls.Add(lblTituloOp, 0, 0);
            layoutOp.Controls.Add(linhaNovoOp, 0, 1);
            layoutOp.Controls.Add(_gridOperadores, 0, 2);
            layoutOp.Controls.Add(pnlAcoesOp, 0, 3);

            cardOperadores.Controls.Add(layoutOp);

            split.Controls.Add(cardClima, 0, 0);
            split.Controls.Add(cardOperadores, 1, 0);

            tab.Controls.Add(split);
            return tab;
        }

        private static System.Windows.Forms.Panel EmpacotarCampo(string rotulo, Control controle)
        {
            var pnl = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 8, 2)
            };
            var lbl = new Label
            {
                Text = rotulo,
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Top,
                Height = 20
            };
            pnl.Controls.Add(controle);
            pnl.Controls.Add(lbl);
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

            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "DataHora", HeaderText = "DATA / HORA", FillWeight = 15 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "PontoApoio", HeaderText = "LOCAL AFETADO", FillWeight = 24 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "TipoProblema", HeaderText = "PROBLEMA", FillWeight = 16 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cidadao", HeaderText = "CIDADÃO", FillWeight = 15 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descricao", HeaderText = "DESCRIÇÃO DETALHADA", FillWeight = 20 });
            _gridOcorrencias.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "SITUAÇÃO", FillWeight = 10 });

            _gridOcorrencias.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string colName = _gridOcorrencias.Columns[e.ColumnIndex].Name;
                if (colName == "TipoProblema" && e.Value != null)
                {
                    e.CellStyle!.ForeColor = Tema.VermelhoAlerta;
                    e.CellStyle.Font = Tema.FontePequenaBold;
                }
                else if (colName == "Status" && e.Value != null)
                {
                    string status = e.Value.ToString() ?? string.Empty;
                    e.CellStyle!.ForeColor = status.Contains("ATENDIDO", StringComparison.OrdinalIgnoreCase)
                        ? Tema.VerdeSucesso
                        : Tema.LaranjaAviso;
                    e.CellStyle.Font = Tema.FontePequenaBold;
                }
            };
        }

        private void ConfigurarColunasGradeOperadores()
        {
            _gridOperadores.Columns.Clear();

            _gridOperadores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 8 });
            _gridOperadores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Usuario", HeaderText = "USUÁRIO", FillWeight = 18 });
            _gridOperadores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "NOME DO OPERADOR", FillWeight = 30 });
            _gridOperadores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cargo", HeaderText = "CARGO", FillWeight = 22 });
            _gridOperadores.Columns.Add(new DataGridViewTextBoxColumn { Name = "UltimoAcesso", HeaderText = "ÚLTIMO ACESSO", FillWeight = 18 });
            _gridOperadores.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", FillWeight = 12 });
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
                    o.Id,
                    o.DataHora.ToString("dd/MM/yyyy HH:mm"),
                    o.NomePontoApoio,
                    o.TipoProblema,
                    o.ObterCidadaoFormatado(),
                    o.ObterDescricaoFormatada(),
                    status
                );
            }
        }

        private void CarregarTabelaOperadores()
        {
            var operadores = _repository.ObterTodosOperadores();
            _gridOperadores.Rows.Clear();

            foreach (var op in operadores)
            {
                string ultimo = op.UltimoAcesso.HasValue ? op.UltimoAcesso.Value.ToString("dd/MM HH:mm") : "Nunca";
                string status = op.Ativo ? "ATIVO" : "INATIVO";
                _gridOperadores.Rows.Add(op.Id, op.Usuario, op.NomeCompleto, op.Cargo, ultimo, status);
            }
        }

        private void CarregarCamposClimaAtual()
        {
            var alerta = _repository.ObterAlertaAtual();
            _txtTempClima.Text = alerta.TemperaturaAtual.ToString("F1", CultureInfo.InvariantCulture);
            _txtUmidadeClima.Text = alerta.UmidadeRelativa.ToString(CultureInfo.InvariantCulture);
            _txtBairroClima.Text = alerta.BairroReferencia;

            int idxCamada = _cboCamadaPadrao.Items.IndexOf(alerta.CamadaMapaPadrao);
            _cboCamadaPadrao.SelectedIndex = idxCamada >= 0 ? idxCamada : 0;

            _lblResumoClimaSalvo.Text =
                $"Ativo no JSON: {alerta.TemperaturaAtual:F1}°C | Sensação: {alerta.SensacaoTermica:F1}°C ({alerta.ClassificarNivelRisco()}) | Atualizado às {alerta.UltimaAtualizacao:HH:mm}";
        }

        private void ExecutarCadastroOuEdicaoPonto()
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

                if (_pontoEmEdicaoId.HasValue)
                {
                    var pontoAtualizado = new PontoApoio(
                        _pontoEmEdicaoId.Value,
                        nome,
                        tipo,
                        cepFormatado,
                        logradouro,
                        numero,
                        bairro,
                        cidade,
                        uf,
                        _chkDisponivel.Checked);

                    _repository.AtualizarPontoApoio(pontoAtualizado);
                    MessageBox.Show(
                        $"Ponto #{_pontoEmEdicaoId.Value} atualizado com sucesso em Data/pontos_apoio.json!",
                        "Edição Concluída",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
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
                        MessageBoxIcon.Information);
                }

                LimparFormularioPonto();
                CarregarTabelaPontos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar ponto de apoio: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CarregarPontoSelecionadoParaEdicao()
        {
            if (_gridPontos.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um ponto na tabela para editar seus dados.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(_gridPontos.SelectedRows[0].Cells["Id"].Value);
            var ponto = _repository.ObterPontoPorId(id);
            if (ponto == null) return;

            _pontoEmEdicaoId = ponto.Id;
            _txtNome.Text = ponto.Nome;
            _cboTipo.SelectedIndex = ponto.Tipo switch
            {
                TipoPontoApoio.BebedouroPublico => 0,
                TipoPontoApoio.AreaSombraDescanso => 1,
                TipoPontoApoio.PostoSaudeClimatizado => 2,
                _ => 0
            };
            _txtCep.Text = ponto.CEP;
            _ultimoCepConsultado = ExtrairDigitosCep(ponto.CEP);
            _txtLogradouro.Text = ponto.Logradouro;
            _txtNumero.Text = ponto.Numero;
            _txtBairro.Text = ponto.Bairro;
            _txtCidade.Text = ponto.Cidade;
            _txtUf.Text = ponto.UF;
            _chkDisponivel.Checked = ponto.Disponivel;

            _lblSecaoCadastro.Text = $"EDITANDO PONTO #{ponto.Id}: {ponto.Nome.ToUpperInvariant()}";
            _lblSecaoCadastro.ForeColor = Tema.AzulPrimario;
            _btnCadastrarPonto.ButtonText = $"Salvar Alterações (#{ponto.Id})";
            _btnCadastrarPonto.Text = $"Salvar Alterações (#{ponto.Id})";
            _btnCancelarEdicao.Visible = true;
            _txtNome.Focus();
        }

        private void LimparFormularioPonto()
        {
            _pontoEmEdicaoId = null;
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

            _lblSecaoCadastro.Text = "CADASTRAR NOVO PONTO DE APOIO (INTEGRAÇÃO AUTOMÁTICA VIACEP)";
            _lblSecaoCadastro.ForeColor = Tema.VerdeSucesso;
            _btnCadastrarPonto.ButtonText = "Cadastrar Recurso no Totem";
            _btnCadastrarPonto.Text = "Cadastrar Recurso no Totem";
            _btnCancelarEdicao.Visible = false;
            _lblStatusCep.Text = "Informe o CEP (8 dígitos) para preenchimento automático do logradouro e bairro.";
            _lblStatusCep.ForeColor = Tema.TextoSecundario;
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

        private void ExcluirPontoSelecionado()
        {
            if (_gridPontos.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um ponto na tabela para excluir.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(_gridPontos.SelectedRows[0].Cells["Id"].Value);
            string nome = _gridPontos.SelectedRows[0].Cells["Nome"].Value?.ToString() ?? $"#{id}";

            var confirm = MessageBox.Show(
                $"Deseja realmente excluir o ponto '{nome}' (ID #{id}) de Data/pontos_apoio.json?",
                "Confirmar Exclusão",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            if (_repository.RemoverPontoApoio(id))
            {
                if (_pontoEmEdicaoId == id) LimparFormularioPonto();
                CarregarTabelaPontos();
            }
        }

        private void AlternarSituacaoOcorrenciaSelecionada()
        {
            if (_gridOcorrencias.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione uma ocorrência na tabela para alterar sua situação.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var linha = _gridOcorrencias.SelectedRows[0];
            if (linha.Cells["Id"].Value is not Guid id) return;

            string statusAtual = linha.Cells["Status"].Value?.ToString() ?? string.Empty;
            bool novoResolvido = !statusAtual.Contains("ATENDIDO", StringComparison.OrdinalIgnoreCase);

            if (_repository.AlternarStatusOcorrencia(id, novoResolvido))
            {
                CarregarTabelaOcorrencias();
            }
        }

        private void ExcluirOcorrenciaSelecionada()
        {
            if (_gridOcorrencias.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione uma ocorrência na tabela para excluir.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var linha = _gridOcorrencias.SelectedRows[0];
            if (linha.Cells["Id"].Value is not Guid id) return;

            var confirm = MessageBox.Show(
                "Deseja realmente remover este relato do arquivo Data/ocorrencias.json?",
                "Confirmar Exclusão",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes && _repository.RemoverOcorrencia(id))
            {
                CarregarTabelaOcorrencias();
            }
        }

        private void SalvarTelemetriaClimaticaManual()
        {
            string tempTexto = _txtTempClima.Text.Trim().Replace(',', '.');
            if (!double.TryParse(tempTexto, NumberStyles.Float, CultureInfo.InvariantCulture, out double temp) || temp is < -10 or > 60)
            {
                MessageBox.Show("Informe uma temperatura válida entre -10°C e 60°C.", "Temperatura Inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(_txtUmidadeClima.Text.Trim(), out int umidade) || umidade is < 0 or > 100)
            {
                MessageBox.Show("Informe uma umidade relativa válida entre 0% e 100%.", "Umidade Inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string bairro = _txtBairroClima.Text.Trim();
            string camada = _cboCamadaPadrao.SelectedItem?.ToString() ?? "temp_new";

            var salvo = _repository.AtualizarAlertaClimatico(temp, umidade, null, bairro, camada);
            CarregarCamposClimaAtual();

            MessageBox.Show(
                $"Telemetria persistida em Data/alerta_climatico.json!\n\n" +
                $"Temperatura: {salvo.TemperaturaAtual:F1}°C\n" +
                $"Umidade: {salvo.UmidadeRelativa}%\n" +
                $"Sensação Térmica Calculada (NOAA): {salvo.SensacaoTermica:F1}°C\n" +
                $"Classificação: {salvo.ClassificarNivelRisco()}",
                "Clima Atualizado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async Task SincronizarClimaOpenWeatherAsync(System.Windows.Forms.Button btn)
        {
            try
            {
                btn.Enabled = false;
                btn.Text = "Sincronizando...";

                var res = await _openWeatherService.ConsultarClimaAtualAsync();
                if (!res.Sucesso)
                {
                    MessageBox.Show(res.Mensagem, "Aviso OpenWeather", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string camada = _cboCamadaPadrao.SelectedItem?.ToString() ?? "temp_new";
                _repository.AtualizarAlertaClimatico(res.TemperaturaC, res.UmidadePct, res.SensacaoC, _txtBairroClima.Text, camada);
                CarregarCamposClimaAtual();

                MessageBox.Show(
                    $"Telemetria sincronizada com OpenWeather e salva em Data/alerta_climatico.json!\n\n" +
                    $"Temperatura: {res.TemperaturaC:F1}°C | Umidade: {res.UmidadePct}% | Sensação: {res.SensacaoC:F1}°C",
                    "Sincronização Concluída",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            finally
            {
                btn.Enabled = true;
                btn.Text = "Sincronizar OpenWeather";
            }
        }

        private void ExecutarCadastroOperador()
        {
            try
            {
                string usuario = _txtNovoOpUsuario.Text.Trim();
                string senha = _txtNovoOpSenha.Text.Trim();
                string nome = _txtNovoOpNome.Text.Trim();
                string cargo = string.IsNullOrWhiteSpace(_txtNovoOpCargo.Text) ? "Operador Municipal" : _txtNovoOpCargo.Text.Trim();

                if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(nome))
                {
                    MessageBox.Show("Preencha Usuário, Senha e Nome Completo para cadastrar um novo operador.", "Campos Obrigatórios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _repository.CadastrarOperador(new OperadorMunicipal(0, usuario, senha, nome, cargo, true));
                _txtNovoOpUsuario.Clear();
                _txtNovoOpSenha.Clear();
                _txtNovoOpNome.Clear();
                _txtNovoOpCargo.Clear();

                CarregarTabelaOperadores();
                MessageBox.Show($"Operador '{usuario}' cadastrado e persistido em Data/operadores.json.", "Operador Cadastrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AlternarStatusOperadorSelecionado()
        {
            if (_gridOperadores.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um operador na tabela.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var linha = _gridOperadores.SelectedRows[0];
                int id = Convert.ToInt32(linha.Cells["Id"].Value);
                string statusAtual = linha.Cells["Status"].Value?.ToString() ?? string.Empty;
                bool novoAtivo = !string.Equals(statusAtual, "ATIVO", StringComparison.OrdinalIgnoreCase);

                _repository.AlternarStatusOperador(id, novoAtivo);
                CarregarTabelaOperadores();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Operação Não Permitida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
