using System;
using System.Windows.Forms;
using GS_TermoAtiva_C_.Views;

namespace GS_TermoAtiva_C_
{
    internal static class Program
    {
        /// <summary>
        /// Ponto de entrada principal da aplicação TermoAtiva Totem.
        /// Inicializa a configuração visual de alta densidade de pixels (DPI)
        /// e executa a tela de inicialização e diagnóstico (SplashForm).
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new SplashForm());
        }
    }
}
