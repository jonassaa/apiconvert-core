(() => {
  const STORAGE_KEY = "apiconvert.docs.runtime";
  const DEFAULT_RUNTIME = "dotnet";

  function getRuntime() {
    const saved = localStorage.getItem(STORAGE_KEY);
    return saved === "typescript" ? "typescript" : DEFAULT_RUNTIME;
  }

  function applyRuntime(runtime) {
    document.body.setAttribute("data-runtime", runtime);
  }

  function bindSelector() {
    const selector = document.getElementById("runtime-selector");
    if (!selector) {
      return;
    }

    const runtime = getRuntime();
    selector.value = runtime;
    applyRuntime(runtime);

    selector.addEventListener("change", () => {
      const selected = selector.value === "typescript" ? "typescript" : DEFAULT_RUNTIME;
      localStorage.setItem(STORAGE_KEY, selected);
      applyRuntime(selected);
    });
  }

  function initializeRuntimeSelector() {
    applyRuntime(getRuntime());
    bindSelector();
  }

  if (typeof document$ !== "undefined") {
    document$.subscribe(() => initializeRuntimeSelector());
  } else {
    document.addEventListener("DOMContentLoaded", initializeRuntimeSelector);
  }
})();
