<script setup>
import { onMounted, ref } from "vue";

const STORAGE_KEY = "apiconvert.docs.runtime";
const DEFAULT_RUNTIME = "dotnet";

const runtime = ref(DEFAULT_RUNTIME);

function normalizeRuntime(value) {
  return value === "typescript" ? "typescript" : DEFAULT_RUNTIME;
}

function applyRuntime(value) {
  document.body.setAttribute("data-runtime", normalizeRuntime(value));
}

function updateRuntime(value) {
  const normalized = normalizeRuntime(value);
  runtime.value = normalized;
  localStorage.setItem(STORAGE_KEY, normalized);
  applyRuntime(normalized);
}

onMounted(() => {
  const saved = normalizeRuntime(localStorage.getItem(STORAGE_KEY));
  updateRuntime(saved);
});
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
