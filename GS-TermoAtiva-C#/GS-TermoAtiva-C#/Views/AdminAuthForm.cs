using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Modal de autenticação segura para acesso restrito de operadores e equipes de zeladoria municipal.
    /// Contém campos de Usuário/Identificador e Senha, além de Teclado Virtual sob demanda (oculto por padrão).
    /// </summary>
    public class AdminAuthForm : Form
    {
        private const string UsuarioPadrao = "admin";
        private const string SenhaPadrao = "1234";

        private readonly TotemRepository _repository;
        private TextBox _txtUsuario = null!;
        private TextBox _txtSenha = null!;
        private TextBox _campoAtivo = null!;
        private System.Windows.Forms.Panel _pnlContainerTeclado = null!;
        private System.Windows.Forms.Button _btnAlternarTeclado = null!;

        private const int AlturaSemTeclado = 430;
        private const int AlturaComTeclado = 720;

        public AdminAuthForm(TotemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            this.Text = "Autenticação - Operador Municipal";
            this.Size = new Size(560, AlturaSemTeclado);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Tema.FundoPrincipal;
            this.ForeColor = Tema.TextoPrincipal;
            this.Font = Tema.FonteGeral;

            var pnlRoot = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22)
            };

            var cardLogin = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(26, 20, 26, 20)
            };

            // Cabeçalho do Modal
            var lblTag = new Label
            {
                Text = "CONTROLE DE ACESSO OPERACIONAL",
                Font = Tema.FonteTag,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblTitulo = new Label
            {
                Text = "AUTENTICAÇÃO DO OPERADOR",
                Font = Tema.FonteSubtitulo,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Top,
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblDica = new Label
            {
                Text = "Credenciais de homologação acadêmica: Usuário 'admin'  |  Senha '1234'",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.LaranjaAviso,
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Painel de Campos (Usuário + Senha)
            var pnlCampos = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 148,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(0, 8, 0, 6)
            };
            pnlCampos.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            pnlCampos.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            pnlCampos.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            pnlCampos.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

            var lblUsuario = new Label
            {
                Text = "Usuário / Identificador Funcional:",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            _txtUsuario = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 14f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 30
            };
            _txtUsuario.Enter += (s, e) => _campoAtivo = _txtUsuario;
            _txtUsuario.KeyDown += TratadorTeclaEnter;

            var lblSenha = new Label
            {
                Text = "Senha / PIN de Segurança:",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            _txtSenha = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 20
            };
            _txtSenha.Enter += (s, e) => _campoAtivo = _txtSenha;
            _txtSenha.KeyDown += TratadorTeclaEnter;

            _campoAtivo = _txtUsuario;

            pnlCampos.Controls.Add(lblUsuario, 0, 0);
            pnlCampos.Controls.Add(_txtUsuario, 0, 1);
            pnlCampos.Controls.Add(lblSenha, 0, 2);
            pnlCampos.Controls.Add(_txtSenha, 0, 3);

            // Botão Alternador do Teclado Virtual (Sob Demanda)
            var pnlBarraTeclado = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(0, 4, 0, 4)
            };

            _btnAlternarTeclado = new System.Windows.Forms.Button
            {
                Text = "Teclado Virtual (Exibir)",
                Dock = DockStyle.Left,
                Width = 220
            };
            Tema.EstilizarBotaoSecundario(_btnAlternarTeclado, 38);
            _btnAlternarTeclado.Font = Tema.FontePequenaBold;
            _btnAlternarTeclado.Click += (s, e) => AlternarVisibilidadeTeclado();

            pnlBarraTeclado.Controls.Add(_btnAlternarTeclado);

            // Container do Teclado Virtual Alfanumérico (Oculto por padrão: Visible = false)
            _pnlContainerTeclado = CriarPainelTecladoVirtual();
            _pnlContainerTeclado.Visible = false;

            // Barra de Ações Inferior (Cancelar / Entrar)
            var pnlAcoes = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 6, 0, 0)
            };
            pnlAcoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            pnlAcoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));

            var btnCancelar = new System.Windows.Forms.Button
            {
                Text = "Cancelar",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0)
            };
            Tema.EstilizarBotaoSecundario(btnCancelar, 46);
            btnCancelar.Click += (s, e) => this.Close();

            var btnConfirmar = Tema.CriarBotaoReaLTaiizor("Autenticar Operador", Tema.AzulPrimario, 46, 8);
            btnConfirmar.Dock = DockStyle.Fill;
            btnConfirmar.Font = Tema.FonteGeralDestaque;
            btnConfirmar.Margin = new Padding(6, 0, 0, 0);
            btnConfirmar.Click += (s, e) => ValidarAcesso();

            pnlAcoes.Controls.Add(btnCancelar, 0, 0);
            pnlAcoes.Controls.Add(btnConfirmar, 1, 0);

            cardLogin.Controls.Add(_pnlContainerTeclado);
            cardLogin.Controls.Add(pnlAcoes);
            cardLogin.Controls.Add(pnlBarraTeclado);
            cardLogin.Controls.Add(pnlCampos);
            cardLogin.Controls.Add(lblDica);
            cardLogin.Controls.Add(lblTitulo);
            cardLogin.Controls.Add(lblTag);

            pnlRoot.Controls.Add(cardLogin);
            this.Controls.Add(pnlRoot);
        }

        private System.Windows.Forms.Panel CriarPainelTecladoVirtual()
        {
            var container = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 8)
            };

            var gridTeclas = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 4,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(6)
            };

            for (int c = 0; c < 6; c++)
            {
                gridTeclas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
            }
            for (int r = 0; r < 4; r++)
            {
                gridTeclas.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            }

            string[] teclas =
            {
                "1", "2", "3", "4", "5", "6",
                "7", "8", "9", "0", "a", "d",
                "m", "i", "n", "u", "s", "r",
                "admin", "1234", "LIMPAR", "APAGAR", "CAMPO", "OK"
            };

            for (int idx = 0; idx < teclas.Length; idx++)
            {
                string valorTecla = teclas[idx];
                var btn = CriarBotaoTecla(valorTecla);

                int linha = idx / 6;
                int coluna = idx % 6;
                gridTeclas.Controls.Add(btn, coluna, linha);
            }

            container.Controls.Add(gridTeclas);
            return container;
        }

        private System.Windows.Forms.Button CriarBotaoTecla(string valorTecla)
        {
            bool especial = valorTecla is "LIMPAR" or "APAGAR" or "CAMPO" or "OK" or "admin" or "1234";

            Color corFundo = valorTecla switch
            {
                "LIMPAR" => Tema.VermelhoAlerta,
                "OK" => Tema.VerdeSucesso,
                "admin" or "1234" => Tema.CorFundoBebedouro,
                _ => Tema.FundoCard
            };

            Color corTexto = valorTecla switch
            {
                "LIMPAR" or "OK" => Color.White,
                "admin" or "1234" => Tema.CorTextoBebedouro,
                _ => Tema.TextoPrincipal
            };

            var btn = new System.Windows.Forms.Button
            {
                Text = valorTecla,
                Dock = DockStyle.Fill,
                Font = especial ? Tema.FontePequenaBold : Tema.FonteGeralDestaque,
                BackColor = corFundo,
                ForeColor = corTexto,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(3),
                TabStop = false
            };
            btn.FlatAppearance.BorderColor = Tema.CinzaBorda;
            btn.FlatAppearance.BorderSize = 1;

            btn.Click += (s, e) => ProcessarToqueTeclado(valorTecla);
            return btn;
        }

        private void ProcessarToqueTeclado(string tecla)
        {
            _campoAtivo ??= _txtUsuario;

            switch (tecla)
            {
                case "LIMPAR":
                    _campoAtivo.Clear();
                    break;

                case "APAGAR":
                    if (_campoAtivo.Text.Length > 0)
                    {
                        _campoAtivo.Text = _campoAtivo.Text[..^1];
                    }
                    break;

                case "CAMPO":
                    _campoAtivo = (_campoAtivo == _txtUsuario) ? _txtSenha : _txtUsuario;
                    _campoAtivo.Focus();
                    break;

                case "OK":
                    ValidarAcesso();
                    break;

                case "admin":
                    _txtUsuario.Text = "admin";
                    _campoAtivo = _txtSenha;
                    _txtSenha.Focus();
                    break;

                case "1234":
                    _txtSenha.Text = "1234";
                    break;

                default:
                    if (_campoAtivo.Text.Length < _campoAtivo.MaxLength)
                    {
                        _campoAtivo.Text += tecla;
                    }
                    break;
            }
        }

        private void AlternarVisibilidadeTeclado()
        {
            bool exibir = !_pnlContainerTeclado.Visible;
            _pnlContainerTeclado.Visible = exibir;
            this.Height = exibir ? AlturaComTeclado : AlturaSemTeclado;
            _btnAlternarTeclado.Text = exibir ? "Teclado Virtual (Ocultar)" : "Teclado Virtual (Exibir)";
            this.CenterToParent();
        }

        private void TratadorTeclaEnter(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ValidarAcesso();
            }
        }

        private void ValidarAcesso()
        {
            string usuarioInformado = _txtUsuario.Text.Trim();
            string senhaInformada = _txtSenha.Text.Trim();

            if (string.Equals(usuarioInformado, UsuarioPadrao, StringComparison.OrdinalIgnoreCase) &&
                senhaInformada == SenhaPadrao)
            {
                this.Hide();
                using var formAdmin = new AdminPontosForm(_repository);
                formAdmin.ShowDialog(this.Owner);
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "Credenciais inválidas. Verifique o Usuário e a Senha informados.\n(Credenciais de avaliação: Usuário 'admin' | Senha '1234')",
                    "Acesso Negado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                _txtSenha.Clear();
                _txtSenha.Focus();
            }
        }
    }
}
