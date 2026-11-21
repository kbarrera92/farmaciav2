<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import PageHeader from '@/components/PageHeader.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'

const auth = useAuthStore()

const shortcuts = computed(() =>
  [
    { title: 'Usuarios', description: 'Registrar empleados y administrar su acceso.', to: '/usuarios', permission: Permissions.UsersView },
    { title: 'Roles y permisos', description: 'Definir qué puede hacer cada rol.', to: '/roles', permission: Permissions.RolesView },
    { title: 'Sucursales', description: 'Catálogo de sucursales de la farmacia.', to: '/sucursales', permission: Permissions.BranchesView },
  ].filter((s) => auth.can(s.permission)),
)
</script>

<template>
  <section class="page">
    <PageHeader
      :title="`Hola, ${auth.user?.fullName ?? ''}`"
      :subtitle="`Sucursal ${auth.user?.branch.name ?? ''} · Rol ${auth.user?.role.name ?? ''}`"
    />

    <div v-if="shortcuts.length" class="shortcuts">
      <RouterLink v-for="s in shortcuts" :key="s.to" :to="s.to" class="card shortcut">
        <h2>{{ s.title }}</h2>
        <p class="muted">{{ s.description }}</p>
      </RouterLink>
    </div>
    <p v-else class="card muted">
      Su rol todavía no tiene módulos asignados. Las funciones operativas (ventas, traslados, reportes) se
      habilitarán en próximas versiones.
    </p>
  </section>
</template>

<style scoped>
.shortcuts {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 1rem;
}

.shortcut {
  color: inherit;
  text-decoration: none;
  transition: border-color 0.15s;
}

.shortcut:hover {
  border-color: var(--color-primary);
}

.shortcut h2 {
  font-size: 1.05rem;
  margin-bottom: 0.35rem;
}

.shortcut p {
  margin: 0;
}
</style>
