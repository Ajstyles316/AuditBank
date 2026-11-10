(() => {
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const state = {
    token: sessionStorage.getItem("auditbank.token"),
    user: null,
    page: "inicio",
    accounts: [],
    transactions: [],
    alerts: [],
    alertOpenTotal: 0,
    alertHighTotal: 0,
    rules: [],
    audit: [],
    alertFilter: "",
    transactionFilter: "",
    modal: null
  };

  const pageNames = {
    inicio: "Resumen",
    alertas: "Alertas",
    transacciones: "Transacciones",
    cuentas: "Cuentas",
    reglas: "Reglas de auditoría",
    auditoria: "Bitácora",
    reportes: "Reportes"
  };

  const escapeHtml = value => String(value ?? "").replace(/[&<>"']/g, char => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
  })[char]);
  const money = (value, currency = "BOB") => new Intl.NumberFormat("es-BO", {
    style: "currency", currency, maximumFractionDigits: 2
  }).format(Number(value || 0));
  const date = value => value ? new Intl.DateTimeFormat("es-BO", {
    dateStyle: "medium", timeStyle: "short"
  }).format(new Date(value)) : "—";
  const enumLabel = value => ({
    Abierta: "Abierta", EnRevision: "En revisión", Cerrada: "Cerrada",
    Alta: "Alta", Media: "Media", Baja: "Baja",
    Deposito: "Depósito", Retiro: "Retiro", Transferencia: "Transferencia",
    CajaAhorro: "Caja de ahorro", Corriente: "Cuenta corriente",
    DepositoPlazoFijo: "Depósito a plazo fijo",
    Administrador: "Administrador", Auditor: "Auditor", Consulta: "Consulta",
    IrregularidadConfirmada: "Irregularidad confirmada", FalsoPositivo: "Falso positivo",
    Crear: "Creación", Modificar: "Modificación", Eliminar: "Eliminación",
    Login: "Inicio de sesión", LoginFallido: "Acceso fallido", Exportar: "Exportación"
  })[value] || value || "—";
  const badgeClass = value => ({
    Alta: "badge-high", Media: "badge-medium", Baja: "badge-low",
    Abierta: "badge-open", EnRevision: "badge-review", Cerrada: "badge-closed",
    Activa: "badge-success", Inactiva: "badge-low"
  })[value] || "badge-low";
  const badge = value => `<span class="badge ${badgeClass(value)}">${escapeHtml(enumLabel(value))}</span>`;

  async function api(path, options = {}) {
    const headers = new Headers(options.headers || {});
    if (state.token) headers.set("Authorization", `Bearer ${state.token}`);
    if (options.body && !(options.body instanceof FormData)) headers.set("Content-Type", "application/json");
    const response = await fetch(path, { ...options, headers });
    if (response.status === 401) {
      logout();
      throw new Error("Tu sesión expiró. Vuelve a iniciar sesión.");
    }
    if (!response.ok) {
      const body = await response.json().catch(() => null);
      throw new Error(body?.mensaje || body?.title || `Error del servidor (${response.status}).`);
    }
    if (response.status === 204) return null;
    return response;
  }

  async function getJson(path) {
    const response = await api(path);
    return response.json();
  }

  function showToast(message, isError = false) {
    const toast = $("#toast");
    toast.textContent = message;
    toast.classList.toggle("error", isError);
    toast.classList.add("show");
    clearTimeout(showToast.timeout);
    showToast.timeout = setTimeout(() => toast.classList.remove("show"), 3500);
  }

  function showLogin() {
    $("#login-screen").classList.remove("hidden");
    $("#app-shell").classList.add("hidden");
  }

  function showApp() {
    $("#login-screen").classList.add("hidden");
    $("#app-shell").classList.remove("hidden");
    const name = state.user?.usuario || "Usuario";
    $("#sidebar-username").textContent = name;
    $("#sidebar-role").textContent = enumLabel(state.user?.rol);
    $("#user-avatar").textContent = name.charAt(0).toUpperCase();
    $("#topbar-avatar").textContent = name.charAt(0).toUpperCase();
    $("#topbar-date").textContent = new Intl.DateTimeFormat("es-BO", {
      weekday: "short", day: "numeric", month: "short"
    }).format(new Date());
    $$(".nav-item").forEach(item => {
      if (item.dataset.page === "alertas") item.classList.toggle("hidden", !canAudit());
      if (item.dataset.page === "reportes") item.classList.toggle("hidden", !canAudit());
      if (item.dataset.page === "reglas") item.classList.toggle("hidden", !canAudit());
      if (item.dataset.page === "auditoria") item.classList.toggle("hidden", !canAudit());
    });
    navigate(state.page);
  }

  function canAudit() {
    return ["Administrador", "Auditor"].includes(state.user?.rol);
  }

  function logout() {
    state.token = null;
    state.user = null;
    sessionStorage.removeItem("auditbank.token");
    showLogin();
  }

  async function login(username, password) {
    const response = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ nombreUsuario: username, password })
    });
    if (!response.ok) {
      const body = await response.json().catch(() => null);
      throw new Error(body?.mensaje || "No se pudo iniciar sesión. Revisa tus credenciales.");
    }
    const result = await response.json();
    state.token = result.token;
    sessionStorage.setItem("auditbank.token", result.token);
    state.user = await getJson("/api/auth/yo");
    showApp();
  }

  async function restoreSession() {
    if (!state.token) return showLogin();
    try {
      state.user = await getJson("/api/auth/yo");
      showApp();
    } catch (error) {
      if (state.token) {
        state.token = null;
        sessionStorage.removeItem("auditbank.token");
      }
      showLogin();
    }
  }

  function navigate(page) {
    state.page = page;
    $("#breadcrumb-current").textContent = pageNames[page] || "Resumen";
    $$(".nav-item").forEach(item => item.classList.toggle("active", item.dataset.page === page));
    closeMobileMenu();
    renderPage().catch(error => {
      $("#page-content").innerHTML = `<div class="panel empty-state">${escapeHtml(error.message)}</div>`;
    });
  }

  async function loadCoreData() {
    const emptyPage = Promise.resolve({ items: [], total: 0 });
    const tasks = [
      getJson("/api/cuentas"),
      getJson("/api/transacciones?pagina=1&tamanoPagina=10"),
      canAudit() ? getJson("/api/alertas?estado=Abierta&pagina=1&tamanoPagina=10") : emptyPage,
      canAudit() ? getJson("/api/alertas?estado=Abierta&severidad=Alta&pagina=1&tamanoPagina=1") : emptyPage,
      canAudit() ? getJson("/api/reglas") : Promise.resolve([]),
      canAudit() ? getJson("/api/auditoria?pagina=1&tamanoPagina=8") : Promise.resolve({ items: [] })
    ];
    const [accounts, transactions, alerts, highAlerts, rules, audit] = await Promise.all(tasks);
    state.accounts = accounts;
    state.transactions = transactions.items || [];
    state.alerts = alerts.items || [];
    state.alertTotal = alerts.total || 0;
    state.alertOpenTotal = alerts.total || 0;
    state.alertHighTotal = highAlerts.total || 0;
    state.rules = rules;
    state.audit = audit.items || [];
    $("#nav-alert-count").textContent = String(state.alertOpenTotal);
  }

  async function renderPage() {
    const content = $("#page-content");
    content.innerHTML = `<div class="loading"><span class="spinner"></span>Cargando datos del sistema…</div>`;
    if (state.page === "inicio") {
      await loadCoreData();
      content.innerHTML = renderDashboard();
    } else if (state.page === "cuentas") {
      state.accounts = await getJson("/api/cuentas");
      content.innerHTML = renderAccounts();
    } else if (state.page === "transacciones") {
      const result = await getJson("/api/transacciones?pagina=1&tamanoPagina=100");
      state.transactions = result.items || [];
      state.accounts = await getJson("/api/cuentas");
      content.innerHTML = renderTransactions();
    } else if (state.page === "alertas") {
      await loadAlerts();
      content.innerHTML = renderAlerts();
    } else if (state.page === "reglas") {
      state.rules = await getJson("/api/reglas");
      content.innerHTML = renderRules();
    } else if (state.page === "auditoria") {
      const result = await getJson("/api/auditoria?pagina=1&tamanoPagina=100");
      state.audit = result.items || [];
      content.innerHTML = renderAudit();
    } else if (state.page === "reportes") {
      content.innerHTML = renderReports();
    }
  }

  async function loadAlerts() {
    const query = new URLSearchParams({ pagina: "1", tamanoPagina: "100" });
    if (state.alertFilter) query.set("estado", state.alertFilter);
    const [result, openAlerts] = await Promise.all([
      getJson(`/api/alertas?${query}`),
      getJson("/api/alertas?estado=Abierta&pagina=1&tamanoPagina=1")
    ]);
    state.alerts = result.items || [];
    state.alertTotal = result.total || 0;
    state.alertOpenTotal = openAlerts.total || 0;
    $("#nav-alert-count").textContent = String(state.alertOpenTotal);
  }

  function pageHeader(kicker, title, description, actions = "") {
    return `<div class="page-heading"><div><p class="eyebrow-content">${escapeHtml(kicker)}</p><h1>${escapeHtml(title)}</h1><p>${escapeHtml(description)}</p></div><div class="heading-actions">${actions}</div></div>`;
  }

  function renderDashboard() {
    const action = state.user?.rol === "Administrador"
      ? `<button class="button button-primary" data-action="new-transaction">＋ Registrar operación</button>` : "";
    return `${pageHeader("CENTRO DE CONTROL", "Resumen general", "Supervisa la actividad y revisa lo que requiere atención.", action)}
      <section class="stats-grid">
        ${statCard("Cuentas activas", state.accounts.filter(account => account.activa).length, "Cuentas registradas en el sistema", "▤", "")}
        ${statCard("Alertas abiertas", state.alertOpenTotal, `${state.alertHighTotal} de severidad alta`, "◈", "amber")}
        ${statCard("Operaciones recientes", state.transactions.length, "Últimos movimientos registrados", "↔", "blue")}
        ${statCard("Reglas activas", state.rules.filter(rule => rule.activa).length, "Reglas del motor de auditoría", "⛭", "red")}
      </section>
      <section class="dashboard-grid">
        <div class="panel">
          <div class="panel-header"><div><h2 class="panel-title">Actividad reciente</h2><p class="panel-subtitle">Últimas operaciones registradas</p></div><button class="text-button" data-navigate="transacciones">Ver transacciones →</button></div>
          ${renderTransactionTable(state.transactions.slice(0, 6), false)}
        </div>
        <div class="panel">
          <div class="panel-header"><div><h2 class="panel-title">Alertas para revisar</h2><p class="panel-subtitle">Incidencias detectadas por el motor</p></div><button class="text-button" data-navigate="alertas">Ver todas →</button></div>
          ${renderAlertActivity(state.alerts.filter(alert => alert.estado !== "Cerrada").slice(0, 5))}
        </div>
      </section>
      <section class="panel" style="margin-top:16px">
        <div class="panel-header"><div><h2 class="panel-title">Bitácora de actividad</h2><p class="panel-subtitle">Cambios registrados automáticamente</p></div><button class="text-button" data-navigate="auditoria">Abrir bitácora →</button></div>
        ${renderAuditActivity(state.audit.slice(0, 4))}
      </section>`;
  }

  function statCard(label, value, caption, icon, tone) {
    return `<article class="stat-card"><div class="stat-head"><span>${escapeHtml(label)}</span><span class="stat-icon ${tone}">${icon}</span></div><div class="stat-value">${escapeHtml(value)}</div><div class="stat-caption">${escapeHtml(caption)}</div></article>`;
  }

  function renderTransactionTable(items, showAccount = true) {
    if (!items.length) return `<div class="empty-state"><div class="empty-icon">↔</div>No hay transacciones para mostrar.</div>`;
    return `<div class="table-wrap"><table><thead><tr><th>OPERACIÓN</th>${showAccount ? "<th>CUENTA</th>" : ""}<th>TIPO</th><th>FECHA</th><th>MONTO</th><th>REGISTRADA POR</th></tr></thead><tbody>${items.map(item => { const currency = state.accounts.find(account => account.id === item.cuentaId)?.moneda || "BOB"; return `<tr><td class="cell-primary">#${item.id}<span class="cell-secondary">${escapeHtml(item.descripcion || "Sin descripción")}</span></td>${showAccount ? `<td>${escapeHtml(item.numeroCuenta)}</td>` : ""}<td>${badge(item.tipo)}</td><td>${escapeHtml(date(item.fechaUtc))}</td><td class="amount">${escapeHtml(money(item.monto, currency))}</td><td>${escapeHtml(item.registradaPor)}</td></tr>`; }).join("")}</tbody></table></div>`;
  }

  function renderAlertActivity(items) {
    if (!items.length) return `<div class="empty-state"><div class="empty-icon">✓</div>No hay alertas pendientes.</div>`;
    return `<div class="activity-list">${items.map(item => `<div class="activity-item"><div class="activity-mark">◈</div><div class="activity-copy"><strong>${escapeHtml(item.reglaNombre)}</strong><p>Cuenta ${escapeHtml(item.numeroCuenta)} · ${escapeHtml(money(item.monto))}</p><div class="activity-time">${escapeHtml(date(item.creadaEn))}</div></div>${badge(item.severidad)}</div>`).join("")}</div>`;
  }

  function renderAuditActivity(items) {
    if (!items.length) return `<div class="empty-state">La bitácora aparecerá aquí cuando haya actividad.</div>`;
    return `<div class="activity-list">${items.map(item => `<div class="activity-item"><div class="activity-mark">≡</div><div class="activity-copy"><strong>${escapeHtml(enumLabel(item.accion))} · ${escapeHtml(item.entidad)}</strong><p>${escapeHtml(item.usuario || "Sistema")} · ${escapeHtml(item.direccionIp || "sin IP")}</p></div><span class="activity-time">${escapeHtml(date(item.fechaUtc))}</span></div>`).join("")}</div>`;
  }

  function renderAccounts() {
    const action = state.user?.rol === "Administrador" ? `<button class="button button-primary" data-action="new-account">＋ Nueva cuenta</button>` : "";
    const rows = state.accounts.map(account => `<tr>
      <td class="cell-primary">${escapeHtml(account.numeroCuenta)}<span class="cell-secondary">${escapeHtml(account.titular)}</span></td>
      <td>${escapeHtml(enumLabel(account.tipo))}</td><td>${escapeHtml(account.moneda)}</td>
      <td class="amount">${escapeHtml(money(account.saldo, account.moneda))}</td><td>${badge(account.activa ? "Activa" : "Inactiva")}</td>
      <td>${escapeHtml(date(account.creadaEn))}</td></tr>`).join("");
    return `${pageHeader("OPERACIÓN BANCARIA", "Cuentas", "Consulta las cuentas y sus saldos actuales.", action)}
      <section class="stats-grid">${statCard("Total de cuentas", state.accounts.length, "Cuentas registradas", "▤", "blue")}${statCard("Cuentas activas", state.accounts.filter(x => x.activa).length, "Disponibles para operar", "✓", "")}${statCard("Saldo total BOB", money(state.accounts.filter(x => x.moneda === "BOB").reduce((n, x) => n + Number(x.saldo), 0)), "Suma de cuentas en bolivianos", "↗", "amber")}${statCard("Saldo total USD", money(state.accounts.filter(x => x.moneda === "USD").reduce((n, x) => n + Number(x.saldo), 0), "USD"), "Suma de cuentas en dólares", "$", "blue")}</section>
      <section class="table-panel"><div class="panel-header"><div><h2 class="panel-title">Directorio de cuentas</h2><p class="panel-subtitle">${state.accounts.length} cuentas en el sistema</p></div></div>
      ${state.accounts.length ? `<div class="table-wrap"><table><thead><tr><th>CUENTA / TITULAR</th><th>TIPO</th><th>MONEDA</th><th>SALDO</th><th>ESTADO</th><th>CREADA</th></tr></thead><tbody>${rows}</tbody></table></div>` : `<div class="empty-state"><div class="empty-icon">▤</div>No hay cuentas registradas. Crea una cuenta para comenzar.</div>`}</section>`;
  }

  function renderTransactions() {
    const action = state.user?.rol === "Administrador" ? `<button class="button button-primary" data-action="new-transaction">＋ Registrar operación</button>` : "";
    const filtered = state.transactionFilter ? state.transactions.filter(t => t.tipo === state.transactionFilter) : state.transactions;
    return `${pageHeader("MOVIMIENTOS", "Transacciones", "Revisa depósitos, retiros y transferencias.", action)}
      <section class="stats-grid">${statCard("Operaciones", state.transactions.length, "Resultados recientes", "↔", "blue")}${statCard("Depósitos", state.transactions.filter(t => t.tipo === "Deposito").length, "Ingresos registrados", "↓", "")}${statCard("Retiros", state.transactions.filter(t => t.tipo === "Retiro").length, "Salidas registradas", "↑", "amber")}${statCard("Transferencias", state.transactions.filter(t => t.tipo === "Transferencia").length, "Movimientos entre cuentas", "⇄", "blue")}</section>
      <section class="table-panel"><div class="table-toolbar"><div><strong class="panel-title">Historial de operaciones</strong><p class="panel-subtitle">Se muestran las últimas 100 operaciones.</p></div><div class="toolbar-right"><select class="filter-select" id="transaction-filter"><option value="">Todos los tipos</option><option value="Deposito">Depósitos</option><option value="Retiro">Retiros</option><option value="Transferencia">Transferencias</option></select></div></div>
      ${renderTransactionTable(filtered, true)}</section>`;
  }

  function renderAlerts() {
    const action = `<button class="button button-secondary" data-navigate="reportes">▧ Exportar alertas</button>`;
    const rows = state.alerts.map(item => `<tr>
      <td class="cell-primary">#${item.id}<span class="cell-secondary">${escapeHtml(item.reglaCodigo)}</span></td>
      <td>${escapeHtml(item.reglaNombre)}</td><td>${badge(item.severidad)}</td><td>${badge(item.estado)}</td>
      <td>${escapeHtml(item.numeroCuenta)}</td><td class="amount">${escapeHtml(money(item.monto))}</td>
      <td>${escapeHtml(date(item.creadaEn))}</td><td><button class="button button-secondary button-small" data-alert-detail="${item.id}">Revisar</button></td></tr>`).join("");
    return `${pageHeader("CENTRO DE INVESTIGACIÓN", "Alertas", "Investiga las señales detectadas por las reglas de auditoría.", action)}
      <section class="stats-grid">${statCard("Alertas totales", state.alertTotal || state.alerts.length, "En los resultados actuales", "◈", "blue")}${statCard("Abiertas", state.alerts.filter(x => x.estado === "Abierta").length, "Pendientes de asignación", "◷", "amber")}${statCard("En revisión", state.alerts.filter(x => x.estado === "EnRevision").length, "Investigaciones activas", "⌕", "blue")}${statCard("Severidad alta", state.alerts.filter(x => x.severidad === "Alta" && x.estado !== "Cerrada").length, "Requieren atención prioritaria", "!", "red")}</section>
      <section class="table-panel"><div class="table-toolbar"><div><strong class="panel-title">Bandeja de alertas</strong><p class="panel-subtitle">${state.alertTotal || state.alerts.length} alertas</p></div><div class="toolbar-right"><select class="filter-select" id="alert-filter"><option value="">Todos los estados</option><option value="Abierta">Abiertas</option><option value="EnRevision">En revisión</option><option value="Cerrada">Cerradas</option></select></div></div>
      ${state.alerts.length ? `<div class="table-wrap"><table><thead><tr><th>ALERTA</th><th>REGLA</th><th>SEVERIDAD</th><th>ESTADO</th><th>CUENTA</th><th>MONTO</th><th>FECHA</th><th></th></tr></thead><tbody>${rows}</tbody></table></div>` : `<div class="empty-state"><div class="empty-icon">✓</div>No hay alertas para este filtro.</div>`}</section>`;
  }

  function renderRules() {
    const cards = state.rules.map(rule => `<article class="rule-card">
      <div class="rule-number">${String(rule.id).padStart(2, "0")}</div>
      <div class="rule-copy"><h3>${escapeHtml(rule.nombre)}</h3><p>${escapeHtml(rule.descripcion)}</p><div class="rule-meta"><span class="cell-secondary">${escapeHtml(rule.codigo)}</span>${badge(rule.severidad)}${rule.activa ? badge("Activa") : badge("Inactiva")}</div></div>
      <div class="rule-actions">${state.user?.rol === "Administrador" ? `<button class="button button-secondary button-small" data-edit-rule="${rule.id}">Editar</button>` : ""}</div>
    </article>`).join("");
    return `${pageHeader("CONFIGURACIÓN", "Reglas de auditoría", "Parámetros que el motor utiliza para detectar operaciones inusuales.")}
      <section class="panel"><div class="panel-header"><div><h2 class="panel-title">Motor de reglas</h2><p class="panel-subtitle">${state.rules.filter(x => x.activa).length} reglas activas de ${state.rules.length}</p></div><span class="badge badge-success">En ejecución</span></div>
      ${cards || `<div class="empty-state">No hay reglas configuradas.</div>`}</section>
      <p class="cell-secondary" style="margin:12px 3px">Los cambios se aplican a nuevas operaciones; las alertas existentes no se recalculan.</p>`;
  }

  function renderAudit() {
    const rows = state.audit.map(item => `<tr>
      <td class="cell-primary">#${item.id}</td><td>${escapeHtml(date(item.fechaUtc))}</td>
      <td>${escapeHtml(item.usuario || "Sistema")}</td><td>${badge(item.accion)}</td>
      <td>${escapeHtml(item.entidad)}${item.entidadId ? `<span class="cell-secondary">ID ${escapeHtml(item.entidadId)}</span>` : ""}</td>
      <td>${escapeHtml(item.direccionIp || "—")}</td>
      <td><button class="text-button" data-audit-detail="${item.id}">Ver detalle</button></td></tr>`).join("");
    return `${pageHeader("TRAZABILIDAD", "Bitácora de auditoría", "Registro inmutable de las acciones realizadas en el sistema.", `<button class="button button-secondary" data-navigate="reportes">▧ Exportar bitácora</button>`)}
      <section class="table-panel"><div class="table-toolbar"><div><strong class="panel-title">Registro de actividad</strong><p class="panel-subtitle">Últimas ${state.audit.length} acciones</p></div></div>
      ${state.audit.length ? `<div class="table-wrap"><table><thead><tr><th>ID</th><th>FECHA</th><th>USUARIO</th><th>ACCIÓN</th><th>ENTIDAD</th><th>DIRECCIÓN IP</th><th></th></tr></thead><tbody>${rows}</tbody></table></div>` : `<div class="empty-state">Aún no hay registros.</div>`}</section>`;
  }

  function renderReports() {
    const reports = [
      ["transacciones", "Transacciones", "Depósitos, retiros y transferencias por periodo.", "↔"],
      ["alertas", "Alertas", "Incidencias, severidad y estado de investigación.", "◈"],
      ["auditoria", "Bitácora de auditoría", "Acciones realizadas por los usuarios del sistema.", "≡"]
    ];
    return `${pageHeader("CENTRO DE REPORTES", "Reportes", "Descarga informes de operaciones, alertas y bitácora en Excel o PDF.")}
      <div class="report-grid">${reports.map(([kind, title, description, icon]) => `<article class="panel report-card"><div class="report-icon">${icon}</div><h3>${title}</h3><p>${description}</p><div class="filter-row"><button class="button button-secondary button-small" data-report="${kind}" data-format="Excel">↓ Excel</button><button class="button button-secondary button-small" data-report="${kind}" data-format="Pdf">↓ PDF</button></div></article>`).join("")}</div>
      <section class="panel" style="margin-top:17px"><div class="panel-header"><div><h2 class="panel-title">Integridad y seguimiento</h2><p class="panel-subtitle">La exportación de cada informe también queda registrada en la bitácora.</p></div></div><div class="activity-item"><div class="activity-mark">✓</div><div class="activity-copy"><strong>Descargas auditadas</strong><p>Cada informe registra el usuario, el formato y los filtros utilizados.</p></div></div></section>`;
  }

  function openModal(title, subtitle, body, submitLabel = "Guardar", onSubmit = null) {
    closeModal();
    const modal = document.createElement("div");
    modal.className = "modal-backdrop";
    modal.innerHTML = `<section class="modal" role="dialog" aria-modal="true" aria-labelledby="modal-title"><header class="modal-header"><div><h2 id="modal-title">${escapeHtml(title)}</h2><p>${escapeHtml(subtitle)}</p></div><button class="icon-button" data-close-modal aria-label="Cerrar">×</button></header><form id="modal-form"><div class="modal-body">${body}</div><footer class="modal-footer"><button class="button button-secondary" type="button" data-close-modal>Cancelar</button><button class="button button-primary" type="submit">${escapeHtml(submitLabel)}</button></footer></form></section>`;
    document.body.append(modal);
    state.modal = modal;
    modal.addEventListener("click", event => { if (event.target === modal || event.target.closest("[data-close-modal]")) closeModal(); });
    modal.querySelector("form").addEventListener("submit", async event => {
      event.preventDefault();
      if (!onSubmit) return;
      const button = modal.querySelector('button[type="submit"]');
      button.disabled = true;
      try {
        await onSubmit(new FormData(event.currentTarget));
        closeModal();
        await renderPage();
      } catch (error) {
        showToast(error.message, true);
        button.disabled = false;
      }
    });
    modal.querySelector("input,select,textarea")?.focus();
  }

  function closeModal() {
    state.modal?.remove();
    state.modal = null;
  }

  function newAccount() {
    openModal("Crear cuenta", "Registra una cuenta bancaria para iniciar operaciones.", `
      <div class="form-grid">
        <label class="field-label">Número de cuenta<input class="field-input" name="numeroCuenta" required maxlength="30" placeholder="Ej. 001-0001"></label>
        <label class="field-label">Titular<input class="field-input" name="titular" required maxlength="150" placeholder="Nombre completo"></label>
        <label class="field-label">Tipo de cuenta<select class="field-select" name="tipo"><option value="CajaAhorro">Caja de ahorro</option><option value="Corriente">Cuenta corriente</option><option value="DepositoPlazoFijo">Depósito a plazo fijo</option></select></label>
        <label class="field-label">Moneda<select class="field-select" name="moneda"><option value="BOB">BOB · Bolivianos</option><option value="USD">USD · Dólares</option></select></label>
        <label class="field-label span-two">Saldo inicial<input class="field-input" name="saldoInicial" type="number" min="0" step="0.01" value="0" required></label>
      </div>`, "Crear cuenta", async form => {
      const body = Object.fromEntries(form);
      body.saldoInicial = Number(body.saldoInicial);
      await api("/api/cuentas", { method: "POST", body: JSON.stringify(body) });
      showToast("La cuenta fue creada correctamente.");
    });
  }

  function newTransaction() {
    if (!state.accounts.length) {
      showToast("Primero registra una cuenta.", true);
      navigate("cuentas");
      return;
    }
    const options = state.accounts.filter(account => account.activa).map(account => `<option value="${account.id}">${escapeHtml(account.numeroCuenta)} · ${escapeHtml(account.titular)} (${escapeHtml(account.moneda)})</option>`).join("");
    openModal("Registrar operación", "La operación quedará en la bitácora y será evaluada por las reglas.", `
      <div class="form-grid">
        <label class="field-label span-two">Tipo de operación<select class="field-select" name="tipo" id="transaction-type"><option value="Deposito">Depósito</option><option value="Retiro">Retiro</option><option value="Transferencia">Transferencia</option></select></label>
        <label class="field-label span-two">Cuenta de origen<select class="field-select" name="cuentaId" required>${options}</select></label>
        <label class="field-label span-two hidden" id="destination-field">Cuenta destino<select class="field-select" name="cuentaDestinoId"><option value="">Selecciona la cuenta destino</option>${options}</select></label>
        <label class="field-label span-two">Monto<input class="field-input" name="monto" type="number" min="0.01" step="0.01" required placeholder="0.00"></label>
        <label class="field-label span-two">Descripción <span style="font-weight:400;color:#929bab">(opcional)</span><input class="field-input" name="descripcion" maxlength="300" placeholder="Motivo de la operación"></label>
      </div>`, "Registrar operación", async form => {
      const body = Object.fromEntries(form);
      body.cuentaId = Number(body.cuentaId);
      body.cuentaDestinoId = body.tipo === "Transferencia" ? Number(body.cuentaDestinoId) : null;
      body.monto = Number(body.monto);
      body.descripcion = body.descripcion || null;
      await api("/api/transacciones", { method: "POST", body: JSON.stringify(body) });
      showToast("La operación fue registrada.");
    });
    $("#transaction-type", state.modal).addEventListener("change", event => {
      $("#destination-field", state.modal).classList.toggle("hidden", event.target.value !== "Transferencia");
      $("#destination-field select", state.modal).required = event.target.value === "Transferencia";
    });
  }

  async function showAlertDetail(id) {
    const alert = await getJson(`/api/alertas/${id}`);
    const findings = await getJson(`/api/alertas/${id}/hallazgos`);
    const canOperate = state.user?.rol === "Administrador" || (state.user?.rol === "Auditor" && alert.asignadaAId === state.user.id);
    const takeButton = state.user?.rol === "Auditor" && !alert.asignadaAId && alert.estado !== "Cerrada"
      ? `<button class="button button-secondary" data-alert-action="take" data-id="${id}">Tomar alerta</button>` : "";
    const reviewButton = canOperate && alert.estado === "Abierta" && alert.asignadaAId
      ? `<button class="button button-secondary" data-alert-action="review" data-id="${id}">Iniciar revisión</button>` : "";
    const commentButton = canOperate && alert.estado !== "Cerrada"
      ? `<button class="button button-secondary" data-alert-action="comment" data-id="${id}">Añadir comentario</button>` : "";
    const closeButton = canOperate && alert.estado === "EnRevision"
      ? `<button class="button button-primary" data-alert-action="close" data-id="${id}">Cerrar investigación</button>` : "";
    openModal(`Alerta #${alert.id}`, alert.reglaNombre, `
      <div>${badge(alert.severidad)} ${badge(alert.estado)}</div>
      <div class="detail-row"><span>Regla</span><span>${escapeHtml(alert.reglaCodigo)}</span></div>
      <div class="detail-row"><span>Cuenta</span><span>${escapeHtml(alert.numeroCuenta)} · ${escapeHtml(money(alert.monto))}</span></div>
      <div class="detail-row"><span>Operación</span><span>${escapeHtml(enumLabel(alert.tipoTransaccion))} · ${escapeHtml(date(alert.creadaEn))}</span></div>
      <div class="detail-row"><span>Asignada a</span><span>${escapeHtml(alert.asignadaA || "Sin asignar")}</span></div>
      <div class="detail-row"><span>Detalle</span><span>${escapeHtml(alert.detalle)}</span></div>
      <div><strong class="panel-title">Historial de investigación</strong>${findings.length ? findings.map(item => `<div class="detail-row"><span>${escapeHtml(enumLabel(item.tipo))} · ${escapeHtml(item.autor)}</span><span>${escapeHtml(item.descripcion)}</span></div>`).join("") : `<p class="cell-secondary">Aún no hay comentarios ni hallazgos.</p>`}</div>
      <div class="filter-row">${takeButton}${reviewButton}${commentButton}${closeButton}</div>`, "Cerrar");
    $("form", state.modal).addEventListener("submit", event => { event.preventDefault(); closeModal(); });
    state.modal.querySelector(".modal-footer").innerHTML = `<button class="button button-secondary" type="button" data-close-modal>Listo</button>`;
  }

  async function alertAction(action, id) {
    if (action === "take" || action === "review") {
      const route = action === "take" ? "tomar" : "iniciar-revision";
      await api(`/api/alertas/${id}/${route}`, { method: "POST" });
      showToast(action === "take" ? "La alerta quedó asignada a tu usuario." : "La revisión fue iniciada.");
      await showAlertDetail(id);
      await renderPage();
      return;
    }
    if (action === "comment") {
      openModal("Añadir comentario", `Alerta #${id}`, `<label class="field-label">Comentario<textarea class="field-textarea" name="texto" maxlength="2000" required placeholder="Describe el avance de la investigación…"></textarea></label>`, "Guardar comentario", async form => {
        await api(`/api/alertas/${id}/comentarios`, { method: "POST", body: JSON.stringify({ texto: form.get("texto") }) });
        showToast("El comentario fue agregado.");
        await showAlertDetail(id);
      });
      return;
    }
    if (action === "close") {
      openModal("Cerrar investigación", `Alerta #${id}`, `
        <label class="field-label">Conclusión<select class="field-select" name="conclusion"><option value="IrregularidadConfirmada">Irregularidad confirmada</option><option value="FalsoPositivo">Falso positivo</option></select></label>
        <label class="field-label">Conclusiones y evidencia<textarea class="field-textarea" name="descripcion" minlength="10" maxlength="2000" required placeholder="Documenta la conclusión y la evidencia revisada…"></textarea></label>`, "Cerrar alerta", async form => {
        await api(`/api/alertas/${id}/cerrar`, { method: "POST", body: JSON.stringify({ conclusion: form.get("conclusion"), descripcion: form.get("descripcion") }) });
        showToast("La investigación quedó cerrada.");
      });
    }
  }

  function editRule(id) {
    const rule = state.rules.find(item => item.id === id);
    if (!rule) return;
    openModal("Editar regla", rule.nombre, `
      <label class="field-label">Severidad<select class="field-select" name="severidad"><option value="Alta" ${rule.severidad === "Alta" ? "selected" : ""}>Alta</option><option value="Media" ${rule.severidad === "Media" ? "selected" : ""}>Media</option><option value="Baja" ${rule.severidad === "Baja" ? "selected" : ""}>Baja</option></select></label>
      <label class="field-label">Parámetros (JSON)<textarea class="field-textarea" style="min-height:130px;font-family:monospace" name="parametros" required>${escapeHtml(JSON.stringify(rule.parametros, null, 2))}</textarea><span class="cell-secondary">Edita cuidadosamente: los campos dependen del tipo de regla.</span></label>
      <label class="field-label" style="grid-template-columns:1fr auto;align-items:center">Regla activa<label class="switch"><input type="checkbox" name="activa" ${rule.activa ? "checked" : ""}><span class="switch-slider"></span></label></label>`, "Guardar cambios", async form => {
      let parametros;
      try { parametros = JSON.parse(form.get("parametros")); } catch { throw new Error("El JSON de parámetros no es válido."); }
      await api(`/api/reglas/${id}`, { method: "PUT", body: JSON.stringify({
        activa: form.has("activa"),
        severidad: form.get("severidad"),
        parametros
      }) });
      showToast("La regla fue actualizada.");
    });
  }

  async function downloadReport(kind, format) {
    const path = `/api/reportes/${kind}?formato=${encodeURIComponent(format)}`;
    const response = await api(path);
    const blob = await response.blob();
    const disposition = response.headers.get("Content-Disposition") || "";
    const match = disposition.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i);
    const name = match ? decodeURIComponent(match[1].replace(/"/g, "")) : `${kind}.${format === "Pdf" ? "pdf" : "xlsx"}`;
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = name;
    document.body.append(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(link.href);
    showToast(`Reporte ${format === "Pdf" ? "PDF" : "Excel"} descargado.`);
  }

  function showAuditDetail(id) {
    const item = state.audit.find(row => row.id === id);
    if (!item) return;
    const formatJson = value => {
      if (!value) return "—";
      try { return JSON.stringify(JSON.parse(value), null, 2); } catch { return value; }
    };
    openModal(`Registro #${item.id}`, `${enumLabel(item.accion)} · ${item.entidad}`, `
      <div class="detail-row"><span>Fecha</span><span>${escapeHtml(date(item.fechaUtc))}</span></div>
      <div class="detail-row"><span>Usuario</span><span>${escapeHtml(item.usuario || "Sistema")}</span></div>
      <div class="detail-row"><span>IP</span><span>${escapeHtml(item.direccionIp || "—")}</span></div>
      <label class="field-label">Valor anterior<pre class="field-textarea">${escapeHtml(formatJson(item.valorAnterior))}</pre></label>
      <label class="field-label">Valor nuevo<pre class="field-textarea">${escapeHtml(formatJson(item.valorNuevo))}</pre></label>`, "Listo");
    $("form", state.modal).addEventListener("submit", event => { event.preventDefault(); closeModal(); });
    state.modal.querySelector(".modal-footer").innerHTML = `<button class="button button-secondary" type="button" data-close-modal>Listo</button>`;
  }

  function closeMobileMenu() {
    $("#sidebar").classList.remove("open");
    $("#sidebar-scrim").classList.remove("open");
  }

  document.addEventListener("submit", async event => {
    if (event.target.id !== "login-form") return;
    event.preventDefault();
    const form = new FormData(event.target);
    const error = $("#login-error");
    const button = event.target.querySelector("button[type=submit]");
    error.textContent = "";
    button.disabled = true;
    button.textContent = "Verificando…";
    try {
      await login(form.get("nombreUsuario"), form.get("password"));
    } catch (failure) {
      error.textContent = failure.message;
    } finally {
      button.disabled = false;
      button.innerHTML = 'Entrar al sistema <span aria-hidden="true">→</span>';
    }
  });

  document.addEventListener("click", async event => {
    const nav = event.target.closest("[data-page], [data-navigate]");
    if (nav) return navigate(nav.dataset.page || nav.dataset.navigate);
    if (event.target.closest("#logout-button")) return logout();
    if (event.target.closest("#mobile-menu")) {
      $("#sidebar").classList.add("open");
      $("#sidebar-scrim").classList.add("open");
      return;
    }
    if (event.target.closest("#mobile-close") || event.target.closest("#sidebar-scrim")) return closeMobileMenu();
    if (event.target.closest('[data-action="new-account"]')) return newAccount();
    if (event.target.closest('[data-action="new-transaction"]')) return newTransaction();
    const alertDetail = event.target.closest("[data-alert-detail]");
    if (alertDetail) {
      try { await showAlertDetail(Number(alertDetail.dataset.alertDetail)); } catch (error) { showToast(error.message, true); }
      return;
    }
    const alertButton = event.target.closest("[data-alert-action]");
    if (alertButton) {
      const id = Number(alertButton.dataset.id);
      const action = alertButton.dataset.alertAction;
      closeModal();
      try { await alertAction(action, id); if (action !== "comment") await renderPage(); } catch (error) { showToast(error.message, true); }
      return;
    }
    const ruleButton = event.target.closest("[data-edit-rule]");
    if (ruleButton) return editRule(Number(ruleButton.dataset.editRule));
    const reportButton = event.target.closest("[data-report]");
    if (reportButton) {
      reportButton.disabled = true;
      try { await downloadReport(reportButton.dataset.report, reportButton.dataset.format); } catch (error) { showToast(error.message, true); } finally { reportButton.disabled = false; }
      return;
    }
    const auditButton = event.target.closest("[data-audit-detail]");
    if (auditButton) return showAuditDetail(Number(auditButton.dataset.auditDetail));
  });

  document.addEventListener("change", async event => {
    if (event.target.id === "alert-filter") {
      state.alertFilter = event.target.value;
      try { await loadAlerts(); $("#page-content").innerHTML = renderAlerts(); $("#alert-filter").value = state.alertFilter; } catch (error) { showToast(error.message, true); }
    }
    if (event.target.id === "transaction-filter") {
      state.transactionFilter = event.target.value;
      const selected = event.target.value;
      const page = $("#page-content");
      const wrapper = event.target.closest(".table-panel");
      wrapper.querySelector(".table-wrap, .empty-state")?.remove();
      wrapper.insertAdjacentHTML("beforeend", renderTransactionTable(selected ? state.transactions.filter(t => t.tipo === selected) : state.transactions, true));
      $("#transaction-filter").value = selected;
    }
  });

  restoreSession();
})();
