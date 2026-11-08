using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Models;

namespace GS_TermoAtiva_C_.Views
{
    /// <summary>
    /// Tela do Fluxo 3 (Munícipe) - Registro ágil de ocorrências e falhas em bebedouros/pontos de refúgio
    /// (zeladoria colaborativa por toque rápido, sem necessidade de digitação em teclado virtual).
    /// </summary>
    public class OcorrenciaForm : Form
    {
        private readonly TotemRepository _repository;
        private ComboBox _cboPontos = null!;
        private string? _problemaSelecionado = null;
        private readonly Button[] _botoesProblemas = new Button[4];

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
                Padding = new Padding(32)
            };

            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f)); // Cabeçalho
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f)); // Seleção do Ponto de Apoio
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Opções Rápidas de Falha
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 95f));  // Botão Confirmar
            pnlPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 85f));  // Botão Cancelar

            // 1. CABEÇALHO
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(24, 15, 24, 15),
                Margin = new Padding(0, 0, 0, 15)
            };

            var lblTitulo = new Label
            {
                Text = "⚠️ INFORMAR PROBLEMA EM BEBEDOURO OU RECURSO",
                Font = Tema.FonteTitulo,
                ForeColor = Tema.VermelhoAlerta,
                Dock = DockStyle.Top,
                Height = 42,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblDescricao = new Label
            {
                Text = "Ajude a zeladoria urbana a manter os pontos de alívio funcionando durante as ondas de calor. Selecione o local e o problema com apenas 2 toques:",
                Font = Tema.FonteGeral,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(lblDescricao);
            pnlHeader.Controls.Add(lblTitulo);
            pnlPrincipal.Controls.Add(pnlHeader, 0, 0);

            // 2. SELEÇÃO DO PONTO DE APOIO
            var cardPonto = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(24, 16, 24, 16),
                Margin = new Padding(0, 0, 0, 15)
            };

            var lblPasso1 = new Label
            {
                Text = "Passo 1: Selecione o Ponto de Apoio / Bebedouro:",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Top,
                Height = 32
            };

            _cboPontos = new ComboBox
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 16f, FontStyle.Regular),
                BackColor = Color.White
            };

            PreencherComboPontos();

            cardPonto.Controls.Add(_cboPontos);
            cardPonto.Controls.Add(lblPasso1);
            pnlPrincipal.Controls.Add(cardPonto, 0, 1);

            // 3. SELEÇÃO RÁPIDA DE PROBLEMA (BOTÕES GIGANTES DE TOQUE)
            var cardProblemas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoCard,
                Padding = new Padding(24, 16, 24, 16),
                Margin = new Padding(0, 0, 0, 15)
            };

            var lblPasso2 = new Label
            {
                Text = "Passo 2: Toque no problema encontrado:",
                Font = Tema.FonteGeralDestaque,
                ForeColor = Tema.TextoPrincipal,
                Dock = DockStyle.Top,
                Height = 35
            };

            var tableProblemas = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0, 8, 0, 0)
            };
            tableProblemas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tableProblemas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tableProblemas.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tableProblemas.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            string[] problemas = { "Sem Água", "Torneira Quebrada", "Água Suja / Imprópria", "Local Fechado" };
            string[] icones = { "🚰", "🔧", "⚠️", "🔒" };

            for (int i = 0; i < problemas.Length; i++)
            {
                string nomeProblema = problemas[i];
                string icone = icones[i];

                var btn = new Button
                {
                    Text = $"{icone}  {nomeProblema}",
                    Dock = DockStyle.Fill,
                    Font = Tema.FonteBotaoToque,
                    BackColor = Color.FromArgb(243, 244, 246),
                    ForeColor = Tema.TextoPrincipal,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(8)
                };
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.BorderColor = Tema.CinzaBorda;

                btn.Click += (s, e) => SelecionarProblema(nomeProblema);

                _botoesProblemas[i] = btn;
                tableProblemas.Controls.Add(btn, i % 2, i / 2);
            }

            cardProblemas.Controls.Add(tableProblemas);
            cardProblemas.Controls.Add(lblPasso2);
            pnlPrincipal.Controls.Add(cardProblemas, 0, 2);

            // 4. BOTÃO CONFIRMAR
            var btnConfirmar = new Button
            {
                Text = "✅  CONFIRMAR REGISTRO DE OCORRÊNCIA",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 8)
            };
            Tema.EstilizarBotao(btnConfirmar, Tema.VerdeSucesso, 85);
            btnConfirmar.Click += (s, e) => SalvarOcorrencia();
            pnlPrincipal.Controls.Add(btnConfirmar, 0, 3);

            // 5. BOTÃO CANCELAR
            var btnCancelar = new Button
            {
                Text = "⬅  CANCELAR E VOLTAR AO MENU",
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            Tema.EstilizarBotaoSecundario(btnCancelar, 75);
            btnCancelar.Click += (s, e) => this.Close();
            pnlPrincipal.Controls.Add(btnCancelar, 0, 4);

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
                if (btn.Text.EndsWith(problema))
                {
                    btn.BackColor = Tema.VermelhoAlerta;
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Tema.VermelhoAlerta;
                }
                else
                {
                    btn.BackColor = Color.FromArgb(243, 244, 246);
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
                        "Por favor, selecione um ponto de apoio no Passo 1.",
                        "Atenção",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                if (string.IsNullOrWhiteSpace(_problemaSelecionado))
                {
                    MessageBox.Show(
                        "Por favor, toque em uma das opções de problema no Passo 2.",
                        "Atenção",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                var novaOcorrencia = new OcorrenciaMunicipe(
                    pontoSelecionado.Id,
                    pontoSelecionado.Nome,
                    _problemaSelecionado
                );

                _repository.RegistrarOcorrencia(novaOcorrencia);

                MessageBox.Show(
                    $"✓ Ocorrência registrada com sucesso!\n\n" +
                    $"Local: {pontoSelecionado.Nome}\n" +
                    $"Problema: {_problemaSelecionado}\n" +
                    $"Horário: {novaOcorrencia.DataHora:HH:mm}\n\n" +
                    $"Nossa equipe de zeladoria municipal foi notificada para providenciar o reparo.",
                    "Ocorrência Recebida",
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
