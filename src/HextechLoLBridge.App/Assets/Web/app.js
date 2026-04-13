const state = {
  app: null,
  polling: null,
  snapshot: null,
  logs: [],
  sdkStatus: null,
  profile: { spellThemes: [], keyMappings: [], keyboardKeys: [], heroThemes: [] },
  heroSearch: '',
  activeSectionId: 'section-overview',
  selectedActionId: 'ability-q-ready'
};

const $ = (id) => document.getElementById(id);

function postMessage(type, payload = {}) {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type, payload });
  }
}

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function formatTime(seconds) {
  const safeSeconds = Number.isFinite(seconds) ? Math.max(0, Math.floor(seconds)) : 0;
  const mins = Math.floor(safeSeconds / 60).toString().padStart(2, '0');
  const secs = (safeSeconds % 60).toString().padStart(2, '0');
  return `${mins}:${secs}`;
}

function formatDateTime(value) {
  if (!value) return '--';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '--' : date.toLocaleString('zh-CN');
}

function percentToWidth(value) {
  const percentage = Math.max(0, Math.min(100, Math.round((value || 0) * 100)));
  return `${percentage}%`;
}

function setText(id, value, fallback = '--') {
  const el = $(id);
  if (el) el.textContent = value ?? fallback;
}

function shortActionLabel(item) {
  switch (item.actionId) {
    case 'ability-q-ready': return 'Q';
    case 'ability-w-ready': return 'W';
    case 'ability-e-ready': return 'E';
    case 'ability-r-ready': return 'R';
    case 'summoner-1-ready': return 'D';
    case 'summoner-2-ready': return 'F';
    case 'ready-check-accept': return '空格';
    default: return item.displayName.slice(0, 2);
  }
}

function themeTestButton(hex, label) {
  return `<button class="action-btn tiny cyan" type="button" data-light-hex="${escapeHtml(hex)}" data-light-label="${escapeHtml(label)}">测试灯光</button>`;
}

function isAbilityReadyCompat(ability) {
  if (!ability || !ability.isLearned) return false;
  const cooldownKnown = ability.cooldownSeconds != null;
  const cooldownReady = !cooldownKnown || Number(ability.cooldownSeconds) <= 0.01;
  if (!cooldownReady) return false;
  return ability.isReady === true || ability.isReady == null;
}

function abilityStatusText(ability) {
  if (!ability?.isLearned) return '未学习';
  if (ability.cooldownSeconds != null && Number(ability.cooldownSeconds) > 0.01) return '冷却中';
  if (ability.isReady === true) return '可用';
  if (ability.isReady === false) return '不可用';
  return '兼容判定可用';
}

function isSpellReadyCompat(spell) {
  if (!spell) return false;
  if (spell.cooldownSeconds != null && Number(spell.cooldownSeconds) > 0.01) return false;
  return spell.isReady === true || spell.isReady == null || (spell.cooldownSeconds != null && Number(spell.cooldownSeconds) <= 0.01);
}

function spellStatusText(spell) {
  if (!spell) return '客户端阶段';
  if (spell.cooldownSeconds != null && Number(spell.cooldownSeconds) > 0.01) return '冷却中';
  if (spell.isReady === true) return '可用';
  if (spell.isReady === false) return '不可用';
  return '兼容判定可用';
}

function getReadyActionIds() {
  const ready = new Set();
  const snapshot = state.snapshot;
  const player = snapshot?.activePlayer;
  const clientPhase = snapshot?.clientPhase;

  if (snapshot?.inGame && player) {
    const abilityMap = {
      Q: 'ability-q-ready',
      W: 'ability-w-ready',
      E: 'ability-e-ready',
      R: 'ability-r-ready'
    };

    (player.abilities || []).forEach((ability) => {
      const actionId = abilityMap[ability.slot];
      if (!actionId) return;
      const readyFlag = isAbilityReadyCompat(ability);
      if (readyFlag) ready.add(actionId);
    });

    (player.summonerSpells || []).forEach((spell, index) => {
      const actionId = index === 0 ? 'summoner-1-ready' : index === 1 ? 'summoner-2-ready' : null;
      if (!actionId) return;
      const readyFlag = isSpellReadyCompat(spell);
      if (readyFlag) ready.add(actionId);
    });
  }

  if (clientPhase?.canAcceptReadyCheck) {
    ready.add('ready-check-accept');
  }

  if (!snapshot?.inGame && clientPhase?.isChampSelect) {
    if ((clientPhase.summonerSpells || []).length >= 1) ready.add('summoner-1-ready');
    if ((clientPhase.summonerSpells || []).length >= 2) ready.add('summoner-2-ready');
  }

  return ready;
}



