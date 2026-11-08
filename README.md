# TermoAtiva - Totem Urbano de Refúgio Climático (CitizenTech)

[![.NET](https://img.shields.io/badge/.NET-8.0%2B-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows Forms](https://img.shields.io/badge/UI-Windows%20Forms%20%2B%20ReaLTaiizor-0078D7?logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/winforms/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Layered%20(Models%20%26%20Views)-2ea44f)](#arquitetura-orientada-a-objetos-poo)
[![Persistence](https://img.shields.io/badge/Persistence-Resilient%20Local%20JSON-orange)](#resiliencia-offline-e-persistencia-local-json)
[![API](https://img.shields.io/badge/Integration-ViaCEP%20REST%20API-009688)](#2-modo-administrador-restrito-operador-de-zeladoria)

---

## Sumário
1. [Sobre o Projeto e Problema Social](#sobre-o-projeto-e-problema-social)
2. [Público-Alvo e Personas Beneficiadas](#público-alvo-e-personas-beneficiadas)
3. [Funcionalidades do Sistema](#funcionalidades-do-sistema)
   - [Visão Pública (Munícipe)](#1-visão-pública-autoatendimento-do-munícipe)
   - [Modo Administrador (Operador Municipal)](#2-modo-administrador-restrito-operador-de-zeladoria)
4. [Arquitetura Orientada a Objetos (POO)](#arquitetura-orientada-a-objetos-poo)
5. [Resiliência Offline e Persistência Local JSON](#resiliência-offline-e-persistência-local-json)
6. [Design System de Alto Contraste Solar (Tema.cs + ReaLTaiizor)](#design-system-de-alto-contraste-solar-temacs--realtaiizor)
7. [Estrutura de Pastas e Diretórios](#estrutura-de-pastas-e-diretórios)
8. [Instruções de Compilação e Execução](#instruções-de-compilação-e-execução)
9. [Credenciais de Teste para Avaliação](#credenciais-de-teste-para-avaliação)
10. [Integrantes da Equipe FIAP](#integrantes-da-equipe-fiap)

---

## Sobre o Projeto e Problema Social

As **ondas de calor extremo** tornaram-se desastres climáticos cada vez mais frequentes nos grandes centros urbanos devido ao avanço das mudanças climáticas e à formação de **ilhas de calor**. Em dias com sensações térmicas superiores a 40°C, a exposição contínua ao ar livre provoca:
- Choque térmico e exaustão por calor (*heat exhaustion*);
- Insolação e desidratação aguda;
- Pico de internações hospitalares e aumento da mortalidade de populações vulneráveis.

Embora existam alertas meteorológicos emitidos por aplicativos de smartphone, **milhões de cidadãos vivem em exclusão digital**: pessoas idosas sem familiaridade com celulares, trabalhadores braçais ao ar livre que não podem manusear aparelhos sob o sol, garis, entregadores e munícipes de baixa renda sem pacote de dados móveis ativo.

O **TermoAtiva** é uma solução de **CitizenTech** concebida como um **totem físico interativo (quiosque urbano)** instalado estrategicamente em vias públicas de grande circulação, praças, terminais de ônibus e centros comerciais. Ele atua como uma rede de proteção climática na cidade, oferecendo informação instantânea, geolocalização de pontos de alívio térmico gratuitos e zeladoria urbana participativa.

---

## Público-Alvo e Personas Beneficiadas

- **Garis, Varredores e Trabalhadores da Limpeza Urbana**: Profissionais expostos ao asfalto quente durante todo o expediente de trabalho, dependentes de bebedouros e áreas de sombra para hidratação frequente e pausas laborais;
- **Idosos e Aposentados**: Cidadãos com menor capacidade termorreguladora natural, alta suscetibilidade à queda de pressão e desidratação, frequentemente desprovidos de smartphones;
- **Entregadores de Aplicativo e Ciclistas**: Trabalhadores em deslocamento contínuo sob radiação solar que precisam reabastecer garrafas de água potável ao longo do dia;
- **Cidadãos em Situação de Vulnerabilidade / População em Trânsito**: Pessoas sem acesso a ambientes com ar-condicionado ou água refrigerada em suas residências.

---

## Funcionalidades do Sistema

O sistema é dividido estritamente em duas jornadas funcionais: a **visão pública e acessível do munícipe** e o **painel restrito de gestão do operador municipal**.

### 1. Visão Pública (Autoatendimento do Munícipe)

- **Monitoramento Climático em Tempo Real (Cabeçalho do Menu)**:
  - Exibição de Temperatura Atual, Sensação Térmica e Umidade Relativa do Ar em blocos de telemetria;
  - Indicador oficial dinâmico com classificação do risco térmico segundo protocolos da Defesa Civil e OMS;
  - Relógio digital com data e hora sincronizados.
- **Módulo 01 — Alerta de Calor Agora (`AlertaForm`)**:
  - Detalhamento térmico em tipografia clara e cores de advertência solar;
  - Lista de recomendações práticas de saúde (intervalos para hidratação obrigatória, pausa laboral e identificação imediata de sintomas de insolação);
  - Botão de toque de retorno ao menu.
- **Módulo 02 — Onde Beber Água / Descansar (`PontosApoioForm`)**:
  - **Consulta pública SOMENTE LEITURA**: o munícipe não corre risco de alterar acidentalmente dados no totem;
  - Filtros rápidos por toque no topo: `Todos os Recursos`, `Bebedouros Públicos`, `Sombra e Descanso` e `Postos Climatizados`;
  - **Diferenciação Visual por Categoria**:
    - **Bebedouros Públicos**: Destaque sutil em tons de Azul;
    - **Áreas de Sombra / Descanso**: Destaque sutil em tons de Verde;
    - **Postos de Saúde Climatizados**: Destaque sutil em tons de Laranja / Âmbar;
  - Tabela exibindo Nome do Local, Categoria, Endereço Completo, Distância Estimada e Situação Operacional (`DISPONÍVEL` ou `EM MANUTENÇÃO`).
- **Módulo 03 — Mapa de Refúgio e Recursos (`MapaForm`)**:
  - Container de visualização geográfica com coordenadas de cobertura do totem, legenda cromática das categorias de refúgio e listagem georreferenciada dos pontos próximos (preparado para acoplamento do provedor `GMap.NET`).
- **Módulo 04 — Informar Problema em Bebedouro (`OcorrenciaForm`)**:
  - Seleção obrigatória em 2 toques (Local + Condição: `Sem Água`, `Torneira Quebrada`, `Água Suja / Imprópria` ou `Local Fechado`);
  - Campos **estritamente opcionais** para **Nome do Cidadão** e **Descrição Adicional do Problema (Multiline)**, permitindo tanto relatos anônimos rápidos quanto relatos detalhados;
  - Gravação resiliente imediata no arquivo JSON de ocorrências.

### 2. Modo Administrador Restrito (Operador de Zeladoria)

- **Autenticação Segura com Teclado Virtual Sob Demanda (`AdminAuthForm`)**:
  - Modal com autenticação de dois fatores locais: **Usuário / Identificador** (`admin`) e **Senha** (`1234`);
  - **Teclado Virtual Sob Demanda**: oculto por padrão (`Visible = false`) para privilegiar a limpeza visual, podendo ser exibido/ocultado a qualquer momento através do botão `[ Teclado Virtual ]`.
- **Painel Administrativo e Gestão de Recursos (`AdminPontosForm`)**:
  - **Integração Assíncrona com API ViaCEP**: ao digitar o CEP ou acionar `[ Buscar CEP ]`, o sistema consome `https://viacep.com.br/ws/{cep}/json/` via `HttpClient` e preenche automaticamente Logradouro/Rua, Bairro, Cidade e UF;
  - **Cálculo Automático de Distância**: a distância até o totem é calculada dinamicamente pelo domínio com base no endereço/CEP, eliminando erros de digitação manual;
  - **Gestão Operacional de Disponibilidade**: botão para alternar rapidamente o status de um ponto entre `DISPONÍVEL` e `INTERDITADO`;
  - **Auditoria de Ocorrências**: aba dedicada com histórico em tempo real das notificações enviadas pela população, incluindo relator e descrição detalhada.

---

## Arquitetura Orientada a Objetos (POO)

O projeto adota uma arquitetura em duas camadas claras e desacopladas (`Models` e `Views`):

```text
GS_TermoAtiva_C_
├── Models/
│   ├── PontoApoio.cs (Entidade com endereço decomposto CEP/Logradouro/Numero/Bairro/Cidade/UF e cálculo de distância)
│   ├── AlertaClimatico.cs (Cálculo de risco térmico e catálogo de recomendações de saúde)
│   ├── OcorrenciaMunicipe.cs (Entidade de zeladoria com suporte opcional a NomeMunicipe e DescricaoDetalhada)
│   └── TotemRepository.cs (Persistência atômica JSON, seed offline e isolamento de I/O)
└── Views/
    ├── Tema.cs (Design System: integração ReaLTaiizor, KioskCardPanel, KioskActionTile e cabeçalhos padronizados)
    ├── SplashForm.cs (Inicialização diagnóstica com HopeProgressBar)
    ├── MenuForm.cs (Dashboard público em grade 2x2 com telemetria climática)
    ├── AlertaForm.cs (Orientações de acolhimento térmico e hidratação)
    ├── PontosApoioForm.cs (Consulta pública com diferenciação cromática por categoria)
    ├── MapaForm.cs (Visualização geográfica e legenda de recursos)
    ├── OcorrenciaForm.cs (Relato ágil de falhas com campos opcionais de cidadão e descrição)
    ├── AdminAuthForm.cs (Modal de login Usuário/Senha com teclado virtual sob demanda)
    └── AdminPontosForm.cs (Painel administrativo com integração ViaCEP e auditoria)
```

---

## Resiliência Offline e Persistência Local JSON

O TermoAtiva utiliza o motor nativo **`System.Text.Json`** para operar mesmo sem conectividade externa:
1. **`Data/pontos_apoio.json`**: Armazena os pontos de hidratação e refúgio com endereço estruturado.
2. **`Data/ocorrencias.json`**: Armazena o registro de falhas reportadas pelos munícipes.
3. **Seed Inicial Automático**: Caso os arquivos não existam no primeiro boot, o `TotemRepository` cria automaticamente a pasta `Data/` e popula a base com **5 pontos de apoio realistas** em São Paulo.

---

## Design System de Alto Contraste Solar (`Tema.cs` + `ReaLTaiizor`)

- **Biblioteca Visual `ReaLTaiizor`**: Uso de botões arredondados (`ParrotButton`), barras de progresso (`HopeProgressBar`) e painéis customizados (`KioskCardPanel` e `KioskActionTile`) com renderização GDI+ anti-aliased.
- **Cabeçalhos Padronizados Sem Truncamento**: Todos os formulários utilizam `Tema.CriarCabecalhoPadrao(...)` com dimensionamento estruturado em `TableLayoutPanel`, garantindo que nenhum título seja cortado em diferentes resoluções ou escalas de DPI.
- **Interface Tipográfica Pura (Zero Emojis)**: Todos os elementos visuais utilizam hierarquia tipográfica, indicadores numéricos e cores semânticas institucionais, sem uso de caracteres de emoji.

---

## Estrutura de Pastas e Diretórios

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
            ├── MapaForm.cs
            ├── OcorrenciaForm.cs
            ├── AdminAuthForm.cs
            └── AdminPontosForm.cs
```

---

## Instruções de Compilação e Execução

```powershell
# 1. Acessar a raiz do repositório
cd "C:\Users\Jonathan Silva\FIAP\GS\C#"

# 2. Compilar a Solution (0 erros, 0 avisos)
dotnet build "GS-TermoAtiva-C#/GS-TermoAtiva-C#.slnx"

# 3. Executar a aplicação do Totem
dotnet run --project "GS-TermoAtiva-C#/GS-TermoAtiva-C#/GS-TermoAtiva-C#.csproj"
```

---

## Credenciais de Teste para Avaliação

| Perfil | Mecanismo de Acesso | Usuário | Senha |
|---|---|---|---|
| **Munícipe / Trabalhador** | Acesso direto nos 4 módulos centrais do totem | *Livre* | *Livre* |
| **Operador Municipal (Admin)** | Botão `[ Acesso Operador ]` no rodapé | **`admin`** | **`1234`** |

---

## Integrantes da Equipe FIAP

- **Jonathan Henrique da Silva** — RM: `561139`
- **Gabrielly Candido Camargo da Silva** — RM: `560916`
- **João Víctor da Silva Ferreira** — RM: `560439`
- **Luíza Saraçol Ribeiro** — RM: `560200`

**Repositório Oficial no GitHub:**  
[https://github.com/Jhow17/GS-2026-2-C-.git](https://github.com/Jhow17/GS-2026-2-C-.git)
