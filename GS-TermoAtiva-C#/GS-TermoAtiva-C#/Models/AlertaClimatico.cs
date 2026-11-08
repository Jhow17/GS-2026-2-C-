using System;
using System.Collections.Generic;

namespace GS_TermoAtiva_C_.Models
{
    /// <summary>
    /// Modelo que encapsula os parâmetros micrometeorológicos locais capturados pelo totem
    /// e aplica regras de triagem de risco à saúde e alertas à população vulnerável.
    /// </summary>
    public class AlertaClimatico
    {
        public double TemperaturaAtual { get; set; }
        public int UmidadeRelativa { get; set; }
        public double SensacaoTermica { get; set; }
        public DateTime UltimaAtualizacao { get; set; } = DateTime.Now;

        public AlertaClimatico()
        {
        }

        public AlertaClimatico(double temperaturaAtual, int umidadeRelativa, double sensacaoTermica, DateTime? ultimaAtualizacao = null)
        {
            TemperaturaAtual = temperaturaAtual;
            UmidadeRelativa = umidadeRelativa;
            SensacaoTermica = sensacaoTermica;
            UltimaAtualizacao = ultimaAtualizacao ?? DateTime.Now;
        }

        /// <summary>
        /// Classifica o nível de risco térmico com base na temperatura e sensação térmica apurada.
        /// Regras:
        /// - Menor que 32°C: Risco Moderado
        /// - 32°C a 38°C: Risco Alto
        /// - Maior que 38°C: Risco Extremo
        /// </summary>
        public string ClassificarNivelRisco()
        {
            // Avalia a maior intensidade entre temperatura e sensação térmica para segurança máxima do munícipe
            double referencia = Math.Max(TemperaturaAtual, SensacaoTermica);

            if (referencia < 32.0)
            {
                return "Risco Moderado";
            }
            else if (referencia <= 38.0)
            {
                return "Risco Alto";
            }
            else
            {
                return "Risco Extremo";
            }
        }

        /// <summary>
        /// Gera lista de recomendações de saúde pública preventivas de acordo com o nível de risco atual.
        /// Voltado a idosos, garis, entregadores e trabalhadores sob estresse térmico direto.
        /// </summary>
        public List<string> ObterRecomendacoes()
        {
            string risco = ClassificarNivelRisco();
            var recomendacoes = new List<string>();

            switch (risco)
            {
                case "Risco Extremo":
                    recomendacoes.Add("HIDRATAÇÃO OBRIGATÓRIA: Beba no mínimo 300ml de água a cada 20 minutos, mesmo sem sede.");
                    recomendacoes.Add("PAUSA LABORAL IMEDIATA: Trabalhadores ao ar livre devem interromper esforços pesados sob o sol.");
                    recomendacoes.Add("REFÚGIO CLIMATIZADO: Dirija-se imediatamente a uma UBS ou área com ar-condicionado.");
                    recomendacoes.Add("ATENÇÃO A SINTOMAS: Tontura, náusea, pele muito quente ou confusão mental exigem socorro imediato.");
                    recomendacoes.Add("MOLHE O CORPO: Umedeça rosto, nuca e pulsos com água fresca no bebedouro mais próximo.");
                    break;

                case "Risco Alto":
                    recomendacoes.Add("HIDRATAÇÃO CONSTANTE: Beba água fresca regularmente ao longo de todo o dia.");
                    recomendacoes.Add("BUSCA POR SOMBRA: Faça pausas de 10 a 15 minutos em praças arborizadas ou marquises.");
                    recomendacoes.Add("PROTEÇÃO SOLAR: Use chapéu de aba larga, roupas leves e óculos escuros se estiver ao sol.");
                    recomendacoes.Add("CUIDADO COM VULNERÁVEIS: Idosos, crianças e animais não devem circular nos horários de pico (10h às 16h).");
                    recomendacoes.Add("REABASTEÇA SUA GARRAFA: Utilize os bebedouros públicos cadastrados no totem.");
                    break;

                case "Risco Moderado":
                default:
                    recomendacoes.Add("MANTENHA-SE HIDRATADO: Carregue uma garrafa reutilizável e beba água periodicamente.");
                    recomendacoes.Add("PREFERÊNCIA POR SOMBRAS: Caminhe preferencialmente pelo lado sombreado das vias públicas.");
                    recomendacoes.Add("ALIMENTAÇÃO LEVE: Prefira frutas, verduras e refeições leves ricas em eletrólitos.");
                    recomendacoes.Add("ACOMPANHE O TOTEM: Verifique novos alertas caso o calor aumente nas próximas horas.");
                    break;
            }

            return recomendacoes;
        }
    }
}
