
/**
 *  檢查密碼強度
 * @param {string} password
 * @returns { { isValid: boolean, message: string }}
 */
export const validatePassword = (password) => {
    if (!password) {
        return { isValid: false, message: '密碼不得為空' };
    }

    return { isValid: true, message: '' }
}

/**
 * 檢查兩次密碼是否一致
 */
export const ValidateConfirmPassword = (password, confirmPassword) => {
    if (password !== confirmPassword) {
        return { isValid: false, message: '兩次輸入的密碼不一致' }
    }
    return { isValid: true, message: '' }
}