function normalizeHeroKey(value) {
  return String(value || '')
    .replaceAll(/['\s\-·・.&]/g, '')
    .trim()
    .toLowerCase();
}

function getCurrentHeroTheme() {
  const current = state.snapshot?.activePlayer?.championName || '';
  const themes = state.profile?.heroThemes || [];
  const normalizedCurrent = normalizeHeroKey(current);
  return themes.find((item) => normalizeHeroKey(item.championKey) === normalizedCurrent
    || normalizeHeroKey(item.displayName) === normalizedCurrent
    || String(item.searchText || '').split(/\s+/).some((token) => normalizeHeroKey(token) === normalizedCurrent)) || null;
}

function getCurrentHeroThemeHex() {
  return getCurrentHeroTheme()?.currentHex || '#3AA8FF';
}

function getCurrentHeroDisplayName() {
  return getCurrentHeroTheme()?.displayName || state.snapshot?.activePlayer?.championName || '--';
}

function getCurrentSummonerHex(index) {
  const inGameSpell = state.snapshot?.activePlayer?.summonerSpells?.[index];
  if (inGameSpell?.themeHex) return inGameSpell.themeHex;
  const champSpell = state.snapshot?.clientPhase?.summonerSpells?.[index];
  return champSpell?.themeHex || '#C8AA6E';
}

function buildKeyboardPreviewColors() {
  const colors = new Map();
  const snapshot = state.snapshot;
  const player = snapshot?.activePlayer;
  const heroHex = getCurrentHeroThemeHex();
  const readyActions = getReadyActionIds();
  const mappings = state.profile?.keyMappings || [];
  const setColor = (keyCode, hex) => { if (keyCode && hex) colors.set(keyCode, hex); };

  [
    'ESC','F1','F2','F3','F4','F5','F6','F7','F8','F9','F10','F11','F12',
    'PRNTSCR','SCROLLLOCK','PAUSEBREAK','GRAVE','TAB','CAPSLOCK','BACKSPACE','LSHIFT','RSHIFT'
  ].forEach((keyCode) => setColor(keyCode, heroHex));

  if (snapshot?.inGame && player) {
    const healthSegments = Math.max(0, Math.min(12, Math.ceil(Number(player.healthPercent || 0) * 12)));
    ['1','2','3','4','5','6','7','8','9','0','MINUS','EQUALS'].slice(0, healthSegments).forEach((keyCode) => setColor(keyCode, '#42E35D'));

    const resourceType = String(player.resourceType || '').toUpperCase();
    const hasResource = Number(player.maxResource || 0) > 0 && !['NONE', '无', '-'].includes(resourceType);
    const manaSegments = hasResource ? Math.max(0, Math.min(10, Math.ceil(Number(player.resourcePercent || 0) * 10))) : 0;
    ['Z','X','C','V','B','N','M','COMMA','PERIOD','SLASH'].slice(0, manaSegments).forEach((keyCode) => setColor(keyCode, '#39A9FF'));
  }

  mappings.forEach((item) => {
    if (!readyActions.has(item.actionId)) return;
    let hex = heroHex;
    if (item.actionId === 'summoner-1-ready') hex = getCurrentSummonerHex(0);
    else if (item.actionId === 'summoner-2-ready') hex = getCurrentSummonerHex(1);
    else if (item.actionId === 'ready-check-accept') hex = '#FFFFFF';
    setColor(item.currentKey, hex);
  });

  return colors;
}

function renderApp() {
  if (!state.app) return;
  setText('versionBadge', state.app.version);
  setText('runtimeInfo', `${state.app.framework} / ${state.app.runtimeDescription}`);
}

function renderPolling() {
  const polling = state.polling || {};
  setText('pollingState', polling.state || '--');
  setText('pollCount', String(polling.pollCount ?? 0));
  setText('lastSuccess', formatDateTime(polling.lastSuccessfulPollAt));
}

function renderClientPhase(clientPhase) {
  const phaseName = clientPhase?.phaseDisplayName || '客户端未连接';
  setText('clientPhaseBadge', phaseName);
  setText('liveClientPhaseBadge', phaseName);
  setText('readyCheckState', clientPhase?.readyCheckState || '--');
  setText('liveReadyCheckState', clientPhase?.readyCheckState || '--');
  setText('readyCheckPlayerResponse', clientPhase?.readyCheckPlayerResponse || '--');
  setText('liveReadyCheckResponse', clientPhase?.readyCheckPlayerResponse || '--');
  const readyTimerText = clientPhase?.readyCheckTimerSeconds != null ? `${clientPhase.readyCheckTimerSeconds}s` : '--';
  setText('readyCheckTimer', readyTimerText);
  setText('liveReadyCheckTimer', readyTimerText);
  setText('readyCheckActionHint', clientPhase?.canAcceptReadyCheck ? '可按空格 / 按钮' : '当前不可接受');
  setText('champSelectPhase', clientPhase?.champSelectTimerPhase || '--');
  setText('liveChampPhase', clientPhase?.champSelectTimerPhase || '--');
  const champTimerText = clientPhase?.champSelectCountdownSeconds != null ? `${clientPhase.champSelectCountdownSeconds}s` : '--';
  setText('champSelectTimer', champTimerText);
  setText('liveChampTimer', champTimerText);
  const spellSummary = (clientPhase?.summonerSpells || []).map((x) => x.displayName).join(' / ') || '--';
  setText('champSelectSpellsSummary', spellSummary);
  const acceptBtn = $('acceptReadyBtn');
  if (acceptBtn) {
    acceptBtn.disabled = !clientPhase?.canAcceptReadyCheck;
  }
}

function renderRecommendation(recommendation) {
  const hasPoint = !!recommendation?.hasAvailableSkillPoint;
  setText('recommendSlot', recommendation?.slot || '-');
  setText('recommendTitle', recommendation?.displayName || '暂无推荐');
  setText('recommendReason', recommendation?.reason || '等待局内状态。');
}

function renderAbilities(abilities) {
  const root = $('abilitiesGrid');
  if (!root) return;
  if (!abilities.length) {
    root.className = 'ability-grid empty-state';
    root.innerHTML = '暂无技能数据';
    return;
  }

  root.className = 'ability-grid';
  root.innerHTML = abilities.filter((item) => item.slot !== 'Passive').map((item) => {
    const status = abilityStatusText(item);

    return `
      <div class="ability-box ${status !== '冷却中' && status !== '未学习' && status !== '不可用' ? 'active' : ''}">
        <div class="ability-slot-box">${escapeHtml(item.slot)}</div>
        <div class="ability-title">${escapeHtml(item.displayName || item.slot)}</div>
        <div class="muted-line">等级：${item.abilityLevel ?? 0}</div>
        <div class="muted-line">状态：${status}</div>
        <div class="muted-line">CD：${item.cooldownSeconds == null ? '--' : `${Number(item.cooldownSeconds).toFixed(1)}s`}</div>
      </div>
    `;
  }).join('');
}

function renderSummonerSpellList(rootId, spells, emptyText) {
  const root = $(rootId);
  if (!root) return;
  if (!spells.length) {
    root.className = 'spell-live-list empty-state';
    root.innerHTML = emptyText;
    return;
  }

  root.className = 'spell-live-list';
  root.innerHTML = spells.map((item, index) => {
    const icon = item.iconUrl
      ? `<img class="spell-icon" src="${escapeHtml(item.iconUrl)}" alt="${escapeHtml(item.displayName || item.slot)}" />`
      : `<div class="spell-icon fallback">${escapeHtml((item.displayName || item.slot || '?').slice(0, 1))}</div>`;
    const readyText = spellStatusText(item);

    return `
      <div class="spell-live-card ${readyText !== '冷却中' ? 'active' : ''}">
        <div class="spell-live-top">
          ${icon}
          <div>
            <div class="spell-live-slot">${index === 0 ? 'D 默认位' : 'F 默认位'} · ${escapeHtml(item.slot)}</div>
            <div class="spell-live-name">${escapeHtml(item.displayName || '--')}</div>
          </div>
        </div>
        <div class="theme-row">
          <span class="theme-dot" style="background:${escapeHtml(item.themeHex || '#6C7A89')}"></span>
          <span>${escapeHtml(item.themeLabel || '默认冷灰')}</span>
          <code>${escapeHtml(item.themeHex || '#6C7A89')}</code>
        </div>
        <div class="muted-line">状态：${readyText}${item.cooldownSeconds != null ? ` · CD ${Number(item.cooldownSeconds).toFixed(1)}s` : ''}</div>
        <div class="muted-line">${escapeHtml(item.themeNote || '')}</div>
      </div>
    `;
  }).join('');
}

function renderSpellThemeSettings() {
  const root = $('spellThemeList');
  const items = state.profile?.spellThemes || [];
  if (!root) return;
  if (!items.length) {
    root.className = 'spell-setting-list empty-state';
    root.innerHTML = '暂无技能色表';
    return;
  }

  root.className = 'spell-setting-list';
  root.innerHTML = items.map((item) => `
    <div class="spell-setting-row">
      <div class="spell-setting-skill">
        <img class="spell-icon large" src="${escapeHtml(item.iconUrl)}" alt="${escapeHtml(item.displayName)}" />
        <div>
          <div class="spell-setting-name">${escapeHtml(item.displayName)}${item.isExtension ? ' <span class="preview-tag">扩展</span>' : ''}</div>
          <div class="muted-line">默认 ${escapeHtml(item.defaultHex)} · ${escapeHtml(item.themeLabel)}</div>
        </div>
      </div>
      <div>
        <input class="color-wheel" type="color" value="${escapeHtml(item.currentHex)}" data-spell-id="${escapeHtml(item.spellId)}" data-role="color-picker" />
      </div>
      <div>
        <input class="hex-input" type="text" value="${escapeHtml(item.currentHex)}" data-spell-id="${escapeHtml(item.spellId)}" data-role="hex-input" maxlength="7" />
      </div>
      <div>
        <button class="action-btn ghost small" type="button" data-reset-spell-id="${escapeHtml(item.spellId)}">恢复默认</button>
      </div>
      <div class="row-actions">
        <span class="theme-dot large" style="background:${escapeHtml(item.currentHex)}"></span>
        ${themeTestButton(item.currentHex, item.displayName)}
      </div>
    </div>
  `).join('');
}

function renderHeroThemeSettings() {
  const root = $('heroThemeList');
  const items = state.profile?.heroThemes || [];
  if (!root) return;

  const keyword = (state.heroSearch || '').trim().toLowerCase();
  const filtered = keyword
    ? items.filter((item) => (item.searchText || `${item.displayName} ${item.championKey}`).toLowerCase().includes(keyword))
    : items;

  if (!filtered.length) {
    root.className = 'spell-setting-list empty-state';
    root.innerHTML = '没有匹配到英雄';
    return;
  }

  root.className = 'spell-setting-list';
  root.innerHTML = filtered.map((item) => `
    <div class="spell-setting-row">
      <div class="spell-setting-skill">
        ${item.iconUrl ? `<img class="spell-icon large" src="${escapeHtml(item.iconUrl)}" alt="${escapeHtml(item.displayName)}" />` : `<div class="spell-icon fallback large">${escapeHtml((item.displayName || '?').slice(0, 1))}</div>`}
        <div>
          <div class="spell-setting-name">${escapeHtml(item.displayName)}</div>
          <div class="muted-line">默认 ${escapeHtml(item.defaultHex)} · ${escapeHtml(item.themeLabel || '英雄主题色')}</div>
        </div>
      </div>
      <div>
        <input class="color-wheel" type="color" value="${escapeHtml(item.currentHex)}" data-champion-key="${escapeHtml(item.championKey)}" data-role="hero-color-picker" />
      </div>
      <div>
        <input class="hex-input" type="text" value="${escapeHtml(item.currentHex)}" data-champion-key="${escapeHtml(item.championKey)}" data-role="hero-hex-input" maxlength="7" />
      </div>
      <div>
        <button class="action-btn ghost small" type="button" data-reset-champion-key="${escapeHtml(item.championKey)}">恢复默认</button>
      </div>
      <div class="row-actions">
        <span class="theme-dot large" style="background:${escapeHtml(item.currentHex)}"></span>
        ${themeTestButton(item.currentHex, `${item.displayName} 主题色`)}
      </div>
    </div>
  `).join('');
}

function renderBuffs(player) {
  const buffs = player.activeBuffs || [];
  setText('buffDataBadge', player.buffDataAvailable ? '已检测到 Buff 字段' : '未读到稳定 Buff 字段');
  setText('baronBuffState', player.hasBaronBuff ? '激活' : (player.buffDataAvailable ? '未激活' : '未知'));

  const root = $('buffsList');
  if (!root) return;
  if (!buffs.length) {
    root.className = 'stack-list empty-state';
    root.innerHTML = player.buffDataAvailable ? '当前没有检测到 Buff' : '暂无 Buff 数据';
    return;
  }

  root.className = 'stack-list';
  root.innerHTML = buffs.map((item) => `
    <div class="stack-item">
      <div class="stack-head">
        <strong>${escapeHtml(item.displayName || '未知 Buff')}</strong>
        <span>${escapeHtml(item.category || 'general')}</span>
      </div>
      <div class="muted-line">标识：${escapeHtml(item.rawId || '--')}</div>
      <div class="muted-line">层数：${item.count ?? 0}${item.durationSeconds != null ? ` · 持续：${Number(item.durationSeconds).toFixed(1)}s` : ''}</div>
    </div>
  `).join('');
}

function renderObjectives(objectives) {
  const list = [objectives?.dragon, objectives?.elderDragon, objectives?.baron, objectives?.riftHerald].filter(Boolean);
  const root = $('objectivesGrid');
  if (!root) return;
  if (!list.length) {
    root.className = 'objective-grid empty-state';
    root.innerHTML = '暂无史诗野怪数据';
    return;
  }

  root.className = 'objective-grid';
  root.innerHTML = list.map((item) => `
    <div class="objective-box">
      <div class="objective-title">${escapeHtml(item.displayName || '--')}</div>
      <div class="objective-score-row">
        <span class="side-pill blue">蓝方 ${item.orderCount ?? 0}</span>
        <span class="side-pill red">红方 ${item.chaosCount ?? 0}</span>
      </div>
      <div class="muted-line">${escapeHtml(item.latestSummary || '暂无')}</div>
      <div class="muted-line">${item.lastEventTimeSeconds == null ? '尚无时间' : `最近时间：${formatTime(item.lastEventTimeSeconds)}`}</div>
    </div>
  `).join('');
}

function renderEvents(events) {
  const root = $('eventsList');
  if (!root) return;
  if (!events.length) {
    root.className = 'stack-list empty-state';
    root.innerHTML = '暂无事件';
    return;
  }

  root.className = 'stack-list';
  root.innerHTML = events.map((item) => `
    <div class="stack-item">
      <div class="stack-head">
        <strong>${escapeHtml(item.summary || item.eventName || '未知事件')}</strong>
        <span>#${item.eventId}</span>
      </div>
      <div class="muted-line">发生时间：${formatTime(item.eventTimeSeconds)}</div>
    </div>
  `).join('');
}

function renderNotes(notes) {
  const root = $('notesList');
  if (!root) return;
  if (!notes.length) {
    root.className = 'stack-list empty-state';
    root.innerHTML = '暂无备注';
    return;
  }
  root.className = 'stack-list';
  root.innerHTML = notes.map((item) => `<div class="stack-item">${escapeHtml(item)}</div>`).join('');
}

function renderLogs() {
  const root = $('logsList');
  const logs = state.logs || [];
  if (!root) return;
  if (!logs.length) {
    root.className = 'stack-list empty-state';
    root.innerHTML = '暂无日志';
    return;
  }

  root.className = 'stack-list';
  root.innerHTML = logs.slice().reverse().map((item) => `
    <div class="stack-item">
      <div class="stack-head">
        <strong>${escapeHtml(item.level)}</strong>
        <span>${formatDateTime(item.timestamp)}</span>
      </div>
      <div>${escapeHtml(item.message)}</div>
    </div>
  `).join('');
}

function renderSdkStatus() {
  const sdk = state.sdkStatus || {};
  setText('sdkStateBadge', sdk.adapterState || 'idle');
  setText('sdkDllFound', sdk.dllFound ? '是' : '否');
  setText('sdkInitialized', sdk.isInitialized ? '是' : '否');
  setText('sdkProbePath', sdk.dllProbePath || '--');
  setText('sdkActiveEffect', sdk.activeEffect || '--');
  setText('sdkLastHex', sdk.activeHex || '--');
  setText('sdkLastAppliedAt', formatDateTime(sdk.lastAppliedAt));
  setText('sdkMessageBox', sdk.message || '尚未尝试初始化 Logitech LED SDK。');
}

function renderActionPills() {
  const root = $('mappingActionPills');
  const mappings = state.profile?.keyMappings || [];
  if (!root) return;
  if (!mappings.length) {
    root.className = 'action-pill-wrap empty-state';
    root.innerHTML = '暂无映射动作';
    return;
  }

  root.className = 'action-pill-wrap';
  root.innerHTML = mappings.map((item) => `
    <button class="action-pill ${state.selectedActionId === item.actionId ? 'active' : ''}" type="button" data-action-id="${escapeHtml(item.actionId)}" style="--accent:${escapeHtml(item.accentHex)}">
      <span class="action-pill-title">${escapeHtml(item.displayName)}</span>
      <span class="action-pill-meta">当前 ${escapeHtml(item.currentKey)}</span>
    </button>
  `).join('');
}

function renderKeyMappingTable() {
  const root = $('keyMappingTable');
  const items = state.profile?.keyMappings || [];
  if (!root) return;
  if (!items.length) {
    root.className = 'stack-list empty-state';
    root.innerHTML = '暂无映射';
    return;
  }

  root.className = 'stack-list';
  root.innerHTML = items.map((item) => `
    <div class="stack-item mapping-row-item">
      <div>
        <strong>${escapeHtml(item.displayName)}</strong>
        <div class="muted-line">默认 ${escapeHtml(item.defaultKey)} · 预览会实时跟随当前灯效</div>
      </div>
      <div class="mapping-row-right">
        <code>${escapeHtml(item.currentKey)}</code>
        <button class="action-btn ghost tiny" type="button" data-reset-action-id="${escapeHtml(item.actionId)}">恢复默认</button>
      </div>
    </div>
  `).join('');
}

function renderKeyboard() {
  renderActionPills();
  renderKeyMappingTable();

  const root = $('keyboardGrid');
  const keys = state.profile?.keyboardKeys || [];
  const mappings = state.profile?.keyMappings || [];
  if (!root) return;
  if (!keys.length) {
    root.className = 'keyboard-grid empty-state';
    root.innerHTML = '暂无键盘布局';
    return;
  }

  const readyActions = getReadyActionIds();
  const previewColors = buildKeyboardPreviewColors();
  const mappedByKey = new Map();
  mappings.forEach((item) => {
    const key = item.currentKey;
    if (!mappedByKey.has(key)) mappedByKey.set(key, []);
    mappedByKey.get(key).push(item);
  });

  const rows = [...new Set(keys.map((item) => item.row))].sort((a, b) => a - b);
  root.className = 'keyboard-grid';
  root.innerHTML = rows.map((row) => {
    const rowKeys = keys.filter((item) => item.row === row);
    return `
      <div class="keyboard-row">
        ${rowKeys.map((key) => {
          const actions = mappedByKey.get(key.keyCode) || [];
          const isReady = actions.some((action) => readyActions.has(action.actionId));
          const previewHex = previewColors.get(key.keyCode);
          const classes = ['keyboard-key'];
          if (isReady) classes.push('ready');
          if (previewHex) classes.push('preview-lit');
          if (state.selectedActionId && actions.some((a) => a.actionId === state.selectedActionId)) classes.push('selected-target');
          return `
            <button class="${classes.join(' ')}" type="button" data-key-code="${escapeHtml(key.keyCode)}" style="--w:${key.widthUnits};${previewHex ? `--preview:${escapeHtml(previewHex)};` : ''}">
              <span class="keyboard-key-label">${escapeHtml(key.displayName)}</span>
              <span class="key-badge-wrap">
                ${actions.map((item) => `<span class="key-badge" style="--accent:${escapeHtml(item.accentHex)}">${escapeHtml(shortActionLabel(item))}</span>`).join('')}
              </span>
            </button>
          `;
        }).join('')}
      </div>
    `;
  }).join('');
}

function renderSnapshot() {
  const snapshot = state.snapshot;
  if (!snapshot) return;

  setText('connectionBadge', snapshot.connectionState || '--');
  setText('inGameBadge', snapshot.inGame ? '局内已连接' : (snapshot.clientPhase?.phaseDisplayName || '未进游戏'));
  setText('mapName', snapshot.game?.mapName || '--');
  setText('gameMode', snapshot.game?.gameMode || '--');
  setText('mapTerrain', snapshot.game?.mapTerrain || '--');
  setText('gameTime', formatTime(snapshot.game?.gameTimeSeconds || 0));

  const player = snapshot.activePlayer || {};
  const riotId = player.riotId && player.riotId !== '-'
    ? player.riotId
    : `${player.riotIdGameName || '-'}#${player.riotIdTagLine || '-'}`;

  setText('riotId', riotId);
  setText('championName', getCurrentHeroDisplayName());
  setText('playerLevel', String(player.level ?? 0));
  setText('deathBadge', player.isDead ? `已死亡 ${Math.ceil(player.respawnTimerSeconds || 0)}s` : '存活');
  setText('skillPointBadge', `可分配技能点 ${player.skillPointsAvailable ?? 0}`);
  setText('healthText', `${Math.round(player.currentHealth || 0)} / ${Math.round(player.maxHealth || 0)}`);
  setText('resourceText', `${Math.round(player.currentResource || 0)} / ${Math.round(player.maxResource || 0)} (${player.resourceType || 'NONE'})`);
  if ($('healthBar')) $('healthBar').style.width = percentToWidth(player.healthPercent);
  if ($('resourceBar')) $('resourceBar').style.width = percentToWidth(player.resourcePercent);
  setText('kills', String(player.kills ?? 0));
  setText('deaths', String(player.deaths ?? 0));
  setText('assists', String(player.assists ?? 0));
  setText('cs', String(player.creepScore ?? 0));

  const teamBadge = $('teamBadge');
  if (teamBadge) {
    const teamDisplay = player.teamDisplayName || '未知方';
    teamBadge.textContent = teamDisplay;
    teamBadge.className = `team-chip ${(player.team || '').toLowerCase() === 'order' ? 'blue' : (player.team || '').toLowerCase() === 'chaos' ? 'red' : 'neutral'}`;
  }

  renderClientPhase(snapshot.clientPhase || {});
  renderRecommendation(player.levelUpRecommendation || {});
  renderAbilities(player.abilities || []);
  renderSummonerSpellList('summonerSpellsGrid', player.summonerSpells || [], snapshot.inGame ? '暂无双招数据' : '当前不在局内');
  renderSummonerSpellList('champSelectSpellsGrid', snapshot.clientPhase?.summonerSpells || [], '暂无选人双招');
  renderBuffs(player);
  renderObjectives(snapshot.objectives || {});
  renderEvents(snapshot.recentEvents || []);
  renderNotes(snapshot.notes || []);
  renderKeyboard();
}

function activateSection(sectionId) {
  state.activeSectionId = sectionId;
  document.querySelectorAll('.nav-item').forEach((nav) => nav.classList.toggle('active', nav.dataset.navTarget === sectionId));
  document.querySelectorAll('.content-section').forEach((section) => section.classList.toggle('active', section.id === sectionId));

  const activeSection = document.getElementById(sectionId);
  if (activeSection) {
    setText('pageTitle', activeSection.dataset.pageTitle || 'Hextech LoL Bridge');
    setText('pageSubtitle', activeSection.dataset.pageSubtitle || '--');
  }

  const contentArea = document.querySelector('.content-area');
  if (contentArea) contentArea.scrollTop = 0;
}

function preventZoom() {
  document.addEventListener('wheel', (event) => {
    if (event.ctrlKey) event.preventDefault();
  }, { passive: false });

  document.addEventListener('keydown', (event) => {
    if (event.ctrlKey && ['+', '-', '=', '_', '0'].includes(event.key)) {
      event.preventDefault();
    }
  });
}

function handleBodyClick(event) {
  const navTarget = event.target.closest('.nav-item');
  if (navTarget) {
    activateSection(navTarget.dataset.navTarget);
    return;
  }

  const testBtn = event.target.closest('[data-light-hex]');
  if (testBtn) {
    postMessage('lighting:test-theme', { hex: testBtn.dataset.lightHex, label: testBtn.dataset.lightLabel });
    return;
  }

  if (event.target.closest('#acceptReadyBtn')) {
    postMessage('queue:accept-ready');
    return;
  }

  const actionBtn = event.target.closest('[data-action-id]');
  if (actionBtn) {
    state.selectedActionId = actionBtn.dataset.actionId;
    renderActionPills();
    renderKeyboard();
    return;
  }

  const keyBtn = event.target.closest('[data-key-code]');
  if (keyBtn && state.selectedActionId) {
    postMessage('settings:set-key-mapping', { actionId: state.selectedActionId, keyCode: keyBtn.dataset.keyCode });
    return;
  }

  const resetSpellBtn = event.target.closest('[data-reset-spell-id]');
  if (resetSpellBtn) {
    postMessage('settings:reset-spell-color', { spellId: resetSpellBtn.dataset.resetSpellId });
    return;
  }

  const resetHeroBtn = event.target.closest('[data-reset-champion-key]');
  if (resetHeroBtn) {
    postMessage('settings:reset-hero-color', { championKey: resetHeroBtn.dataset.resetChampionKey });
    return;
  }

  const resetActionBtn = event.target.closest('[data-reset-action-id]');
  if (resetActionBtn) {
    postMessage('settings:reset-key-mapping', { actionId: resetActionBtn.dataset.resetActionId });
  }
}

function handleBodyChange(event) {
  const picker = event.target.closest('[data-role="color-picker"]');
  if (picker) {
    postMessage('settings:set-spell-color', { spellId: picker.dataset.spellId, hex: picker.value });
    return;
  }

  const heroPicker = event.target.closest('[data-role="hero-color-picker"]');
  if (heroPicker) {
    postMessage('settings:set-hero-color', { championKey: heroPicker.dataset.championKey, hex: heroPicker.value });
  }
}

function handleBodyBlur(event) {
  const input = event.target.closest('[data-role="hex-input"]');
  if (input) {
    let value = String(input.value || '').trim().toUpperCase();
    if (!value.startsWith('#')) value = `#${value}`;
    if (/^#[0-9A-F]{6}$/.test(value)) {
      postMessage('settings:set-spell-color', { spellId: input.dataset.spellId, hex: value });
    } else {
      renderSpellThemeSettings();
    }
    return;
  }

  const heroInput = event.target.closest('[data-role="hero-hex-input"]');
  if (!heroInput) return;
  let value = String(heroInput.value || '').trim().toUpperCase();
  if (!value.startsWith('#')) value = `#${value}`;
  if (/^#[0-9A-F]{6}$/.test(value)) {
    postMessage('settings:set-hero-color', { championKey: heroInput.dataset.championKey, hex: value });
  } else {
    renderHeroThemeSettings();
  }
}

function handleGlobalKeyDown(event) {
  const target = event.target;
  if (target && ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)) {
    return;
  }

  if (event.code === 'Space' && state.snapshot?.clientPhase?.canAcceptReadyCheck) {
    event.preventDefault();
    postMessage('queue:accept-ready');
  }
}

function wireEvents() {
  $('startBtn').addEventListener('click', () => postMessage('polling:start'));
  $('stopBtn').addEventListener('click', () => postMessage('polling:stop'));
  $('refreshBtn').addEventListener('click', () => postMessage('snapshot:refresh'));
  $('clearLogsBtn').addEventListener('click', () => postMessage('logs:clear'));
  $('sdkInitBtn').addEventListener('click', () => postMessage('sdk:initialize'));
  $('sdkSyncBtn').addEventListener('click', () => postMessage('lighting:sync-current'));
  $('heroSearchInput')?.addEventListener('input', (event) => { state.heroSearch = event.target.value || ''; renderHeroThemeSettings(); });
  document.body.addEventListener('click', handleBodyClick);
  document.body.addEventListener('change', handleBodyChange);
  document.body.addEventListener('blur', handleBodyBlur, true);
  document.addEventListener('keydown', handleGlobalKeyDown, true);
}

function handleHostMessage(event) {
  const message = event.data || {};
  switch (message.type) {
    case 'bootstrap':
      state.app = message.payload?.app || null;
      state.profile = message.payload?.profile || state.profile;
      state.sdkStatus = message.payload?.sdkStatus || null;
      renderApp();
      renderSpellThemeSettings();
      renderHeroThemeSettings();
      renderKeyboard();
      renderSdkStatus();
      break;
    case 'profile':
      state.profile = message.payload || state.profile;
      renderSpellThemeSettings();
      renderHeroThemeSettings();
      renderKeyboard();
      break;
    case 'polling-status':
      state.polling = message.payload || null;
      renderPolling();
      break;
    case 'snapshot':
      state.snapshot = message.payload || null;
      renderSnapshot();
      break;
    case 'logs':
      state.logs = message.payload || [];
      renderLogs();
      break;
    case 'sdk-status':
      state.sdkStatus = message.payload || null;
      renderSdkStatus();
      break;
  }
}

function bootstrap() {
  wireEvents();
  preventZoom();
  activateSection(state.activeSectionId);
  if (window.chrome?.webview) {
    window.chrome.webview.addEventListener('message', handleHostMessage);
    postMessage('app:ready');
  }
}

document.addEventListener('DOMContentLoaded', bootstrap);
