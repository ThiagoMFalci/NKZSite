# 🎮 GUIA COMPLETO - KITSUNE ACADEMY

## 🚀 Como Começar a Usar

### 1. Iniciando o Servidor
```bash
cd /Users/igorparente/Projetos/NKZ/NKZSite/kitsune-academy
python3 -m http.server 8000
```

Então acesse: **http://localhost:8000**

---

## 🎯 FUNCIONALIDADES POR PÁGINA

### 📍 **HOME (index.html)**

#### Botões Interativos:
- **"COMEÇAR AGORA – GRATUITO"** → Abre modal de registro
- **"ENTRAR"** (Header) → Abre modal de login
- **"EXPLORAR RECURSOS"** → Botão secundário

#### Elementos Dinâmicos:
- Navbar com ícones (🏠 🏆 👥 📊)
- Animações ao fazer scroll
- Hover effects em botões

---

### 🏆 **TORNEIOS (tournaments.html)**

#### Funcionalidades:
1. **Filtrar Torneios**: Use FilterSystem para filtrar por status
   ```javascript
   FilterSystem.filterTournaments('active');  // Ativos
   FilterSystem.filterTournaments('running'); // Em andamento
   FilterSystem.filterTournaments('upcoming'); // Em breve
   ```

2. **Inscrever em Torneio**: Clique em "INSCREVER EQUIPE"
   - Abre modal com formulário
   - Pede nome do time e líder
   - Mostra confirmação de sucesso

3. **Ver Detalhes**: Clique em "VER RESULTADOS"
   - Exibe informações do torneio
   - Mostra status e prêmios
   - Botão para se inscrever

4. **Criar Torneio**: "CRIAR TORNEIO AGORA"
   - Abre modal de registro
   - Permite criar novo torneio

#### Torneios Disponíveis:
- 🔥 **Campeonato Brasileiro 2026** (Status: Ativo)
- 🔥 **Torneio Solo Ranking 2026** (Status: Em Andamento)
- ⭐ **Scrim League** (Status: Em Breve)

---

### 👥 **TIMES (teams.html)**

#### Funcionalidades:
1. **Buscar Times**: Use a função search
   ```javascript
   FilterSystem.searchTeams('Kitsune');
   ```

2. **Ordenar Times**: Sort por ranking ou win rate
   ```javascript
   FilterSystem.sortTeams('rank');    // Por ranking
   FilterSystem.sortTeams('wr');      // Por win rate
   ```

3. **Ver Detalhes do Time**: Clique em "VER TIME"
   - Mostra roster do time (5 jogadores)
   - Exibe estatísticas (W-L, WR%, Membros)
   - Botões para "Entrar" ou "Compartilhar"

4. **Criar Time**: Clique em "CRIAR TIME AGORA"
   - Abre modal de registro
   - Permite criar novo time

