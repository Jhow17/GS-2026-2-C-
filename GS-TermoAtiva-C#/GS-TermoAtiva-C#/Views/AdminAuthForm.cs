using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Modal de autenticação segura para acesso restrito de operadores e equipes de zeladoria municipal.
    /// Inclui teclado numérico virtual para facilitar digitação em quiosques de autoatendimento.
    /// </summary>
    public class AdminAuthForm : Form
    {
        private const string SenhaPadrao = "1234";
        private readonly TotemRepository _repository;
        private TextBox _txtSenha = null!;

        public AdminAuthForm(TotemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            this.Text = "Autenticação - Operador Municipal";
            this.Size = new Size(520, 680);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Tema.FundoPrincipal;
            this.ForeColor = Tema.TextoPrincipal;
            this.Font = Tema.FonteGeral;

            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };

            var lblTitulo = new Label
            {
                Text = "🔒 ACESSO RESTRITO",
                Font = Tema.FonteSubtitulo,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSubtitulo = new Label
            {
                Text = "Digite o PIN / Senha do operador de zeladoria:",
                Font = Tema.FonteGeral,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Campo de Senha
            var pnlInput = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(30, 10, 30, 10)
            };

            _txtSenha = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                MaxLength = 10
            };
            _txtSenha.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ValidarAcesso();
                }
            };
            pnlInput.Controls.Add(_txtSenha);

            // Dica de avaliação acadêmica
            var lblDica = new Label
            {
                Text = "🔑 Dica para avaliação do projeto: Digite a senha '1234'",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.LaranjaAviso,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Teclado Numérico Virtual Touchscreen
            var pnlTeclado = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlTeclado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlTeclado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlTeclado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            for (int r = 0; r < 4; r++)
            {
                pnlTeclado.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            }

            // Adiciona botões 1 a 9
            for (int i = 1; i <= 9; i++)
            {
                string digito = i.ToString();
                var btn = CriarBotaoTeclado(digito);
                btn.Click += (s, e) => InserirDigito(digito);
                int linha = (i - 1) / 3;
                int coluna = (i - 1) % 3;
                pnlTeclado.Controls.Add(btn, coluna, linha);
            }

            // Linha 4: Limpar (C), 0, Apagar (⌫)
            var btnLimpar = CriarBotaoTeclado("Limpar", Color.FromArgb(239, 68, 68), Color.White);
            btnLimpar.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnLimpar.Click += (s, e) => _txtSenha.Clear();

            var btnZero = CriarBotaoTeclado("0");
            btnZero.Click += (s, e) => InserirDigito("0");

            var btnApagar = CriarBotaoTeclado("⌫", Color.FromArgb(243, 244, 246), Tema.TextoPrincipal);
            btnApagar.Click += (s, e) =>
            {
                if (_txtSenha.Text.Length > 0)
                {
                    _txtSenha.Text = _txtSenha.Text.Substring(0, _txtSenha.Text.Length - 1);
                }
            };

            pnlTeclado.Controls.Add(btnLimpar, 0, 3);
            pnlTeclado.Controls.Add(btnZero, 1, 3);
            pnlTeclado.Controls.Add(btnApagar, 2, 3);

            // Botões de Ação Inferiores (Confirmar / Cancelar)
            var pnlAcoes = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Padding = new Padding(20, 10, 20, 10)
            };

            var btnConfirmar = new Button
            {
                Text = "Entrar no Painel",
                Dock = DockStyle.Top,
                Height = 50
            };
            Tema.EstilizarBotao(btnConfirmar, Tema.AzulPrimario, 50);
            btnConfirmar.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            btnConfirmar.Click += (s, e) => ValidarAcesso();

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Dock = DockStyle.Bottom,
                Height = 45
            };
            Tema.EstilizarBotaoSecundario(btnCancelar, 45);
            btnCancelar.Click += (s, e) => this.Close();

            pnlAcoes.Controls.Add(btnConfirmar);
            pnlAcoes.Controls.Add(btnCancelar);

            pnlContainer.Controls.Add(pnlTeclado);
            pnlContainer.Controls.Add(pnlAcoes);
            pnlContainer.Controls.Add(lblDica);
            pnlContainer.Controls.Add(pnlInput);
            pnlContainer.Controls.Add(lblSubtitulo);
            pnlContainer.Controls.Add(lblTitulo);

            this.Controls.Add(pnlContainer);
        }

        private static Button CriarBotaoTeclado(string texto, Color? corFundo = null, Color? corTexto = null)
        {
            var btn = new Button
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                BackColor = corFundo ?? Tema.FundoCard,
                ForeColor = corTexto ?? Tema.TextoPrincipal,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4)
            };
            btn.FlatAppearance.BorderColor = Tema.CinzaBorda;
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private void InserirDigito(string digito)
        {
            if (_txtSenha.Text.Length < 10)
            {
                _txtSenha.Text += digito;
            }
        }

        private void ValidarAcesso()
        {
            if (_txtSenha.Text == SenhaPadrao)
            {
                this.Hide();
                var formAdmin = new AdminPontosForm(_repository);
                formAdmin.ShowDialog(this.Owner);
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "Senha ou PIN incorreto! Verifique a credencial do operador e tente novamente.\n(Dica para avaliação: 1234)",
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
