<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions, type PermissionCode } from '@/modules/auth/types'

interface NavItem {
  label: string
  to: string
  permission?: PermissionCode
  /** Section title shown above the first visible item of the group. */
  group?: string
}

const navItems: NavItem[] = [
  { label: 'Inicio', to: '/' },
  { label: 'Usuarios', to: '/usuarios', permission: Permissions.UsersView },
  { label: 'Roles y permisos', to: '/roles', permission: Permissions.RolesView },
  { label: 'Sucursales', to: '/sucursales', permission: Permissions.BranchesView },
  { label: 'Empresa', to: '/configuracion/empresa', permission: Permissions.SettingsView },
  { label: 'Formas de pago', to: '/configuracion/formas-pago', permission: Permissions.SettingsView },
  { label: 'Categorías', to: '/catalogo/categorias', permission: Permissions.CatalogView, group: 'Catálogo' },
]

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const menuOpen = ref(false)

const visibleItems = computed(() => navItems.filter((item) => !item.permission || auth.can(item.permission)))

function startsGroup(index: number): boolean {
  const group = visibleItems.value[index].group
  return !!group && visibleItems.value[index - 1]?.group !== group
}

watch(
  () => route.fullPath,
  () => (menuOpen.value = false),
)

function isActive(item: NavItem): boolean {
  return item.to === '/' ? route.path === '/' : route.path.startsWith(item.to)
}

async function logout(): Promise<void> {
  await auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div class="app-layout">
    <aside class="sidebar" :class="{ open: menuOpen }">
      <div class="brand">
        <span class="brand-mark" aria-hidden="true">+</span>
        <span>Farmacia</span>
      </div>
      <nav aria-label="Menú principal">
        <template v-for="(item, index) in visibleItems" :key="item.to">
          <p v-if="startsGroup(index)" class="nav-group">{{ item.group }}</p>
          <RouterLink :to="item.to" class="nav-link" :class="{ active: isActive(item) }">
            {{ item.label }}
          </RouterLink>
        </template>
      </nav>
    </aside>

    <div v-if="menuOpen" class="backdrop" @click="menuOpen = false" />

    <div class="main">
      <header class="topbar">
        <button type="button" class="menu-toggle" aria-label="Abrir menú" @click="menuOpen = !menuOpen">☰</button>
        <div v-if="auth.user" class="user-info">
          <span class="user-name">{{ auth.user.fullName }}</span>
          <span class="muted small">{{ auth.user.role.name }} · {{ auth.user.branch.name }}</span>
        </div>
        <div class="topbar-actions">
          <RouterLink to="/cambiar-contrasena" class="btn btn-secondary">Contraseña</RouterLink>
          <button type="button" class="btn btn-secondary" @click="logout">Salir</button>
        </div>
      </header>

      <main class="content">
        <!-- Keyed by path so create → edit navigations remount the form with fresh data. -->
        <RouterView :key="route.path" />
      </main>
    </div>
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  min-height: 100vh;
}

.sidebar {
  width: 240px;
  flex-shrink: 0;
  background: var(--color-sidebar);
  color: var(--color-sidebar-text);
  padding: 1.25rem 0.75rem;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0 0.5rem 1.25rem;
  font-size: 1.15rem;
  font-weight: 700;
  color: #fff;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: var(--color-primary);
  font-size: 1.3rem;
}

nav {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.nav-link {
  padding: 0.6rem 0.75rem;
  border-radius: 6px;
  color: var(--color-sidebar-text);
  text-decoration: none;
  font-weight: 500;
}

.nav-group {
  margin: 1rem 0 0.25rem;
  padding: 0 0.75rem;
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  opacity: 0.6;
}

.nav-link:hover {
  background: rgb(255 255 255 / 8%);
  color: #fff;
}

.nav-link.active {
  background: var(--color-primary);
  color: #fff;
}

.main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.topbar {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem 1.5rem;
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
}

.user-info {
  display: flex;
  flex-direction: column;
  margin-right: auto;
  min-width: 0;
}

.user-name {
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.topbar-actions {
  display: flex;
  gap: 0.5rem;
}

.menu-toggle {
  display: none;
  background: none;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  padding: 0.35rem 0.6rem;
  font-size: 1.1rem;
  cursor: pointer;
}

.content {
  flex: 1;
  padding: 1.5rem;
  max-width: 1200px;
  width: 100%;
}

.backdrop {
  display: none;
}

@media (max-width: 860px) {
  .sidebar {
    position: fixed;
    inset: 0 auto 0 0;
    z-index: 20;
    transform: translateX(-100%);
    transition: transform 0.2s;
  }

  .sidebar.open {
    transform: translateX(0);
  }

  .backdrop {
    display: block;
    position: fixed;
    inset: 0;
    z-index: 10;
    background: rgb(0 0 0 / 35%);
  }

  .menu-toggle {
    display: inline-block;
  }

  .topbar {
    padding: 0.75rem 1rem;
  }

  .content {
    padding: 1rem;
  }
}

@media (max-width: 520px) {
  .topbar-actions .btn:first-child {
    display: none;
  }
}
</style>
