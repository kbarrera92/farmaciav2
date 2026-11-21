import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './router'
import { setUnauthorizedHandler } from './services/http'
import { useAuthStore } from './modules/auth/stores/authStore'
import './style.css'

const app = createApp(App)
app.use(createPinia())
app.use(router)

// Session expired or closed by an administrator: clear it and go back to the login screen.
setUnauthorizedHandler(() => {
  useAuthStore().clearSession()
  const current = router.currentRoute.value
  if (!current.meta.public) {
    void router.push({ name: 'login', query: { redirect: current.fullPath } })
  }
})

app.mount('#app')
