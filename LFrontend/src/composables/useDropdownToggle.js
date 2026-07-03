import { onBeforeUnmount, onMounted, ref, unref } from 'vue'

export const useDropdownToggle = (options = {}) => {
  const dropdownRef = ref(null)
  const isDropdownOpen = ref(false)

  const isDisabled = () => {
    const disabled = options.disabled
    return typeof disabled === 'function' ? Boolean(disabled()) : Boolean(unref(disabled))
  }

  const closeDropdown = () => {
    isDropdownOpen.value = false
  }

  const toggleDropdown = () => {
    if (isDisabled()) return
    isDropdownOpen.value = !isDropdownOpen.value
  }

  const runDropdownAction = async (action, ...args) => {
    closeDropdown()
    if (typeof action === 'function') {
      await action(...args)
    }
  }

  const handleDocumentClick = (event) => {
    if (dropdownRef.value && !dropdownRef.value.contains(event.target)) {
      closeDropdown()
    }
  }

  onMounted(() => {
    document.addEventListener('click', handleDocumentClick)
  })

  onBeforeUnmount(() => {
    document.removeEventListener('click', handleDocumentClick)
  })

  return {
    closeDropdown,
    dropdownRef,
    isDropdownOpen,
    runDropdownAction,
    toggleDropdown,
  }
}
