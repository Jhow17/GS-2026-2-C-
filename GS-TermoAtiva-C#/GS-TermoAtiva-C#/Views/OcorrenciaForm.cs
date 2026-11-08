using System;
using System.Drawing;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela do Fluxo 3 (Munícipe) - Registro ágil de ocorrências e falhas em bebedouros/pontos de refúgio.
    /// Inclui seleção rápida por toque e campos estritamente opcionais para nome do cidadão e detalhamento.
    /// </summary>
    public class OcorrenciaForm : Form
    {
        private readonly TotemRepository _repository;
        private ComboBox _cboPontos = null!;
        private TextBox _txtNomeCidadao = null!;
        private TextBox _txtDescricaoDetalhada = null!;
        private string? _problemaSelecionado = null;
        private readonly System.Windows.Forms.Button[] _botoesProblemas = new System.Windows.Forms.Button[4];

        public OcorrenciaForm(TotemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            ConstruirInterface();
        }

        private void ConstruirInterface()
        {
            Tema.ConfigurarJanelaTotem(this);
            this.Text = "TermoAtiva - Informar Problema em Bebedouro ou Ponto de Apoio";

            var pnlPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Tema.FundoPrincipal,
                Padding = new Padding(28, 22, 28, 22)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 122f)); // Cabeçalho Padronizado Sem Truncamento
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 108f)); // Passo 1: Seleção do Ponto
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));   // Passo 2: Tipo de Problema (Obrigatório)
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));   // Passo 3: Identificação e Relato (Opcionais)
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 78f));  // Barra de Ações Inferior (Confirmar / Voltar)

            // 1. CABEÇALHO PADRONIZADO
            var header = Tema.CriarCabecalhoPadrao(
                "ZELADORIA URBANA PARTICIPATIVA",
                "INFORMAR PROBLEMA EM BEBEDOURO OU RECURSO",
                "Ajude a manter os pontos de hidratação ativos. Selecione o local e o tipo de problema; os campos de identificação são opcionais.",
                Tema.LaranjaAviso);
            pnlPrincipal.Controls.Add(header, 0, 0);

            // 2. PASSO 1: SELEÇÃO DO PONTO DE APOIO
            var cardPonto = new KioskCardPanel(Tema.AzulPrimario)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(22, 12, 22, 12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblPasso1 = new Label
            {
                Text = "ETAPA 01 (OBRIGATÓRIO): SELECIONE O PONTO DE APOIO OU BEBEDOURO",
                Font = Tema.FonteTag,
                ForeColor = Tema.AzulPrimario,
                Dock = DockStyle.Top,
                Height = 24
            };

            _cboPontos = new ComboBox
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 14.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Tema.TextoPrincipal
            };

            PreencherComboPontos();

            cardPonto.Controls.Add(_cboPontos);
            cardPonto.Controls.Add(lblPasso1);
            pnlPrincipal.Controls.Add(cardPonto, 0, 1);

            // 3. PASSO 2: SELEÇÃO RÁPIDA DE PROBLEMA (BOTÕES DE TOQUE SEM EMOJIS)
            var cardProblemas = new KioskCardPanel(Tema.LaranjaAviso)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(22, 12, 22, 12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblPasso2 = new Label
            {
                Text = "ETAPA 02 (OBRIGATÓRIO): TOQUE NA CONDIÇÃO ENCONTRADA NO LOCAL",
                Font = Tema.FonteTag,
                ForeColor = Tema.LaranjaAviso,
                Dock = DockStyle.Top,
                Height = 24
            };

            var tableProblemas = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0, 4, 0, 0)
            };
            tableProblemas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tableProblemas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tableProblemas.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tableProblemas.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            string[] problemas = { "Sem Água", "Torneira Quebrada", "Água Suja / Imprópria", "Local Fechado" };

            for (int i = 0; i < problemas.Length; i++)
            {
                string nomeProblema = problemas[i];

                var btn = new System.Windows.Forms.Button
                {
                    Text = nomeProblema.ToUpperInvariant(),
                    Tag = nomeProblema,
                    Dock = DockStyle.Fill,
                    Font = Tema.FonteBotaoToque,
                    BackColor = Color.FromArgb(248, 250, 252),
                    ForeColor = Tema.TextoPrincipal,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(6)
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Tema.CinzaBorda;

                btn.Click += (s, e) => SelecionarProblema(nomeProblema);

                _botoesProblemas[i] = btn;
                tableProblemas.Controls.Add(btn, i % 2, i / 2);
            }

            cardProblemas.Controls.Add(tableProblemas);
            cardProblemas.Controls.Add(lblPasso2);
            pnlPrincipal.Controls.Add(cardProblemas, 0, 2);

            // 4. PASSO 3: CAMPOS OPCIONAIS (NOME DO CIDADÃO E DESCRIÇÃO MULTILINE)
            var cardOpcionais = new KioskCardPanel(Tema.TextoSuave)
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(22, 12, 22, 12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblPasso3 = new Label
            {
                Text = "ETAPA 03 (OPCIONAL): IDENTIFICAÇÃO DO CIDADÃO E DETALHAMENTO DA OCORRÊNCIA",
                Font = Tema.FonteTag,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Top,
                Height = 24
            };

            var gridOpcionais = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0, 4, 0, 0)
            };
            gridOpcionais.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            gridOpcionais.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            gridOpcionais.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            gridOpcionais.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblNomeCidadao = new Label
            {
                Text = "Nome do Cidadão (Opcional):",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblDescricao = new Label
            {
                Text = "Descrição Adicional do Problema (Opcional):",
                Font = Tema.FontePequenaBold,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlInputNome = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 16, 2)
            };

            _txtNomeCidadao = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 13f, FontStyle.Regular),
                PlaceholderText = "Deixe em branco para relato anônimo",
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblAvisoAnonimo = new Label
            {
                Text = "O preenchimento não é obrigatório.\nVocê pode enviar apenas selecionando o ponto e o problema acima.",
                Font = Tema.FontePequena,
                ForeColor = Tema.TextoSuave,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 0)
            };

            pnlInputNome.Controls.Add(lblAvisoAnonimo);
            pnlInputNome.Controls.Add(_txtNomeCidadao);

            _txtDescricaoDetalhada = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Regular),
                PlaceholderText = "Caso deseje, descreva mais detalhes sobre o problema observado no local...",
                BorderStyle = BorderStyle.FixedSingle
            };

            gridOpcionais.Controls.Add(lblNomeCidadao, 0, 0);
            gridOpcionais.Controls.Add(lblDescricao, 1, 0);
            gridOpcionais.Controls.Add(pnlInputNome, 0, 1);
            gridOpcionais.Controls.Add(_txtDescricaoDetalhada, 1, 1);

            cardOpcionais.Controls.Add(gridOpcionais);
            cardOpcionais.Controls.Add(lblPasso3);
            pnlPrincipal.Controls.Add(cardOpcionais, 0, 3);

            // 5. BARRA INFERIOR DE BOTÕES (CONFIRMAR E VOLTAR)
            var tableAcoes = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            tableAcoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tableAcoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));

            var btnCancelar = new System.Windows.Forms.Button
            {
                Text = "CANCELAR E VOLTAR AO MENU",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };
            Tema.EstilizarBotaoSecundario(btnCancelar, 66);
            btnCancelar.Click += (s, e) => this.Close();

            var btnConfirmar = Tema.CriarBotaoReaLTaiizor("CONFIRMAR REGISTRO DE OCORRÊNCIA", Tema.VerdeSucesso, 66);
            btnConfirmar.Dock = DockStyle.Fill;
            btnConfirmar.Margin = new Padding(8, 0, 0, 0);
            btnConfirmar.Click += (s, e) => SalvarOcorrencia();

            tableAcoes.Controls.Add(btnCancelar, 0, 0);
            tableAcoes.Controls.Add(btnConfirmar, 1, 0);
            pnlPrincipal.Controls.Add(tableAcoes, 0, 4);

            this.Controls.Add(pnlPrincipal);
        }

        private void PreencherComboPontos()
        {
            var pontos = _repository.ObterTodosPontos();
            _cboPontos.Items.Clear();

            foreach (var p in pontos)
            {
                _cboPontos.Items.Add(new ItemPontoCombo(p.Id, p.Nome, p.ObterTipoFormatado()));
            }

            if (_cboPontos.Items.Count > 0)
            {
                _cboPontos.SelectedIndex = 0;
            }
        }

        private void SelecionarProblema(string problema)
        {
            _problemaSelecionado = problema;

            foreach (var btn in _botoesProblemas)
            {
                if (string.Equals(btn.Tag?.ToString(), problema, StringComparison.Ordinal))
                {
                    btn.BackColor = Tema.VermelhoAlerta;
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Tema.VermelhoAlerta;
                }
                else
                {
                    btn.BackColor = Color.FromArgb(248, 250, 252);
                    btn.ForeColor = Tema.TextoPrincipal;
                    btn.FlatAppearance.BorderColor = Tema.CinzaBorda;
                }
            }
        }

        private void SalvarOcorrencia()
        {
            try
            {
                if (_cboPontos.SelectedItem is not ItemPontoCombo pontoSelecionado)
                {
                    MessageBox.Show(
                        "Por favor, selecione um ponto de apoio na Etapa 01.",
                        "Seleção Obrigatória",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                if (string.IsNullOrWhiteSpace(_problemaSelecionado))
                {
                    MessageBox.Show(
                        "Por favor, selecione uma das opções de problema na Etapa 02.",
                        "Seleção Obrigatória",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Nome e Descrição são estritamente opcionais
                string? nomeCidadao = string.IsNullOrWhiteSpace(_txtNomeCidadao.Text)
                    ? null
                    : _txtNomeCidadao.Text.Trim();

                string? descricaoDetalhada = string.IsNullOrWhiteSpace(_txtDescricaoDetalhada.Text)
                    ? null
                    : _txtDescricaoDetalhada.Text.Trim();

                var novaOcorrencia = new OcorrenciaMunicipe(
                    pontoSelecionado.Id,
                    pontoSelecionado.Nome,
                    _problemaSelecionado,
                    nomeCidadao,
                    descricaoDetalhada
                );

                _repository.RegistrarOcorrencia(novaOcorrencia);

                MessageBox.Show(
                    $"Ocorrência registrada com sucesso no sistema.\n\n" +
                    $"Local: {pontoSelecionado.Nome}\n" +
                    $"Problema: {_problemaSelecionado}\n" +
                    $"Relator: {novaOcorrencia.ObterCidadaoFormatado()}\n" +
                    $"Horário: {novaOcorrencia.DataHora:HH:mm}\n\n" +
                    $"A equipe de zeladoria municipal foi notificada para providenciar o atendimento.",
                    "Ocorrência Registrada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao registrar ocorrência: {ex.Message}",
                    "Erro no Sistema",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private record ItemPontoCombo(int Id, string Nome, string TipoFormatado)
        {
            public override string ToString() => $"{Nome} ({TipoFormatado})";
        }
    }
}
