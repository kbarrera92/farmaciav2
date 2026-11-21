import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions, type PermissionCode } from '@/modules/auth/types'
import AppLayout from '@/layouts/AppLayout.vue'
import AuthLayout from '@/layouts/AuthLayout.vue'

declare module 'vue-router' {
  interface RouteMeta {
    /** Route reachable without a session (login). */
    public?: boolean
    /** Permission required to open the route. */
    permission?: PermissionCode
    title?: string
  }
}

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: AuthLayout,
      children: [
        {
          path: 'login',
          name: 'login',
          component: () => import('@/modules/auth/views/LoginView.vue'),
          meta: { public: true, title: 'Iniciar sesión' },
        },
        {
          path: 'cambiar-contrasena',
          name: 'change-password',
          component: () => import('@/modules/auth/views/ChangePasswordView.vue'),
          meta: { title: 'Cambiar contraseña' },
        },
      ],
    },
    {
      path: '/',
      component: AppLayout,
      children: [
        {
          path: '',
          name: 'home',
          component: () => import('@/modules/home/HomeView.vue'),
          meta: { title: 'Inicio' },
        },
        {
          path: 'usuarios',
          name: 'users',
          component: () => import('@/modules/users/views/UserListView.vue'),
          meta: { permission: Permissions.UsersView, title: 'Usuarios' },
        },
        {
          path: 'usuarios/nuevo',
          name: 'user-create',
          component: () => import('@/modules/users/views/UserFormView.vue'),
          meta: { permission: Permissions.UsersManage, title: 'Nuevo usuario' },
        },
        {
          path: 'usuarios/:id',
          name: 'user-edit',
          component: () => import('@/modules/users/views/UserFormView.vue'),
          props: (route) => ({ id: Number(route.params.id) }),
          meta: { permission: Permissions.UsersView, title: 'Usuario' },
        },
        {
          path: 'roles',
          name: 'roles',
          component: () => import('@/modules/roles/views/RoleListView.vue'),
          meta: { permission: Permissions.RolesView, title: 'Roles' },
        },
        {
          path: 'roles/nuevo',
          name: 'role-create',
          component: () => import('@/modules/roles/views/RoleFormView.vue'),
          meta: { permission: Permissions.RolesManage, title: 'Nuevo rol' },
        },
        {
          path: 'roles/:id',
          name: 'role-edit',
          component: () => import('@/modules/roles/views/RoleFormView.vue'),
          props: (route) => ({ id: Number(route.params.id) }),
          meta: { permission: Permissions.RolesView, title: 'Rol' },
        },
        {
          path: 'sucursales',
          name: 'branches',
          component: () => import('@/modules/branches/views/BranchListView.vue'),
          meta: { permission: Permissions.BranchesView, title: 'Sucursales' },
        },
        {
          path: 'sucursales/nueva',
          name: 'branch-create',
          component: () => import('@/modules/branches/views/BranchFormView.vue'),
          meta: { permission: Permissions.BranchesManage, title: 'Nueva sucursal' },
        },
        {
          path: 'sucursales/:id',
          name: 'branch-edit',
          component: () => import('@/modules/branches/views/BranchFormView.vue'),
          props: (route) => ({ id: Number(route.params.id) }),
          meta: { permission: Permissions.BranchesView, title: 'Sucursal' },
        },
      ],
    },
    { path: '/:pathMatch(.*)*', redirect: { name: 'home' } },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!auth.initialized) await auth.loadSession()

  if (to.meta.public) {
    return auth.isAuthenticated ? { name: 'home' } : true
  }

  if (!auth.isAuthenticated) {
    return { name: 'login', query: to.fullPath !== '/' ? { redirect: to.fullPath } : {} }
  }

  if (auth.user?.mustChangePassword && to.name !== 'change-password') {
    return { name: 'change-password' }
  }

  if (to.meta.permission && !auth.can(to.meta.permission)) {
    return { name: 'home' }
  }

  return true
})

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} · Farmacia` : 'Farmacia'
})
