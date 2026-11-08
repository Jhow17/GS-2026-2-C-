using System;

namespace GS_TermoAtiva_C_.Models
{
    /// <summary>
    /// Representa uma notificação de zeladoria urbana aberta por um munícipe ou trabalhador
    /// diretamente no totem sobre problemas em um ponto de refúgio (ex: bebedouro quebrado ou sem água).
    /// Suporta identificação e relato detalhado opcionais.
    /// </summary>
    public class OcorrenciaMunicipe
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int PontoApoioId { get; set; }
        public string NomePontoApoio { get; set; } = string.Empty;
        public string TipoProblema { get; set; } = string.Empty;
        public string? NomeMunicipe { get; set; }
        public string? DescricaoDetalhada { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public bool Resolvido { get; set; } = false;

        public OcorrenciaMunicipe()
        {
        }

        public OcorrenciaMunicipe(
            int pontoApoioId,
            string nomePontoApoio,
            string tipoProblema,
            string? nomeMunicipe = null,
            string? descricaoDetalhada = null)
        {
            Id = Guid.NewGuid();
            PontoApoioId = pontoApoioId;
            NomePontoApoio = nomePontoApoio;
            TipoProblema = tipoProblema;
            NomeMunicipe = string.IsNullOrWhiteSpace(nomeMunicipe) ? null : nomeMunicipe.Trim();
            DescricaoDetalhada = string.IsNullOrWhiteSpace(descricaoDetalhada) ? null : descricaoDetalhada.Trim();
            DataHora = DateTime.Now;
            Resolvido = false;
        }

        public string ObterCidadaoFormatado()
        {
            return string.IsNullOrWhiteSpace(NomeMunicipe) ? "Anônimo" : NomeMunicipe;
        }

        public string ObterDescricaoFormatada()
        {
            return string.IsNullOrWhiteSpace(DescricaoDetalhada) ? "Sem observações adicionais" : DescricaoDetalhada;
        }

        public override string ToString()
        {
            string status = Resolvido ? "Resolvido" : "Pendente";
            string autor = ObterCidadaoFormatado();
            return $"[{DataHora:dd/MM/yyyy HH:mm}] {NomePontoApoio}: {TipoProblema} - Relator: {autor} ({status})";
        }
    }
}
