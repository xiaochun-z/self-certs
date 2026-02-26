import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import App from '../App.vue'
import router from '../router'

describe('App', () => {
  it('renders properly', async () => {
    // 挂载 App 并注入路由插件
    const wrapper = mount(App, {
      global: {
        plugins: [router],
      },
    })

    // 等待路由初始化完成，确保 HomeView 被渲染
    await router.isReady()

    // 验证是否包含 HomeView 中的关键文本
    expect(wrapper.text()).toContain('Hello Tailwind!')
  })
})