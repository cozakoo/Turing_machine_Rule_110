const API_URL = "/api/turing/simulate";
const TABLE_URL = "/api/turing/table";
const THEME_KEY = "regla110-theme";
const MIN_CELLS = 8;
const INT32_MAX = 2147483647n;

const form = document.querySelector("#simulation-form");
const input = document.querySelector("#tape-input");
const button = document.querySelector("#simulate-button");
const buttonLabel = button.querySelector(".button__label");
const status = document.querySelector("#status");
const emptyState = document.querySelector("#empty-state");
const errorPanel = document.querySelector("#error-panel");
const errorMessage = document.querySelector("#error-message");
const results = document.querySelector("#results");
const stats = document.querySelector("#stats");
const grid = document.querySelector("#grid");
const historyBody = document.querySelector("#history-body");
const tapeError = document.querySelector("#tape-error");
const states = document.querySelector(".states");
const presetButtons = document.querySelectorAll("[data-preset]");
const traceToggle = document.querySelector("#trace-toggle");
const themeToggle = document.querySelector("#theme-toggle");
const themeLink = document.querySelector("#theme-dark");

const machinePanel = document.querySelector("#machine-panel");
const machineMeta = document.querySelector("#machine-meta");
const moduleFilter = document.querySelector("#module-filter");
const tableSearch = document.querySelector("#table-search");
const tableBody = document.querySelector("#table-body");
const tableCount = document.querySelector("#table-count");
const traceBody = document.querySelector("#trace-body");
const tapeView = document.querySelector("#tape-view");

let isLoading = false;
let transitionRows = [];
let alphabet = null;

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (isLoading) {
    return;
  }

  clearError();
  const validation = validateInput(input.value);
  if (!validation.ok) {
    showFieldError(validation.error);
    showError(validation.error);
    return;
  }

  setLoading(true);
  setView("loading");
  status.className = "status";
  status.textContent = "Simulando la cinta…";

  try {
    const url = traceToggle.checked ? `${API_URL}?trace=true` : API_URL;
    const response = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ tapeInput: validation.value })
    });

    if (!response.ok) {
      throw new Error(`La API respondió con estado ${response.status}.`);
    }

    const data = await response.json();
    if (!data.isValid) {
      showError(data.errorMessage || "La entrada no es válida.");
      return;
    }

    if (!Array.isArray(data.steps) || data.steps.length === 0) {
      throw new Error("La API no devolvió generaciones.");
    }

    renderResults(data.steps);
    renderMachine(data.machine);
    setView("results");
    status.className = "status is-success";
    status.textContent = `Simulación completada: ${data.steps.length} generaciones.`;
  } catch (error) {
    const message = error instanceof Error
      ? error.message
      : "No se pudo completar la simulación.";
    showError(`No se pudo completar la simulación: ${message}`);
  } finally {
    setLoading(false);
  }
});

input.addEventListener("input", () => {
  clearFieldError();
  if (!errorPanel.hidden) {
    errorPanel.hidden = true;
    setView("empty");
  }
});

for (const preset of presetButtons) {
  preset.addEventListener("click", () => {
    input.value = preset.dataset.preset;
    clearError();
    setView("empty");
    status.className = "status";
    status.textContent = "";
    input.focus();
    input.setSelectionRange(0, input.value.length);
  });
}

function validateInput(rawValue) {
  const value = rawValue.trim();
  if (value.length === 0) {
    return { ok: false, error: "Ingresá una cinta inicial." };
  }

  const parts = value.split("#");
  if (parts.length !== 3 || parts[2] !== "") {
    return {
      ok: false,
      error: "El formato debe ser K_binario#celdas_binarias#."
    };
  }

  const [kRaw, cells] = parts;
  if (!/^[01]+$/.test(kRaw)) {
    return { ok: false, error: "K debe ser un entero binario no vacío." };
  }

  if (!/^[01]+$/.test(cells)) {
    return { ok: false, error: "Las celdas deben contener solo 0 y 1." };
  }

  if (cells.length < MIN_CELLS) {
    return {
      ok: false,
      error: `La cinta debe contener al menos ${MIN_CELLS} celdas.`
    };
  }

  try {
    const iterations = BigInt(`0b${kRaw}`);
    if (iterations > INT32_MAX) {
      return {
        ok: false,
        error: "K excede el máximo entero soportado (Int32)."
      };
    }
  } catch {
    return { ok: false, error: "K no es un entero binario válido." };
  }

  return { ok: true, value };
}

function setLoading(loading) {
  isLoading = loading;
  button.disabled = loading;
  button.setAttribute("aria-busy", String(loading));
  buttonLabel.textContent = loading ? "Simulando" : "Simular";
}

