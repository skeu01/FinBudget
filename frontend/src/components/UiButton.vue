<script setup lang="ts">
withDefaults(defineProps<{
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md'
  type?: 'button' | 'submit'
  disabled?: boolean
}>(), { variant: 'primary', size: 'md', type: 'button', disabled: false })
</script>

<template>
  <button :type="type" :disabled="disabled" class="ui-button" :class="[`ui-button--${variant}`, `ui-button--${size}`]">
    <slot />
  </button>
</template>
