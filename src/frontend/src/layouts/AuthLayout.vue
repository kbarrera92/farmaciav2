<script setup lang="ts">
import { RouterView } from 'vue-router'
</script>

<template>
  <div class="auth-layout">
    <main class="auth-panel">
      <div class="brand">
        <span class="brand-mark" aria-hidden="true">+</span>
        <span class="brand-name">Farmacia</span>
      </div>
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.auth-layout {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 1.5rem 1rem;
  background: linear-gradient(160deg, #e3f4f2 0%, var(--color-bg) 55%);
}

.auth-panel {
  width: 100%;
  max-width: 400px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  box-shadow: 0 8px 24px rgb(16 24 40 / 8%);
  padding: 2rem 1.75rem;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 1.5rem;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 36px;
  height: 36px;
  border-radius: 9px;
  background: var(--color-primary);
  color: #fff;
  font-size: 1.5rem;
  font-weight: 700;
}

.brand-name {
  font-size: 1.25rem;
  font-weight: 700;
}
</style>