function setView(view) {
  emptyState.hidden = view !== "empty";
  results.hidden = view !== "results";
  errorPanel.hidden = view !== "error";
}

function showFieldError(message) {
  tapeError.textContent = message;
  tapeError.hidden = false;
  input.setAttribute("aria-invalid", "true");
  input.focus();
}

function clearFieldError() {
  tapeError.textContent = "";
  tapeError.hidden = true;
  input.removeAttribute("aria-invalid");
}

function showError(message) {
  setLoading(false);
  errorMessage.textContent = message;
  setView("error");
  status.className = "status is-error";
  status.textContent = message;
}

function clearError() {
  clearFieldError();
  errorPanel.hidden = true;
  status.className = "status";
  status.textContent = "";
}

function renderResults(steps) {
  const generationCount = steps.length;
  const cellCount = steps[0].tapeState.length;
  const iterations = Math.max(generationCount - 1, 0);
  const finalState = steps[generationCount - 1].tapeState;

  const finalStat = createStat("Estado final", finalState);
  finalStat.classList.add("stat--wide");

  stats.replaceChildren(
    createStat("K", String(iterations)),
    createStat("N", String(cellCount)),
    createStat("Generaciones", String(generationCount)),
    createStat("Frontera", "Cíclica"),
    finalStat
  );

  renderGrid(steps, cellCount);
  renderHistoryTable(steps);

  grid.setAttribute(
    "aria-label",
    `Cuadrícula de ${generationCount} generaciones de ${cellCount} celdas. Estado final: ${finalState}.`
  );

  if (states) {
    states.open = false;
  }
}

function createStat(label, value) {
  const wrapper = document.createElement("div");
  wrapper.className = "stat";
  const term = document.createElement("dt");
  term.textContent = label;
  const description = document.createElement("dd");
  description.textContent = value;
  wrapper.append(term, description);
  return wrapper;
}

function renderGrid(steps, cellCount) {
  const fragment = document.createDocumentFragment();
  grid.style.setProperty("--cell-count", String(cellCount));

  for (let index = 0; index < steps.length; index++) {
    const step = steps[index];
    const row = document.createElement("div");
    row.className = "generation-row";
    row.style.setProperty("--row-index", String(Math.min(index, 24)));
    row.title = `Generación t = ${step.generation}`;

    if (index === steps.length - 1) {
      row.classList.add("is-final");
    }

    const label = document.createElement("span");
    label.className = "generation-label";
    label.textContent = `t = ${step.generation}`;

    const cells = document.createElement("div");
    cells.className = "cells";

    for (const value of step.tapeState) {
      const cell = document.createElement("span");
      cell.className = value === "1" ? "cell one" : "cell";
      cell.setAttribute("aria-hidden", "true");
      cells.append(cell);
    }

    row.append(label, cells);
    fragment.append(row);
  }

  grid.replaceChildren(fragment);
}

function renderHistoryTable(steps) {
  const fragment = document.createDocumentFragment();

  for (const step of steps) {
    const row = document.createElement("tr");
    const generationCell = document.createElement("td");
    const stateCell = document.createElement("td");
    generationCell.textContent = step.generation;
    stateCell.className = "state";
    stateCell.textContent = step.tapeState;
    row.append(generationCell, stateCell);
    fragment.append(row);
  }

  historyBody.replaceChildren(fragment);
}

function renderTransitionTable(rows) {
  const fragment = document.createDocumentFragment();

  for (const row of rows) {
    const tr = document.createElement("tr");
    const values = [row.state, row.read, row.write, row.nextState, row.move, row.module];

    values.forEach((value, index) => {
      const cell = document.createElement("td");
      cell.className = index === 5 ? "state module" : "state";
      cell.textContent = value;
      tr.append(cell);
    });

    fragment.append(tr);
  }

  tableBody.replaceChildren(fragment);
}

function renderFilteredTable() {
  const module = moduleFilter.value;
  const term = tableSearch.value.trim().toLowerCase();

  const rows = transitionRows.filter((row) => {
    if (module && row.module !== module) {
      return false;
    }

    if (!term) {
      return true;
    }

    return (
      row.state.toLowerCase().includes(term) ||
      row.nextState.toLowerCase().includes(term) ||
      row.module.toLowerCase().includes(term) ||
      row.read.toLowerCase() === term ||
      row.write.toLowerCase() === term
    );
  });

  renderTransitionTable(rows);
  tableCount.textContent = `${rows.length} de ${transitionRows.length} transiciones`;
}

