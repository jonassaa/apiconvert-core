<script setup>
import { nextTick, onMounted, ref, watch } from "vue";
import { useRoute } from "vitepress";

const STORAGE_KEY = "apiconvert.docs.runtime";
const DEFAULT_RUNTIME = "dotnet";

const runtime = ref(DEFAULT_RUNTIME);
const route = useRoute();

function normalizeRuntime(value) {
  return value === "typescript" ? "typescript" : DEFAULT_RUNTIME;
}

function applyRuntime(value) {
  const normalized = normalizeRuntime(value);
  document.documentElement.setAttribute("data-runtime", normalized);
  document.body.setAttribute("data-runtime", normalized);
}

function headingIdsWithin(className) {
  const headings = document.querySelectorAll(
    `.${className} h2[id], .${className} h3[id], .${className} h4[id], .${className} h5[id], .${className} h6[id]`
  );
  return new Set(Array.from(headings).map((heading) => `#${heading.id}`));
}

function syncOutline(selectedRuntime) {
  const normalized = normalizeRuntime(selectedRuntime);
  const dotnetIds = headingIdsWithin("runtime-dotnet");
  const typescriptIds = headingIdsWithin("runtime-typescript");
  const hiddenIds = normalized === "dotnet" ? typescriptIds : dotnetIds;

  const outlineLinks = document.querySelectorAll(
    ".VPDocAside a[href^='#'], .outline a[href^='#'], aside a[href^='#']"
  );

  outlineLinks.forEach((link) => {
    const href = link.getAttribute("href")?.split("?")[0] ?? "";
    const container = link.closest("li") ?? link;
    container.style.display = hiddenIds.has(href) ? "none" : "";
  });
}

function queueOutlineSync(value) {
  const normalized = normalizeRuntime(value);
  const run = () => syncOutline(normalized);

  nextTick(() => {
    run();
    requestAnimationFrame(run);
    setTimeout(run, 60);
  });
}

function updateRuntime(value) {
  const normalized = normalizeRuntime(value);
  runtime.value = normalized;
  localStorage.setItem(STORAGE_KEY, normalized);
  applyRuntime(normalized);
  queueOutlineSync(normalized);
}

onMounted(() => {
  const saved = normalizeRuntime(localStorage.getItem(STORAGE_KEY));
  updateRuntime(saved);
});

watch(
  () => route.path,
  () => {
    queueOutlineSync(runtime.value);
  }
);
</script>

<template>
  <div class="runtime-selector-control" role="region" aria-label="Runtime selector">
    <label for="runtime-selector"><strong>Runtime:</strong></label>
    <select
      id="runtime-selector"
      :value="runtime"
      aria-label="Select runtime"
      @change="updateRuntime(($event.target).value)"
    >
      <option value="dotnet">.NET</option>
      <option value="typescript">TypeScript</option>
    </select>
  </div>
</template>
