using System;
using System.Collections.Generic;

namespace GS_TermoAtiva_C_.Models
{
    /// <summary>
    /// Modelo que encapsula os parâmetros micrometeorológicos locais capturados pelo totem,
    /// calcula o Índice de Calor (regressão de Rothfusz / NOAA) e aplica regras de triagem de risco
    /// à saúde e alertas à população vulnerável. Persistido em Data/alerta_climatico.json.
    /// </summary>
    public class AlertaClimatico
    {
        public double TemperaturaAtual { get; set; }
        public int UmidadeRelativa { get; set; }
        public double SensacaoTermica { get; set; }
        public string BairroReferencia { get; set; } = "Sé - Centro";
        public string CamadaMapaPadrao { get; set; } = "temp_new";
        public DateTime UltimaAtualizacao { get; set; } = DateTime.Now;

        public AlertaClimatico()
        {
        }

        public AlertaClimatico(
            double temperaturaAtual,
            int umidadeRelativa,
            double? sensacaoTermica = null,
            DateTime? ultimaAtualizacao = null,
            string bairroReferencia = "Sé - Centro",
            string camadaMapaPadrao = "temp_new")
        {
            TemperaturaAtual = Math.Round(temperaturaAtual, 1);
            UmidadeRelativa = Math.Clamp(umidadeRelativa, 0, 100);
            SensacaoTermica = sensacaoTermica.HasValue
                ? Math.Round(sensacaoTermica.Value, 1)
                : CalcularIndiceCalor(TemperaturaAtual, UmidadeRelativa);
            BairroReferencia = string.IsNullOrWhiteSpace(bairroReferencia) ? "Sé - Centro" : bairroReferencia.Trim();
            CamadaMapaPadrao = string.IsNullOrWhiteSpace(camadaMapaPadrao) ? "temp_new" : camadaMapaPadrao.Trim();
            UltimaAtualizacao = ultimaAtualizacao ?? DateTime.Now;
        }

        /// <summary>
        /// Calcula o Índice de Calor (Heat Index / Sensação Térmica Aparente) pela equação de Rothfusz (NOAA).
        /// </summary>
        public static double CalcularIndiceCalor(double temperaturaC, int umidadeRelativa)
        {
            double rh = Math.Clamp(umidadeRelativa, 0, 100);
            double tFahrenheit = (temperaturaC * 9.0 / 5.0) + 32.0;

            double hiSimples = 0.5 * (tFahrenheit + 61.0 + ((tFahrenheit - 68.0) * 1.2) + (rh * 0.094));
            double hiFahrenheit = hiSimples;

            if ((hiSimples + tFahrenheit) / 2.0 >= 80.0)
            {
                hiFahrenheit = -42.379
                    + 2.04901523 * tFahrenheit
                    + 10.14333127 * rh
                    - 0.22475541 * tFahrenheit * rh
                    - 0.00683783 * tFahrenheit * tFahrenheit
                    - 0.05481717 * rh * rh
                    + 0.00122874 * tFahrenheit * tFahrenheit * rh
                    + 0.00085282 * tFahrenheit * rh * rh
                    - 0.00000199 * tFahrenheit * tFahrenheit * rh * rh;

                if (rh < 13 && tFahrenheit is >= 80.0 and <= 112.0)
                {
                    hiFahrenheit -= ((13.0 - rh) / 4.0) * Math.Sqrt((17.0 - Math.Abs(tFahrenheit - 95.0)) / 17.0);
                }
                else if (rh > 85 && tFahrenheit is >= 80.0 and <= 87.0)
                {
                    hiFahrenheit += ((rh - 85.0) / 10.0) * ((87.0 - tFahrenheit) / 5.0);
                }
            }

            double hiCelsius = (hiFahrenheit - 32.0) * 5.0 / 9.0;
            return Math.Round(Math.Max(temperaturaC, hiCelsius), 1);
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