function renderMachine(machine) {
  if (!machine) {
    machinePanel.hidden = true;
    return;
  }

  machinePanel.hidden = false;

  machineMeta.replaceChildren(
    createStat("Estado final", machine.finalState),
    createStat("Pasos", String(machine.steps)),
    createStat("Generaciones", String(machine.generations)),
    createStat("Transiciones δ", String(transitionRows.length))
  );

  renderTrace(machine.trace);
  renderTape(machine.tape);
}

function renderTrace(trace) {
  if (!Array.isArray(trace) || trace.length === 0) {
    const row = document.createElement("tr");
    const cell = document.createElement("td");
    cell.colSpan = 7;
    cell.className = "empty-cell";
    cell.textContent = "Activá “Incluir traza paso a paso de la máquina” para ver el detalle.";
    row.append(cell);
    traceBody.replaceChildren(row);
    return;
  }

  const fragment = document.createDocumentFragment();

  for (const step of trace) {
    const tr = document.createElement("tr");
    const values = [
      step.step,
      step.state,
      step.headPosition,
      step.readSymbol,
      step.writtenSymbol,
      step.move,
      step.nextState
    ];

    values.forEach((value, index) => {
      const cell = document.createElement("td");
      cell.className = index === 1 || index === 6 ? "state" : "state state--num";
      cell.textContent = value;
      tr.append(cell);
    });

    fragment.append(tr);
  }

  traceBody.replaceChildren(fragment);
}

function renderTape(tape) {
  if (!Array.isArray(tape) || tape.length === 0) {
    tapeView.replaceChildren();
    return;
  }

  const fragment = document.createDocumentFragment();

  for (const cell of tape) {
    const item = document.createElement("span");
    item.className = "tape-cell";

    const symbol = document.createElement("span");
    symbol.className = "tape-cell__symbol";
    symbol.textContent = cell.symbol === " " ? "·" : cell.symbol;

    const position = document.createElement("span");
    position.className = "tape-cell__position";
    position.textContent = cell.position;

    item.append(symbol, position);
    fragment.append(item);
  }

  tapeView.replaceChildren(fragment);
}

async function loadTransitionTable() {
  try {
    const response = await fetch(TABLE_URL);
    if (!response.ok) {
      return;
    }

    const data = await response.json();
    transitionRows = Array.isArray(data.rows) ? data.rows : [];

    const modules = [...new Set(transitionRows.map((row) => row.module))];
    for (const module of modules) {
      const option = document.createElement("option");
      option.value = module;
      option.textContent = module;
      moduleFilter.append(option);
    }

    renderFilteredTable();
  } catch {
    transitionRows = [];
  }
}

moduleFilter.addEventListener("change", renderFilteredTable);
tableSearch.addEventListener("input", renderFilteredTable);

const parameters = new URLSearchParams(window.location.search);
const sharedTape = parameters.get("tape");
const requestedTheme = parameters.get("theme");

const prefersDark = window.matchMedia("(prefers-color-scheme: dark)");

function readStoredTheme() {
  try {
    return window.localStorage.getItem(THEME_KEY);
  } catch {
    return null;
  }
}

function storeTheme(theme) {
  try {
    window.localStorage.setItem(THEME_KEY, theme);
  } catch {
    /* el navegador puede bloquear el almacenamiento local: no es crítico */
  }
}

function applyTheme(theme) {
  const isDark = theme === "dark";

  themeLink.media = isDark ? "all" : "not all";
  document.documentElement.style.colorScheme = theme;
  themeToggle.classList.toggle("is-dark", isDark);
  themeToggle.setAttribute("aria-pressed", String(isDark));
  themeToggle.setAttribute(
    "aria-label",
    isDark ? "Activar el modo claro" : "Activar el modo oscuro"
  );
}

function currentTheme() {
  const stored = readStoredTheme();

  if (stored === "dark" || stored === "light") {
    return stored;
  }

  return prefersDark.matches ? "dark" : "light";
}

applyTheme(requestedTheme === "dark" || requestedTheme === "light" ? requestedTheme : currentTheme());

themeToggle.addEventListener("click", () => {
  const theme = currentTheme() === "dark" ? "light" : "dark";
  applyTheme(theme);
  storeTheme(theme);
});

prefersDark.addEventListener("change", () => {
  if (!readStoredTheme()) {
    applyTheme(currentTheme());
  }
});

if (sharedTape) {
  input.value = sharedTape;
}

if (parameters.get("trace") === "1") {
  traceToggle.checked = true;
}

loadTransitionTable();

if (sharedTape && parameters.get("auto") === "1") {
  form.requestSubmit();
}
