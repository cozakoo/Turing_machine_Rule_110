const API_URL = "/api/turing/simulate";
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

let isLoading = false;

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
    const response = await fetch(API_URL, {
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
  renderTable(steps);

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

function renderTable(steps) {
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
