using System;

namespace GS_TermoAtiva_C_.Models
{
    /// <summary>
    /// Representa uma notificação de zeladoria urbana aberta por um munícipe ou trabalhador
    /// diretamente no totem sobre problemas em um ponto de refúgio (ex: bebedouro quebrado ou sem água).
    /// </summary>
    public class OcorrenciaMunicipe
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int PontoApoioId { get; set; }
        public string NomePontoApoio { get; set; } = string.Empty;
        public string TipoProblema { get; set; } = string.Empty;
        public DateTime DataHora { get; set; } = DateTime.Now;
        public bool Resolvido { get; set; } = false;

        public OcorrenciaMunicipe()
        {
        }

        public OcorrenciaMunicipe(int pontoApoioId, string nomePontoApoio, string tipoProblema)
        {
            Id = Guid.NewGuid();
            PontoApoioId = pontoApoioId;
            NomePontoApoio = nomePontoApoio;
            TipoProblema = tipoProblema;
            DataHora = DateTime.Now;
            Resolvido = false;
        }

        public override string ToString()
        {
            string status = Resolvido ? "Resolvido" : "Pendente";
            return $"[{DataHora:dd/MM/yyyy HH:mm}] {NomePontoApoio}: {TipoProblema} ({status})";
        }
    }
}
