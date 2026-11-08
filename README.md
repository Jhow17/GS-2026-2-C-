# ☀️ TermoAtiva - Totem Urbano de Refúgio Climático (CitizenTech)

[![.NET](https://img.shields.io/badge/.NET-8.0%2B-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows Forms](https://img.shields.io/badge/UI-Windows%20Forms%20(Kiosk%20Mode)-0078D7?logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/winforms/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Layered%20(Models%20%26%20Views)-2ea44f)](#arquitetura-do-sistema)
[![Persistence](https://img.shields.io/badge/Persistence-Resilient%20Local%20JSON-orange)](#resiliencia-offline-e-persistencia-local-json)
[![Status](https://img.shields.io/badge/Status-Functional%20Prototype-success)]()

---

## 📌 Sumário
1. [Sobre o Projeto e Problema Social](#-sobre-o-projeto-e-problema-social)
2. [Público-Alvo e Personas Beneficiadas](#-público-alvo-e-personas-beneficiadas)
3. [Funcionalidades do Sistema](#-funcionalidades-do-sistema)
   - [Visão Pública (Munícipe)](#1-visão-pública-autoatendimento-do-munícipe)
   - [Modo Administrador (Operador Municipal)](#2-modo-administrador-restrito-operador-de-zeladoria)
4. [Arquitetura Orientada a Objetos (POO)](#-arquitetura-orientada-a-objetos-poo)
5. [Resiliência Offline e Persistência Local JSON](#-resiliência-offline-e-persistência-local-json)
6. [Design System de Alto Contraste Solar (Tema.cs)](#-design-system-de-alto-contraste-solar-temacs)
7. [Estrutura de Pastas e Diretórios](#-estrutura-de-pastas-e-diretórios)
8. [Instruções de Compilação e Execução](#-instruções-de-compilação-e-execução)
9. [Credenciais de Teste para Avaliação](#-credenciais-de-teste-para-avaliação)
10. [Integrantes da Equipe FIAP](#-integrantes-da-equipe-fiap)

---

## 🌍 Sobre o Projeto e Problema Social

As **ondas de calor extremo** tornaram-se desastres climáticos cada vez mais frequentes nos grandes centros urbanos devido ao avanço das mudanças climáticas e à formação de **ilhas de calor**. Em dias com sensações térmicas superiores a 40°C, a exposição contínua ao ar livre provoca:
- Choque térmico e exaustão por calor (*heat exhaustion*);
- Insolação e desidratação aguda;
- Pico de internações hospitalares e aumento da mortalidade de populações vulneráveis.

Embora existam alertas meteorológicos emitidos por aplicativos de smartphone, **milhões de cidadãos vivem em exclusão digital**: pessoas idosas sem familiaridade com celulares, trabalhadores braçais ao ar livre que não podem manusear aparelhos sob o sol, garis, entregadores e munícipes de baixa renda sem pacote de dados móveis ativo.

O **TermoAtiva** é uma solução de **CitizenTech** concebida como um **totem físico interativo (quiosque urbano)** instalado estrategicamente em vias públicas de grande circulação, praças, terminais de ônibus e centros comerciais. Ele atua como uma boia de salvação climática na cidade, oferecendo informação instantânea, geolocalização de pontos de alívio térmico gratuitos e zeladoria urbana participativa.

---

## 👥 Público-Alvo e Personas Beneficiadas

- **Garis, Varredores e Trabalhadores da Limpeza Urbana**: Profissionais expostos ao asfalto quente durante todo o expediente de trabalho, dependentes de bebedouros e áreas de sombra para hidratação frequente e pausas laborais;
- **Idosos e Aposentados**: Cidadãos com menor capacidade termorreguladora natural, alta suscetibilidade à queda de pressão e desidratação, frequentemente desprovidos de smartphones;
- **Entregadores de Aplicativo e Ciclistas**: Trabalhadores em deslocamento contínuo sob radiação solar que precisam reabastecer garrafas de água potável ao longo do dia;
- **Cidadãos em Situação de Vulnerabilidade / População em Trânsito**: Pessoas sem acesso a ambientes com ar-condicionado ou água refrigerada em suas residências.

---

## 🚀 Funcionalidades do Sistema

O sistema é dividido estritamente em duas jornadas funcionais: a **visão pública e acessível do munícipe** e o **painel restrito de gestão do operador municipal**.

### 1. Visão Pública (Autoatendimento do Munícipe)

- **Monitoramento Climático em Tempo Real (Cabeçalho do Menu)**:
  - Exibição de Temperatura Atual, Sensação Térmica e Umidade Relativa do Ar;
  - *Badge* luminoso dinâmico com classificação do risco térmico segundo protocolos da Defesa Civil e OMS;
  - Relógio digital com data e hora sincronizados.
- **Fluxo 1 — 🌡️ Alerta de Calor Agora (`AlertaForm`)**:
  - Detalhamento térmico em tipografia gigante e cores de advertência solar;
  - Lista de recomendações práticas de saúde (intervalos para hidratação obrigatória, cuidados com pele, identificação imediata de sintomas de insolação e desmaio);
  - Botão de toque de retorno ao menu.
- **Fluxo 2 — 💧 Onde Beber Água / Descansar (`PontosApoioForm`)**:
  - **Consulta pública SOMENTE LEITURA**: o munícipe não corre risco de alterar acidentalmente dados no totem;
  - Filtros rápidos por toque no topo: `[ Todos ]`, `[ 💧 Bebedouros ]`, `[ 🌳 Sombra / Descanso ]` e `[ 🏥 Postos Climatizados ]`;
  - Tabela com linhas amplas (52px de altura) informando Nome do Local, Categoria, Endereço Completo, Distância em Metros e Situação Operacional ("Disponível" ou "Em Manutenção").
- **Fluxo 3 — ⚠️ Informar Problema em Bebedouro (`OcorrenciaForm`)**:
  - Zeladoria urbana em **apenas 2 toques**, eliminando a necessidade de teclado virtual:
    1. Seleção do local em lista suspensa com fonte grande;
    2. Toque no botão de falha correspondente: `Sem Água`, `Torneira Quebrada`, `Água Suja / Imprópria` ou `Local Fechado`;
  - Gravação resiliente imediata no arquivo JSON de ocorrências com protocolo de confirmação.

### 2. Modo Administrador Restrito (Operador de Zeladoria)

- **Autenticação Segura com Teclado Numérico Virtual (`AdminAuthForm`)**:
  - Modal com proteção por PIN/Senha para operadores e técnicos municipais;
  - Teclado touchscreen numérico integrado (0 a 9, limpar e apagar) para facilitar a digitação física no quiosque;
  - Senha padrão de homologação: `1234`.
- **Painel Administrativo e Gestão de Recursos (`AdminPontosForm`)**:
  - **Cadastro de Novos Recursos**: inclusão de novos bebedouros, praças arborizadas ou UBS climatizadas com validação de campos obrigatórios;
  - **Gestão Operacional de Disponibilidade**: botão de toque para alternar rapidamente o status de um ponto entre `Disponível` e `Interditado / Em Manutenção` durante vistorias;
  - **Auditoria de Ocorrências**: aba dedicada com histórico em tempo real das notificações enviadas pela população, com carimbo de data, horário, local e problema detectado.

---

## 🏛️ Arquitetura Orientada a Objetos (POO)

O projeto adota uma arquitetura em duas camadas claras e desacopladas (`Models` e `Views`), sem o uso de banco de dados relacional ou bibliotecas pesadas de terceiros:

```
GS_TermoAtiva_C_
├── Models/
│   ├── TipoPontoApoio.cs (Enum: BebedouroPublico, AreaSombraDescanso, PostoSaudeClimatizado)
│   ├── PontoApoio.cs (Entidade de recurso urbano com regras de formatação amigável)
│   ├── AlertaClimatico.cs (Cálculo de risco térmico e catálogo de recomendações de saúde)
│   ├── OcorrenciaMunicipe.cs (Entidade de notificação de zeladoria aberta por cidadãos)
│   └── TotemRepository.cs (Persistência atômica JSON, seed offline e isolamento de I/O)
└── Views/
    ├── Tema.cs (Design System estático: paleta solar, tipografia touch e estilização)
    ├── SplashForm.cs (Inicialização diagnóstica e validação de persistência)
    ├── MenuForm.cs (Tela inicial pública do totem com botões gigantes e monitor climático)
    ├── AlertaForm.cs (Orientações de acolhimento térmico e hidratação)
    ├── PontosApoioForm.cs (Consulta pública de recursos próximos somente leitura)
    ├── OcorrenciaForm.cs (Relato ágil de falhas em pontos de água)
    ├── AdminAuthForm.cs (Modal de autenticação com teclado virtual touch)
    └── AdminPontosForm.cs (Painel de controle de recursos e ocorrências)
```

### Detalhamento das Classes de Domínio:

- **`PontoApoio`**: Encapsula as informações geolocalizadas do refúgio (`Id`, `Nome`, `Tipo`, `Endereco`, `DistanciaMetros`, `Disponivel`), provendo métodos como `ObterTipoFormatado()` e `FormatarExibicao()`.
- **`AlertaClimatico`**: Processa `TemperaturaAtual`, `SensacaoTermica` e `UmidadeRelativa`. O método `ClassificarNivelRisco()` implementa a lógica de negócio de triagem de gravidade térmica (< 32°C = Risco Moderado; 32°C a 38°C = Risco Alto; > 38°C = Risco Extremo) e `ObterRecomendacoes()` devolve instruções de proteção civil específicas para cada faixa.
- **`OcorrenciaMunicipe`**: Registra cada reporte com `Guid` único, `PontoApoioId`, `NomePontoApoio`, `TipoProblema`, `DataHora` e indicador de resolução.
- **`TotemRepository`**: Camada de persistência local thread-safe com mecanismo de bloqueio (`lock`), garantindo que leituras e escritas concorrentes em arquivos JSON ocorram sem corrupção.

---

## 💾 Resiliência Offline e Persistência Local JSON

Em catástrofes climáticas e apagões, redes de telefonia celular e conexões de internet costumam oscilar ou cair completamente. Um totem de emergência de saúde pública **não pode depender de servidores em nuvem ou bancos de dados externos centralizados**.

Por este motivo, o TermoAtiva utiliza o motor nativo **`System.Text.Json`**:
1. **Estrutura Local**:
   - `Data/pontos_apoio.json`: Armazena os pontos de hidratação e refúgio cadastrados.
   - `Data/ocorrencias.json`: Armazena o registro de falhas reportadas pelos munícipes.
2. **Seed Inicial Automático**: Caso os arquivos ainda não existam no primeiro boot, o `TotemRepository` cria automaticamente a pasta `Data/` e popula a base com **5 pontos de apoio realistas** (Praça da Sé, Parque Trianon, UBS Santa Cecília, Terminal Rodoviário Tietê e Largo do Arouche), deixando o terminal 100% pronto para uso imediato.
3. **Formatação Legível**: Salvamento com `WriteIndented = true` e suporte integral à codificação Unicode (acentuação em português).

---

## 🎨 Design System de Alto Contraste Solar (`Tema.cs`)

Quiosques urbanos operam sob iluminação natural severa e reflexos do sol. O `Tema.cs` implementa uma interface programática limpa (100% em código C#, sem uso de arquivos pesados `.Designer.cs`):

- **Paleta de Cores Anti-Reflexo**:
  - Fundo Principal: `Color.FromArgb(248, 249, 250)` (Tom claro gelo anti-ofuscamento);
  - Fundo de Cards: `Color.White`;
  - Texto Principal: `Color.FromArgb(17, 24, 39)` (Grafite escuro de alto contraste WCAG AAA);
  - Azul Institucional: `Color.FromArgb(15, 76, 129)`;
  - Vermelho Alerta: `Color.FromArgb(185, 28, 28)`;
  - Laranja Aviso: `Color.FromArgb(217, 119, 6)`;
  - Verde Sucesso: `Color.FromArgb(22, 101, 52)`.
- **Tipografia Ampliada (Segoe UI)**:
  - Destaques Térmicos: `42pt Bold`;
  - Títulos Principais: `26pt Bold`;
  - Botões de Toque: `18pt Bold`;
  - Corpo / Instruções: `15pt Regular`.
- **Modo Quiosque / Totem**: Janelas maximizadas sem borda do sistema operacional (`FormBorderStyle = None`, `WindowState = Maximized`).

---

## 📁 Estrutura de Pastas e Diretórios

```text
C:/Users/Jonathan Silva/FIAP/GS/C#/
├── .gitignore
├── integrantes.txt
├── README.md
└── GS-TermoAtiva-C#/
    ├── GS-TermoAtiva-C#.slnx
    └── GS-TermoAtiva-C#/
        ├── GS-TermoAtiva-C#.csproj
        ├── Program.cs
        ├── Data/
        │   ├── pontos_apoio.json
        │   └── ocorrencias.json
        ├── Models/
        │   ├── PontoApoio.cs
        │   ├── AlertaClimatico.cs
        │   ├── OcorrenciaMunicipe.cs
        │   └── TotemRepository.cs
        └── Views/
            ├── Tema.cs
            ├── SplashForm.cs
            ├── MenuForm.cs
            ├── AlertaForm.cs
            ├── PontosApoioForm.cs
            ├── OcorrenciaForm.cs
            ├── AdminAuthForm.cs
            └── AdminPontosForm.cs
```

---

## 🛠️ Instruções de Compilação e Execução

### Pré-requisitos
- **SDK do .NET 8.0 ou superior** instalado no ambiente Windows ([Download .NET](https://dotnet.microsoft.com/download)).
- Sistema Operacional: Windows 10 / 11 ou Windows Server com suporte a desktop Windows Forms.

### Passo a Passo via Terminal

1. Abra o terminal (PowerShell ou Prompt de Comando) e navegue até a raiz do repositório:
   ```powershell
   cd "C:\Users\Jonathan Silva\FIAP\GS\C#"
   ```

2. Restaure as dependências e compile o projeto:
   ```powershell
   dotnet build "GS-TermoAtiva-C#/GS-TermoAtiva-C#/GS-TermoAtiva-C#.csproj"
   ```
   *Certifique-se de que a compilação retorne `0 Erro(s)` e `0 Aviso(s)`.*

3. Execute a aplicação do Totem:
   ```powershell
   dotnet run --project "GS-TermoAtiva-C#/GS-TermoAtiva-C#/GS-TermoAtiva-C#.csproj"
   ```

4. Ao iniciar:
   - A tela de **Splash** será exibida por 2 segundos validando a integridade dos sensores e do banco local JSON;
   - Em seguida, o **Menu Principal em Modo Totem** será aberto em tela cheia com os botões gigantes de autoatendimento.

---

## 🔑 Credenciais de Teste para Avaliação

| Perfil | Mecanismo de Acesso | Credencial / PIN |
|---|---|---|
| **Munícipe / Trabalhador** | Acesso livre nos botões centrais do totem | *Não requer autenticação* |
| **Operador Municipal (Admin)** | Botão `[ 🔒 Acesso Operador ]` no rodapé | **Senha:** `1234` |

> *Nota: O modal de autenticação conta com teclado touchscreen virtual e aceita tanto cliques na tela quanto digitação física no teclado.*

---

## 👨‍💻 Integrantes da Equipe FIAP

- **Jonathan Henrique da Silva** — RM: `561139`
- **Gabrielly Candido Camargo da Silva** — RM: `560916`
- **João Víctor da Silva Ferreira** — RM: `560439`
- **Luíza Saraçol Ribeiro** — RM: `560200`

**Repositório Oficial no GitHub:**  
[https://github.com/Jhow17/GS-2026-2-C-.git](https://github.com/Jhow17/GS-2026-2-C-.git)