#### Times Disponíveis:
1. 👑 **KSN Esports** (#1) - 15-2 (94% WR)
2. 🔥 **ProFire Gaming** (#2) - 13-3 (81% WR)
3. ⭐ **Alpha Legends** (#3) - 12-4 (75% WR)
4. 🚀 **Elite Squad** (#4) - 10-5 (66% WR)
5. 🔥 **Apex Titans** (#5) - 9-6 (60% WR)
6. 🏆 **Shadow Legends** (#6) - 8-7 (53% WR)

---

### 📊 **DASHBOARD (dashboard.html)**

#### Seções:
1. **Perfil do Usuário**
   - Nome: Igor Parente
   - Rank: Diamante II
   - LP: 1245
   - Time: Kitsune Legends

2. **Estatísticas**
   - Partidas Jogadas: 127
   - Win Rate: 62%
   - Campeão Principal: Ahri
   - (Anima números ao carregar)

3. **Últimas Partidas**
   - Lista de 3 últimas partidas
   - Mostra resultado (V/D) e duração

4. **Próximos Torneios**
   - Campeonato Brasileiro 2026
   - Scrim League

5. **Ações Rápidas**
   - 🔧 Criar Torneio
   - 👥 Convidar Jogadores
   - ⚙️ Configurações

---

## 💻 TESTE DO CONSOLE

### Para testar as funcionalidades via console (F12 → Console):

#### 1. Mostrar Notificações
```javascript
NotificationSystem.show('Teste de Sucesso!', 'success');
NotificationSystem.show('Isso é um erro!', 'error');
NotificationSystem.show('Atenção!', 'warning');
NotificationSystem.show('Informação importante', 'info');
```

#### 2. Criar Modais
```javascript
// Modal simples
ModalSystem.create('Título', 'Conteúdo do modal', [
  { label: 'Fechar', type: 'secondary' }
]);

// Modal com ações
ModalSystem.create('Confirmação', 'Tem certeza?', [
  { label: 'Cancelar', type: 'secondary' },
  { label: 'Confirmar', type: 'primary' }
]);
```

#### 3. Abrir Modais Específicos
```javascript
openRegisterModal();          // Modal de registro
joinTournament(1);            // Inscrever em torneio
viewTeam('KSN Esports');      // Ver detalhes do time
viewTournament(1);            // Ver torneio
```

#### 4. Filtrar e Buscar
```javascript
FilterSystem.filterTournaments('active');
FilterSystem.searchTeams('Kitsune');
FilterSystem.sortTeams('rank');
```

#### 5. Alternar Tema
```javascript
toggleTheme();  // Alterna dark/light
```

#### 6. Animar Números
```javascript
const element = document.querySelector('[data-stat="matches"]');
DashboardInteractive.countUp(element, 0, 250, 2000);
```

---

## 🎨 EFEITOS VISUAIS

### Hover Effects
- Passe o mouse sobre qualquer card (Torneios, Times, Features)
- Verá: Elevação, escala e efeito glow

### Animações de Scroll
- Role a página
- Elementos aparecem com efeito de reveal
- Transição suave de 0.6s

### Tema Escuro/Claro
- Clique no ícone de lua/sol no header
- Tema muda instantaneamente
- Preferência salva em localStorage

### Notificações
- Aparecem no canto superior direito
- Fade in animation
- Auto-desaparecem após 3 segundos

### Modais
- Overlay com blur backdrop
- Slide up animation
- Clique fora para fechar
- Animações suaves de 0.3-0.4s

---

## 📱 RESPONSIVIDADE

Testado em breakpoints:
- ✅ 1024px (Tablets)
- ✅ 768px (iPad)
- ✅ 480px (Mobile)
- ✅ 360px (Small phones)

Todos os elementos se adaptam perfeitamente!

---

## 🔧 ESTRUTURA DE ARQUIVOS

```
kitsune-academy/
├── index.html              # Página inicial
├── tournaments.html        # Página de torneios
├── teams.html             # Página de times
├── dashboard.html         # Página do dashboard
├── style.css              # Estilos (1900+ linhas)
├── main.js                # Animações de scroll originais
├── interactive.js         # Sistema interativo (570+ linhas)
└── FUNCIONALIDADES.md     # Este arquivo
```

---

## 🎯 CHECKLIST DE FUNCIONALIDADES

### Sistema de Modais
- ✅ Modal overlay com blur
- ✅ Header com título e botão de fechar
- ✅ Body com conteúdo customizável
- ✅ Footer com ações
- ✅ Animação slide-up
- ✅ Fechar ao clicar fora

### Sistema de Notificações
- ✅ Toast notifications
- ✅ 4 tipos: success, error, warning, info
- ✅ Ícones automaticamente
- ✅ Auto-dismiss após 3s
- ✅ Posição fixa superior direita
- ✅ Animação slide-in

### Formulários
- ✅ Inputs com estilo neon
- ✅ Validação básica
- ✅ Focus glow effect
- ✅ Checkboxes estilizadas
- ✅ Responsive layout

### Animações
- ✅ Hover effects em cards
- ✅ Scroll reveal animations
- ✅ CountUp number animations
- ✅ Page transitions
- ✅ Theme toggle smooth

### Filtros
- ✅ Filtrar torneios por status
- ✅ Buscar times por nome
- ✅ Ordenar times por ranking/WR
- ✅ Feedback visual com notificações

### Sistema de Tema
- ✅ Toggle dark/light
- ✅ Persistência em localStorage
- ✅ Botão animado no header
- ✅ Transição suave

---

## 🐛 TROUBLESHOOTING

### Modais não aparecem
- Verifique se `interactive.js` está carregado
- Abra F12 → Console e procure por erros
- Tente chamar diretamente: `openRegisterModal()`

### Notificações não aparecem
- Certifique-se de que o DOM está carregado
- Verifique a posição no CSS (fixed, top, right)
- Tente: `NotificationSystem.show('Teste', 'info')`

### Efeitos de hover não funcionam
- Verifique se HoverEffects foi inicializado
- Procure por erros no console
- Verifique CSS transitions

### Tema não persiste
- Verifique localStorage habilitado
- Limpe cache do navegador
- Veja console para erros

---

## 📊 MÉTRICAS

- **Tamanho do interactive.js**: ~14KB
- **Linhas de código CSS adicionado**: 200+
- **Sistemas interativos implementados**: 10+
- **Modais customizáveis**: 3+
- **Tipos de animações**: 7+
- **Breakpoints responsivos**: 5+

---

## 🚀 DICAS DE USO

1. **Para Desenvolvedores**:
   - Inspect Element (F12) para ver estrutura DOM
   - Console para testar funções
   - Network tab para monitorar requisições

2. **Para Usuários**:
   - Explore todos os botões
   - Teste responsividade (F12 → Toggle Device)
   - Alterne tema escuro/claro
   - Verifique animações ao fazer scroll

3. **Performance**:
   - Site carrega muito rápido
   - Sem dependências externas de JS
   - Apenas CSS3 e Vanilla JS
   - FontAwesome icons via CDN

---

## 📞 SUPORTE

Para questões sobre as funcionalidades:
1. Verifique a documentação (este arquivo)
2. Abra console (F12)
3. Procure por mensagens de erro
4. Teste funções manualmente

---

**Versão**: 1.0  
**Status**: ✅ Completo e Testado  
**Última Atualização**: 2026  
**Desenvolvido por**: Kitsune Academy Team
